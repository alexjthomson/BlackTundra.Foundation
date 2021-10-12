#if ENABLE_INPUT_SYSTEM

using System;
using System.Runtime.InteropServices;

using UnityEngine.InputSystem;

namespace BlackTundra.Foundation.Control {

    [ComVisible(true)]
    [Flags]
    public enum ControlDevices : uint {

        /// <summary>
        /// No control device.
        /// </summary>
        None = 0u,

        /// <summary>
        /// All devices.
        /// </summary>
        All = KeyboardMouse | Gamepad/* | XR*/,

        /// <summary>
        /// Keyboard mouse pair.
        /// </summary>
        KeyboardMouse = Keyboard | Mouse,
        /*
        /// <summary>
        /// Group of common XR devices such as headset and both right and left hand controllers.
        /// </summary>
        XR = XRHeadset | XRRightController | XRLeftController,
        */
        /// <summary>
        /// Keyboard.
        /// </summary>
        Keyboard = 1u << 0,

        /// <summary>
        /// Mouse.
        /// </summary>
        Mouse = 1u << 1,

        /// <summary>
        /// Gamepad such as an XboxOne controller or PS4 controller.
        /// </summary>
        Gamepad = 1u << 2,
        /*
        /// <summary>
        /// XR headset such as Oculus Quest 2.
        /// </summary>
        XRHeadset = 1u << 3,
        
        /// <summary>
        /// XR controller for the right hand.
        /// </summary>
        XRRightController = 1u << 4,

        /// <summary>
        /// XR controller for the left hand.
        /// </summary>
        XRLeftController = 1u << 5*/

    }

    public static class ControlDevicesUtility {

        public static readonly int MaxControlDevices = GetDeviceCount(ControlDevices.All);

        public static Type GetInputDeviceType(this ControlDevices device) {
            if (GetDeviceCount(device) != 1) throw new ArgumentException(string.Concat(nameof(device), ": expected one control device."));
            return device switch {
                ControlDevices.Keyboard => typeof(Keyboard),
                ControlDevices.Mouse => typeof(Mouse),
                ControlDevices.Gamepad => typeof(Gamepad),
                _ => throw new ArgumentException(string.Concat(nameof(device), ": unknown control device: ", device))
            };
        }

        public static int GetDeviceCount(this ControlDevices devices) {
            if (devices == 0u) return 0;
            uint flags = (uint)devices;
            uint allFlags = (uint)ControlDevices.All;
            uint flag;
            int count = 0;
            for (int i = 0; i < 32; i++) {
                flag = 1u << i;
                if ((flags & flag) != 0u && (flag & allFlags) != 0u) count++;
            }
            return count;
        }

    }

}

#endif