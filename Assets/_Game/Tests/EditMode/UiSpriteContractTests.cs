using NUnit.Framework;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.Tests
{
    public sealed class UiSpriteContractTests
    {
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
                        Assert.AreEqual(4, groups.arraySize, view.name);
                        for (int i = 0; i < groups.arraySize; i++)
                        {
                            Assert.IsTrue(groups.GetArrayElementAtIndex(i).objectReferenceValue, view.name);
                        }
                    }
                }
                Assert.AreEqual(3, count);
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
