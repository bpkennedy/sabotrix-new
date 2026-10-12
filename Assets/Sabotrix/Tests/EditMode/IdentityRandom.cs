using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    // Always returns the highest value. The Fisher-Yates shuffle then swaps each card with itself,
    // so decks keep their listed order and tests know exactly which cards are in each hand.
    // Initiative ties pick player two.
    public sealed class IdentityRandom : IRandom
    {
        private readonly double _nextDouble;

        public IdentityRandom(double nextDouble = 0.99)
        {
            _nextDouble = nextDouble;
        }

        public int Next(int maxExclusive) => maxExclusive - 1;

        public double NextDouble() => _nextDouble;
    }
}
