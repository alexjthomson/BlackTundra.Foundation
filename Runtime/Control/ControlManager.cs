using BlackTundra.Foundation.Collections.Generic;

using System;
using System.Collections.Generic;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;

namespace BlackTundra.Foundation.Control {

    /// <summary>
    /// Manages control for the application.
    /// </summary>
    public static class ControlManager {

        #region constant

        /// <summary>
        /// Contains every <see cref="IControllable"/> that can be controlled.
        /// </summary>
#if UNITY_EDITOR
        internal
#else
        private
#endif
        static readonly Stack<IControllable> ControlStack = new Stack<IControllable>();

        /// <summary>
        /// <see cref="PackedBuffer{T}"/> containing every registered <see cref="InputDevice"/>.
        /// </summary>
        private static readonly PackedBuffer<InputDevice> InputDeviceBuffer = new PackedBuffer<InputDevice>(4);

        #endregion

        #region variable

        /// <inheritdoc cref="ControlFlags"/>
        private static ControlFlags controlFlags = ControlFlags.None;

        #endregion

        #region property

        /// <summary>
        /// Current global <see cref="ControlFlags"/>.
        /// </summary>
        public static ControlFlags ControlFlags {
            get => controlFlags;
            set {
                if (value == controlFlags) return;
                Cursor.lockState = (value & ControlFlags.LockCursor) != 0 ? CursorLockMode.Locked : CursorLockMode.None;
                Cursor.visible = (value & ControlFlags.HideCursor) == 0;
                controlFlags = value;
            }
        }

        #endregion

        #region logic

        #region Initialise

        [CoreInitialise]
        private static void Initialise() {
            InputSystem.onDeviceChange += OnInputDeviceChange;
        }

        #endregion

        #region Terminate

        [CoreTerminate]
        private static void Terminate() {
            InputSystem.onDeviceChange -= OnInputDeviceChange;
        }

        #endregion

        #region OnInputDeviceChange

        private static void OnInputDeviceChange(InputDevice device, InputDeviceChange change) {
            switch (change) {
                case InputDeviceChange.Enabled: {
                    InputDeviceBuffer.AddLast(device, true);
                    break;
                }
                case InputDeviceChange.Disabled: {
                    InputDeviceBuffer.Remove(device);
                    break;
                }
            }
        }

        #endregion

        #region GainControl

        /// <summary>
        /// Starts controlling the <paramref name="controllable"/>.
        /// </summary>
        /// <param name="allowOneInstance">
        /// When <c>true</c>, only one instance of the <paramref name="controllable"/> will be allowed in the control stack at one time. If any references exist lower in the
        /// stack, they will be removed before control is given to the <paramref name="controllable"/>.
        /// </param>
        /// <returns>
        /// Returns <c>true</c> if the <paramref name="controllable"/> received control without any exceptions occurring while invoking <see cref="IControllable.OnControlGained"/>.
        /// </returns>
        public static bool GainControl(this IControllable controllable, in bool allowOneInstance) {
            if (controllable == null) throw new ArgumentNullException(nameof(controllable));
            bool success = true;
            IControllable currentControllable = GetCurrentControllable();
            ControlFlags flags = controlFlags;
            if (currentControllable == null) { // there is no current controllable
                //ControlStack.Clear(); // clear the control stack (there are no elements in the stack if the current controllable is null)
                ControlStack.Push(controllable); // push the current controllable to the top of the control stack
                if (!InvokeOnControlGained(controllable, ref flags)) { // successfully gained control
                    //ControlStack.Pop(); // pop the current controllable from the control stack
                    success = false;
                }
            } else { // there is currently a controllable
                //ControlStack.Pop(); // pop the current controllable from the control stack
                InvokeOnControlRevoked(currentControllable);
                if (allowOneInstance) RemoveFromControlStack(controllable); // remove references to the controllable from the stack (and also clean it from null references because why not)
                ControlStack.Push(controllable); // push the current controllable
                if (!InvokeOnControlGained(controllable, ref flags)) { // failed to gain control
                    //ControlStack.Pop(); // pop the current controllable from the control stack
                    success = false;
                }
            }
            ControlFlags = flags; // set the control flags
            return success; // return the success state of the operation
        }

        #endregion

        #region RevokeControl

        /// <summary>
        /// Revokes control from the <paramref name="controllable"/>.
        /// </summary>
        /// <param name="force">
        /// When <c>true</c>, control will be revoked, even if the <paramref name="controllable"/> is not currently in control. This essentially just removes
        /// all references to the <paramref name="controllable"/> from the control stack.
        /// </param>
        public static void RevokeControl(this IControllable controllable, in bool force) {
            if (controllable == null) throw new ArgumentNullException(nameof(controllable));
            IControllable currentControllable = GetCurrentControllable();
            if (currentControllable == controllable) {
                ControlFlags flags = controlFlags;
                InvokeOnControlRevoked(controllable);
                if (force) RemoveFromControlStack(controllable);
                else ControlStack.Pop();
                currentControllable = GetCurrentControllable();
                if (currentControllable != null) InvokeOnControlGained(currentControllable, ref flags);
                ControlFlags = flags;
            } else if (force) RemoveFromControlStack(controllable); // remove the controllable from the control stack
        }

        #endregion

        #region GetCurrentControllable

        /// <returns>
        /// Returns the current <see cref="IControllable"/> at the top of the <see cref="ControlStack"/> or <c>null</c> if no <see cref="IControllable"/> was found.
        /// </returns>
        public static IControllable GetCurrentControllable() {
            int stackSize = ControlStack.Count; // total number of elements in the control stack
            if (stackSize == 0) return null;
            IControllable currentControllable = ControlStack.Peek(); // peek the top control stack value
            if (currentControllable == null) { // top stack value is null
                for (int i = stackSize - 2; i >= 1; i--) { // iterate remaining stack
                    ControlStack.Pop(); // pop the last value since it is a null value
                    currentControllable = ControlStack.Peek(); // peek at the next value
                    if (currentControllable != null) return currentControllable;
                }
            }
            return currentControllable;
        }

        #endregion

        #region RemoveFromControlStack

        /// <summary>
        /// Removes any references to the <paramref name="controllable"/> from the <see cref="ControlStack"/>.
        /// </summary>
        /// <returns>
        /// Returns <c>true</c> if any <paramref name="controllable"/> references were removed from the <see cref="ControlStack"/>.
        /// </returns>
        private static bool RemoveFromControlStack(in IControllable controllable) {
            if (!ControlStack.Contains(controllable)) return false;
            IControllable currentControllable;
            IControllable[] controlStackBuffer = ControlStack.ToArray();
            ControlStack.Clear(); // clear the control stack
            for (int i = controlStackBuffer.Length - 1; i >= 0; i--) { // iterate each remaining element in the control stack
                currentControllable = controlStackBuffer[i]; // store the currently evaulated IControllable instance
                if (currentControllable != null && currentControllable != controllable) { // the instance is not null and is not the current controllable
                    ControlStack.Push(currentControllable); // push the element back into the stack
                }
            }
            return true;
        }

        #endregion

        #region InvokeOnControlGained

        private static bool InvokeOnControlGained(in IControllable controllable, ref ControlFlags flags) {
            try {
                flags = controllable.OnControlGained();
            } catch (Exception exception) {
                Console.Error($"[{nameof(ControlManager)}] Exception occurred while invoking \"{nameof(IControllable.OnControlGained)}\".", exception);
                return false; // failed
            }
            return true; // successful
        }

        #endregion

        #region InvokeOnControlRevoked

        private static bool InvokeOnControlRevoked(in IControllable controllable) {
            try {
                controllable.OnControlRevoked();
            } catch (Exception exception) {
                Console.Error($"[{nameof(ControlManager)}] Exception occurred while invoking \"{nameof(IControllable.OnControlRevoked)}\".", exception);
                return false; // failed
            }
            return true; // successful
        }

        #endregion

        #region SetMotorRumble

        /// <summary>
        /// Sets the motor rumble amount.
        /// </summary>
        /// <param name="value">
        /// Rumble amount normalized between <c>0.0</c> (minimum rumble) and <c>1.0</c> (maximum rumble).
        /// </param>
        /// <seealso cref="SetMotorRumble(in float, in float)"/>
        public static void SetMotorRumble(in float value) => SetMotorRumble(value, value);

        /// <summary>
        /// Sets the motor rumble amount.
        /// </summary>
        /// <param name="left">
        /// Left rumble amount normalized between <c>0.0</c> (minimum rumble) and <c>1.0</c> (maximum rumble).
        /// </param>
        /// <param name="right">
        /// Right rumble amount normalized between <c>0.0</c> (minimum rumble) and <c>1.0</c> (maximum rumble).
        /// </param>
        /// <seealso cref="SetMotorRumble(in float)"/>
        public static void SetMotorRumble(in float left, in float right) {
            InputDevice device;
            for (int i = InputDeviceBuffer.Count - 1; i >= 0; i--) {
                device = InputDeviceBuffer[i];
                if (device is IDualMotorRumble dualMotorRumble)
                    dualMotorRumble.SetMotorSpeeds(left, right); // set rumble speeds
            }
        }

        #endregion

        #endregion

    }

}