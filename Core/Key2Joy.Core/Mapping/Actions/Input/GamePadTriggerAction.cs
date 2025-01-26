using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonServiceLocator;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.LowLevelInput.SimulatedGamePad;
using Key2Joy.LowLevelInput.XInput;
using Key2Joy.Mapping.Triggers.GamePad;
using Key2Joy.Mapping.Triggers.Mouse;

namespace Key2Joy.Mapping.Actions.Input;

[Action(
    Description = "GamePad Trigger Simulation",
    NameFormat = "Move {0} Trigger on GamePad #{3} by {1}",
    GroupName = "GamePad Simulation",
    GroupImage = "joystick"
)]
public class GamePadTriggerAction : CoreAction, IProvideReverseAspect, IEquatable<GamePadTriggerAction>
{
    private const byte MIN_TRIGGER_VALUE = XInputGamePad.TriggerValueMin;
    private const byte MAX_TRIGGER_VALUE = XInputGamePad.TriggerValueMax;

    private const float EXACT_SCALE = (MAX_TRIGGER_VALUE - MIN_TRIGGER_VALUE) / 2f;

    /// <summary>
    /// Which side to simulate
    /// </summary>
    public GamePadSide Side { get; set; }

    /// <summary>
    /// How much to simulate the trigger being pulled back as a fraction from 0 - 1.
    /// If null, then the inputbag of the input that triggered this will be used if possible.
    /// </summary>
    public float? Delta { get; set; } = null;

    /// <summary>
    /// If Delta is null, this is the scale factor to apply to the inputbag
    /// </summary>
    public float InputScale { get; set; } = 1;

    /// <summary>
    /// Which gamepad to simulate
    /// </summary>
    public int GamePadIndex { get; set; }

    public GamePadTriggerAction(string name) : base(name)
    { }

    /// <inheritdoc/>
    public void MakeReverse(AbstractMappingAspect aspect)
    {
        var reverse = aspect as GamePadTriggerAction;

        if (this.Delta.HasValue)
        {
            reverse.Delta = this.Delta.Value * -1;
        }
    }

    /// <inheritdoc/>
    public override void OnStartListening(AbstractTriggerListener listener, ref IList<AbstractAction> otherActions)
    {
        base.OnStartListening(listener, ref otherActions);

        var gamePadService = ServiceLocator.Current.GetInstance<ISimulatedGamePadService>();
        gamePadService.EnsurePluggedIn(this.GamePadIndex);
    }

    /// <markdown-doc>
    /// <parent-name>Input</parent-name>
    /// <path>Api/Input</path>
    /// </markdown-doc>
    /// <summary>
    /// Simulate pulling back a gamepad trigger
    /// </summary>
    /// <markdown-example>
    /// Pulls the left gamepad trigger halfway back, then resets after 500ms
    /// <code language="lua">
    /// <![CDATA[
    /// GamePad.SimulateTrigger(0.5)
    /// SetTimeout(function()
    ///    GamePad.Reset()
    /// end, 500)
    /// ]]>
    /// </code>
    /// </markdown-example>
    /// <param name="delta">The fraction by which to pull the trigger back (between 0 and 1)</param>
    /// <param name="side">Which gamepad trigger to pull, either GamePadSide.Left (default) or .Right</param>
    /// <param name="gamepadIndex">Which of 4 possible gamepads to simulate: 0 (default), 1, 2 or 3</param>
    /// <name>GamePad.SimulateTrigger</name>
    [ExposesScriptingMethod("GamePad.SimulateTrigger")]
    public async void ExecuteForScript(
        float delta,
        GamePadSide side = GamePadSide.Left,
        int gamepadIndex = 0)
    {
        this.Delta = delta;
        this.Side = side;
        this.GamePadIndex = gamepadIndex;

        var gamePadService = ServiceLocator.Current.GetInstance<ISimulatedGamePadService>();
        gamePadService.EnsurePluggedIn(this.GamePadIndex);

        await this.Execute();
    }

    /// <summary>
    /// Scales InputBag values to a reasonable range for gamepad triggers
    /// </summary>
    /// <param name="value"></param>
    /// <param name="sensitivity"></param>
    /// <returns></returns>
    private static byte Scale(float value, float sensitivity = 1.0f)
    {
        var scaled = (int)(value * sensitivity * (MAX_TRIGGER_VALUE - MIN_TRIGGER_VALUE));
        var gamePadTriggerX = (byte)Math.Min(Math.Max(scaled, MIN_TRIGGER_VALUE), MAX_TRIGGER_VALUE);

        return gamePadTriggerX;
    }

    /// <inheritdoc/>
    public override async Task Execute(AbstractInputBag inputBag = null)
    {
        var gamePadService = ServiceLocator.Current.GetInstance<ISimulatedGamePadService>();
        var gamePad = gamePadService.GetGamePad(this.GamePadIndex);

        if (!gamePad.GetIsPluggedIn())
        {
            gamePad.PlugIn();
        }

        gamePad.AccessState((state) =>
        {
            var deltaX = (byte)0;
            var deltaY = (byte)0;

            if (this.Delta is not null)
            {
                deltaX = Scale((float)this.Delta, EXACT_SCALE);
                deltaY = Scale((float)this.Delta, EXACT_SCALE);
            }
            else if (inputBag is AxisDeltaInputBag axisInputBag)
            {
                deltaX = Scale(axisInputBag.DeltaX, this.InputScale);
                deltaY = Scale(axisInputBag.DeltaY, this.InputScale);
            }
            else if (inputBag is GamePadTriggerInputBag triggerInputBag)
            {
                // TODO: This is now hard-coded, but I'd love for 'modifiers' to exist in between triggers and actions. Those could (with more fine tuning) be configured by the user.
                if (this.Side == GamePadSide.Left)
                {
                    deltaX = Scale(triggerInputBag.LeftTriggerDelta, this.InputScale);
                }
                else
                {
                    deltaX = Scale(triggerInputBag.RightTriggerDelta, this.InputScale);
                }
            }

            if (this.Side == GamePadSide.Left)
            {
                state.LeftTrigger = Math.Max(deltaX, deltaY);
            }
            else
            {
                state.RightTrigger = Math.Max(deltaX, deltaY);
            }

            return StateAccessorResult.Changed;
        });
    }

    /// <inheritdoc/>
    public override string GetNameDisplay()
        => this.Name.Replace("{0}", Enum.GetName(typeof(GamePadSide), this.Side))
            .Replace("{1}", this.Delta?.ToString() ?? $"<biggest(TriggerInputX,TriggerInputY) * {this.InputScale}>")
            .Replace("{3}", this.GamePadIndex.ToString());

    /// <inheritdoc/>
    public bool Equals(GamePadTriggerAction other)
        => other is not null
        && this.Side == other.Side
        && this.Delta == other.Delta
        && this.InputScale == other.InputScale
        && this.GamePadIndex == other.GamePadIndex;

    /// <inheritdoc/>
    public override int GetHashCode()
    {
        var hashCode = 1651596494;
        hashCode = (hashCode * -1521134295) + this.Side.GetHashCode();
        hashCode = (hashCode * -1521134295) + this.Delta.GetHashCode();
        hashCode = (hashCode * -1521134295) + this.InputScale.GetHashCode();
        hashCode = (hashCode * -1521134295) + this.GamePadIndex.GetHashCode();
        return hashCode;
    }
}
