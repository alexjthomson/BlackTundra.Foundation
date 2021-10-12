#if ENABLE_INPUT_SYSTEM

using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.LowLevel;

using BlackTundra.Foundation.Collections.Generic;

namespace BlackTundra.Foundation.Control {

    public static class ControlDeviceManager {

        #region constant

        /// <summary>
        /// Tracks every unused <see cref="InputDevice"/>.
        /// </summary>
        private static readonly PackedBuffer<InputDevice> UnusedInputDeviceBuffer = new PackedBuffer<InputDevice>(2);

        #endregion

        #region logic

        #region Initialise

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
#pragma warning disable IDE0051 // remove unread private members
        private static void Initialise() {
#pragma warning restore IDE0051 // remove unread private members
            InputUser.onUnpairedDeviceUsed += OnUnpairedDeviceUsed;
            ++InputUser.listenForUnpairedDeviceActivity;
            ScanUnpairedDevices();
        }

        #endregion

        #region ScanUnpairedDevices

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void ScanUnpairedDevices() {
            InputControlList<InputDevice> unpairedDevices = InputUser.GetUnpairedInputDevices(); // get every unpaired input device
            foreach (InputDevice inputDevice in unpairedDevices) {
                if (UnusedInputDeviceBuffer.IsFull) UnusedInputDeviceBuffer.Expand(1);
                UnusedInputDeviceBuffer.AddLast(inputDevice, true);
            }
        }

        #endregion

        #region OnUnpairedDeviceUsed

        /// <summary>
        /// Called when an <see cref="InputDevice"/> is used that is not used/paired to
        /// any user.
        /// </summary>
        private static void OnUnpairedDeviceUsed(InputControl inputControl, InputEventPtr inputEventPointer) {
            InputDevice inputDevice = inputControl.device;
            if (UnusedInputDeviceBuffer.IsFull) UnusedInputDeviceBuffer.Expand(1);
            UnusedInputDeviceBuffer.AddLast(inputDevice, true);
        }

        #endregion

        #region GetUnusedDevices

        /// <summary>
        /// Gets unused devices.
        /// </summary>
        /// <param name="devices">Unused devices to find.</param>
        /// <param name="requireAll">When <c>true</c>, all <paramref name="devices"/> are required.</param>
        /// <returns>
        /// Returns a <see cref="List{InputDevice}"/> containing every device in the <paramref name="devices"/>
        /// flags that was found. If <paramref name="requireAll"/> is set and not all devices in the
        /// <paramref name="devices"/> are found, <c>null</c> is returned.
        /// </returns>
        internal static List<InputDevice> GetUnusedDevices(in ControlDevices devices, in bool requireAll) {
            List<InputDevice> deviceList = new List<InputDevice>();
            int deviceCount = UnusedInputDeviceBuffer.Count;
            if (deviceCount > 0) {
                InputDevice device;
                for (int i = 0; i < deviceCount; i++) {
                    device = UnusedInputDeviceBuffer[i];
                    if ((devices & ControlDevices.Keyboard) != 0u && device is Keyboard) deviceList.Add(device);
                    else if ((devices & ControlDevices.Mouse) != 0u && device is Mouse) deviceList.Add(device);
                    else if ((devices & ControlDevices.Gamepad) != 0u && device is Gamepad) deviceList.Add(device);
                }
            }
            if (requireAll && deviceList.Count != devices.GetDeviceCount()) return null;
            return deviceList;
        }

        #endregion

        #region MarkAsUsed

        /// <summary>
        /// Marks the <paramref name="devices"/> as used.
        /// </summary>
        internal static void MarkAsUsed(in InputDevice[] devices) => UnusedInputDeviceBuffer.RemoveAll(devices);

        #endregion

        #endregion

    }

}

#endif