using System;
using SimWinInput;

namespace Key2Joy.LowLevelInput.SimulatedGamePad;

/// <summary>
/// Implementation based on https://github.com/DavidRieman/SimWinInput
/// </summary>
public class SimulatedGamePad : ISimulatedGamePad
{
    private const string GAMEPAD_NAME = "Simulated";

    /// <inheritdoc />
    public int Index { get; private set; }

    private bool isPluggedIn = false;
    private readonly IGamePadInfo gamePadInfo;

    private object stateLock = new();

    public SimulatedGamePad(int index)
    {
        this.Index = index;
        this.gamePadInfo = new GamePadInfo(index, GAMEPAD_NAME);
    }

    public IGamePadInfo GetInfo()
        => this.gamePadInfo;

    /// <inheritdoc />
    public void PlugIn()
    {
        SimGamePad.Instance.PlugIn(this.Index);
        this.isPluggedIn = true;

        // Ensure the state starts reset fixes problem where other (real) gamepad may get button stuck
        this.ResetState();
    }

    /// <inheritdoc />
    public bool GetIsPluggedIn()
        => this.isPluggedIn;

    /// <inheritdoc />
    public void Unplug()
    {
        SimGamePad.Instance.Unplug(this.Index);
        this.isPluggedIn = false;
    }

    /// <inheritdoc />
    public void Use(GamePadControl control, int holdTimeMS = 50)
    {
        SimGamePad.Instance.Use(control, this.Index, holdTimeMS);
        this.gamePadInfo.OnActivityOccurred();
    }

    /// <inheritdoc />
    public void SetControl(GamePadControl control)
    {
        SimGamePad.Instance.SetControl(control, this.Index);
        this.gamePadInfo.OnActivityOccurred();
    }

    /// <inheritdoc />
    public void ReleaseControl(GamePadControl control)
    {
        SimGamePad.Instance.ReleaseControl(control, this.Index);
        this.gamePadInfo.OnActivityOccurred();
    }

    /// <inheritdoc />
    public void AccessState(Func<SimulatedGamePadState, StateAccessorResult> stateAccessor)
    {
        // Commented, because as expected this freezes the app up when
        // everyone has to wait for the lock to be released. And it didn't
        // fix #61 like I hoped it would.
        //lock (this.stateLock)
        {
            var state = SimGamePad.Instance.State[this.Index];

            if (stateAccessor(state) == StateAccessorResult.Unchanged)
            {
                return;
            }

            SimGamePad.Instance.Update(this.Index);
            this.gamePadInfo.OnActivityOccurred();
        }
    }

    /// <inheritdoc />
    public void ResetState()
        => this.AccessState((state) =>
        {
            state.Reset();

            return StateAccessorResult.Changed;
        });
}
