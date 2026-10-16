using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Full-screen elliptical vignette. Darkens the edges of its overlay, reaching further in
    /// from the left and right than from the top and bottom. The gradient is generated at runtime
    /// and regenerated whenever a parameter changes, so it can be tweaked live in the inspector.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class ScreenVignette : MonoBehaviour
    {
        [SerializeField] private Image target;

        [Header("Look")]
        [SerializeField] private Color color = Color.black;
        [Tooltip("How dark the edges get. 0 = invisible, 1 = fully opaque at the edge.")]
        [SerializeField, Range(0f, 1f)] private float intensity = 0.85f;
        [Tooltip("How far the clear region reaches sideways. Smaller values darken the left/right more.")]
        [SerializeField, Min(0.01f)] private float horizontalRadius = 0.6f;
        [Tooltip("How far the clear region reaches vertically. Larger than the horizontal radius keeps the top/bottom lighter.")]
        [SerializeField, Min(0.01f)] private float verticalRadius = 1f;
        [Tooltip("Distance from the centre where the darkening starts.")]
        [SerializeField, Range(0f, 2f)] private float clearRadius = 0.6f;
        [Tooltip("Width of the fade between the clear centre and the dark edge.")]
        [SerializeField, Range(0.01f, 2f)] private float softness = 0.6f;

        [Header("Quality")]
        [SerializeField, Range(16, 512)] private int resolution = 256;

        private Texture2D _texture;
        private Sprite _sprite;
        private int _lastHash;

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnDisable()
        {
            ReleaseGenerated();
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            Rebuild();
        }

        private void Reset()
        {
            target = GetComponent<Image>();
            Rebuild();
        }
#endif

        public void Rebuild()
        {
            if (target == null) target = GetComponent<Image>();
            if (target == null) return;

            int hash = ComputeHash();
            if (_sprite != null && hash == _lastHash) return;
            _lastHash = hash;

            int size = Mathf.Clamp(resolution, 16, 512);
            if (_texture == null || _texture.width != size)
            {
                ReleaseGenerated();

                _texture = new Texture2D(size, size, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = "ui_vignette_generated"
                };
            }

            var pixels = new Color32[size * size];
            float inner = Mathf.Max(0f, clearRadius);
            float outer = inner + Mathf.Max(0.01f, softness);

            for (int y = 0; y < size; y++)
            {
                float v = ((y + 0.5f) / size) * 2f - 1f;

                for (int x = 0; x < size; x++)
                {
                    float u = ((x + 0.5f) / size) * 2f - 1f;

                    float du = u / horizontalRadius;
                    float dv = v / verticalRadius;
                    float distance = Mathf.Sqrt(du * du + dv * dv);

                    float t = Mathf.Clamp01((distance - inner) / (outer - inner));
                    t = t * t * (3f - 2f * t);

                    pixels[y * size + x] = new Color32(255, 255, 255, (byte)Mathf.RoundToInt(t * intensity * 255f));
                }
            }

            _texture.SetPixels32(pixels);
            _texture.Apply(false, false);

            if (_sprite != null) DestroySafe(_sprite);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, size, size), new Vector2(0.5f, 0.5f), 100f);
            _sprite.name = "ui_vignette_generated";
            _sprite.hideFlags = HideFlags.HideAndDontSave;

            target.sprite = _sprite;
            target.color = color;
            target.type = Image.Type.Simple;
            target.raycastTarget = false;
        }

        private int ComputeHash()
        {
            unchecked
            {
                int hash = 17;
                hash = hash * 31 + intensity.GetHashCode();
                hash = hash * 31 + horizontalRadius.GetHashCode();
                hash = hash * 31 + verticalRadius.GetHashCode();
                hash = hash * 31 + clearRadius.GetHashCode();
                hash = hash * 31 + softness.GetHashCode();
                hash = hash * 31 + resolution;
                hash = hash * 31 + color.GetHashCode();
                return hash;
            }
        }

        private void ReleaseGenerated()
        {
            if (_sprite != null)
            {
                DestroySafe(_sprite);
                _sprite = null;
            }

            if (_texture != null)
            {
                DestroySafe(_texture);
                _texture = null;
            }

            _lastHash = 0;
        }

        private static void DestroySafe(Object obj)
        {
            if (obj == null) return;

            if (Application.isPlaying) Destroy(obj);
            else DestroyImmediate(obj);
        }
    }
}
