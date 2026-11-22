using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Full-screen elliptical vignette. Darkens the edges of its overlay, reaching further in from
    /// the left and right than from the top and bottom. The gradient texture is generated when the
    /// component is enabled or a setting changes, and is destroyed when the component is disabled.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(Image))]
    public sealed class ScreenVignette : MonoBehaviour
    {
        #region Configuration and Runtime

        private const int MINIMUM_RESOLUTION = 16;
        private const int MAXIMUM_RESOLUTION = 512;
        private const float MINIMUM_SOFTNESS = 0.01f;
        /// <summary>Samples each texel at its centre rather than its corner.</summary>
        private const float TEXEL_CENTRE = 0.5f;
        /// <summary>Texture coordinates are remapped to [-1, 1] around the centre.</summary>
        private const float NORMALIZED_MIN = -1f;
        private const float NORMALIZED_MAX = 1f;
        private const float SPRITE_PIXELS_PER_UNIT = 100f;
        private const float SQUARE_ASPECT = 1f;
        private static readonly Vector2 SPRITE_PIVOT = new Vector2(0.5f, 0.5f);
        private const string GENERATED_NAME = "ui_vignette_generated";

        [SerializeField] private Image _target;

        [Header("Look")]
        [SerializeField] private Color _color = Color.black;
        [Tooltip("How dark the edges get. 0 = invisible, 1 = fully opaque at the edge.")]
        [SerializeField, Range(0f, 1f)] private float _intensity = 0.85f;
        [Tooltip("How far the clear region reaches sideways. Smaller values darken the left/right more.")]
        [SerializeField, Min(MINIMUM_SOFTNESS)] private float _horizontalRadius = 0.6f;
        [Tooltip("How far the clear region reaches vertically. Larger than the horizontal radius keeps the top/bottom lighter.")]
        [SerializeField, Min(MINIMUM_SOFTNESS)] private float _verticalRadius = 1f;
        [Tooltip("Distance from the centre where the darkening starts.")]
        [SerializeField, Range(0f, 2f)] private float _clearRadius = 0.6f;
        [Tooltip("Width of the fade between the clear centre and the dark edge.")]
        [SerializeField, Range(MINIMUM_SOFTNESS, 2f)] private float _softness = 0.6f;

        [Header("Quality")]
        [Tooltip("Texels along the longer side; the shorter side follows the target's aspect ratio so the sprite is never stretched.")]
        [SerializeField, Range(MINIMUM_RESOLUTION, MAXIMUM_RESOLUTION)] private int _resolution = 256;

        private Texture2D _texture;
        private Sprite _sprite;
        private Settings _builtSettings;

        private struct Settings
        {
            public Color Color;
            public float Intensity;
            public float HorizontalRadius;
            public float VerticalRadius;
            public float ClearRadius;
            public float Softness;
            public int Width;
            public int Height;
        }

        #endregion

        #region Lifecycle

        private void OnEnable()
        {
            Rebuild();
        }

        private void OnDisable()
        {
            ReleaseGenerated();
        }

        private void OnRectTransformDimensionsChange()
        {
            if (isActiveAndEnabled)
            {
                Rebuild();
            }
        }

        #endregion

        #region Generation

        public void Rebuild()
        {
            if (!_target)
            {
                return;
            }
            Settings settings = CaptureSettings();
            if (_sprite && settings.Equals(_builtSettings))
            {
                return;
            }
            _builtSettings = settings;

            int width = settings.Width;
            int height = settings.Height;
            if (!_texture || _texture.width != width || _texture.height != height)
            {
                ReleaseGenerated();
                _texture = new Texture2D(width, height, TextureFormat.RGBA32, false)
                {
                    hideFlags = HideFlags.HideAndDontSave,
                    wrapMode = TextureWrapMode.Clamp,
                    filterMode = FilterMode.Bilinear,
                    name = GENERATED_NAME
                };
            }

            var pixels = new Color32[width * height];
            float inner = settings.ClearRadius;
            float outer = inner + settings.Softness;
            for (int y = 0; y < height; y++)
            {
                float v = Mathf.Lerp(NORMALIZED_MIN, NORMALIZED_MAX, (y + TEXEL_CENTRE) / height) / settings.VerticalRadius;
                for (int x = 0; x < width; x++)
                {
                    float u = Mathf.Lerp(NORMALIZED_MIN, NORMALIZED_MAX, (x + TEXEL_CENTRE) / width) / settings.HorizontalRadius;
                    float distance = Mathf.Sqrt(u * u + v * v);
                    float darkness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, distance));
                    byte alpha = (byte)Mathf.RoundToInt(darkness * settings.Intensity * byte.MaxValue);
                    pixels[y * width + x] = new Color32(byte.MaxValue, byte.MaxValue, byte.MaxValue, alpha);
                }
            }
            _texture.SetPixels32(pixels);
            _texture.Apply(false, false);

            DestroyGenerated(_sprite);
            _sprite = Sprite.Create(_texture, new Rect(0f, 0f, width, height), SPRITE_PIVOT, SPRITE_PIXELS_PER_UNIT);
            _sprite.name = GENERATED_NAME;
            _sprite.hideFlags = HideFlags.HideAndDontSave;
            _target.sprite = _sprite;
            _target.color = settings.Color;
        }

        private Settings CaptureSettings()
        {
            int resolution = Mathf.Clamp(_resolution, MINIMUM_RESOLUTION, MAXIMUM_RESOLUTION);
            Vector2 size = _target.rectTransform.rect.size;
            float aspect = size.x > 0f && size.y > 0f ? size.x / size.y : SQUARE_ASPECT;
            int width = aspect >= SQUARE_ASPECT ? resolution : Mathf.Max(MINIMUM_RESOLUTION, Mathf.RoundToInt(resolution * aspect));
            int height = aspect >= SQUARE_ASPECT ? Mathf.Max(MINIMUM_RESOLUTION, Mathf.RoundToInt(resolution / aspect)) : resolution;
            return new Settings
            {
                Color = _color,
                Intensity = _intensity,
                HorizontalRadius = _horizontalRadius,
                VerticalRadius = _verticalRadius,
                ClearRadius = _clearRadius,
                Softness = Mathf.Max(MINIMUM_SOFTNESS, _softness),
                Width = width,
                Height = height
            };
        }

        private void ReleaseGenerated()
        {
            if (_target && _target.sprite == _sprite)
            {
                _target.sprite = null;
            }
            DestroyGenerated(_sprite);
            _sprite = null;
            DestroyGenerated(_texture);
            _texture = null;
        }

        private static void DestroyGenerated(Object generated)
        {
            if (!generated)
            {
                return;
            }
            if (Application.isPlaying)
            {
                Destroy(generated);
            }
            else
            {
                DestroyImmediate(generated);
            }
        }

        #endregion

#if UNITY_EDITOR
        #region Editor Wiring

        private void OnValidate()
        {
            if (!_target)
            {
                _target = GetComponent<Image>();
            }
            if (isActiveAndEnabled)
            {
                Rebuild();
            }
        }

        #endregion
#endif
    }
}
