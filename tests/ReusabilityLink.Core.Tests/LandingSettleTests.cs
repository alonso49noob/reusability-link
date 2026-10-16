using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class LandingSettleTests
    {
        static readonly LinkSettings S = new LinkSettings();

        [Fact]
        public void Update_LandedStillFor10s_ReturnsTrue()
        {
            var settle = new LandingSettle();
            Assert.False(settle.Update(true, 0.1, 9, S));
            Assert.True(settle.Update(true, 0.1, 1.5, S));
        }

        [Fact]
        public void Update_MovingResetsTimer()
        {
            var settle = new LandingSettle();
            settle.Update(true, 0.1, 8, S);
            settle.Update(true, 2, 1, S);
            Assert.False(settle.Update(true, 0.1, 8, S));
        }

        [Fact]
        public void Update_LeavingGroundResetsTimer()
        {
            var settle = new LandingSettle();
            settle.Update(true, 0.1, 8, S);
            settle.Update(false, 0.1, 0.1, S);
            Assert.False(settle.Update(true, 0.1, 8, S));
        }

        [Fact]
        public void Update_NotLanded_NeverTrue()
        {
            var settle = new LandingSettle();
            Assert.False(settle.Update(false, 0, 100, S));
        }

        [Fact]
        public void Reset_ClearsTimer()
        {
            var settle = new LandingSettle();
            settle.Update(true, 0.1, 9, S);
            settle.Reset();
            Assert.False(settle.Update(true, 0.1, 1.5, S));
        }
    }
}
