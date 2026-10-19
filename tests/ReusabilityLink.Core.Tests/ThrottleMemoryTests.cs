using System;
using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class ThrottleMemoryTests
    {
        static readonly Guid Upper = Guid.NewGuid();
        static readonly Guid Booster = Guid.NewGuid();

        [Fact]
        public void Arriving_NeverFlownVessel_GetsZeroThrottle()
        {
            // The bug: the booster inherited the upper stage's throttle and lit its engines.
            var memory = new ThrottleMemory();
            memory.Leaving(Upper, 1f);
            Assert.Equal(0f, memory.Arriving(Booster));
        }

        [Fact]
        public void Arriving_GetsTheThrottleItWasLeftWith()
        {
            var memory = new ThrottleMemory();
            memory.Leaving(Upper, 0.7f);
            memory.Leaving(Booster, 0f);
            Assert.Equal(0.7f, memory.Arriving(Upper));
        }

        [Fact]
        public void LeftVessel_KeepsBurning()
        {
            var memory = new ThrottleMemory();
            memory.Leaving(Upper, 0.7f);
            Assert.Equal(0.7f, memory.Held(Upper));
        }

        [Fact]
        public void Arriving_StopsHoldingThatVessel()
        {
            // Once active, the player's throttle controls it, not the memory.
            var memory = new ThrottleMemory();
            memory.Leaving(Upper, 0.7f);
            memory.Arriving(Upper);
            Assert.Equal(0f, memory.Held(Upper));
        }

        [Fact]
        public void Forget_DropsTheVessel()
        {
            var memory = new ThrottleMemory();
            memory.Leaving(Upper, 0.7f);
            memory.Forget(Upper);
            Assert.Equal(0f, memory.Held(Upper));
        }

        [Theory]
        [InlineData(1.5f, 1f)]
        [InlineData(-0.2f, 0f)]
        [InlineData(float.NaN, 0f)]
        public void Leaving_ClampsToValidThrottle(float input, float expected)
        {
            var memory = new ThrottleMemory();
            memory.Leaving(Upper, input);
            Assert.Equal(expected, memory.Held(Upper));
        }
    }
}
