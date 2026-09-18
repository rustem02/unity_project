#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
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
            var mats = CreateMaterials();
            var scenario = CreateScenarioAsset();
            BuildLobbyScene(mats);
            BuildTrainingScene(mats, scenario);
            SetupBuildSettings();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            if (showDialog)
            {
                EditorUtility.DisplayDialog(
                    "VR Training",
                    "Контент собран.\nСцены: Lobby, Training.\nОткройте Lobby и нажмите Play.",
                    "OK");
            }
            else
            {
                Debug.Log("[VR Training] Project content built successfully.");
            }
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
            if (existing != null)
            {
                existing.color = color;
                if (existing.HasProperty("_BaseColor"))
                    existing.SetColor("_BaseColor", color);
                EditorUtility.SetDirty(existing);
                return existing;
            }

            var shader = Shader.Find("Universal Render Pipeline/Lit")
                         ?? Shader.Find("Lit")
                         ?? Shader.Find("Standard");
            var mat = new Material(shader) { name = name };
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            else
                mat.color = color;

            if (transparent)
            {
                // Soft zone tint — best-effort across pipelines.
                mat.SetFloat("_Surface", 1f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.renderQueue = 3000;
                var c = color;
                c.a = 0.35f;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", c);
                else
                    mat.color = c;
            }

            AssetDatabase.CreateAsset(mat, path);
            return mat;
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

            var canvas = CreateWorldCanvas("LobbyCanvas", new Vector3(0f, 1.6f, 2.5f), new Vector2(1200, 800), 0.0022f);
            var panel = CreatePanel(canvas.transform, "Panel", Vector2.zero, new Vector2(1100, 700));
            CreateTmp(panel.transform, "Title", "VR TRAINING", 64, new Vector2(0, 220), new Vector2(1000, 100), TextAlignmentOptions.Center);
            CreateTmp(panel.transform, "Subtitle", "Лобби · выберите действие", 36, new Vector2(0, 120), new Vector2(1000, 60), TextAlignmentOptions.Center);

            var startBtn = CreateUiButton(panel.transform, "StartButton", "Начать тренировку", new Vector2(0, -40), new Vector2(520, 100));
            var menu = canvas.gameObject.AddComponent<LobbyMenuController>();
            var so = new SerializedObject(menu);
            so.FindProperty("startTrainingButton").objectReferenceValue = startBtn;
            so.FindProperty("trainingSceneName").stringValue = "Training";
            so.ApplyModifiedPropertiesWithoutUndo();

            canvas.gameObject.AddComponent<DualInputUiBootstrap>();
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
            systems.AddComponent<DesktopInteractionRay>();
            systems.AddComponent<DualInputUiBootstrap>();
            systems.AddComponent<InteractableResetService>();

            // Tables / props
            var docsTable = CreatePrimitive(PrimitiveType.Cube, "DocumentsTable", new Vector3(-4f, 0.4f, 2f), new Vector3(1.6f, 0.8f, 0.9f), mats["Table"]);
            var inspTable = CreatePrimitive(PrimitiveType.Cube, "InspectionTable", new Vector3(3.5f, 0.4f, 2f), new Vector3(1.8f, 0.8f, 1.1f), mats["Table"]);
            var exitDesk = CreatePrimitive(PrimitiveType.Cube, "ExitDesk", new Vector3(0f, 0.4f, 5.5f), new Vector3(1.4f, 0.8f, 0.8f), mats["Table"]);

            CreateZone("zone_documents", new Vector3(-4f, 0.05f, 0.6f), new Vector3(2.2f, 0.1f, 2.2f), mats["Zone"]);
            CreateZone("zone_inspection", new Vector3(3.5f, 0.05f, 0.5f), new Vector3(2.4f, 0.1f, 2.4f), mats["Zone"]);
            CreateZone("zone_exit", new Vector3(0f, 0.05f, 4.2f), new Vector3(2.2f, 0.1f, 2f), mats["Zone"]);
            // Wrong zone distractor
            CreateZone("zone_wrong", new Vector3(-7f, 0.05f, -2f), new Vector3(1.8f, 0.1f, 1.8f), mats["Zone"]);

            var passport = CreateInteractableCube("passport", new Vector3(-4f, 0.95f, 2f), new Vector3(0.25f, 0.04f, 0.35f), mats["Document"], grab: true);
            var wrongPaper = CreateInteractableCube("wrong_paper", new Vector3(-3.5f, 0.95f, 2.15f), new Vector3(0.22f, 0.03f, 0.3f), mats["Panel"], grab: true);

            var bag = CreateInteractableCube("suspicious_bag", new Vector3(3.3f, 0.95f, 2f), new Vector3(0.45f, 0.3f, 0.55f), mats["Accent"], click: true);
            var decoy = CreateInteractableCube("decoy_box", new Vector3(3.9f, 0.95f, 2.2f), new Vector3(0.3f, 0.25f, 0.3f), mats["Wall"], click: true);
            var contraband = CreateInteractableCube("contraband", new Vector3(3.5f, 0.95f, 1.55f), new Vector3(0.2f, 0.15f, 0.2f), mats["Contraband"], grab: true);

            var indicator = CreateInteractableCube("exit_indicator", new Vector3(0.5f, 1.4f, 5.5f), new Vector3(0.25f, 0.25f, 0.25f), mats["Accent"], click: true);

            // Scenario UI near documents
            var confirmCanvas = CreateWorldCanvas("ConfirmCanvas", new Vector3(-4f, 1.55f, 2.55f), new Vector2(600, 220), 0.0018f);
            CreateTmp(confirmCanvas.transform, "Label", "Документы", 40, new Vector2(0, 60), new Vector2(560, 50), TextAlignmentOptions.Center);
            CreateScenarioButton(confirmCanvas.transform, "ConfirmBtn", "Подтвердить", "btn_confirm", new Vector2(0, -30), new Vector2(420, 90));
            CreateScenarioButton(confirmCanvas.transform, "WrongBtn", "Отклонить", "btn_reject", new Vector2(0, -130), new Vector2(420, 70));

            var finishCanvas = CreateWorldCanvas("FinishCanvas", new Vector3(0f, 1.55f, 6.1f), new Vector2(600, 220), 0.0018f);
            CreateTmp(finishCanvas.transform, "Label", "Выход", 40, new Vector2(0, 60), new Vector2(560, 50), TextAlignmentOptions.Center);
            CreateScenarioButton(finishCanvas.transform, "FinishBtn", "Завершить", "btn_finish", new Vector2(0, -30), new Vector2(420, 90));

            // Info + results + HUD
            var hudCanvas = CreateWorldCanvas("HudCanvas", new Vector3(0f, 2.2f, 0f), new Vector2(900, 260), 0.0015f);
            hudCanvas.transform.SetParent(Camera.main != null ? Camera.main.transform : null, false);
            // Attach HUD to player camera if present
            var cam = Object.FindAnyObjectByType<Camera>();
            if (cam != null)
            {
                hudCanvas.transform.SetParent(cam.transform, false);
                hudCanvas.transform.localPosition = new Vector3(0f, -0.15f, 0.7f);
                hudCanvas.transform.localRotation = Quaternion.identity;
                hudCanvas.transform.localScale = Vector3.one * 0.0012f;
            }

            var infoGo = new GameObject("InfoPanel", typeof(RectTransform), typeof(CanvasGroup));
            infoGo.transform.SetParent(hudCanvas.transform, false);
            var infoRt = infoGo.GetComponent<RectTransform>();
            infoRt.sizeDelta = new Vector2(860, 220);
            var infoBg = infoGo.AddComponent<Image>();
            infoBg.color = new Color(0.08f, 0.1f, 0.14f, 0.85f);
            var infoTitle = CreateTmp(infoGo.transform, "InfoTitle", "", 34, new Vector2(0, 70), new Vector2(820, 50), TextAlignmentOptions.Center);
            var infoBody = CreateTmp(infoGo.transform, "InfoBody", "", 28, new Vector2(0, -20), new Vector2(820, 140), TextAlignmentOptions.Top);
            var infoPanel = infoGo.AddComponent<ScenarioInfoPanel>();
            var infoSo = new SerializedObject(infoPanel);
            infoSo.FindProperty("canvasGroup").objectReferenceValue = infoGo.GetComponent<CanvasGroup>();
            infoSo.FindProperty("titleText").objectReferenceValue = infoTitle;
            infoSo.FindProperty("bodyText").objectReferenceValue = infoBody;
            infoSo.ApplyModifiedPropertiesWithoutUndo();

            var resultsCanvas = CreateWorldCanvas("ResultsCanvas", new Vector3(0f, 1.7f, 1.5f), new Vector2(1100, 900), 0.002f);
            var resultsPanelGo = CreatePanel(resultsCanvas.transform, "ResultsPanel", Vector2.zero, new Vector2(1000, 820));
            var resultsCg = resultsPanelGo.AddComponent<CanvasGroup>();
            var summary = CreateTmp(resultsPanelGo.transform, "Summary", "Итог", 42, new Vector2(0, 340), new Vector2(920, 60), TextAlignmentOptions.Center);
            var details = CreateTmp(resultsPanelGo.transform, "Details", "", 28, new Vector2(0, 40), new Vector2(920, 520), TextAlignmentOptions.TopLeft);
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

            var light = new GameObject("Directional Light");
            var l = light.AddComponent<Light>();
            l.type = LightType.Directional;
            l.intensity = 1.1f;
            light.transform.rotation = Quaternion.Euler(50f, -30f, 0f);
            light.transform.SetParent(root.transform);

            RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.45f, 0.5f, 0.6f);
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
            teleSo.ApplyModifiedPropertiesWithoutUndo();

            return player;
        }

        private static GameObject CreateZone(string id, Vector3 pos, Vector3 size, Material mat)
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

        private static Canvas CreateWorldCanvas(string name, Vector3 pos, Vector2 size, float scale)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            go.transform.position = pos;
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

        private static TextMeshProUGUI CreateTmp(
            Transform parent, string name, string text, float fontSize, Vector2 pos, Vector2 size, TextAlignmentOptions align)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            var tmp = go.AddComponent<TextMeshProUGUI>();
            tmp.text = text;
            tmp.fontSize = fontSize;
            tmp.alignment = align;
            tmp.color = Color.white;
            tmp.textWrappingMode = TextWrappingModes.Normal;
            return tmp;
        }

        private static Button CreateUiButton(Transform parent, string name, string label, Vector2 pos, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
            go.transform.SetParent(parent, false);
            var rt = go.GetComponent<RectTransform>();
            rt.anchoredPosition = pos;
            rt.sizeDelta = size;
            go.GetComponent<Image>().color = new Color(0.2f, 0.45f, 0.75f, 1f);
            var tmp = CreateTmp(go.transform, "Label", label, 34, Vector2.zero, size, TextAlignmentOptions.Center);
            tmp.raycastTarget = false;
            return go.GetComponent<Button>();
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
