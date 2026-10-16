using ReusabilityLink.Core;
using Xunit;

namespace ReusabilityLink.Core.Tests
{
    public class FlagGuardTests
    {
        [Fact]
        public void TwoGuards_InHarmonyOrder_LeaveFlagAsFound()
        {
            // Harmony runs finalizers in the same order as prefixes, not reversed.
            bool flag = false;
            bool a = FlagGuard.Enter(ref flag, protect: true);
            bool b = FlagGuard.Enter(ref flag, protect: true);
            Assert.True(flag);
            FlagGuard.Exit(ref flag, a);
            FlagGuard.Exit(ref flag, b);
            Assert.False(flag);
        }

        [Fact]
        public void FlagAlreadySetByKsp_IsLeftSet()
        {
            bool flag = true;
            bool flipped = FlagGuard.Enter(ref flag, protect: true);
            FlagGuard.Exit(ref flag, flipped);
            Assert.True(flag);
        }

        [Fact]
        public void NotProtected_DoesNotTouchFlag()
        {
            bool flag = false;
            bool flipped = FlagGuard.Enter(ref flag, protect: false);
            Assert.False(flag);
            Assert.False(flipped);
        }
    }
}
