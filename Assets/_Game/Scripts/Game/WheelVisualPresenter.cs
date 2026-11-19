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
        void Fill(ZoneWheel wheel, WheelDefinition definition, RectTransform rotation, Image baseImage, WheelLayout layout);
    }

    /// <summary>Shared live, incoming-disc and editor-preview wheel projection.</summary>
    public sealed class WheelVisualPresenter : IWheelVisualPresenter
    {
        private readonly RewardCatalogPresenter _catalog;

        public WheelVisualPresenter(RewardCatalogPresenter catalog)
        {
            _catalog = catalog;
        }

        public void Fill(ZoneWheel wheel, WheelDefinition definition, RectTransform rotation, Image baseImage, WheelLayout layout)
        {
            if (wheel == null || !layout) return;
            if (baseImage && definition && definition.BaseSprite) baseImage.sprite = definition.BaseSprite;
            layout.SetSliceCount(wheel.SliceCount);
            for (int i = 0; i < wheel.SliceCount; i++)
            {
                Image icon = layout.GetIcon(i);
                var amount = layout.GetAmountText(i);
                if (!icon) continue;
                WheelSlice slice = wheel[i];
                if (slice.IsBomb)
                {
                    icon.sprite = definition ? definition.BombSprite : null;
                    layout.SetSlotIconScale(i, layout.BombIconScale);
                    if (amount) amount.gameObject.SetActive(false);
                }
                else
                {
                    RewardDefinition reward = _catalog.Resolve(slice.Reward.RewardId);
                    icon.sprite = reward ? reward.Icon : null;
                    layout.SetSlotIconScale(i, layout.StandardIconScale * (reward ? reward.IconScaleMultiplier : 1f));
                    if (amount)
                    {
                        amount.gameObject.SetActive(true);
                        amount.text = RewardAmountFormatter.Format(slice.Reward.Amount);
                    }
                }
                icon.color = Color.white;
            }
            if (rotation) rotation.localRotation = Quaternion.identity;
            layout.ApplyLayout();
        }
    }
}
