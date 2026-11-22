using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class UiSpriteContractTests
    {
        private const string GAME_SCENE = "Assets/_Game/Scenes/Game.unity";
        private const string PREFAB_ROOT = "Assets/_Game/Prefabs";

        [Test]
        public void SceneHierarchyUsesTypeTokensAndCorrectedValueNames()
        {
            var scene = EditorSceneManager.OpenPreviewScene(GAME_SCENE);
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects()) ValidateHierarchy(root);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void AllFirstPartyUiPrefabsMeetSpriteNamingAndReferenceContracts()
        {
            string[] guids = UnityEditor.AssetDatabase.FindAssets("t:Prefab", new[] { PREFAB_ROOT });
            Assert.Greater(guids.Length, 0);
            foreach (string guid in guids)
            {
                string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
                GameObject root = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Assert.IsTrue(root, path);
                ValidateHierarchy(root);
                ValidateReferences(root);
                foreach (Image image in root.GetComponentsInChildren<Image>(true))
                {
                    if (!image.sprite) continue;
                    if (image.type == Image.Type.Simple) Assert.IsTrue(image.preserveAspect, path + ": " + image.name);
                    if (image.type != Image.Type.Sliced) continue;
                    Vector4 border = image.sprite.border;
                    Assert.Greater(border.x + border.y + border.z + border.w, 0f, path + ": " + image.name);
                    Assert.LessOrEqual(border.x + border.z, image.sprite.rect.width, image.name);
                    Assert.LessOrEqual(border.y + border.w, image.sprite.rect.height, image.name);
                }
            }
        }

        [Test]
        public void SceneHasNoBrokenReferencesAndPreservesCanvasContract()
        {
            var scene = EditorSceneManager.OpenPreviewScene(GAME_SCENE);
            int scalers = 0;
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    ValidateReferences(root);
                    foreach (CanvasScaler scaler in root.GetComponentsInChildren<CanvasScaler>(true))
                    {
                        scalers++;
                        Assert.AreEqual(CanvasScaler.ScaleMode.ScaleWithScreenSize, scaler.uiScaleMode);
                        Assert.AreEqual(CanvasScaler.ScreenMatchMode.Expand, scaler.screenMatchMode);
                    }
                    foreach (Button button in root.GetComponentsInChildren<Button>(true))
                    {
                        Assert.AreEqual(0, button.onClick.GetPersistentEventCount(), button.name);
                    }
                    Assert.IsEmpty(root.GetComponentsInChildren<Text>(true), "Use TextMeshPro, not legacy Text.");
                }
                Assert.Greater(scalers, 0);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        private static void ValidateHierarchy(GameObject root)
        {
            foreach (RectTransform rect in root.GetComponentsInChildren<RectTransform>(true))
            {
                StringAssert.IsMatch(@"^ui_(canvas|panel|image|text|button|layout|list|scroll)_[a-z0-9_]+$", rect.name);
                if (rect.name.StartsWith("ui_text_leave") || rect.name == "ui_text_restart" ||
                    rect.name.StartsWith("ui_text_results_restart") || rect.name == "ui_text_safe_zone" ||
                    rect.name == "ui_text_super_zone" || rect.name.EndsWith("_caption"))
                {
                    Assert.IsFalse(rect.name.EndsWith("_value"), rect.name);
                }
                if (rect.name.StartsWith("ui_image_wheel_base") || rect.name.StartsWith("ui_image_wheel_indicator") ||
                    rect.name.StartsWith("ui_text_wheel_amount") || rect.name.StartsWith("ui_image_reward_icon"))
                {
                    StringAssert.EndsWith("_value", rect.name);
                }
            }
        }

        private static void ValidateReferences(GameObject root)
        {
            foreach (Transform transform in root.GetComponentsInChildren<Transform>(true))
            {
                Assert.AreEqual(0, UnityEditor.GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject), transform.name);
                foreach (Component component in transform.GetComponents<Component>())
                {
                    if (!component) continue;
                    var serialized = new UnityEditor.SerializedObject(component);
                    var property = serialized.GetIterator();
                    while (property.Next(true))
                    {
                        if (property.propertyType != UnityEditor.SerializedPropertyType.ObjectReference) continue;
                        Assert.IsFalse(!property.objectReferenceValue && property.objectReferenceInstanceIDValue != 0,
                            transform.name + ": " + property.propertyPath);
                    }
                }
            }
        }

        private static readonly string[] HUD_SECTIONS =
        {
            "ui_panel_game_header", "ui_panel_game_rewards", "ui_panel_game_zone_maps", "ui_panel_game_wheel"
        };

        [Test]
        public void ModalViewsReferenceEveryHudSection()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/Game.unity");
            int count = 0;
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (MonoBehaviour view in root.GetComponentsInChildren<MonoBehaviour>(true))
                    {
                        if (!view) continue;
                        var serialized = new UnityEditor.SerializedObject(view);
                        var groups = serialized.FindProperty("_underlyingHud");
                        if (groups == null) continue;
                        count++;
                        var hidden = new System.Collections.Generic.HashSet<string>();
                        for (int i = 0; i < groups.arraySize; i++)
                        {
                            UnityEngine.Object group = groups.GetArrayElementAtIndex(i).objectReferenceValue;
                            Assert.IsTrue(group, view.name);
                            hidden.Add(group.name);
                        }
                        foreach (string section in HUD_SECTIONS)
                        {
                            Assert.IsTrue(hidden.Contains(section), view.name + " must hide " + section);
                        }
                    }
                }
                Assert.AreEqual(4, count);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void SlicedSceneImagesHaveNonzeroBordersWithinSpriteDimensions()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/Game.unity");
            int count = 0;
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (image.type != Image.Type.Sliced) continue;
                        count++;
                        Assert.IsTrue(image.sprite, image.name);
                        Vector4 border = image.sprite.border;
                        Assert.Greater(border.x + border.y + border.z + border.w, 0f, image.name);
                        Assert.LessOrEqual(border.x + border.z, image.sprite.rect.width, image.name);
                        Assert.LessOrEqual(border.y + border.w, image.sprite.rect.height, image.name);
                    }
                }
                Assert.Greater(count, 0);
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }

        [Test]
        public void AuthoredSimpleSceneImagesPreserveAspectRatio()
        {
            var scene = EditorSceneManager.OpenPreviewScene("Assets/_Game/Scenes/Game.unity");
            try
            {
                foreach (GameObject root in scene.GetRootGameObjects())
                {
                    foreach (Image image in root.GetComponentsInChildren<Image>(true))
                    {
                        if (!image.sprite || image.type != Image.Type.Simple) continue;
                        // Generated screen-space gradients deliberately stretch to cover their panel.
                        if (string.IsNullOrEmpty(UnityEditor.AssetDatabase.GetAssetPath(image.sprite))) continue;
                        Assert.IsTrue(image.preserveAspect, image.name);
                    }
                }
            }
            finally
            {
                EditorSceneManager.ClosePreviewScene(scene);
            }
        }
    }
}
