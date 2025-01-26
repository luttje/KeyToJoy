using System.Collections.Generic;
using CommonServiceLocator;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.LowLevelInput.XInput;

namespace Key2Joy.Mapping.Triggers.GamePad;

public class GamePadStickLookup : Dictionary<int, List<AbstractMappedOption>>
{ }

public class GamePadStickTriggerListener : CoreTriggerListener
{
    public static GamePadStickTriggerListener instance;

    public static GamePadStickTriggerListener Instance
    {
        get
        {
            instance ??= new GamePadStickTriggerListener();

            return instance;
        }
    }

    /// <summary>
    /// Force a new instance to be created, used to reset the listener when mappings
    /// are re-armed.
    /// </summary>
    /// <returns></returns>
    public static GamePadStickTriggerListener NewInstance() => instance = new GamePadStickTriggerListener();

    private readonly Dictionary<GamePadSide, GamePadStickLookup> stickAxisLookups;
    private readonly IXInputService xInputService;

    private GamePadStickTriggerListener()
    {
        this.xInputService = ServiceLocator.Current.GetInstance<IXInputService>();

        this.stickAxisLookups = new()
        {
            [GamePadSide.Left] = new GamePadStickLookup(),
            [GamePadSide.Right] = new GamePadStickLookup()
        };
    }

    /// <inheritdoc/>
    protected override void Start()
    {
        this.xInputService.StateChanged += this.XInputService_StateChanged;

        base.Start();
    }

    /// <inheritdoc/>
    protected override void Stop()
    {
        this.xInputService.StateChanged -= this.XInputService_StateChanged;
        instance = null;

        base.Stop();
    }

    /// <inheritdoc/>
    public override void AddMappedOption(AbstractMappedOption mappedOption)
    {
        var trigger = mappedOption.Trigger as GamePadStickTrigger;
        var lookup = this.stickAxisLookups[trigger.StickSide];

        if (!lookup.TryGetValue(trigger.GetInputHash(), out var mappedOptions))
        {
            lookup.Add(trigger.GetInputHash(), mappedOptions = new List<AbstractMappedOption>());
        }

        mappedOptions.Add(mappedOption);
    }

    /// <inheritdoc/>
    public override bool GetIsTriggered(AbstractTrigger trigger)
    {
        if (trigger is not GamePadStickTrigger gamePadStickTrigger)
        {
            return false;
        }

        var state = this.xInputService.GetState(gamePadStickTrigger.GamePadIndex);

        if (state is null)
        {
            return false;
        }

        return state.Value.Gamepad.IsThumbstickMoved(gamePadStickTrigger.StickSide, gamePadStickTrigger.DeltaMargin);
    }

    /// <summary>
    /// This method is called when the state of a gamepad has changed.
    /// We'll use it to find in the lookup which mapped options are triggered based on
    /// the triggered device index and margins (using <see cref="GetIsTriggered"/>).
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void XInputService_StateChanged(object sender, DeviceStateChangedEventArgs e)
    {
        var state = e.NewState;

        List<AbstractMappedOption> mappedOptions = new();

        foreach (var stickAxisLookupKvp in this.stickAxisLookups)
        {
            var sideToLookup = stickAxisLookupKvp.Key;
            var lookup = stickAxisLookupKvp.Value;

            foreach (var lookupKvp in lookup)
            {
                var triggerHash = lookupKvp.Key;
                var lookupMappedOptions = lookupKvp.Value;

                foreach (var mappedOption in lookupMappedOptions)
                {
                    if (this.GetIsTriggered(mappedOption.Trigger))
                    {
                        mappedOptions.AddRange(lookupMappedOptions);
                    }
                }
            }
        }

        GamePadStickInputBag inputBag = new()
        {
            LeftStickDelta = state.Gamepad.GetStickDelta(GamePadSide.Left),
            RightStickDelta = state.Gamepad.GetStickDelta(GamePadSide.Right),
        };

        this.DoExecuteTrigger(
            mappedOptions,
            inputBag,
            this.GetIsTriggered
        );
    }
}
