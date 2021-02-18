using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace FreeIva
{
    public static class Settings
    {
        public static KeyCode UnbuckleKey = KeyCode.Y;
        public static KeyCode OpenHatchKey = KeyCode.F;
        public static KeyCode ModifierKey = KeyCode.LeftAlt;
        
        public static KeyCode ForwardKey = KeyCode.W;
        public static KeyCode BackwardKey = KeyCode.S;
        public static KeyCode LeftKey = KeyCode.A;
        public static KeyCode RightKey = KeyCode.D;
        public static KeyCode RollCCWKey = KeyCode.Q;
        public static KeyCode RollCWKey = KeyCode.E;
        public static KeyCode UpKey = KeyCode.LeftShift;
        public static KeyCode DownKey = KeyCode.LeftControl;
        public static KeyCode JumpKey = KeyCode.Space;

        public static float ForwardSpeed = 1f;
        public static float HorizontalSpeed = 1f;
        public static float VerticalSpeed = 1f;
        public static float YawSpeed = 7f;
        public static float PitchSpeed = 7f;
        public static float RollSpeed = 100f;

        public static float HelmetSize = 0.6f;
        public static float NoHelmetSize = 0.4f;

        public static void LoadSettings()
        {
            Debug.Log("[FreeIVA] Loading settings...");
            ConfigNode settings = GameDatabase.Instance.GetConfigNode("FreeIva/settings/FreeIvaConfig");
            if (settings == null)
            {
                Debug.LogWarning("[FreeIVA] FreeIva/settings.cfg not found! Using default values.");
                return;
            }

            // Keys
            if (settings.HasValue("UnbuckleKey")) UnbuckleKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("UnbuckleKey"));
            if (settings.HasValue("OpenHatchKey")) OpenHatchKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("OpenHatchKey"));
            if (settings.HasValue("ModifierKey")) ModifierKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("ModifierKey"));
            if (settings.HasValue("ForwardKey")) ForwardKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("ForwardKey"));
            if (settings.HasValue("BackwardKey")) BackwardKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("BackwardKey"));
            if (settings.HasValue("LeftKey")) LeftKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("LeftKey"));
            if (settings.HasValue("RightKey")) RightKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("RightKey"));
            if (settings.HasValue("RollCCWKey")) RollCCWKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("RollCCWKey"));
            if (settings.HasValue("RollCWKey")) RollCWKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("RollCWKey"));
            if (settings.HasValue("UpKey")) UpKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("UpKey"));
            if (settings.HasValue("DownKey")) DownKey = (KeyCode)Enum.Parse(typeof(KeyCode), settings.GetValue("DownKey"));

            // Axis multipliers
            if (settings.HasValue("ForwardSpeed")) ForwardSpeed = float.Parse(settings.GetValue("ForwardSpeed"));
            if (settings.HasValue("HorizontalSpeed")) HorizontalSpeed = float.Parse(settings.GetValue("HorizontalSpeed"));
            if (settings.HasValue("VerticalSpeed")) VerticalSpeed = float.Parse(settings.GetValue("VerticalSpeed"));
            if (settings.HasValue("YawSpeed")) YawSpeed = float.Parse(settings.GetValue("YawSpeed"));
            if (settings.HasValue("PitchSpeed")) PitchSpeed = float.Parse(settings.GetValue("PitchSpeed"));
            if (settings.HasValue("RollSpeed")) RollSpeed = float.Parse(settings.GetValue("RollSpeed"));

            if (settings.HasValue("HeadSize")) HelmetSize = float.Parse(settings.GetValue("HeadSize"));
        }
    }
}
