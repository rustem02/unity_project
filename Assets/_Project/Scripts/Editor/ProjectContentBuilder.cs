#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using VRTraining.Audio;
using VRTraining.Highlight;
using VRTraining.Interaction;
using VRTraining.Lobby;
using VRTraining.Scenario.Data;
using VRTraining.Scenario.Runtime;
using VRTraining.UI;
using VRTraining.VR;

namespace VRTraining.EditorTools
{
    /// <summary>
    /// One-click content bootstrap: materials, scenario asset, Lobby + Training scenes.
    /// Menu: VR Training → Build Project Content
    /// </summary>
    public static class ProjectContentBuilder
    {
        private const string Root = "Assets/_Project";
        private const string ScenesPath = Root + "/Scenes";
        private const string MaterialsPath = Root + "/Materials";
        private const string SoPath = Root + "/ScriptableObjects";
        private const string FontsPath = Root + "/Fonts";

        private static Font _uiFont;

        [MenuItem("VR Training/Build Project Content", priority = 0)]
        public static void BuildAll()
        {
            BuildAllInternal(showDialog: true);
        }

        /// <summary>Batchmode entry: Unity.exe -batchmode -executeMethod VRTraining.EditorTools.ProjectContentBuilder.BuildAllBatch</summary>
        public static void BuildAllBatch()
        {
            try
            {
                BuildAllInternal(showDialog: false);
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
            }
        }

        private static void BuildAllInternal(bool showDialog)
        {
            EnsureFolders();
            if (!UrpProjectSetup.EnsureUrpConfigured())
                throw new System.Exception("URP pipeline was not configured. Materials would render magenta.");

            _uiFont = CreateCyrillicFont();
            var mats = CreateMaterials();
            var scenario = CreateScenarioAsset();
            BuildLobbyScene(mats);
            BuildTrainingScene(mats, scenario);
            SetupBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var verify = VerifyProjectHealth();
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "VR Training",
                    verify
                        ? "Контент собран и проверен.\nСцены: Lobby, Training.\nОткройте Lobby и Play."
                        : "Контент собран, но проверка нашла проблемы. Смотри Console.",
                    "OK");
            }
            else
            {
                if (!verify)
                    throw new System.Exception("Project health check failed after build.");
                Debug.Log("[VR Training] Project content built and verified successfully.");
            }
        }

        /// <summary>Batch screenshot: Unity -executeMethod VRTraining.EditorTools.ProjectContentBuilder.CaptureTrainingScreenshot</summary>
        public static void CaptureTrainingScreenshot()
        {
            try
            {
                UrpProjectSetup.EnsureUrpConfigured();
                var scenePath = ScenesPath + "/Training.unity";
                if (!File.Exists(scenePath))
                    BuildAllInternal(showDialog: false);

                EditorSceneManager.OpenScene(scenePath);

                // Hide end-of-run UI for a clean overview shot (Awake not run in edit-mode capture).
                foreach (var canvas in Object.FindObjectsByType<Canvas>())
                {
                    if (canvas.name is "ResultsCanvas" or "HudCanvas")
                        canvas.gameObject.SetActive(false);
                }

                // Edit-mode Dynamic fonts need an explicit OS atlas or Cyrillic collapses to lines.
                ApplyEditModeReadableFont();

                var cam = Object.FindAnyObjectByType<Camera>();
                if (cam == null)
                    throw new System.Exception("No camera in Training scene.");

                // Overview from inside the room looking toward tables/zones.
                cam.transform.position = new Vector3(0f, 2.4f, -4.2f);
                cam.transform.rotation = Quaternion.Euler(15f, 0f, 0f);

                const int width = 1280;
                const int height = 720;
                var rt = new RenderTexture(width, height, 24);
                var prev = cam.targetTexture;
                cam.targetTexture = rt;
                cam.Render();

                RenderTexture.active = rt;
                var tex = new Texture2D(width, height, TextureFormat.RGB24, false);
                tex.ReadPixels(new Rect(0, 0, width, height), 0, 0);
                tex.Apply();
                cam.targetTexture = prev;
                RenderTexture.active = null;
                Object.DestroyImmediate(rt);

                var outDir = Path.Combine(Directory.GetParent(Application.dataPath)!.FullName, "Logs");
                Directory.CreateDirectory(outDir);
                var outPath = Path.Combine(outDir, "verify_training.png");
                File.WriteAllBytes(outPath, tex.EncodeToPNG());
                Object.DestroyImmediate(tex);

                // Magenta detector: sample center pixels shouldn't be pure magenta error color.
                // (Rough check already done via shader names in VerifyProjectHealth.)
                Debug.Log($"[VR Training] Screenshot saved: {outPath}");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
            }
        }

        private static void ApplyEditModeReadableFont()
        {
            var font = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Tahoma", "Microsoft Sans Serif" }, 64);
            if (font == null)
                return;

            const string sample =
                "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
                "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
                "Документы Подтвердить Отклонить Выход Завершить Досмотр Служебка " +
                "Попытаться ещё Возврат ходьба обзор телепорт клик взять Итог";
            // Few sizes only — flooding the dynamic atlas makes glyphs render as lines.
            foreach (var size in new[] { 64, 48, 40, 32 })
                font.RequestCharactersInTexture(sample, size, FontStyle.Normal);

            var texts = Object.FindObjectsByType<Text>(FindObjectsInactive.Include, FindObjectsSortMode.None);
            foreach (var ui in texts)
            {
                if (ui == null)
                    continue;
                ui.font = font;
                if (!string.IsNullOrEmpty(ui.text))
                    font.RequestCharactersInTexture(ui.text, ui.fontSize, ui.fontStyle);
            }

            Canvas.ForceUpdateCanvases();
            foreach (var ui in texts)
            {
                if (ui == null)
                    continue;
                var value = ui.text;
                ui.text = string.Empty;
                ui.text = value;
            }

            Canvas.ForceUpdateCanvases();
        }

        /// <summary>Batch smoke: validates interactables are reachable by look-ray from spawn.</summary>
        public static void SmokeTestInteractionsBatch()
        {
            try
            {
                BuildAllInternal(showDialog: false);
                EditorSceneManager.OpenScene(ScenesPath + "/Training.unity");

                var cam = Object.FindAnyObjectByType<Camera>();
                var player = GameObject.FindGameObjectWithTag("Player");
                if (cam == null || player == null)
                    throw new System.Exception("Missing camera/player.");

                var passport = GameObject.Find("passport");
                var bag = GameObject.Find("suspicious_bag");
                if (passport == null || bag == null)
                    throw new System.Exception("Missing interactables.");

                // Aim camera at passport and ensure a raycast hit.
                cam.transform.position = player.transform.position + Vector3.up * 1.6f;
                cam.transform.LookAt(passport.transform.position);
                if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hitGrab, 12f))
                    throw new System.Exception("Ray miss toward passport.");
                if (hitGrab.collider.GetComponentInParent<GrabbableInteractable>() == null)
                    throw new System.Exception("Passport ray did not hit GrabbableInteractable.");

                cam.transform.LookAt(bag.transform.position);
                if (!Physics.Raycast(cam.transform.position, cam.transform.forward, out var hitClick, 12f))
                    throw new System.Exception("Ray miss toward bag.");
                if (hitClick.collider.GetComponentInParent<ClickableInteractable>() == null)
                    throw new System.Exception("Bag ray did not hit ClickableInteractable.");

                if (Object.FindAnyObjectByType<TeleportLocomotion>() == null)
                    throw new System.Exception("TeleportLocomotion missing.");
                if (Object.FindAnyObjectByType<DesktopInteractionRay>() == null)
                    throw new System.Exception("DesktopInteractionRay missing.");
                if (Object.FindAnyObjectByType<UiFontBootstrap>() == null)
                    throw new System.Exception("UiFontBootstrap missing.");

                Debug.Log("[VR Training] Interaction smoke PASS.");
                EditorApplication.Exit(0);
            }
            catch (System.Exception ex)
            {
                Debug.LogError(ex);
                EditorApplication.Exit(1);
            }
        }

        private static bool VerifyProjectHealth()
        {
            var ok = true;
            if (GraphicsSettings.currentRenderPipeline == null)
            {
                Debug.LogError("[VR Training] Verify FAIL: no render pipeline assigned.");
                ok = false;
            }

            var mats = AssetDatabase.FindAssets("t:Material", new[] { MaterialsPath });
            for (var i = 0; i < mats.Length; i++)
            {
                var path = AssetDatabase.GUIDToAssetPath(mats[i]);
                var mat = AssetDatabase.LoadAssetAtPath<Material>(path);
                if (mat == null || mat.shader == null)
                {
                    Debug.LogError($"[VR Training] Verify FAIL: missing material/shader at {path}");
                    ok = false;
                    continue;
                }

                if (mat.shader.name.Contains("Hidden/InternalErrorShader") || mat.shader.name == "Hidden/InternalErrorShader")
                {
                    Debug.LogError($"[VR Training] Verify FAIL: error shader on {path}");
                    ok = false;
                }
            }

            if (!File.Exists(ScenesPath + "/Lobby.unity") || !File.Exists(ScenesPath + "/Training.unity"))
            {
                Debug.LogError("[VR Training] Verify FAIL: Lobby/Training scenes missing.");
                ok = false;
            }

            var scenario = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(SoPath + "/TrainingScenario.asset");
            if (scenario == null || scenario.Groups == null || scenario.Groups.Count != 3)
            {
                Debug.LogError("[VR Training] Verify FAIL: TrainingScenario must have 3 groups.");
                ok = false;
            }
            else
            {
                for (var g = 0; g < scenario.Groups.Count; g++)
                {
                    if (scenario.Groups[g].Steps == null || scenario.Groups[g].Steps.Count != 3)
                    {
                        Debug.LogError($"[VR Training] Verify FAIL: group {g} must have 3 steps.");
                        ok = false;
                    }
                }
            }

            Debug.Log(ok ? "[VR Training] Verify PASS." : "[VR Training] Verify FAIL.");
            return ok;
        }

        [MenuItem("VR Training/Open Lobby Scene")]
        public static void OpenLobby()
        {
            var path = ScenesPath + "/Lobby.unity";
            if (File.Exists(path))
                EditorSceneManager.OpenScene(path);
            else
                BuildAll();
        }

        private static void EnsureFolders()
        {
            Directory.CreateDirectory(ScenesPath);
            Directory.CreateDirectory(MaterialsPath);
            Directory.CreateDirectory(SoPath);
            Directory.CreateDirectory(FontsPath);
            Directory.CreateDirectory(Root + "/Prefabs");
            Directory.CreateDirectory(Root + "/Audio");
        }

        private static Dictionary<string, Material> CreateMaterials()
        {
            var map = new Dictionary<string, Material>();
            map["Floor"] = CreateLit("M_Floor", new Color(0.22f, 0.24f, 0.28f));
            map["Wall"] = CreateLit("M_Wall", new Color(0.55f, 0.58f, 0.62f));
            map["Accent"] = CreateLit("M_Accent", new Color(0.18f, 0.45f, 0.72f));
            map["Document"] = CreateLit("M_Document", new Color(0.92f, 0.9f, 0.82f));
            map["Contraband"] = CreateLit("M_Contraband", new Color(0.75f, 0.25f, 0.2f));
            map["Zone"] = CreateLit("M_Zone", new Color(0.2f, 0.7f, 1f, 0.35f), true);
            map["Table"] = CreateLit("M_Table", new Color(0.4f, 0.28f, 0.18f));
            map["Panel"] = CreateLit("M_Panel", new Color(0.12f, 0.14f, 0.18f));
            return map;
        }

        private static Material CreateLit(string name, Color color, bool transparent = false)
        {
            var path = $"{MaterialsPath}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing == null)
            {
                var shader = Shader.Find("Universal Render Pipeline/Lit")
                             ?? Shader.Find("Lit")
                             ?? Shader.Find("Standard");
                existing = new Material(shader) { name = name };
                AssetDatabase.CreateAsset(existing, path);
            }

            UrpProjectSetup.ForceUrpLitOnMaterial(existing, color, transparent);
            return existing;
        }

        private static ScenarioDefinition CreateScenarioAsset()
        {
            var path = SoPath + "/TrainingScenario.asset";
            var asset = AssetDatabase.LoadAssetAtPath<ScenarioDefinition>(path);
            if (asset == null)
            {
                asset = ScriptableObject.CreateInstance<ScenarioDefinition>();
                AssetDatabase.CreateAsset(asset, path);
            }

            asset.ScenarioName = "Таможенный контроль";
            asset.Groups = new List<StepGroupDefinition>
            {
                new StepGroupDefinition
                {
                    Title = "Проверка документов",
                    InfoMessage =
                        "1) Подойдите к зоне проверки документов.\n" +
                        "2) Возьмите паспорт со стола.\n" +
                        "3) Нажмите кнопку «Подтвердить».",
                    Steps = new List<StepDefinition>
                    {
                        new StepDefinition
                        {
                            Id = "doc_zone",
                            Description = "Подойти к зоне проверки документов",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.ReachZone, TargetId = "zone_documents" }
                            }
                        },
                        new StepDefinition
                        {
                            Id = "doc_grab",
                            Description = "Взять паспорт",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.Grab, TargetId = "passport" }
                            }
                        },
                        new StepDefinition
                        {
                            Id = "doc_confirm",
                            Description = "Нажать кнопку «Подтвердить»",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.PressUIButton, TargetId = "btn_confirm" }
                            }
                        }
                    }
                },
                new StepGroupDefinition
                {
                    Title = "Поиск нарушений",
                    InfoMessage =
                        "1) Подойдите к зоне досмотра багажа.\n" +
                        "2) Кликните лучом по подозрительному предмету.\n" +
                        "3) Возьмите изъятый предмет.",
                    Steps = new List<StepDefinition>
                    {
                        new StepDefinition
                        {
                            Id = "insp_zone",
                            Description = "Подойти к зоне досмотра",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.ReachZone, TargetId = "zone_inspection" }
                            }
                        },
                        new StepDefinition
                        {
                            Id = "insp_click",
                            Description = "Кликнуть по подозрительному объекту",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.Click, TargetId = "suspicious_bag" }
                            }
                        },
                        new StepDefinition
                        {
                            Id = "insp_grab",
                            Description = "Изъять предмет (grab)",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.Grab, TargetId = "contraband" }
                            }
                        }
                    }
                },
                new StepGroupDefinition
                {
                    Title = "Завершение контроля",
                    InfoMessage =
                        "1) Подойдите к зоне выхода.\n" +
                        "2) Кликните по световому индикатору.\n" +
                        "3) Нажмите «Завершить».",
                    Steps = new List<StepDefinition>
                    {
                        new StepDefinition
                        {
                            Id = "exit_zone",
                            Description = "Подойти к зоне выхода",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.ReachZone, TargetId = "zone_exit" }
                            }
                        },
                        new StepDefinition
                        {
                            Id = "exit_click",
                            Description = "Кликнуть по индикатору",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.Click, TargetId = "exit_indicator" }
                            }
                        },
                        new StepDefinition
                        {
                            Id = "exit_ui",
                            Description = "Нажать кнопку «Завершить»",
                            ExpectedActions = new List<ExpectedAction>
                            {
                                new ExpectedAction { ActionType = ActionType.PressUIButton, TargetId = "btn_finish" }
                            }
                        }
                    }
                }
            };

            // Distractors for wrong-target failures
            EditorUtility.SetDirty(asset);
            return asset;
        }

        private static void BuildLobbyScene(Dictionary<string, Material> mats)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEnvironment("LobbyRoom", mats, new Vector3(12f, 3f, 10f));
            var player = CreatePlayer(new Vector3(0f, 0.1f, -3f));

            var canvas = CreateWorldCanvas("LobbyCanvas", new Vector3(0f, 1.6f, 2.5f), new Vector2(1200, 800), 0.0028f);
            var panel = CreatePanel(canvas.transform, "Panel", Vector2.zero, new Vector2(1100, 700));
            CreateUiText(panel.transform, "Title", "VR TRAINING", 64, new Vector2(0, 220), new Vector2(1000, 100), TextAnchor.MiddleCenter);
            CreateUiText(panel.transform, "Subtitle", "Лобби - выберите действие", 36, new Vector2(0, 120), new Vector2(1000, 60), TextAnchor.MiddleCenter);

            var startBtn = CreateUiButton(panel.transform, "StartButton", "Начать тренировку", new Vector2(0, -40), new Vector2(520, 100));
            var menu = canvas.gameObject.AddComponent<LobbyMenuController>();
            var so = new SerializedObject(menu);
            so.FindProperty("startTrainingButton").objectReferenceValue = startBtn;
            so.FindProperty("trainingSceneName").stringValue = "Training";
            so.ApplyModifiedPropertiesWithoutUndo();

            canvas.gameObject.AddComponent<DualInputUiBootstrap>();
            canvas.gameObject.AddComponent<UiFontBootstrap>();
            CreateEventSystem();

            EditorSceneManager.SaveScene(scene, ScenesPath + "/Lobby.unity");
        }

        private static void BuildTrainingScene(Dictionary<string, Material> mats, ScenarioDefinition scenario)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            CreateEnvironment("TrainingRoom", mats, new Vector3(18f, 3f, 14f));
            CreatePlayer(new Vector3(0f, 0.1f, -5f));

            // Systems
            var systems = new GameObject("Systems");
            var controller = systems.AddComponent<ScenarioController>();
            var controllerSo = new SerializedObject(controller);
            controllerSo.FindProperty("scenario").objectReferenceValue = scenario;
            controllerSo.FindProperty("autoStart").boolValue = true;
            controllerSo.ApplyModifiedPropertiesWithoutUndo();

            systems.AddComponent<HighlightDirector>();
            systems.AddComponent<FeedbackAudioPlayer>();
            var desktopRay = systems.AddComponent<DesktopInteractionRay>();
            var raySo = new SerializedObject(desktopRay);
            var playerCam = Object.FindAnyObjectByType<Camera>();
            if (playerCam != null)
                raySo.FindProperty("rayCamera").objectReferenceValue = playerCam;
            raySo.ApplyModifiedPropertiesWithoutUndo();
            systems.AddComponent<DualInputUiBootstrap>();
            systems.AddComponent<InteractableResetService>();
            systems.AddComponent<UiFontBootstrap>();

            // On-screen controls hint (readable, not a gameplay blocker).
            var hint = CreateWorldCanvas("ControlsHint", new Vector3(0f, 2.55f, -3.5f), new Vector2(1100, 180), 0.0024f, raycastTarget: false);
            CreateUiText(
                hint.transform,
                "Hint",
                "WASD - ходьба | ПКМ - обзор | T - телепорт | ЛКМ - клик | E - взять",
                32,
                Vector2.zero,
                new Vector2(1050, 140),
                TextAnchor.MiddleCenter);

            // Tables / props
            var docsTable = CreatePrimitive(PrimitiveType.Cube, "DocumentsTable", new Vector3(-4f, 0.4f, 2f), new Vector3(1.6f, 0.8f, 0.9f), mats["Table"]);
            var inspTable = CreatePrimitive(PrimitiveType.Cube, "InspectionTable", new Vector3(3.5f, 0.4f, 2f), new Vector3(1.8f, 0.8f, 1.1f), mats["Table"]);
            var exitDesk = CreatePrimitive(PrimitiveType.Cube, "ExitDesk", new Vector3(0f, 0.4f, 5.5f), new Vector3(1.4f, 0.8f, 0.8f), mats["Table"]);

            CreateZone("zone_documents", new Vector3(-4f, 0.05f, 0.6f), new Vector3(2.2f, 0.1f, 2.2f), mats["Zone"], "Документы");
            CreateZone("zone_inspection", new Vector3(3.5f, 0.05f, 0.5f), new Vector3(2.4f, 0.1f, 2.4f), mats["Zone"], "Досмотр");
            CreateZone("zone_exit", new Vector3(0f, 0.05f, 4.2f), new Vector3(2.2f, 0.1f, 2f), mats["Zone"], "Выход");
            // Wrong zone distractor
            CreateZone("zone_wrong", new Vector3(-7f, 0.05f, -2f), new Vector3(1.8f, 0.1f, 1.8f), mats["Zone"], "Служебка");

            var passport = CreateInteractableCube("passport", new Vector3(-4f, 0.98f, 2f), new Vector3(0.35f, 0.08f, 0.45f), mats["Document"], grab: true);
            var wrongPaper = CreateInteractableCube("wrong_paper", new Vector3(-3.45f, 0.98f, 2.15f), new Vector3(0.3f, 0.06f, 0.38f), mats["Panel"], grab: true);

            var bag = CreateInteractableCube("suspicious_bag", new Vector3(3.3f, 1.05f, 2f), new Vector3(0.5f, 0.35f, 0.55f), mats["Accent"], click: true);
            var decoy = CreateInteractableCube("decoy_box", new Vector3(3.9f, 1.0f, 2.2f), new Vector3(0.35f, 0.3f, 0.35f), mats["Wall"], click: true);
            var contraband = CreateInteractableCube("contraband", new Vector3(3.5f, 0.98f, 1.55f), new Vector3(0.28f, 0.2f, 0.28f), mats["Contraband"], grab: true);

            var indicator = CreateInteractableCube("exit_indicator", new Vector3(0.55f, 1.45f, 5.5f), new Vector3(0.35f, 0.35f, 0.35f), mats["Accent"], click: true);

            // Scenario UI in FRONT of tables (toward player) — behind the desk reads as thin lines.
            var confirmCanvas = CreateWorldCanvas("ConfirmCanvas", new Vector3(-4f, 1.55f, 1.35f), new Vector2(700, 280), 0.0028f);
            CreateScenarioButton(confirmCanvas.transform, "ConfirmBtn", "Подтвердить", "btn_confirm", new Vector2(0, 50), new Vector2(520, 100));
            CreateScenarioButton(confirmCanvas.transform, "WrongBtn", "Отклонить", "btn_reject", new Vector2(0, -70), new Vector2(520, 90));

            // Offset on X so the panel is viewed at an angle (dead-center backface reads as a line).
            var finishCanvas = CreateWorldCanvas("FinishCanvas", new Vector3(1.15f, 1.9f, 3.7f), new Vector2(700, 160), 0.0032f);
            CreateScenarioButton(finishCanvas.transform, "FinishBtn", "Завершить", "btn_finish", new Vector2(0, 0), new Vector2(560, 110));

            // Info + results + HUD
            var hudCanvas = CreateWorldCanvas("HudCanvas", new Vector3(0f, 2.2f, 0f), new Vector2(900, 260), 0.0015f, raycastTarget: false);
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                hudCanvas.transform.SetParent(cam.transform, false);
                hudCanvas.transform.localPosition = new Vector3(0f, -0.12f, 0.85f);
                hudCanvas.transform.localRotation = Quaternion.identity;
                hudCanvas.transform.localScale = Vector3.one * 0.0016f;
            }

            var infoGo = new GameObject("InfoPanel", typeof(RectTransform), typeof(CanvasGroup));
            infoGo.transform.SetParent(hudCanvas.transform, false);
            var infoRt = infoGo.GetComponent<RectTransform>();
            infoRt.sizeDelta = new Vector2(860, 220);
            var infoBg = infoGo.AddComponent<Image>();
            infoBg.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            var infoTitle = CreateUiText(infoGo.transform, "InfoTitle", "", 34, new Vector2(0, 70), new Vector2(820, 50), TextAnchor.MiddleCenter);
            var infoBody = CreateUiText(infoGo.transform, "InfoBody", "", 28, new Vector2(0, -20), new Vector2(820, 140), TextAnchor.UpperCenter);
            var infoPanel = infoGo.AddComponent<ScenarioInfoPanel>();
            var infoSo = new SerializedObject(infoPanel);
            infoSo.FindProperty("canvasGroup").objectReferenceValue = infoGo.GetComponent<CanvasGroup>();
            infoSo.FindProperty("titleText").objectReferenceValue = infoTitle;
            infoSo.FindProperty("bodyText").objectReferenceValue = infoBody;
            infoSo.ApplyModifiedPropertiesWithoutUndo();

            var resultsCanvas = CreateWorldCanvas("ResultsCanvas", new Vector3(0f, 1.7f, 1.5f), new Vector2(1100, 900), 0.0026f);
            var resultsPanelGo = CreatePanel(resultsCanvas.transform, "ResultsPanel", Vector2.zero, new Vector2(1000, 820));
            var resultsCg = resultsPanelGo.AddComponent<CanvasGroup>();
            var summary = CreateUiText(resultsPanelGo.transform, "Summary", "Итог", 42, new Vector2(0, 340), new Vector2(920, 60), TextAnchor.MiddleCenter);
            var details = CreateUiText(resultsPanelGo.transform, "Details", "", 28, new Vector2(0, 40), new Vector2(920, 520), TextAnchor.UpperLeft);
            var restart = CreateUiButton(resultsPanelGo.transform, "Restart", "Попытаться ещё", new Vector2(-230, -330), new Vector2(400, 90));
            var lobby = CreateUiButton(resultsPanelGo.transform, "Lobby", "Возврат в Лобби", new Vector2(230, -330), new Vector2(400, 90));
            var results = resultsPanelGo.AddComponent<ResultsPanel>();
            var resultsSo = new SerializedObject(results);
            resultsSo.FindProperty("canvasGroup").objectReferenceValue = resultsCg;
            resultsSo.FindProperty("summaryText").objectReferenceValue = summary;
            resultsSo.FindProperty("detailsText").objectReferenceValue = details;
            resultsSo.FindProperty("restartButton").objectReferenceValue = restart;
            resultsSo.FindProperty("lobbyButton").objectReferenceValue = lobby;
            resultsSo.FindProperty("scenarioController").objectReferenceValue = controller;
            resultsSo.FindProperty("lobbySceneName").stringValue = "Lobby";
            resultsSo.ApplyModifiedPropertiesWithoutUndo();
            resultsCg.alpha = 0f;
            resultsCg.interactable = false;
            resultsCg.blocksRaycasts = false;

            CreateEventSystem();
            EditorSceneManager.SaveScene(scene, ScenesPath + "/Training.unity");
        }

        private static void SetupBuildSettings()
        {
            var lobby = ScenesPath + "/Lobby.unity";
            var training = ScenesPath + "/Training.unity";
            EditorBuildSettings.scenes = new[]
            {
                new EditorBuildSettingsScene(lobby, true),
                new EditorBuildSettingsScene(training, true)
            };
        }

        private static GameObject CreateEnvironment(string name, Dictionary<string, Material> mats, Vector3 size)
        {
            var root = new GameObject(name);
            var floor = CreatePrimitive(PrimitiveType.Cube, "Floor", new Vector3(0f, -0.05f, 0f), new Vector3(size.x, 0.1f, size.z), mats["Floor"]);
            floor.transform.SetParent(root.transform);

            CreatePrimitive(PrimitiveType.Cube, "WallN", new Vector3(0f, size.y * 0.5f, size.z * 0.5f), new Vector3(size.x, size.y, 0.15f), mats["Wall"])
                .transform.SetParent(root.transform);
            CreatePrimitive(PrimitiveType.Cube, "WallS", new Vector3(0f, size.y * 0.5f, -size.z * 0.5f), new Vector3(size.x, size.y, 0.15f), mats["Wall"])
                .transform.SetParent(root.transform);
            CreatePrimitive(PrimitiveType.Cube, "WallE", new Vector3(size.x * 0.5f, size.y * 0.5f, 0f), new Vector3(0.15f, size.y, size.z), mats["Wall"])
                .transform.SetParent(root.transform);
            CreatePrimitive(PrimitiveType.Cube, "WallW", new Vector3(-size.x * 0.5f, size.y * 0.5f, 0f), new Vector3(0.15f, size.y, size.z), mats["Wall"])
                .transform.SetParent(root.transform);

            CreatePrimitive(PrimitiveType.Cube, "Ceiling", new Vector3(0f, size.y, 0f), new Vector3(size.x, 0.1f, size.z), mats["Panel"])
                .transform.SetParent(root.transform);

            var light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.25f;
            l.color = new Color(1f, 0.98f, 0.94f);
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.transform.SetParent(root.transform);

            var fill = new GameObject("Fill Light");
            var fl = fill.AddComponent<Light>();
            fl.type = LightType.Directional;
            fl.intensity = 0.35f;
            fl.color = new Color(0.7f, 0.8f, 1f);
            fill.transform.rotation = Quaternion.Euler(20f, 140f, 0f);
            fill.transform.SetParent(root.transform);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.5f, 0.6f);
            RenderSettings.ambientEquatorColor = new Color(0.25f, 0.27f, 0.3f);
            RenderSettings.ambientGroundColor = new Color(0.12f, 0.12f, 0.14f);
            return root;
        }

        private static GameObject CreatePlayer(Vector3 position)
        {
            var player = new GameObject("Player");
            player.tag = "Player";
            player.transform.position = position;
            player.AddComponent<PlayerMarker>();

            var controller = player.AddComponent<CharacterController>();
            controller.height = 1.8f;
            controller.radius = 0.25f;
            controller.center = new Vector3(0f, 0.9f, 0f);

            var head = new GameObject("Head");
            head.transform.SetParent(player.transform);
            head.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            head.tag = "MainCamera";
            var cam = head.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            head.AddComponent<AudioListener>();

            var slide = player.AddComponent<SlideLocomotion>();
            var slideSo = new SerializedObject(slide);
            slideSo.FindProperty("characterController").objectReferenceValue = controller;
            slideSo.FindProperty("headYawSource").objectReferenceValue = head.transform;
            slideSo.ApplyModifiedPropertiesWithoutUndo();

            var teleport = player.AddComponent<TeleportLocomotion>();
            var teleSo = new SerializedObject(teleport);
            teleSo.FindProperty("playerRoot").objectReferenceValue = player.transform;
            teleSo.FindProperty("aimCamera").objectReferenceValue = cam;
            teleSo.FindProperty("characterController").objectReferenceValue = controller;
            teleSo.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        private static GameObject CreateZone(string id, Vector3 pos, Vector3 size, Material mat, string labelOverride = null)
        {
            var go = CreatePrimitive(PrimitiveType.Cube, "Zone_" + id, pos, size, mat);
            Object.DestroyImmediate(go.GetComponent<Collider>());
            var box = go.AddComponent<BoxCollider>();
            box.isTrigger = true;
            var zone = go.AddComponent<InteractionZone>();
            var so = new SerializedObject(zone);
            so.FindProperty("targetId").stringValue = id;
            so.ApplyModifiedPropertiesWithoutUndo();
            var outline = go.AddComponent<OutlineHighlighter>();
            var oSo = new SerializedObject(outline);
            oSo.FindProperty("targetId").stringValue = id;
            oSo.ApplyModifiedPropertiesWithoutUndo();

            // Label uses its own unscaled world canvas — TextMesh under scaled zone becomes a "line".
            var label = string.IsNullOrEmpty(labelOverride)
                ? id.Replace("zone_", string.Empty).ToUpperInvariant()
                : labelOverride;
            var canvas = CreateWorldCanvas("Label_" + id, pos + Vector3.up * 1.35f, new Vector2(520, 120), 0.003f, raycastTarget: false);
            CreateUiText(canvas.transform, "Text", label, 48, Vector2.zero, new Vector2(500, 100), TextAnchor.MiddleCenter);
            return go;
        }

        private static GameObject CreateInteractableCube(
            string id, Vector3 pos, Vector3 scale, Material mat, bool grab = false, bool click = false)
        {
            var go = CreatePrimitive(PrimitiveType.Cube, id, pos, scale, mat);
            if (grab)
            {
                var rb = go.AddComponent<Rigidbody>();
                rb.isKinematic = true;
                var g = go.AddComponent<GrabbableInteractable>();
                var gSo = new SerializedObject(g);
                gSo.FindProperty("targetId").stringValue = id;
                gSo.ApplyModifiedPropertiesWithoutUndo();
                go.AddComponent<InteractionEventRelay>();
            }

            if (click)
            {
                var c = go.AddComponent<ClickableInteractable>();
                var cSo = new SerializedObject(c);
                cSo.FindProperty("targetId").stringValue = id;
                cSo.ApplyModifiedPropertiesWithoutUndo();
                if (go.GetComponent<InteractionEventRelay>() == null)
                    go.AddComponent<InteractionEventRelay>();
            }

            var outline = go.AddComponent<OutlineHighlighter>();
            var oSo = new SerializedObject(outline);
            oSo.FindProperty("targetId").stringValue = id;
            oSo.ApplyModifiedPropertiesWithoutUndo();
            return go;
        }

        private static GameObject CreatePrimitive(PrimitiveType type, string name, Vector3 pos, Vector3 scale, Material mat)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.position = pos;
            go.transform.localScale = scale;
            if (mat != null)
                go.GetComponent<MeshRenderer>().sharedMaterial = mat;
            return go;
        }

        private static Canvas CreateWorldCanvas(string name, Vector3 pos, Vector2 size, float scale, bool raycastTarget = true)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            if (raycastTarget)
                go.AddComponent<GraphicRaycaster>();
            go.transform.position = pos;
            var toPlayer = new Vector3(0f, pos.y, -5f) - pos;
            toPlayer.y = 0f;
            if (toPlayer.sqrMagnitude < 0.001f)
                toPlayer = Vector3.back;
            // Forward away from player → front faces the spawn (readable, not mirrored).
            go.transform.rotation = Quaternion.LookRotation((-toPlayer).normalized, Vector3.up);
            go.transform.localScale = Vector3.one * scale;
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = go.GetComponent<RectTransform>();
            rt.sizeDelta = size;
            return canvas;
        }

        private static GameObject CreatePanel(Transform parent, string name, Vector2 anchoredPos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = anchoredPos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.1f, 0.12f, 0.16f, 0.92f);
            return go;
        }

        private static Font CreateCyrillicFont()
        {
            // Must be a project asset so Text.font serializes into scenes.
            // Runtime UiFontBootstrap swaps to OS Dynamic for reliable Cyrillic glyphs.
            var imported = UiFontImporter.EnsureDynamicFont();
            if (imported != null)
            {
                const string sample =
                    "АБВГДЕЁЖЗИЙКЛМНОПРСТУФХЦЧШЩЪЫЬЭЮЯабвгдеёжзийклмнопрстуфхцчшщъыьэюя" +
                    "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789" +
                    "Документы Подтвердить Отклонить Выход Завершить Итог Лобби Досмотр Служебка " +
                    "Попытаться ещё Возврат ходьба обзор телепорт клик взять | -";
                foreach (var size in new[] { 64, 48, 40, 32 })
                    imported.RequestCharactersInTexture(sample, size, FontStyle.Normal);
                return imported;
            }

            var os = Font.CreateDynamicFontFromOSFont(
                new[] { "Segoe UI", "Arial", "Tahoma", "Microsoft Sans Serif" },
                64);
            return os
                   ?? Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf")
                   ?? Resources.GetBuiltinResource<Font>("Arial.ttf");
        }

        private static Text CreateUiText(
            Transform parent, string name, string text, int fontSize, Vector2 pos, Vector2 size, TextAnchor align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var ui = go.AddComponent<Text>();
            if (_uiFont != null)
            {
                ui.font = _uiFont;
                if (!string.IsNullOrEmpty(text))
                    _uiFont.RequestCharactersInTexture(text, fontSize, FontStyle.Normal);
            }

            ui.text = text;
            ui.fontSize = fontSize;
            ui.alignment = align;
            ui.color = Color.white;
            ui.horizontalOverflow = HorizontalWrapMode.Overflow;
            ui.verticalOverflow = VerticalWrapMode.Overflow;
            ui.supportRichText = false;
            ui.resizeTextForBestFit = false;
            return ui;
        }

        private static Button CreateUiButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var image = go.GetComponent<Image>();
            image.color = new Color(0.2f, 0.45f, 0.75f, 1f);
            var button = go.GetComponent<Button>();
            button.targetGraphic = image;
            button.transition = Selectable.Transition.None;

            // Sibling label under same parent canvas (not tinted / crushed by Button graphic).
            var labelUi = CreateUiText(parent, name + "_Label", label, 40, pos, size - new Vector2(20f, 20f), TextAnchor.MiddleCenter);
            labelUi.raycastTarget = false;
            // Pull label toward camera to avoid Z-fight with the Image (reads as a white line).
            var labelRt = labelUi.rectTransform;
            labelRt.localPosition = new Vector3(labelRt.localPosition.x, labelRt.localPosition.y, -3f);
            labelUi.transform.SetSiblingIndex(go.transform.GetSiblingIndex() + 1);
            return button;
        }

        private static void CreateScenarioButton(
            Transform parent, string name, string label, string targetId, Vector2 pos, Vector2 size)
        {
            var btn = CreateUiButton(parent, name, label, pos, size);
            var scenarioBtn = btn.gameObject.AddComponent<ScenarioUIButton>();
            var so = new SerializedObject(scenarioBtn);
            so.FindProperty("targetId").stringValue = targetId;
            so.FindProperty("button").objectReferenceValue = btn;
            so.ApplyModifiedPropertiesWithoutUndo();

            var outline = btn.gameObject.AddComponent<OutlineHighlighter>();
            var oSo = new SerializedObject(outline);
            oSo.FindProperty("targetId").stringValue = targetId;
            oSo.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateEventSystem()
        {
            if (Object.FindAnyObjectByType<EventSystem>() != null)
                return;
            var go = new GameObject("EventSystem", typeof(EventSystem), typeof(StandaloneInputModule));
        }
    }
}
#endif
