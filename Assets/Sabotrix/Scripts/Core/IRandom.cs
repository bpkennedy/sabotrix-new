namespace Sabotrix.Core
{
    // All match randomness (shuffle, initiative ties, sabotage reversal) goes through this,
    // so tests can use a fixed seed and get the same result every run.
    public interface IRandom
    {
        // Returns 0 to maxExclusive - 1.
        int Next(int maxExclusive);

        // Returns 0.0 (inclusive) to 1.0 (exclusive).
        double NextDouble();
    }
}
