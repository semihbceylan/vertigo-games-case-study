using UnityEngine;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Full-screen elliptical vignette. Darkens the edges of its overlay, reaching further in from
    /// the left and right than from the top and bottom. Optional fractal noise roughens the edge
    /// and mottles the fade so it reads as a texture rather than a clean gradient. The gradient texture is generated when the
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
        /// <summary>Each fractal noise octave doubles frequency and halves amplitude.</summary>
        private const float NOISE_LACUNARITY = 2f;
        private const float NOISE_GAIN = 0.5f;
        /// <summary>Perlin noise is centred on this value; offsets are measured from it.</summary>
        private const float NOISE_MIDPOINT = 0.5f;
        private const float NOISE_TO_SIGNED = 2f;
        /// <summary>Spreads seeds far apart in noise space so neighbouring seeds look unrelated.</summary>
        private const float SEED_SPACING = 37.17f;
        private const int MINIMUM_OCTAVES = 1;
        private const int MAXIMUM_OCTAVES = 6;

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

        [Header("Texture")]
        [Tooltip("How far noise pushes the fade boundary in and out, so the edge is irregular instead of a clean ellipse.")]
        [SerializeField, Range(0f, 1f)] private float _edgeBreakup;
        [Tooltip("How strongly noise mottles the darkness inside the fade. 0 = smooth.")]
        [SerializeField, Range(0f, 1f)] private float _grain;
        [Tooltip("Noise features across the overlay's shorter side; larger values give smaller blotches.")]
        [SerializeField, Min(MINIMUM_SOFTNESS)] private float _noiseScale = 3f;
        [SerializeField, Range(MINIMUM_OCTAVES, MAXIMUM_OCTAVES)] private int _noiseOctaves = 4;
        [SerializeField] private int _noiseSeed;

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
            public float EdgeBreakup;
            public float Grain;
            public float NoiseScale;
            public int NoiseOctaves;
            public int NoiseSeed;
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
            bool hasNoise = settings.EdgeBreakup > 0f || settings.Grain > 0f;
            // Square noise cells: scale by the shorter side so blotches are not stretched.
            float cellsPerTexel = settings.NoiseScale / Mathf.Min(width, height);
            float seedOffset = settings.NoiseSeed * SEED_SPACING;
            for (int y = 0; y < height; y++)
            {
                float v = Mathf.Lerp(NORMALIZED_MIN, NORMALIZED_MAX, (y + TEXEL_CENTRE) / height) / settings.VerticalRadius;
                for (int x = 0; x < width; x++)
                {
                    float u = Mathf.Lerp(NORMALIZED_MIN, NORMALIZED_MAX, (x + TEXEL_CENTRE) / width) / settings.HorizontalRadius;
                    float distance = Mathf.Sqrt(u * u + v * v);
                    float noise = NOISE_MIDPOINT;
                    if (hasNoise)
                    {
                        noise = FractalNoise(x * cellsPerTexel + seedOffset, y * cellsPerTexel + seedOffset, settings.NoiseOctaves);
                        distance += (noise - NOISE_MIDPOINT) * NOISE_TO_SIGNED * settings.EdgeBreakup;
                    }
                    float darkness = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(inner, outer, distance));
                    darkness *= Mathf.Lerp(1f, noise / NOISE_MIDPOINT, settings.Grain);
                    darkness = Mathf.Clamp01(darkness);
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

        /// <summary>Summed Perlin octaves normalised back to [0, 1].</summary>
        private static float FractalNoise(float x, float y, int octaves)
        {
            float sum = 0f;
            float amplitude = 1f;
            float frequency = 1f;
            float total = 0f;
            for (int i = 0; i < octaves; i++)
            {
                sum += Mathf.PerlinNoise(x * frequency, y * frequency) * amplitude;
                total += amplitude;
                amplitude *= NOISE_GAIN;
                frequency *= NOISE_LACUNARITY;
            }
            return sum / total;
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
                EdgeBreakup = _edgeBreakup,
                Grain = _grain,
                NoiseScale = Mathf.Max(MINIMUM_SOFTNESS, _noiseScale),
                NoiseOctaves = Mathf.Clamp(_noiseOctaves, MINIMUM_OCTAVES, MAXIMUM_OCTAVES),
                NoiseSeed = _noiseSeed,
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
