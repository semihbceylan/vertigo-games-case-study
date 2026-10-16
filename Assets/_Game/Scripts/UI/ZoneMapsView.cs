using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>
    /// Right-hand stacked zone info maps (Super Zone on top, Safe Zone below).
    /// Shows the closest upcoming gate strictly after the current zone. The safe gate skips super
    /// zones (a super zone is bomb-free but is not a Safe zone by kind).
    /// Visuals come only from the assigned panel sprites; no tinting.
    /// Each map carries a ui_card_zone_map_frame overlay frame on top of its background.
    /// Title / number layout is owned by the scene; this script only writes text.
    /// </summary>
    [DisallowMultipleComponent]
    public class ZoneMapsView : MonoBehaviour
    {
        [FormerlySerializedAs("superBadge")]
        [SerializeField] private Image superMap;
        [SerializeField] private TMP_Text superLabel;
        [SerializeField] private TMP_Text superValue;
        [FormerlySerializedAs("safeBadge")]
        [SerializeField] private Image safeMap;
        [SerializeField] private TMP_Text safeLabel;
        [SerializeField] private TMP_Text safeValue;
        [Tooltip("Frame overlay drawn over each zone map (ui_card_zone_map_frame).")]
        [SerializeField] private Sprite mapFrame;
        [Tooltip("How far the frame bleeds outside the map edges (px each side).")]
        [SerializeField, Min(0f)] private float frameBleed = 6f;

        private void Awake()
        {
            MigrateNames();
            ResolveReferences();
        }

        private void Start()
        {
            // Frames reorder the map's children. That must not happen during Awake/OnValidate,
            // where Unity reports "SendMessage cannot be called during ..." for
            // OnTransformChildrenChanged, so it is deferred to Start at runtime.
            EnsureFrames();
        }

        public void Set(int currentZone, int safeInterval, int superInterval, int finalZone = 0)
        {
            MigrateNames();
            ResolveReferences();
            EnsureFrames();

            safeInterval = Mathf.Max(1, safeInterval);
            superInterval = Mathf.Max(1, superInterval);

            int nextSuper = ((currentZone / superInterval) + 1) * superInterval;

            // Super zones are bomb-free, so they are safe in practice, but they are not Safe zones
            // by kind — ZoneClassifier gives Super precedence. The safe map must therefore skip any
            // gate that is a super zone and show the next real safe gate (e.g. 29 -> 35, not 30).
            int nextSafe = ((currentZone / safeInterval) + 1) * safeInterval;
            while (nextSafe % superInterval == 0)
                nextSafe += safeInterval;

            // The run ends at finalZone (0 = endless). Once a map's next gate would land past the
            // final zone there is nothing left to advertise, so the whole map is hidden.
            bool showSuper = finalZone <= 0 || nextSuper <= finalZone;
            bool showSafe = finalZone <= 0 || nextSafe <= finalZone;

            if (superMap != null) superMap.gameObject.SetActive(showSuper);
            if (safeMap != null) safeMap.gameObject.SetActive(showSafe);

            // Left side: single-line title. Number lives in its own label on the right.
            if (showSuper)
            {
                if (superLabel != null) superLabel.text = "SUPER ZONE";
                if (superValue != null) superValue.text = nextSuper.ToString();
                else if (superLabel != null) superLabel.text = "SUPER ZONE " + nextSuper;
            }

            if (showSafe)
            {
                if (safeLabel != null) safeLabel.text = "SAFE ZONE";
                if (safeValue != null) safeValue.text = nextSafe.ToString();
                else if (safeLabel != null) safeLabel.text = "SAFE ZONE " + nextSafe;
            }
        }

        /// <summary>Renames any lingering "badge" objects to "map" so the hierarchy stays consistent.</summary>
        private void MigrateNames()
        {
            if (gameObject.name == "ui_game_zone_badges")
                gameObject.name = "ui_game_zone_maps";

            RenameChild("ui_image_super_zone_badge", "ui_image_super_zone_map");
            RenameChild("ui_image_safe_zone_badge", "ui_image_safe_zone_map");
        }

        private void RenameChild(string oldName, string newName)
        {
            var oldChild = transform.Find(oldName);
            if (oldChild != null && transform.Find(newName) == null)
                oldChild.name = newName;
        }

        private void ResolveReferences()
        {
            if (superMap == null)
                superMap = FindMap("ui_image_super_zone_map", "ui_image_super_zone_badge");
            if (safeMap == null)
                safeMap = FindMap("ui_image_safe_zone_map", "ui_image_safe_zone_badge");

            if (superMap != null)
            {
                if (superLabel == null)
                    superLabel = FindText(superMap.transform, "ui_text_super_zone_value");
                if (superValue == null)
                    superValue = FindText(superMap.transform, "ui_text_super_zone_number_value", exclude: superLabel);
                if (superValue == null && superLabel != null)
                    superValue = CreateValueLabel(superMap.transform, superLabel, "super");
            }

            if (safeMap != null)
            {
                if (safeLabel == null)
                    safeLabel = FindText(safeMap.transform, "ui_text_safe_zone_value");
                if (safeValue == null)
                    safeValue = FindText(safeMap.transform, "ui_text_safe_zone_number_value", exclude: safeLabel);
                if (safeValue == null && safeLabel != null)
                    safeValue = CreateValueLabel(safeMap.transform, safeLabel, "safe");
            }
        }

        private Image FindMap(params string[] names)
        {
            for (int i = 0; i < names.Length; i++)
            {
                var t = transform.Find(names[i]);
                if (t != null)
                {
                    var image = t.GetComponent<Image>();
                    if (image != null) return image;
                }
            }
            return null;
        }

        private static TMP_Text FindText(Transform root, string name, TMP_Text exclude = null)
        {
            var named = root.Find(name);
            if (named != null)
            {
                var text = named.GetComponent<TMP_Text>();
                if (text != null && text != exclude) return text;
            }

            var texts = root.GetComponentsInChildren<TMP_Text>(true);
            for (int i = 0; i < texts.Length; i++)
            {
                if (texts[i] != null && texts[i] != exclude) return texts[i];
            }
            return null;
        }

        private static TMP_Text CreateValueLabel(Transform map, TMP_Text title, string prefix)
        {
            var go = new GameObject("ui_text_" + prefix + "_zone_number_value", typeof(RectTransform));
            go.layer = map.gameObject.layer;
            go.transform.SetParent(map, false);

            var label = go.AddComponent<TextMeshProUGUI>();
            if (title != null)
            {
                label.font = title.font;
                label.fontSize = title.fontSize;
                label.color = title.color;
            }
            else
            {
                label.fontSize = 38;
                label.color = Color.white;
            }

            // Sensible default for brand-new labels only. Existing scene layout is never touched.
            label.alignment = TextAlignmentOptions.MidlineRight;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Overflow;
            label.fontStyle = FontStyles.Bold;
            label.raycastTarget = false;

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0.62f, 0f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(1f, 0.5f);
            rect.offsetMin = new Vector2(0f, 12f);
            rect.offsetMax = new Vector2(-24f, -12f);

            return label;
        }

        private void EnsureFrames()
        {
            if (superMap != null) EnsureMapFrame(superMap.transform);
            if (safeMap != null) EnsureMapFrame(safeMap.transform);
        }

        /// <summary>
        /// Ensures the ui_card_zone_map_frame overlay exists over the map background.
        /// The frame sits behind the texts but above the panel background.
        /// </summary>
        private void EnsureMapFrame(Transform map)
        {
            if (map == null) return;

            var frameTransform = map.Find("ui_image_zone_map_frame");
            Image frame;
            if (frameTransform != null)
            {
                frame = frameTransform.GetComponent<Image>();
                if (frame == null) frame = frameTransform.gameObject.AddComponent<Image>();
            }
            else
            {
                var go = new GameObject("ui_image_zone_map_frame", typeof(RectTransform), typeof(Image));
                go.layer = map.gameObject.layer;
                go.transform.SetParent(map, false);
                frame = go.GetComponent<Image>();
            }

            if (mapFrame != null)
                frame.sprite = mapFrame;

            frame.type = Image.Type.Sliced;
            frame.fillCenter = true;
            frame.preserveAspect = false;
            frame.color = Color.white;
            frame.raycastTarget = false;
            frame.maskable = false;

            var rect = (RectTransform)frame.transform;
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.offsetMin = new Vector2(-frameBleed, -frameBleed);
            rect.offsetMax = new Vector2(frameBleed, frameBleed);
            rect.localScale = Vector3.one;

            // Above the panel background, below the texts.
            rect.SetSiblingIndex(0);
            for (int i = 0; i < map.childCount; i++)
            {
                var child = map.GetChild(i);
                if (child != rect.transform && child.GetComponent<TMP_Text>() != null)
                {
                    child.SetAsLastSibling();
                }
            }
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            MigrateNames();
            ResolveReferences();

            if (mapFrame == null)
            {
                mapFrame = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/Art/ui_card_zone_map_frame.png");
                if (mapFrame != null)
                    UnityEditor.EditorUtility.SetDirty(this);
            }

            // Frames are structural (safe to build in edit mode); text layout is the scene's own.
            // Deferred out of OnValidate so reordering children does not trigger the
            // "SendMessage cannot be called during Awake/OnValidate" warning.
            if (!Application.isPlaying)
                ScheduleEnsureFrames();
        }

        private bool _ensureFramesQueued;

        private void ScheduleEnsureFrames()
        {
            if (_ensureFramesQueued) return;

            _ensureFramesQueued = true;
            UnityEditor.EditorApplication.delayCall += RunQueuedEnsureFrames;
        }

        private void RunQueuedEnsureFrames()
        {
            _ensureFramesQueued = false;
            if (this == null) return;

            EnsureFrames();
            UnityEditor.EditorUtility.SetDirty(this);
        }
#endif
    }
}
