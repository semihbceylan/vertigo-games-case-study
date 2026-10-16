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
        private const string ArtFolder = "Assets/_Game/Art";
        private const string UiSourceFolder = "Assets/Art";
        private const string IconSourceFolder = "Assets/Art/Icons";

        [MenuItem("Tools/Wheel of Fortune/Rebuild Sprite Atlases")]
        public static void Rebuild()
        {
            if (!AssetDatabase.IsValidFolder(ArtFolder))
                AssetDatabase.CreateFolder("Assets/_Game", "Art");

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
            AddSpritesFromTopLevel(UiSourceFolder, sources);
            CreateAtlas(ArtFolder + "/UI_Atlas.spriteatlas", sources);
        }

        private static void BuildIconAtlas()
        {
            var sources = new List<Object>();
            AddFolder(IconSourceFolder, sources);
            CreateAtlas(ArtFolder + "/Icons_Atlas.spriteatlas", sources);
        }

        private static void AddSpritesFromTopLevel(string folder, List<Object> into)
        {
            if (!AssetDatabase.IsValidFolder(folder)) return;

            string[] files = Directory.GetFiles(folder, "*.png", SearchOption.TopDirectoryOnly);
            for (int i = 0; i < files.Length; i++)
            {
                string assetPath = files[i].Replace('\\', '/');
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
                if (sprite != null) into.Add(sprite);
            }
        }

        private static void AddFolder(string folder, List<Object> into)
        {
            var obj = AssetDatabase.LoadAssetAtPath<Object>(folder);
            if (obj != null) into.Add(obj);
        }

        private static void CreateAtlas(string path, List<Object> sources)
        {
            var atlas = new SpriteAtlas();

            atlas.SetPackingSettings(new SpriteAtlasPackingSettings
            {
                padding = 2,
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

            if (AssetDatabase.LoadAssetAtPath<SpriteAtlas>(path) != null)
                AssetDatabase.DeleteAsset(path);

            AssetDatabase.CreateAsset(atlas, path);
            EditorUtility.SetDirty(atlas);
        }
    }
}
#endif
