using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using CommonServiceLocator;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.LowLevelInput.SimulatedGamePad;

namespace Key2Joy.Mapping.Actions.Input;

[Action(
    Description = "GamePad Reset Simulation",
    Visibility = MappingMenuVisibility.Never,
    NameFormat = "Reset GamePad #{0}",
    GroupName = "GamePad Simulation",
    GroupImage = "joystick"
)]
public class GamePadResetAction : CoreAction
{
    public int GamePadIndex { get; set; }

    public GamePadResetAction(string name)
        : base(name)
    { }

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
    /// Reset the gamepad so the stick returns to the resting position (0,0)
    /// </summary>
    /// <markdown-example>
    /// Moves the left gamepad joystick halfway down and to the right, then resets after 500ms
    /// <code language="lua">
    /// <![CDATA[
    /// GamePad.SimulateMove(0.5,0.5)
    /// SetTimeout(function()
    ///    GamePad.Reset()
    /// end, 500)
    /// ]]>
    /// </code>
    /// </markdown-example>
    /// <param name="gamepadIndex">Which of 4 possible gamepads to simulate: 0 (default), 1, 2 or 3</param>
    /// <name>GamePad.Reset</name>
    [ExposesScriptingMethod("GamePad.Reset")]
    public async void ExecuteForScript(int gamepadIndex = 0)
    {
        this.GamePadIndex = gamepadIndex;

        var gamePadService = ServiceLocator.Current.GetInstance<ISimulatedGamePadService>();
        var gamePad = gamePadService.GetGamePad(this.GamePadIndex);

        if (!gamePad.GetIsPluggedIn())
        {
            gamePad.PlugIn();
        }

        gamePad.ResetState();
    }

    public override async Task Execute(AbstractInputBag inputBag = null)
    {
        var gamePadService = ServiceLocator.Current.GetInstance<ISimulatedGamePadService>();
        var gamePad = gamePadService.GetGamePad(this.GamePadIndex);

        gamePad.ResetState();
    }

    public override string GetNameDisplay() => this.Name.Replace("{0}", this.GamePadIndex.ToString());

    public override bool Equals(object obj)
    {
        if (obj is not GamePadResetAction)
        {
            return false;
        }

        return true;
    }
}
