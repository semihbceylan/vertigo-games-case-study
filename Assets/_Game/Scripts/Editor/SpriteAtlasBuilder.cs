#if UNITY_EDITOR
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.U2D;
using UnityEngine;
using UnityEngine.U2D;

namespace Vertigo.WheelOfFortune.EditorTools
{
    /// <summary>
    /// Builds the project's sprite atlases from the raw art folders. Reusable via the Tools menu
    /// so atlases can be regenerated after art changes.
    /// </summary>
    public static class SpriteAtlasBuilder
    {
        #region Atlas Building

        private const string ART_FOLDER = "Assets/_Game/Art";
        private const string UI_SOURCE_FOLDER = "Assets/Art";
        private const string ICON_SOURCE_FOLDER = "Assets/Art/Icons";
        private const string GAME_FOLDER = "Assets/_Game";
        private const string ART_FOLDER_NAME = "Art";
        private const string UI_ATLAS_PATH = ART_FOLDER + "/UI_Atlas.spriteatlas";
        private const string ICON_ATLAS_PATH = ART_FOLDER + "/Icons_Atlas.spriteatlas";
        /// <summary>Texels between packed sprites so bilinear sampling never bleeds neighbours.</summary>
        private const int ATLAS_PADDING = 2;

        [MenuItem("Tools/Wheel of Fortune/Rebuild Sprite Atlases")]
        public static void Rebuild()
        {
            if (!AssetDatabase.IsValidFolder(ART_FOLDER))
                AssetDatabase.CreateFolder(GAME_FOLDER, ART_FOLDER_NAME);

            BuildUiAtlas();
            BuildIconAtlas();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[SpriteAtlasBuilder] Sprite atlases rebuilt.");
        }

        private static void BuildUiAtlas()
        {
            // Top-level UI sprites only; the Icons subfolder has its own atlas.
            var sources = new List<Object>();
            AddSpritesFromTopLevel(UI_SOURCE_FOLDER, sources);
            CreateAtlas(UI_ATLAS_PATH, sources);
        }

        private static void BuildIconAtlas()
        {
            var sources = new List<Object>();
            AddFolder(ICON_SOURCE_FOLDER, sources);
            CreateAtlas(ICON_ATLAS_PATH, sources);
        }

        private static void AddSpritesFromTopLevel(string folder, List<Object> into)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;

            string[] files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = files[i].Replace('\\', '/');
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite)
                {
                    into.Add(sprite);
                }
            }
        }

        private static void AddFolder(string folder, List<Object> into)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(folder);
            if (obj)
            {
                into.Add(obj);
            }
        }

        private static void CreateAtlas(string path, List<Object> sources)
        {
            var atlas = new SpriteAtlas();

            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                padding = ATLAS_PADDING,
                enableRotation = false,
                enableTightPacking = false,
                enableAlphaDilation = false
            });

            atlas.SetTextureSettings(new SpriteAtlasTextureSettings
            {
                readable = false,
                generateMipMaps = false,
                sRGB = true,
                filterMode = FilterMode.Bilinear
            });

            if (sources.Count > 0)
                SpriteAtlasExtensions.Add(atlas, sources.ToArray());

            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path))
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(atlas, path);
            EditorUtility.SetDirty(atlas);
        }

        #endregion
    }
}
#endif
