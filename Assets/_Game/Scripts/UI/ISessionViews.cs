using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using Vertigo.WheelOfFortune.Core;
using Vertigo.WheelOfFortune.Data;

namespace Vertigo.WheelOfFortune.UI
{
    #region Input Sources

    /// <summary>Local click notification raised by a button wrapper.</summary>
    public interface IClickSource
    {
        event UnityAction Clicked;
    }

    public interface IButtonInteractability
    {
        bool IsInteractable { get; set; }
    }

    /// <summary>Loss panel commands requested by the player.</summary>
    public interface ILossPanelInput
    {
        event Action RestartRequested;
        event Action ReviveRequested;
        void ShowInsufficientFunds();
    }

    public interface IResultsInput
    {
        event Action RestartRequested;
    }

    #endregion

    #region Session Projection

    /// <summary>HUD commands used by session projection, without exposing child components.</summary>
    public interface ISessionHudView
    {
        void SetZone(int zone, ZoneCatalog zones);
        void SetZoneMaps(int zone, ZoneCatalog zones);
        void SetRewards(IReadOnlyList<RewardRowData> rows);
        void SetLeaveAvailable(bool isAvailable);
        void SetLeaveVisible(bool isVisible);
        void SetRestartAvailable(bool isAvailable);
    }

    /// <summary>Presentation commands; the view owns its serialized widget references.</summary>
    public interface ISessionScreenView : ISessionHudView
    {
        void ShowBomb(Sprite bombIcon);
        void SetReviveOffer(long cost, int remaining, IReadOnlyList<RewardRowData> rows);
        void ShowResults(IReadOnlyList<RewardRowData> rows);
        Sequence PlayZoneTransition(int zone, ZoneCatalog zones);
        void HideFeedback();
        void ResetPanels();
        void HideJackpot();
    }

    /// <summary>Persistent balance HUD shown with the loss panel.</summary>
    public interface ICurrencyHudView
    {
        void Bind(ICurrencyWallet wallet, IEventBus events);
        void SetDeathScreenVisible(bool isVisible);
    }

    #endregion

    #region Reward Presentation

    public interface IRewardShowcase
    {
        event Action Dismissed;
        void Show(Sprite icon, string label, long amount);
        void HideImmediate();
    }

    /// <summary>Destinations for flown reward icons.</summary>
    public interface IRewardRowTargets
    {
        RectTransform ListRect { get; }
        bool TryGetRowIcon(string id, out RectTransform icon);
        void PunchRow(string id);
    }

    #endregion
}
