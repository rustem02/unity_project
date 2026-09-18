#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace VRTraining.EditorTools
{
    /// <summary>
    /// Ensures URP pipeline asset exists and is assigned in Graphics/Quality settings.
    /// Without this, URP Lit materials render magenta (error shader).
    /// </summary>
    public static class UrpProjectSetup
    {
        private const string SettingsFolder = "Assets/_Project/Settings";
        private const string RendererPath = SettingsFolder + "/URP_Renderer.asset";
        private const string PipelinePath = SettingsFolder + "/URP_Pipeline.asset";

        [MenuItem("VR Training/Fix URP Pipeline", priority = 1)]
        public static void FixUrpMenu()
        {
            var ok = EnsureUrpConfigured();
            EditorUtility.DisplayDialog(
                "VR Training",
                ok
                    ? "URP pipeline РЅР°Р·РЅР°С‡РµРЅ. РњР°С‚РµСЂРёР°Р»С‹ Р±РѕР»СЊС€Рµ РЅРµ РґРѕР»Р¶РЅС‹ Р±С‹С‚СЊ СЂРѕР·РѕРІС‹РјРё."
                    : "РќРµ СѓРґР°Р»РѕСЃСЊ РЅР°СЃС‚СЂРѕРёС‚СЊ URP. РџСЂРѕРІРµСЂСЊС‚Рµ, С‡С‚Рѕ РїР°РєРµС‚ URP СѓСЃС‚Р°РЅРѕРІР»РµРЅ.",
                "OK");
        }

        public static bool EnsureUrpConfigured()
        {
            if (!AssetDatabase.IsValidFolder("Assets/_Project"))
                AssetDatabase.CreateFolder("Assets", "_Project");
            if (!AssetDatabase.IsValidFolder(SettingsFolder))
                AssetDatabase.CreateFolder("Assets/_Project", "Settings");

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>(RendererPath);
            if (renderer == null)
            {
                renderer = ScriptableObject.CreateInstance<UniversalRendererData>();
                AssetDatabase.CreateAsset(renderer, RendererPath);
            }

            var pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(PipelinePath);
            if (pipeline == null)
            {
                pipeline = UniversalRenderPipelineAsset.Create(renderer);
                AssetDatabase.CreateAsset(pipeline, PipelinePath);
            }
            else
            {
                // Keep renderer reference healthy.
                var so = new SerializedObject(pipeline);
                var prop = so.FindProperty("m_RendererDataList");
                if (prop != null && prop.isArray && prop.arraySize > 0)
                {
                    prop.GetArrayElementAtIndex(0).objectReferenceValue = renderer;
                    so.ApplyModifiedPropertiesWithoutUndo();
                }
            }

            GraphicsSettings.defaultRenderPipeline = pipeline;
            QualitySettings.renderPipeline = pipeline;

            // All quality levels
            var count = QualitySettings.names.Length;
            for (var i = 0; i < count; i++)
            {
                QualitySettings.SetQualityLevel(i, false);
                QualitySettings.renderPipeline = pipeline;
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            var active = GraphicsSettings.currentRenderPipeline;
            var ok = active != null && active is UniversalRenderPipelineAsset;
            Debug.Log(ok
                ? $"[VR Training] URP OK: {AssetDatabase.GetAssetPath(pipeline)}"
                : "[VR Training] URP FAILED to activate.");
            return ok;
        }

        public static void ForceUrpLitOnMaterial(Material mat, Color color, bool transparent)
        {
            if (mat == null)
                return;

            var shader = Shader.Find("Universal Render Pipeline/Lit");
            if (shader == null)
                shader = Shader.Find("Lit");
            if (shader == null)
            {
                Debug.LogError("[VR Training] URP Lit shader not found.");
                return;
            }

            mat.shader = shader;
            if (mat.HasProperty("_BaseColor"))
                mat.SetColor("_BaseColor", color);
            if (mat.HasProperty("_Color"))
                mat.SetColor("_Color", color);

            if (transparent)
            {
                mat.SetFloat("_Surface", 1f);
                mat.SetFloat("_Blend", 0f);
                mat.SetOverrideTag("RenderType", "Transparent");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
                mat.SetInt("_ZWrite", 0);
                mat.DisableKeyword("_ALPHATEST_ON");
                mat.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Transparent;
                var c = color;
                c.a = 0.35f;
                if (mat.HasProperty("_BaseColor"))
                    mat.SetColor("_BaseColor", c);
            }
            else
            {
                mat.SetFloat("_Surface", 0f);
                mat.SetOverrideTag("RenderType", "Opaque");
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.One);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.Zero);
                mat.SetInt("_ZWrite", 1);
                mat.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
                mat.renderQueue = (int)RenderQueue.Geometry;
            }

            EditorUtility.SetDirty(mat);
        }
    }
}
#endif
