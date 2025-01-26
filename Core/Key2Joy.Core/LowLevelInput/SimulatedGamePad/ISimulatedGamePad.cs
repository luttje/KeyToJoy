using System;
using SimWinInput;

namespace Key2Joy.LowLevelInput.SimulatedGamePad;

/// <summary>
/// Result for the <see cref="ISimulatedGamePad.AccessState"/> method.
/// </summary>
public enum StateAccessorResult
{
    /// <summary>
    /// The accessor didn't change the state, and we don't have to do anything.
    /// </summary>
    Unchanged = 0,

    /// <summary>
    /// The state was changed by the accessor, the state needs to be updated.
    /// </summary>
    Changed = 1,
}

/// <summary>
/// Represents a simulated gamepad device.
/// </summary>
public interface ISimulatedGamePad
{
    /// <summary>
    /// Gets the index of the simulated gamepad.
    /// </summary>
    int Index { get; }

    /// <summary>
    /// Plugs in the simulated gamepad.
    /// </summary>
    void PlugIn();

    /// <summary>
    /// Checks if the simulated gamepad is currently plugged in.
    /// </summary>
    /// <returns>True if the gamepad is plugged in; otherwise, false.</returns>
    bool GetIsPluggedIn();

    /// <summary>
    /// Unplugs the simulated gamepad.
    /// </summary>
    void Unplug();

    /// <summary>
    /// Simulates pressing and holding a control on the gamepad.
    /// </summary>
    /// <param name="control">The control to simulate.</param>
    /// <param name="holdTimeMS">The duration (in milliseconds) to hold the control (default is 50ms).</param>
    void Use(GamePadControl control, int holdTimeMS = 50);

    /// <summary>
    /// Sets a specific control on the gamepad.
    /// </summary>
    /// <param name="control">The control to set.</param>
    void SetControl(GamePadControl control);

    /// <summary>
    /// Releases a specific control on the gamepad.
    /// </summary>
    /// <param name="control">The control to release.</param>
    void ReleaseControl(GamePadControl control);

    /// <summary>
    /// Access the raw input state from the GamePad with this callback.
    /// If the callback returns true the state will be updated to whatever
    /// it was mutated to.
    /// Mutation should be locked to one thread at a time.
    /// </summary>
    /// <param name="stateAccessor"></param>
    void AccessState(Func<SimulatedGamePadState, StateAccessorResult> stateAccessor);

    /// <summary>
    /// Resets the GamePad state to the natural at-rest stat
    /// </summary>
    void ResetState();

    /// <summary>
    /// Returns the gamepad info on this device
    /// </summary>
    /// <returns></returns>
    IGamePadInfo GetInfo();
}
