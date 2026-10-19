using System;
using System.Collections.Generic;

namespace ReusabilityLink.Core
{
    /// <summary>
    /// KSP has a single global throttle. Without this, the vessel you switch to inherits the throttle
    /// of the one you leave. Each vessel keeps its own throttle instead: the one you leave keeps
    /// burning at the throttle it had, the one you arrive at gets back the throttle it was left with.
    /// </summary>
    public sealed class ThrottleMemory
    {
        readonly Dictionary<Guid, float> held = new Dictionary<Guid, float>();

        /// <summary>The player leaves vessel <paramref name="id"/> with this throttle.</summary>
        public void Leaving(Guid id, float throttle)
        {
            held[id] = float.IsNaN(throttle) ? 0f : Math.Max(0f, Math.Min(1f, throttle));
        }

        /// <summary>The player takes control of vessel <paramref name="id"/>; returns its own throttle.</summary>
        public float Arriving(Guid id)
        {
            float throttle = Held(id);
            held.Remove(id);
            return throttle;
        }

        /// <summary>Throttle an inactive vessel must keep; 0 when it was not left burning.</summary>
        public float Held(Guid id) => held.TryGetValue(id, out var throttle) ? throttle : 0f;

        public void Forget(Guid id) => held.Remove(id);
    }
}
