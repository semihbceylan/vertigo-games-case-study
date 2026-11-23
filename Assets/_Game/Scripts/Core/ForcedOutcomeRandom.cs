using System;

namespace Vertigo.WheelOfFortune.Core
{
    /// <summary>
    /// Debug decorator for the spin random source: the next draw can be forced to a chosen slot,
    /// after which draws come from the wrapped source again. A forced value outside the requested
    /// range is discarded rather than returned, so it can never select a slot the wheel lacks.
    /// </summary>
    public sealed class ForcedOutcomeRandom : IRandomSource
    {
        #region State

        private const int NOT_FORCED = -1;

        private readonly IRandomSource _inner;
        private int _forced = NOT_FORCED;

        public ForcedOutcomeRandom(IRandomSource inner)
        {
            _inner = inner ?? throw new ArgumentNullException(nameof(inner));
        }

        public bool HasForcedOutcome
        {
            get { return _forced != NOT_FORCED; }
        }

        public int ForcedOutcome
        {
            get { return _forced; }
        }

        #endregion

        #region Forcing

        public void Force(int value)
        {
            if (value < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _forced = value;
        }

        public void ClearForced()
        {
            _forced = NOT_FORCED;
        }

        #endregion

        #region IRandomSource

        public int NextInt(int minInclusive, int maxExclusive)
        {
            int forced = _forced;
            _forced = NOT_FORCED;
            if (forced != NOT_FORCED && forced >= minInclusive && forced < maxExclusive)
            {
                return forced;
            }
            return _inner.NextInt(minInclusive, maxExclusive);
        }

        public double NextDouble()
        {
            return _inner.NextDouble();
        }

        #endregion
    }
}
