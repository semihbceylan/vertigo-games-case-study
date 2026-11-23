using System;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>
    /// Presentation operations complete through local callbacks, not broadcast events.
    /// Cancel must discard callbacks without advancing the session.
    /// </summary>
    public interface ISessionPresentation
    {
        void PlaySpin(int sliceIndex, ZoneWheel wheel, Action onComplete);
        void PresentReward(RewardGrant grant, int sliceIndex, Action onComplete);
        void PlayZoneTransition(Action onComplete);
        /// <summary>Shows the bomb hit on <paramref name="bombSliceIndex"/> and then the loss panel.</summary>
        void ShowLoss(int zone, int bombSliceIndex);
        void ShowResults();
        void Reset();
        void Cancel();
    }
}
