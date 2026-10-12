using System.Linq;
using NUnit.Framework;
using Sabotrix.Core;

namespace Sabotrix.Tests.EditMode
{
    public class SeededRandomTests
    {
        private const int Seed = 42;
        private const int SampleCount = 100;

        [Test]
        public void SeededRandom_SameSeed_ProducesSameSequence()
        {
            SeededRandom first = new SeededRandom(Seed);
            SeededRandom second = new SeededRandom(Seed);

            int[] firstSequence = Enumerable.Range(0, SampleCount).Select(_ => first.Next(1000)).ToArray();
            int[] secondSequence = Enumerable.Range(0, SampleCount).Select(_ => second.Next(1000)).ToArray();

            Assert.That(firstSequence, Is.EqualTo(secondSequence));
        }

        [Test]
        public void Next_ManyRolls_StaysInRange()
        {
            SeededRandom random = new SeededRandom(Seed);

            for (int i = 0; i < SampleCount; i++)
            {
                Assert.That(random.Next(5), Is.InRange(0, 4));
            }
        }

        [Test]
        public void NextDouble_ManyRolls_StaysBetweenZeroAndOne()
        {
            SeededRandom random = new SeededRandom(Seed);

            for (int i = 0; i < SampleCount; i++)
            {
                double roll = random.NextDouble();
                Assert.That(roll, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
            }
        }
    }
}
