namespace ReusabilityLink.Core
{
    /// <summary>
    /// Detects that a tracked vessel has landed and come to rest (spec §3.1, end of tracking):
    /// LandedSettleSeconds in a row landed/splashed with speed below LandedSettleSpeed.
    /// </summary>
    public sealed class LandingSettle
    {
        double settledSeconds;

        public bool Update(bool landedOrSplashed, double surfaceSpeed, double dt, LinkSettings s)
        {
            if (!landedOrSplashed || surfaceSpeed >= s.LandedSettleSpeed)
            {
                settledSeconds = 0;
                return false;
            }
            settledSeconds += dt;
            return settledSeconds >= s.LandedSettleSeconds;
        }

        public void Reset() => settledSeconds = 0;
    }
}
