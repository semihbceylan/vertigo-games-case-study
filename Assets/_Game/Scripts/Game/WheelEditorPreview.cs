using UnityEngine;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.Wheel;

namespace Vertigo.WheelOfFortune.Game
{
    /// <summary>
    /// Edit-mode only: renders the starting zone's wheel so the scene looks like a fresh run.
    /// Uses the configured seed so the preview is stable; it does nothing at runtime.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class WheelEditorPreview : MonoBehaviour
    {
        #region Serialized References

        [SerializeField] private GameConfiguration _configuration;
        [SerializeField] private WheelView _wheelView;

        #endregion

#if UNITY_EDITOR
        #region Editor Preview

        private bool _isPreviewQueued;

        private void OnValidate()
        {
            if (!_wheelView) _wheelView = GetComponentInChildren<WheelView>(true);
            if (Application.isPlaying || !_configuration || !_configuration.Zones || !_wheelView || _isPreviewQueued)
            {
                return;
            }
            // Destroying and recreating slots inside OnValidate is not allowed, so defer it.
            _isPreviewQueued = true;
            UnityEditor.EditorApplication.delayCall += RunPreview;
        }

        private void RunPreview()
        {
            _isPreviewQueued = false;
            if (!this || Application.isPlaying)
            {
                return;
            }
            ZoneConfiguration zones = _configuration.Zones;
            var catalog = new ZoneCatalog(zones);
            int zone = catalog.StartingZone;
            ZoneKind kind = catalog.Rules.Resolve(zone);
            var rewards = new RewardCatalogPresenter();
            rewards.Configure(zones);
            ZoneWheel wheel = new WheelTierFactory(zones).CreateWheel(zone, kind, new SystemRandomSource(_configuration.RandomSeed));
            WheelDefinition definition = zones.GetWheel(kind);
            _wheelView.SetIndicator(definition.IndicatorSprite);
            new WheelVisualPresenter(rewards).Fill(wheel, definition, _wheelView.Disc);
        }

        #endregion
#endif
    }
}
