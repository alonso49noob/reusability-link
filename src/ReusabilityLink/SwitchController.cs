using System;
using ReusabilityLink.Core;
using UnityEngine;

namespace ReusabilityLink
{
    /// <summary>Hotkey and button switch to the other tracked vessel without a loading screen (spec §3.4).</summary>
    public sealed class SwitchController
    {
        readonly TrackingService tracking;
        readonly KeyChord chord;
        readonly KeyCode key = KeyCode.None;

        public SwitchController(LinkSettings settings, TrackingService tracking)
        {
            this.tracking = tracking;
            chord = settings.SwitchKey;
            try
            {
                key = (KeyCode)Enum.Parse(typeof(KeyCode), chord.Key, true);
            }
            catch (ArgumentException)
            {
                Log.Warn($"SwitchKey '{chord}': tecla desconocida, el atajo queda desactivado");
            }
        }

        public void Update()
        {
            if (key == KeyCode.None || !Input.GetKeyDown(key) || !ModifiersMatch()) return;
            // KSP locks VESSEL_SWITCHING while a text field has focus (it is part of KEYBOARDINPUT).
            if (GUIUtility.keyboardControl != 0 || InputLockManager.IsLocked(ControlTypes.VESSEL_SWITCHING) || FlightDriver.Pause)
                return;
            SwitchToOther();
        }

        public bool SwitchToOther()
        {
            var other = tracking.OtherTracked;
            if (other == null) return false;
            Log.Info($"cambio a {other.vesselName} (loaded={other.loaded}, packed={other.packed})");
            return FlightGlobals.ForceSetActiveVessel(other);
        }

        bool ModifiersMatch()
        {
            bool alt = Input.GetKey(KeyCode.LeftAlt) || Input.GetKey(KeyCode.RightAlt);
            bool ctrl = Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            bool shift = Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift);
            return alt == chord.Alt && ctrl == chord.Ctrl && shift == chord.Shift;
        }
    }
}
