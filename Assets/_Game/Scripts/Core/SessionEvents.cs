namespace Vertigo.WheelOfFortune.Core
{
    public readonly struct SessionChanged { }
    public readonly struct RunRestarted { }

    public readonly struct RewardCollected
    {
        public RewardGrant Grant { get; }

        public RewardCollected(RewardGrant grant)
        {
            Grant = grant;
        }
    }

    public readonly struct BombHit
    {
        public int Zone { get; }

        public BombHit(int zone)
        {
            Zone = zone;
        }
    }

    public readonly struct ZoneAdvanced
    {
        public int Zone { get; }

        public ZoneAdvanced(int zone)
        {
            Zone = zone;
        }
    }

    public readonly struct RunCompleted
    {
        public long BankedTotal { get; }

        public RunCompleted(long bankedTotal)
        {
            BankedTotal = bankedTotal;
        }
    }
}
