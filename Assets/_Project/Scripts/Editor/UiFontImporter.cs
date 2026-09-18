using System.IO;
using UnityEditor;
using UnityEngine;

namespace VRTraining.EditorTools
{
    /// <summary>
    /// Ensures UIFont.ttf is imported as Dynamic (full Unicode/Cyrillic support).
    /// </summary>
    public static class UiFontImporter
    {
        public const string FontPath = "Assets/_Project/Fonts/UIFont.ttf";

        [MenuItem("VR Training/Reimport UI Font", priority = 2)]
        public static void ReimportMenu()
        {
            var font = EnsureDynamicFont();
            EditorUtility.DisplayDialog(
                "VR Training",
                font != null ? "UIFont imported as Dynamic." : "UIFont.ttf not found.",
                "OK");
        }

        public static Font EnsureDynamicFont()
        {
            if (!File.Exists(FontPath) && !File.Exists(Path.GetFullPath(FontPath)))
            {
                // Unity project-relative check
                if (AssetDatabase.LoadAssetAtPath<Font>(FontPath) == null)
                    return null;
            }

            var importer = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
            if (importer != null)
            {
                importer.fontTextureCase = FontTextureCase.Dynamic;
                importer.fontRenderingMode = FontRenderingMode.Smooth;
                importer.fontSize = 64;
                importer.includeFontData = true;
                importer.SaveAndReimport();
            }
            else
            {
                AssetDatabase.ImportAsset(FontPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(FontPath) as TrueTypeFontImporter;
                if (importer != null)
                {
                    importer.fontTextureCase = FontTextureCase.Dynamic;
                    importer.fontRenderingMode = FontRenderingMode.Smooth;
                    importer.fontSize = 64;
                    importer.includeFontData = true;
                    importer.SaveAndReimport();
                }
            }

            return AssetDatabase.LoadAssetAtPath<Font>(FontPath);
        }
    }
}
