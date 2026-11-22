using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    /// <summary>HUD commands used by session projection, without exposing child components.</summary>
    public interface ISessionHudView
    {
        void SetZone(int zone, ZoneConfiguration configuration);
        void SetZoneMaps(int zone, ZoneConfiguration configuration);
        void SetRewards(IReadOnlyList<RewardRowData> rows);
        void SetLeaveAvailable(bool available);
        void SetLeaveVisible(bool visible);
        void SetRestartAvailable(bool available);
    }

    /// <summary>Presentation commands; the view owns its serialized widget references.</summary>
    public interface ISessionScreenView : ISessionHudView
    {
        void ShowBomb(Sprite bombIcon);
        void SetReviveOffer(long cost, int remaining, IReadOnlyList<RewardRowData> rows);
        void ShowResults(IReadOnlyList<RewardRowData> rows);
        Sequence PlayZoneTransition(int zone, ZoneConfiguration configuration, float duration, Ease ease);
        void HideFeedback();
        void ResetPanels();
        void HideJackpot();
    }

    public interface IButtonInteractability
    {
        bool Interactable { get; set; }
    }
}
