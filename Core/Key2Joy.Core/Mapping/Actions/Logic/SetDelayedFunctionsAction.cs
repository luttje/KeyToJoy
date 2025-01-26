using System;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;

namespace Key2Joy.Mapping.Actions.Logic;

[Action(
    Description = "Timeout for a specified duration before executing a function",
    Visibility = MappingMenuVisibility.Never,
    NameFormat = "Timeout for {0}ms",
    GroupName = "Logic",
    GroupImage = "application_xp_terminal"
)]
public class SetDelayedFunctionsAction : CoreAction
{
    [JsonInclude]
    public TimeSpan WaitTime;

    public SetDelayedFunctionsAction(string name)
        : base(name)
    {
    }

    /// <markdown-doc>
    /// <parent-name>Logic</parent-name>
    /// <path>Api/Logic</path>
    /// </markdown-doc>
    /// <summary>
    /// Execute functions whilst waiting the specified time between them.
    ///
    /// The first function is executed immediately.
    /// </summary>
    /// <markdown-example>
    /// Shows how to count down from 3 and execute a command using Lua.
    /// <code language="lua">
    /// <![CDATA[
    /// SetDelayedFunctions(
    ///    1000,
    ///    function ()
    ///       Print("Aborting in 3 second...")
    ///    end,
    ///    function ()
    ///       Print("Three")
    ///    end,
    ///    function ()
    ///       Print("Two")
    ///    end,
    ///    function ()
    ///       Print("One")
    ///    end,
    ///    function ()
    ///       App.Command("Abort")
    ///    end
    /// )
    /// ]]>
    /// </code>
    /// </markdown-example>
    /// <param name="waitTime">Time to wait (in milliseconds) between function calls</param>
    /// <param name="callbacks">One or more functions to execute</param>
    /// <name>SetDelayedFunctions</name>
    [ExposesScriptingMethod("SetDelayedFunctions")]
    public async void ExecuteForScript(long waitTime, params Action[] callbacks)
    {
        this.WaitTime = TimeSpan.FromMilliseconds(waitTime);

        for (var i = 0; i < callbacks.Length; i++)
        {
            if (i > 0)
            {
                await Task.Delay(this.WaitTime);
            }

            callbacks[i].Invoke();
        }
    }

    public override Task Execute(AbstractInputBag inputBag = null) =>
        // Irrelevant because only scripts should use this function
        Task.Delay(this.WaitTime);

    public override string GetNameDisplay() =>
        // Irrelevant because only scripts should use this function
        this.Name.Replace("{0}", this.WaitTime.TotalMilliseconds.ToString());
}
