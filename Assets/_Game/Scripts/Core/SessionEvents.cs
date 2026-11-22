namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>The session entered a new state; projections re-read its capabilities.</summary>
    public readonly struct SessionChanged
    {
    }

    /// <summary>The run ended successfully and its rewards were banked.</summary>
    public readonly struct RunCompleted
    {
        public long BankedTotal { get; }

        public RunCompleted(long bankedTotal)
        {
            BankedTotal = bankedTotal;
        }
    }
}
