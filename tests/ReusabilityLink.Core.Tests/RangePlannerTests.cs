using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class RangePlannerTests
    {
        [Fact]
        public void ForMode_Physics_UsesHugePackRanges()
        {
            var r = RangePlanner.ForMode(LinkMode.Physics, new LinkSettings());
            Assert.Equal((2_500_000f, 2_600_000f, 2_550_000f, 2_400_000f), (r.Load, r.Unload, r.Pack, r.Unpack));
        }

        [Theory]
        [InlineData(LinkMode.Rails)]
        [InlineData(LinkMode.OutOfRange)]
        public void ForMode_RailsLike_UsesMinDistance(LinkMode mode)
        {
            var r = RangePlanner.ForMode(mode, new LinkSettings());
            Assert.Equal((2_500_000f, 2_600_000f, 2_500f, 2_000f), (r.Load, r.Unload, r.Pack, r.Unpack));
        }

        [Fact]
        public void ForMode_Rails_FollowsRailsMinDistanceSetting()
        {
            var r = RangePlanner.ForMode(LinkMode.Rails, new LinkSettings { RailsMinDistance = 5000 });
            Assert.Equal((5000f, 4500f), (r.Pack, r.Unpack));
        }

        [Theory]
        [InlineData(LinkMode.Physics)]
        [InlineData(LinkMode.Rails)]
        [InlineData(LinkMode.OutOfRange)]
        public void ForMode_Invariants(LinkMode mode)
        {
            var r = RangePlanner.ForMode(mode, new LinkSettings());
            Assert.True(r.Unpack < r.Pack);
            Assert.True(r.Load < r.Unload);
        }
    }
}
