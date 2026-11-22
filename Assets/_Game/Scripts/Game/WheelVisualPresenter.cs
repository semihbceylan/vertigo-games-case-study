using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;
using Vertigo.WheelOfFortune.UI;
using Vertigo.WheelOfFortune.Wheel;

namespace Vertigo.WheelOfFortune.Game
{
    public interface IWheelVisualPresenter
    {
        void Fill(ZoneWheel wheel, WheelDefinition definition, IWheelDisc disc);
    }

    /// <summary>Shared live, incoming-disc and editor-preview wheel projection.</summary>
    public sealed class WheelVisualPresenter : IWheelVisualPresenter
    {
        #region Projection

        private readonly RewardCatalogPresenter _catalog;

        public WheelVisualPresenter(RewardCatalogPresenter catalog)
        {
            _catalog = catalog;
        }

        public void Fill(ZoneWheel wheel, WheelDefinition definition, IWheelDisc disc)
        {
            WheelLayout layout = disc.Layout;
            disc.Base.sprite = definition.BaseSprite;
            layout.SetSliceCount(wheel.SliceCount);
            for (int i = 0; i < wheel.SliceCount; i++)
            {
                Image icon = layout.GetIcon(i);
                TMP_Text amount = layout.GetAmountText(i);
                WheelSlice slice = wheel[i];
                if (slice.IsBomb)
                {
                    icon.sprite = definition.BombSprite;
                    layout.SetSlotIconScale(i, definition.BombIconScale);
                    amount.gameObject.SetActive(false);
                    continue;
                }
                RewardDefinition reward = _catalog.ResolveRequired(slice.Reward.RewardId);
                icon.sprite = reward.Icon;
                layout.SetSlotIconScale(i, layout.RewardIconScale * reward.IconScaleMultiplier);
                amount.gameObject.SetActive(true);
                amount.text = RewardAmountFormatter.Format(slice.Reward.Amount);
            }
            disc.Rect.localRotation = Quaternion.identity;
            layout.ApplyLayout();
        }

        #endregion
    }
}
