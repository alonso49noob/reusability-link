using System;
using System.Collections.Generic;
using System.Reflection;
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
            WarnStockConflicts();
        }

        /// <summary>KSP's own key bindings ignore modifiers, so Alt+B would also toggle BRAKES.</summary>
        void WarnStockConflicts()
        {
            var bindings = new List<KeyValuePair<string, string>>();
            foreach (var field in typeof(GameSettings).GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!(field.GetValue(null) is KeyBinding binding)) continue;
                if (binding.primary != null) bindings.Add(new KeyValuePair<string, string>(field.Name, binding.primary.code.ToString()));
                if (binding.secondary != null) bindings.Add(new KeyValuePair<string, string>(field.Name, binding.secondary.code.ToString()));
            }
            var conflicts = KeyConflicts.Find(chord, bindings);
            if (conflicts.Count == 0) return;
            string names = string.Join(", ", conflicts.ToArray());
            Log.Warn($"SwitchKey '{chord}' coincide con teclas de KSP ({names}); también se activarán al cambiar de nave");
            ScreenMessages.PostScreenMessage($"Reusability Link: {chord} también activa {names} en KSP. Cambia SwitchKey en Settings.cfg", 8f);
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
