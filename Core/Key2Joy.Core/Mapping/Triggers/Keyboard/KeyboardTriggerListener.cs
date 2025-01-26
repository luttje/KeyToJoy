using System.Collections.Generic;
using System.Windows.Forms;
using CommonServiceLocator;
using Key2Joy.Config;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.LowLevelInput;

namespace Key2Joy.Mapping.Triggers.Keyboard;

public class KeyboardTriggerListener : PressReleaseTriggerListener<KeyboardTrigger>, IOverrideDefaultBehavior
{
    private static KeyboardTriggerListener instance;

    public static KeyboardTriggerListener Instance
    {
        get
        {
            instance ??= new KeyboardTriggerListener();

            return instance;
        }
    }

    /// <summary>
    /// Force a new instance to be created, used to reset the listener when mappings
    /// are re-armed.
    /// </summary>
    /// <returns></returns>
    public static KeyboardTriggerListener NewInstance() => instance = new KeyboardTriggerListener();

    private GlobalInputHook globalKeyboardHook;
    private readonly VirtualKeyConverter virtualKeyConverter = new();
    private readonly Dictionary<Keys, bool> currentKeysPressed = new();

    public bool GetKeyDown(Keys key) => this.currentKeysPressed.ContainsKey(key);

    /// <inheritdoc/>
    public bool ShouldListenerOverrideDefault(bool executedAny)
    {
        var configManager = ServiceLocator.Current.GetInstance<IConfigManager>();
        var config = configManager.GetConfigState();
        var listenerOverrideDefaultAll = config.ListenerOverrideDefaultKeyboardAll;

        if (listenerOverrideDefaultAll)
        {
            return true;
        }

        var listenerOverrideDefault = config.ListenerOverrideDefaultKeyboard;

        return listenerOverrideDefault && executedAny;
    }

    /// <inheritdoc/>
    protected override void Start()
    {
        // This captures global keyboard input and blocks default behaviour by setting e.Handled
        this.globalKeyboardHook = new GlobalInputHook();
        this.globalKeyboardHook.KeyboardInputEvent += this.OnKeyInputEvent;
        this.currentKeysPressed.Clear();

        base.Start();
    }

    /// <inheritdoc/>
    protected override void Stop()
    {
        instance = null;
        this.globalKeyboardHook.KeyboardInputEvent -= this.OnKeyInputEvent;
        this.globalKeyboardHook.Dispose();
        this.globalKeyboardHook = null;
        this.currentKeysPressed.Clear();

        base.Stop();
    }

    /// <inheritdoc/>
    public override bool GetIsTriggered(AbstractTrigger trigger)
    {
        if (trigger is not KeyboardTrigger keyboardTrigger)
        {
            return false;
        }

        return this.currentKeysPressed.ContainsKey(keyboardTrigger.Keys);
    }

    private void OnKeyInputEvent(object sender, GlobalKeyboardHookEventArgs e)
    {
        if (!this.IsActive)
        {
            return;
        }

        // Test if this is a bound key, if so halt default input behaviour
        var keys = this.virtualKeyConverter.KeysFromVirtual(e.KeyboardData.VirtualCode);
        Dictionary<int, List<AbstractMappedOption>> dictionary;

        if (e.KeyboardState == KeyboardState.KeyDown)
        {
            dictionary = this.LookupPress;

            if (this.currentKeysPressed.ContainsKey(keys))
            {
                return; // Prevent firing multiple times for a single key press
            }
            else
            {
                this.currentKeysPressed.Add(keys, true);
            }
        }
        else
        {
            dictionary = this.LookupRelease;

            if (this.currentKeysPressed.ContainsKey(keys))
            {
                this.currentKeysPressed.Remove(keys);
            }
        }

        KeyboardInputBag inputBag = new()
        {
            State = e.KeyboardState,
            Keys = keys
        };

        var hash = KeyboardTrigger.GetInputHashFor(keys);
        dictionary.TryGetValue(hash, out var mappedOptions);

        if (this.DoExecuteTrigger(
            mappedOptions,
            inputBag,
            trigger =>
            {
                var keyboardTrigger = trigger as KeyboardTrigger;
                return keyboardTrigger.GetInputHash() == hash
                    && keyboardTrigger.GetKeyboardState() == e.KeyboardState;
            }))
        {
            e.Handled = true;
        }
    }
}
