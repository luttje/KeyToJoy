using System.Collections.Generic;
using CommonServiceLocator;
using Key2Joy.Config;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.LowLevelInput;
using Key2Joy.Mapping.Triggers.Keyboard;

namespace Key2Joy.Mapping.Triggers.Mouse;

public class MouseButtonTriggerListener : PressReleaseTriggerListener<MouseButtonTrigger>, IOverrideDefaultBehavior
{
    public static MouseButtonTriggerListener instance;

    public static MouseButtonTriggerListener Instance
    {
        get
        {
            instance ??= new MouseButtonTriggerListener();

            return instance;
        }
    }

    /// <summary>
    /// Force a new instance to be created, used to reset the listener when mappings
    /// are re-armed.
    /// </summary>
    /// <returns></returns>
    public static MouseButtonTriggerListener NewInstance() => instance = new MouseButtonTriggerListener();

    private GlobalInputHook globalMouseButtonHook;
    private readonly Dictionary<LowLevelInput.Mouse.Buttons, bool> currentButtonsDown = new();

    /// <inheritdoc/>
    public bool ShouldListenerOverrideDefault(bool executedAny)
    {
        var configManager = ServiceLocator.Current.GetInstance<IConfigManager>();
        var config = configManager.GetConfigState();
        var listenerOverrideDefaultAll = config.ListenerOverrideDefaultMouseAll;

        if (listenerOverrideDefaultAll)
        {
            return true;
        }

        var listenerOverrideDefault = config.ListenerOverrideDefaultMouse;

        return listenerOverrideDefault && executedAny;
    }

    public bool GetButtonsDown(LowLevelInput.Mouse.Buttons buttons)
        => this.currentButtonsDown.ContainsKey(buttons);

    /// <inheritdoc/>
    protected override void Start()
    {
        // This captures global mouse input and blocks default behaviour by setting e.Handled
        this.globalMouseButtonHook = new GlobalInputHook();
        this.globalMouseButtonHook.MouseInputEvent += this.OnMouseInputEvent;

        base.Start();
    }

    /// <inheritdoc/>
    protected override void Stop()
    {
        instance = null;
        this.globalMouseButtonHook.MouseInputEvent -= this.OnMouseInputEvent;
        this.globalMouseButtonHook.Dispose();
        this.globalMouseButtonHook = null;

        base.Stop();
    }

    /// <inheritdoc/>
    public override bool GetIsTriggered(AbstractTrigger trigger)
    {
        if (trigger is not MouseButtonTrigger mouseButtonTrigger)
        {
            return false;
        }

        return this.currentButtonsDown.ContainsKey(mouseButtonTrigger.MouseButtons);
    }

    private void OnMouseInputEvent(object sender, GlobalMouseHookEventArgs e)
    {
        if (!this.IsActive)
        {
            return;
        }

        /// Mouse movement is handled through <see cref="MouseMoveTriggerListener"/>
        if (e.MouseState == MouseState.Move)
        {
            return;
        }

        var buttons = LowLevelInput.Mouse.ButtonsFromEvent(e, out var isDown);
        var dictionary = this.LookupRelease;

        if (isDown)
        {
            dictionary = this.LookupPress;

            if (this.currentButtonsDown.ContainsKey(buttons))
            {
                return; // Prevent firing multiple times for a single key press
            }
            else
            {
                this.currentButtonsDown.Add(buttons, true);
            }
        }
        else
        {
            if (this.currentButtonsDown.ContainsKey(buttons))
            {
                this.currentButtonsDown.Remove(buttons);
            }
        }

        MouseButtonInputBag inputBag = new()
        {
            State = e.MouseState,
            IsDown = isDown,
            LastX = e.RawData.Position.X,
            LastY = e.RawData.Position.Y,
        };

        var hash = MouseButtonTrigger.GetInputHashFor(buttons);
        dictionary.TryGetValue(hash, out var mappedOptions);

        if (this.DoExecuteTrigger(
            mappedOptions,
            inputBag,
            trigger =>
            {
                var mouseTrigger = trigger as MouseButtonTrigger;
                return mouseTrigger.GetInputHash() == hash
                    && mouseTrigger.MouseButtons == buttons;
            }))
        {
            e.Handled = true;
        }
    }
}
