#if ENABLE_INPUT_SYSTEM

using System;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.Haptics;
using UnityEngine.InputSystem.Users;
using UnityEngine.InputSystem.Utilities;

using BlackTundra.Foundation.Collections.Generic;
using BlackTundra.Foundation.Utility;

namespace BlackTundra.Foundation.Control {

    public sealed class ControlUser : IDisposable {

        #region constant

        /// <summary>
        /// Buffer that tracks every <see cref="ControlUser"/> instance.
        /// </summary>
#if UNITY_EDITOR
        internal
#else
        private
#endif
        static readonly PackedBuffer<ControlUser> ControlUserBuffer = new PackedBuffer<ControlUser>(1);

        #endregion

        #region variable

        /// <summary>
        /// Unique ID assigned to this <see cref="ControlUser"/>.
        /// </summary>
        public readonly int id;

        /// <summary>
        /// <see cref="InputDevice"/> buffer containing all devices bound to this <see cref="ControlUser"/>.
        /// </summary>
        private readonly PackedBuffer<InputDevice> devices;

        /// <summary>
        /// Buffer of <see cref="IInputActionCollection"/> references. This tracks every collection that the
        /// <see cref="ControlUser"/> assiciates itself with.
        /// </summary>
        private readonly PackedBuffer<IInputActionCollection> actions;

        /// <summary>
        /// Stack that describes the order that a <see cref="ControlUser"/> controls <see cref="IControllable"/>
        /// in.
        /// </summary>
        private readonly Stack<IControllable> controlStack;

        /// <summary>
        /// <see cref="InputUser"/> that the <see cref="ControlUser"/> controls.
        /// </summary>
        private InputUser user;

        /// <summary>
        /// Tracks if the <see cref="ControlUser"/> has been disposed or not.
        /// </summary>
        private bool disposed = false;

        /// <summary>
        /// Rolling ID used to assign a unique id to a <see cref="ControlUser"/>.
        /// </summary>
        private static int rollingControlUserId = int.MinValue;

        /// <summary>
        /// Global <see cref="ControlFlags"/>.
        /// </summary>
        internal static ControlFlags controlFlags = ControlFlags.None;

        #endregion

        #region event

        /// <summary>
        /// Invoked when the current controllable has changed.
        /// </summary>
        public event Action<IControllable> OnCurrentControllableChanged;

        /// <summary>
        /// Invoked when the <see cref="controlStack"/> has been modified.
        /// </summary>
        public event Action OnControlStackModified;

        #endregion

        #region property

        /// <summary>
        /// Main <see cref="ControlUser"/>. This is the default user that will be used as a
        /// fallback when no other user is provided. This user can be assumed to be the
        /// main user.
        /// </summary>
#pragma warning disable IDE1006 // naming styles
        public static ControlUser main { get; private set; } = null;
#pragma warning restore IDE1006 // naming styles

        /// <summary>
        /// Global <see cref="ControlFlags"/>.
        /// </summary>
        public static ControlFlags ControlFlags {
            get => controlFlags;
            set {
                if (value == controlFlags) return;
                Cursor.lockState = (value & ControlFlags.LockCursor) == 0 ? CursorLockMode.None : CursorLockMode.Locked;
                Cursor.visible = (value & ControlFlags.HideCursor) != 0;
                controlFlags = value;
            }
        }

        #endregion

        #region constructor

        private ControlUser() => throw new InvalidOperationException(); // do not allow creation of a control user from the default constructor

        /// <summary>
        /// Creates a <see cref="ControlUser"/> with some <see cref="InputDevice"/> instances to
        /// assume control over.
        /// </summary>
        /// <param name="devices">
        /// Array of <see cref="InputDevice"/> instances to bind to the <see cref="ControlUser"/>.
        /// </param>
        internal ControlUser(in InputDevice[] devices) {

            #region assign id
            id = rollingControlUserId; // assign a unique id
            rollingControlUserId = rollingControlUserId == int.MaxValue ? int.MinValue : (rollingControlUserId + 1); // wrap the rolling id
            #endregion

            #region create device buffer

            if (devices != null && devices.Length > 0) { // check devices were provided
                for (int i = 0; i < devices.Length; i++) { // iterate each device provided
                    if (devices[i] == null) // deivce is null, no null references are allowed
                        throw new ArgumentException($"{nameof(devices)}[{i}] is null; the devices array must not contain any null references.");
                }
                this.devices = new PackedBuffer<InputDevice>(devices); // populate the devices with the devices provided
                ResetDevices(); // reset the state of every device provided
            } else { // no devices were provided while creating this control user
                this.devices = new PackedBuffer<InputDevice>(0); // create an empty device buffer
            }

            #endregion

            #region create actions buffer

            actions = new PackedBuffer<IInputActionCollection>(1);

            #endregion

            #region create control stack

            controlStack = new Stack<IControllable>(4);

            #endregion

            #region create input user

            user = InputUser.CreateUserWithoutPairedDevices(); // create the input user
            if (!this.devices.IsEmpty) { // there are devices
                for (int i = 0; i < this.devices.Count; i++) // iterate each device
                    user = InputUser.PerformPairingWithDevice(this.devices[i], user); // pair with user
            }

            #endregion

            #region assign to control user buffer

            if (ControlUserBuffer.IsFull) ControlUserBuffer.Expand(1);
            ControlUserBuffer.AddLast(this);

            #endregion

            #region singleton logic

            if (main == null) main = this;

            #endregion

        }

        #endregion

        #region destructor

        ~ControlUser() => Dispose();

        #endregion

        #region logic

        #region Initialise

        /// <summary>
        /// Invoked after a scene is loaded.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
#pragma warning disable IDE0051 // remove unread private members
        private static void Initialise() {
#pragma warning restore IDE0051 // remove unread private members
            if (main == null) { // there is no main control device
#if UNITY_STANDALONE || UNITY_WSA
                main = CreateControlUser(ControlDevices.Keyboard | ControlDevices.Mouse, ControlDevices.Gamepad);
#elif UNITY_PS4 || UNITY_XBOXONE
                main = CreateControlUser(ControlDevices.Gamepad);
#else
                #error Unknown platform
#endif
                if (main == null) Debug.LogError("Failed to create main ControlUser: device requirements not met.");
            }
        }

        #endregion

        #region Dispose

        public void Dispose() {
            if (disposed) return;
            Core.Enqueue(SafeDispose);
        }

        #endregion

        #region SafeDispose

        private void SafeDispose() {

            #region handle controllables

            if (controlStack.Any()) { // the control stack is not empty
                IControllable current = controlStack.Peek();
                ControlFlags flags = ControlFlags;
                try {
                    flags = current.OnControlRevoked(this);
                } catch (Exception exception) {
                    exception.Handle($"An unhandled exception occured while revoking control from an IControllable during disposal of ControlUser {id}.");
                }
                ControlFlags = flags;
                controlStack.Clear(); // remove every element from the control stack
                InvokeOnControlStackModified();
                InvokeOnCurrentControllableChanged(null);
            }

            #endregion

            #region handle user buffer
            if (ControlUserBuffer.Remove(this) > 0) ControlUserBuffer.TryShrink(1);
            #endregion

            #region singleton logic
            if (main == this) main = ControlUserBuffer.IsEmpty ? null : ControlUserBuffer.First;
            #endregion

            #region handle input devices and user
            for (int i = 0; i < devices.Count; i++) UnbindDevice(devices[i]); // remove devices
            user.UnpairDevicesAndRemoveUser(); // destroy the input user
            #endregion

            disposed = true;

        }

        #endregion

        #region Reset

        /// <summary>
        /// Restores the state of the <see cref="ControlUser"/> back to default.
        /// This will get rid of any controller vibrations etc caused by
        /// <see cref="InputDevice"/> instances bound to this
        /// <see cref="ControlUser"/>.
        /// </summary>
        public void RestoreState() => ResetDevices();

        #endregion

        #region ResetDevice

        private static void ResetDevice(in InputDevice device) {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (device is IDualMotorRumble dualMotorRumble) dualMotorRumble.SetMotorSpeeds(0.0f, 0.0f); // reset rumble motor speeds
        }

        #endregion

        #region ResetDevices

        private void ResetDevices() {
            for (int i = 0; i < devices.Count; i++) ResetDevice(devices[i]); // reset each device
        }

        #endregion

        #region BindDevice

        public void BindDevice(in InputDevice device) {
            if (device == null) throw new ArgumentNullException(nameof(device));
            if (devices.IsFull) devices.Expand(1);
            ResetDevice(device);
            devices.AddLast(device, true);
            user = InputUser.PerformPairingWithDevice(device, user); // pair with user
            UpdateControlScheme();
        }

        #endregion

        #region UnbindDevice

        public void UnbindDevice(in InputDevice device) {
            if (device == null) throw new ArgumentNullException(nameof(device));
            int index = devices[device];
            if (index != -1) return; // device not bound
            ResetDevice(device);
            devices.RemoveAt(index);
            user.UnpairDevice(device);
            UpdateControlScheme();
        }

        #endregion

        #region HasDevice

        /// <returns>
        /// Returns <c>true</c> if the <see cref="ControlUser"/> has the <paramref name="device"/> bound.
        /// </returns>
        public bool HasDevice(in InputDevice device) => devices[device ?? throw new ArgumentNullException(nameof(device))] != -1;

        /// <returns>
        /// Returns <c>true</c> if the <see cref="ControlUser"/> has one or more <see cref="InputDevice"/>
        /// instances of type <typeparamref name="T"/> as a bound device.
        /// </returns>
        public bool HasDevice<T>() where T : InputDevice {
            for (int i = 0; i < devices.Count; i++) {
                if (devices[i] is T) return true;
            }
            return false;
        }

        /// <param name="device">Single device flag to check.</param>
        /// <returns>
        /// Returns <c>true</c> if the <see cref="ControlUser"/> has one or more <paramref name="device"/>
        /// instance.
        /// </returns>
        /// <seealso cref="HasDevices(in ControlDevices)"/>
        public bool HasDevice(in ControlDevices device) {
            if (device.GetDeviceCount() != 1) throw new ArgumentException(string.Concat(nameof(device), ": expected one device."));
            return device switch {
                ControlDevices.Keyboard => HasDevice<Keyboard>(),
                ControlDevices.Mouse => HasDevice<Mouse>(),
                ControlDevices.Gamepad => HasDevice<Gamepad>(),
                _ => throw new ArgumentException(string.Concat(nameof(device), ": unknown ControlDevice flag: ", device)),
            };
        }

        #endregion

        #region HasDevices

        /// <param name="devices">Device flags to check.</param>
        /// <returns>
        /// Returns <c>true</c> if the <see cref="ControlUser"/> has one or more instance of each device
        /// contained in the <paramref name="devices"/> flags.
        /// </returns>
        /// <seealso cref="HasDevice(in ControlDevices)"/>
        public bool HasDevices(in ControlDevices devices) {
            uint flags = (uint)devices;
            uint device;
            int deviceCount = this.devices.Count;
            int limit = ControlDevicesUtility.MaxControlDevices;
            for (int i = 0; i < limit; i++) {
                device = 1u << i;
                if ((flags & device) != 0u) {
                    Type type = ControlDevicesUtility.GetInputDeviceType((ControlDevices)devices);
                    bool found = false;
                    for (int j = 0; j < deviceCount; j++) {
                        if (!type.Equals(this.devices[j].GetType())) {
                            found = true;
                            break;
                        }
                    }
                    if (!found) return false; // current type not found
                }
            }
            return true; // all types found
        }

        #endregion

        #region OverrideDevices

        /// <summary>
        /// Overrides the existing <see cref="devices"/> buffer with the <paramref name="devices"/>
        /// passed into the method.
        /// </summary>
        /// <param name="devices">
        /// New <see cref="InputDevice"/> buffer to replace the existing bound devices.
        /// </param>
        internal void OverrideDevices(in InputDevice[] devices) {
            if (devices == null) throw new ArgumentNullException(nameof(devices));
            ResetDevices();
            this.devices.Clear(devices); // clear the device buffer and assign a new set of devices
            for (int i = 0; i < this.devices.Count; i++)
                user = InputUser.PerformPairingWithDevice(this.devices[i], user); // pair to user
            UpdateControlScheme();
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
        public void SetMotorRumble(in float value) => SetMotorRumble(value, value);

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
        public void SetMotorRumble(in float left, in float right) {
            InputDevice device;
            for (int i = 0; i < devices.Capacity; i++) {
                device = devices[i];
                if (device is IDualMotorRumble dualMotorRumble)
                    dualMotorRumble.SetMotorSpeeds(left, right); // set rumble speeds
            }
        }

        #endregion

        #region InvokeOnCurrentControllableChanged

        private void InvokeOnCurrentControllableChanged(in IControllable newControllable) {
            if (OnCurrentControllableChanged == null) return;
            try {
                OnCurrentControllableChanged.Invoke(newControllable);
            } catch (Exception exception) {
                exception.Handle($"An unhandled exception occurred while invoking OnCurrentControllableChanged on ControlUser {id}.");
            }
        }

        #endregion

        #region InvokeOnControlStackModified

        private void InvokeOnControlStackModified() {
            if (OnControlStackModified == null) return;
            try {
                OnControlStackModified.Invoke();
            } catch (Exception exception) {
                exception.Handle($"An unhandled exception occurred while invoking OnControlStackModified on ControlUser {id}.");
            }
        }

        #endregion

        #region InvokeOnControlGained

        private void InvokeOnControlGained(in IControllable controllable, ref ControlFlags flags) {
            if (controllable == null) throw new ArgumentNullException(nameof(controllable));
            try {
                flags = controllable.OnControlGained(this);
            } catch (Exception exception) {
                exception.Handle($"An unhandled exception occurred while gaining control to an IControllable (user: {id}).");
            }
        }

        #endregion

        #region InvokeOnControlRevoked

        private void InvokeOnControlRevoked(in IControllable controllable, ref ControlFlags flags) {
            if (controllable == null) throw new ArgumentNullException(nameof(controllable));
            try {
                flags = controllable.OnControlRevoked(this);
            } catch (Exception exception) {
                exception.Handle($"An unhandled exception occurred while revoking control from an IControllable (user: {id}).");
            }
        }

        #endregion

        #region GainControl

        /// <summary>
        /// Starts controlling an <see cref="IControllable"/> object.
        /// </summary>
        /// <param name="controllable"><see cref="IControllable"/> to gain control of.</param>
        /// <param name="force">
        /// When <c>true</c>, the <see cref="ControlUser"/> will forcibly take control of the
        /// <see cref="IControllable"/>, even if it is already controlled by a different
        /// <see cref="ControlUser"/>.
        /// </param>
        /// <returns>
        /// Returns <c>true</c> if the <see cref="ControlUser"/> successfully gained control
        /// of the <paramref name="controllable"/>.
        /// </returns>
        public bool GainControl(in IControllable controllable, in bool force = false) {
            if (controllable == null) throw new ArgumentNullException(nameof(controllable));
            ControlUser user = FindControlUser(controllable); // get the current user in control of the controllable
            ControlFlags flags = controlFlags; // get the current control flags
            if (user != null) { // the controllable already has a control user
                if (user == this) return true; // the user is already in control of the target controllable
                if (!force) return false; // cannot force control of target controllable, therefore cannot gain control; return false
                #region revoke control
                IControllable current = user.controlStack.Peek(); // get the current controllable of the user in control of the target controllable
                user.RemoveFromControlStack(controllable); // remove the target controllable from the other users control stack
                if (current == controllable) { // the current controllable of the user is the same controllable as the target controllable
                    user.InvokeOnControlRevoked(current, ref flags); // force the user to revoke control of the target controllable (current controllable)
                    if (user.controlStack.Any()) { // the user has more controllables that can be controlled
                        current = user.controlStack.Peek(); // get the next controllable in the users control stack
                        user.InvokeOnControlGained(current, ref flags); // force the user to gain control of the new controllable
                    } else // the user does not have any more controllables to gain control over in their control stack
                        current = null; // mark the current controllable as null
                    user.InvokeOnControlStackModified();
                    user.InvokeOnCurrentControllableChanged(current); // mark the new controllable
                }
                #endregion
            }
            if (controlStack.Any()) { // the control stack contains elements
                IControllable current = controlStack.Peek(); // get the current controllable
                // there is no need to check if the current controllable is the one trying to be controlled since it was checked earler
                InvokeOnControlRevoked(current, ref flags); // revoke control from the current controllable
                RemoveFromControlStack(controllable); // remove the target controllable from the control stack so it can be added ontop
            }
            controlStack.Push(controllable); // add the controllable to the top of the control stack
            InvokeOnControlGained(controllable, ref flags); // gain control of the target controllable
            ControlFlags = flags;
            InvokeOnControlStackModified();
            InvokeOnCurrentControllableChanged(controllable);
            return true; // successfully gained control of the target controllable
        }

        #endregion

        #region RevokeControl

        public void RevokeControl(in IControllable controllable) {
            if (controllable == null) throw new ArgumentNullException(nameof(controllable));
            if (controlStack.Any()) { // the control stack contains items
                IControllable current = controlStack.Peek();
                RemoveFromControlStack(controllable);
                if (current == controllable) { // the current controllable object was the controllable requested to be removed
                    ControlFlags flags = controlFlags;
                    InvokeOnControlRevoked(current, ref flags);
                    if (controlStack.Any()) { // the control stack is still not empty
                        current = controlStack.Peek(); // the new current controllable has changed
                        InvokeOnControlGained(current, ref flags);
                    } else // the control stack is empty
                        current = null; // unassign the current controllable
                    ControlFlags = flags;
                    InvokeOnControlStackModified();
                    InvokeOnCurrentControllableChanged(current);
                }
            }
        }

        #endregion

        #region RemoveFromControlStack

        /// <summary>
        /// Removes all occurrances of the <paramref name="controllable"/> from the <see cref="controlStack"/>.
        /// </summary>
        private void RemoveFromControlStack(in IControllable controllable) {
            if (controllable == null) throw new ArgumentNullException(nameof(controllable));
            IControllable[] stack = controlStack.ToArray();
            controlStack.Clear();
            stack.Remove(controllable, true);
            for (int i = stack.Length - 1; i >= 0; i--) controlStack.Push(stack[i]);
        }

        #endregion

        #region GetControlStack

        /// <returns>Array of <see cref="IControllable"/> instances that make up the <see cref="ControlUser"/> control stack.</returns>
        public IControllable[] GetControlStack() => controlStack.ToArray();

        #endregion

        #region UpdateControlScheme

        /// <summary>
        /// Called after one or more <see cref="InputDevice"/> has been bound/unbound from
        /// the <see cref="ControlUser"/>. This updates the current active control scheme.
        /// </summary>
        private void UpdateControlScheme() {
            if (devices.IsEmpty || actions.IsEmpty) return; // no update required
            InputControlScheme controlScheme;
            IInputActionCollection action = actions[0]; // get the first action
            try {
                controlScheme = FindControlScheme(action.controlSchemes); // find the best control scheme
            } catch (NotSupportedException exception) { // no control scheme found
                exception.Handle($"Failed to find control scheme that supports control user {id}.");
                return;
            }
            user.ActivateControlScheme(controlScheme);
        }

        #endregion

        #region FindControlScheme

        /// <summary>
        /// Finds the best <see cref="InputControlScheme"/> based off of the devices bound to
        /// the <see cref="ControlUser"/>. The algorithm used will prioritise control schemes
        /// with the most devices attached to them.
        /// </summary>
        /// <param name="controlSchemes">List of possible candidates.</param>
        /// <returns>
        /// Best control scheme. If no control scheme is found, a NotSupportedException will
        /// be thrown since <c>null</c> cannot be returned.
        /// </returns>
        private InputControlScheme FindControlScheme(in ReadOnlyArray<InputControlScheme> controlSchemes) {
            int bestIndex = -1; // track the best index out of all of the control schemes
            int bestScore = 0; // track the best score of the control scheme at the bestIndex
            int deviceCount = devices.Count; // get the total number of devices
            if (deviceCount == 0) throw new NotSupportedException("Cannot find control scheme with no devices."); // no devices bound
            for (int i = 0; i < controlSchemes.Count; i++) { // iterate each control scheme
                InputControlScheme controlScheme = controlSchemes[i]; // get the control scheme
                int score = 0; // create an additive score for the control scheme
                foreach (InputControlScheme.DeviceRequirement requirement in controlScheme.deviceRequirements) { // iterate each device requirement in the control scheme
                    string controlPath = requirement.controlPath; // get the control path of the requirement, this describes what kind of device is required
                    bool found = false; // create a boolean to track if the device required is bound to this control user
                    for (int j = 0; j < deviceCount; j++) { // iterate each device on the user
                        if (controlPath.Equals(devices[j].path)) { // check if the paths match
                            found = true; // the device was found
                            break; // stop here
                        }
                    }
                    if (found) score += requirement.isOptional ? 10 : 1; // increase the score of the device since the device was found
                    else if (!requirement.isOptional) { score = int.MinValue; break; } // didn't find required device
                }
                if (score > bestScore) { // check if this score scores better than the current best scoring control scheme
                    bestIndex = i; // update the best control scheme
                    bestScore = score; // update the best score
                }
            }
            if (bestIndex == -1) throw new NotSupportedException("ControlDevice did not fulfil the requirements of any control schemes."); // no control scheme found
            return controlSchemes[bestIndex]; // return the best control scheme
        }

        #endregion

        #region FindControlUser

        /// <summary>
        /// Finds the <see cref="ControlUser"/> that is controlling the <paramref name="controllable"/>.
        /// </summary>
        /// <returns>
        /// Returns the <see cref="ControlUser"/> controlling the <paramref name="controllable"/>; if none
        /// is found, <c>null</c> is returned.
        /// </returns>
        public static ControlUser FindControlUser(in IControllable controllable) {
            if (controllable == null) throw new ArgumentNullException(nameof(controllable));
            ControlUser user; // temporary reference
            for (int i = 0; i < ControlUserBuffer.Count; i++) { // iterate each control user
                user = ControlUserBuffer[i]; // get the current control user
                if (user.controlStack.Contains(controllable)) // the controllable is in the control user control stack
                    return user; // return this user
            }
            return null; // no user was found
        }

        /// <returns>
        /// Returns the <see cref="ControlUser"/> that the <paramref name="device"/> is bound to or
        /// returns <c>null</c> if none is found.
        /// </returns>
        public static ControlUser FindControlUser(in InputDevice device) {
            if (device == null) throw new ArgumentNullException(nameof(device));
            ControlUser user;
            for (int i = 0; i < ControlUserBuffer.Count; i++) {
                user = ControlUserBuffer[i];
                if (user.HasDevice(device))
                    return user;
            }
            return null; // none found
        }

        #endregion

        #region CreateControlUser

        /// <summary>
        /// Creates a <see cref="ControlUser"/>.
        /// </summary>
        /// <param name="requiredDevices">
        /// Flags defining which <see cref="ControlDevices"/> are required for the <see cref="ControlUser"/>
        /// creation to succeed.
        /// </param>
        /// <param name="optionalDevices">
        /// Flags defining which <see cref="ControlDevices"/> are optional for the <see cref="ControlUser"/>.
        /// As many optional devices will be added to the <see cref="ControlUser"/> as possible but not all
        /// devices may be added if a device is not available.
        /// </param>
        /// <returns>
        /// If creation is successfull, a configured <see cref="ControlUser"/> instance is returned, otherwise
        /// <c>null</c> is returned.
        /// </returns>
        public static ControlUser CreateControlUser(in ControlDevices requiredDevices, in ControlDevices optionalDevices = ControlDevices.None) {

            List<InputDevice> devices = null;

            #region check required devices
            if (requiredDevices != ControlDevices.None) {
                devices = ControlDeviceManager.GetUnusedDevices(requiredDevices, true);
                if (devices == null) return null; // failed to find all required devices
            }
            #endregion

            #region check optional devices
            if (optionalDevices != ControlDevices.None) {
                ControlDevices flags = optionalDevices & ~requiredDevices; // remove the required devices from the optional devices so no duplicates are found
                if (flags != 0u) {
                    if (devices == null)
                        devices = ControlDeviceManager.GetUnusedDevices(flags, false);
                    else
                        devices.Concat(ControlDeviceManager.GetUnusedDevices(flags, false));
                }
            }
            #endregion

            InputDevice[] deviceBuffer = devices.ToArray();
            ControlDeviceManager.MarkAsUsed(deviceBuffer);
            return new ControlUser(deviceBuffer);

        }

        #endregion

        #region BindActions

        /// <summary>
        /// Binds an <see cref="IInputActionCollection"/> of type
        /// <typeparamref name="T"/> to the <see cref="ControlUser"/>.
        /// </summary>
        /// <param name="controlScheme">
        /// Control scheme to use, if none is provided, a control scheme will be
        /// found.
        /// </param>
        /// <returns>
        /// If the input action of type <typeparamref name="T"/> is already bound, the
        /// instance bound will be returned. Otherwise, a new instance will be created
        /// and bound to the <see cref="ControlUser"/>. <c>null</c> is never returned.
        /// </returns>
        public T BindActions<T>(in string controlScheme = null) where T : class, IInputActionCollection, new() {
            int actionCount = actions.Count;
            if (actionCount > 0) {
                for (int i = actionCount - 1; i >= 0; i--) {
                    if (actions[i] is T t) {
                        if (i != 0) actions.Swap(0, i); // move action collection to front of actions
                        if (controlScheme != null) user.ActivateControlScheme(controlScheme);
                        else UpdateControlScheme();
                        return t;
                    }
                }
            }
            if (!user.valid) throw new InvalidOperationException($"Can not bind input actions to ControlUser (id: {id}) with an invalid InputUser.");
            T instance = new T(); // create new instance of the input action
            if (actions.IsFull) actions.Expand(1);
            if (actions.AddFirst(instance) != -1) { // assign actions
                instance.Enable(); // enable the input actions
                user.AssociateActionsWithUser(instance); // associate action collection with user
                if (controlScheme != null) user.ActivateControlScheme(controlScheme);
                else UpdateControlScheme();
                return instance;
            }
            instance.Disable(); // this is likely not require, but is done as a precaution
            throw new InvalidOperationException($"Failed to bind actions to ControlUser {id}."); // failed
        }

        #endregion

        #region UnbindActions

        /// <summary>
        /// Unbinds an <see cref="IInputActionCollection"/> of type <typeparamref name="T"/>.
        /// </summary>
        /// <returns>
        /// Returns the instance of <see cref="IInputActionCollection"/> of type <typeparamref name="T"/> from
        /// the <see cref="ControlUser"/>. If no instance was bound, <c>null</c> is returned.
        /// </returns>
        public T UnbindActions<T>() where T : class, IInputActionCollection, new() {
            int actionCount = actions.Count;
            if (actionCount > 0) {
                for (int i = actionCount - 1; i >= 0; i--) {
                    if (actions[i] is T t) {
                        actions.RemoveAt(i);
                        t.Disable();
                        return t;
                    }
                }
            }
            return null;
        }

        #endregion

        #region GetActions

        /// <returns>
        /// Returns the action collection instance of type <typeparamref name="T"/> associated with
        /// the <see cref="ControlUser"/>.
        /// </returns>
        /// <seealso cref="BindActions{T}"/>
        private T GetActions<T>() where T : class, IInputActionCollection, new() {
            int actionCount = actions.Count;
            if (actionCount > 0) {
                for (int i = actionCount - 1; i >= 0; i--) {
                    if (actions[i] is T t) return t;
                }
            }
            return null;
        }

        #endregion

        #endregion

    }

}

#endif