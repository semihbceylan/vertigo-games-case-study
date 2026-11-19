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
        void ShowLoss(int zone);
        void ShowResults();
        void Reset();
        void Cancel();
    }
}
