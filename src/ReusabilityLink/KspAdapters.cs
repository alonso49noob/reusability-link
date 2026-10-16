using ReusabilityLink.Core;

namespace ReusabilityLink
{
    /// <summary>Small translations from KSP objects to the plain data Core works with.</summary>
    internal static class KspAdapters
    {
        public static FlightSituation ToSituation(Vessel.Situations s)
        {
            switch (s)
            {
                case Vessel.Situations.PRELAUNCH: return FlightSituation.Prelaunch;
                case Vessel.Situations.LANDED: return FlightSituation.Landed;
                case Vessel.Situations.SPLASHED: return FlightSituation.Splashed;
                case Vessel.Situations.FLYING: return FlightSituation.Flying;
                case Vessel.Situations.SUB_ORBITAL: return FlightSituation.SubOrbital;
                case Vessel.Situations.ORBITING: return FlightSituation.Orbiting;
                case Vessel.Situations.ESCAPING: return FlightSituation.Escaping;
                default: return FlightSituation.Docked;
            }
        }

        public static bool HasCommand(Vessel v)
        {
            foreach (var part in v.parts)
                if (part.FindModuleImplementing<ModuleCommand>() != null) return true;
            return false;
        }

        public static bool HasEngine(Vessel v)
        {
            foreach (var part in v.parts)
                if (part.FindModuleImplementing<ModuleEngines>() != null) return true;
            return false;
        }

        public static bool EngineThrusting(Vessel v)
        {
            if (!v.loaded) return false;
            foreach (var part in v.parts)
                foreach (var engine in part.FindModulesImplementing<ModuleEngines>())
                    if (engine.finalThrust > 0f) return true;
            return false;
        }

        public static LinkVesselModule Module(Vessel v) =>
            v == null ? null : v.FindVesselModuleImplementing<LinkVesselModule>();
    }
}
