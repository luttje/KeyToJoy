using System;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.Contracts.Plugins;
using Key2Joy.Mapping.Actions.Scripting;

namespace Key2Joy.Mapping.Actions.Logic;

[Action(
    Description = "Sets a timer which executes a function or specified piece of code once the timer expires",
    Visibility = MappingMenuVisibility.Never,
    GroupName = "Logic",
    GroupImage = "application_xp_terminal"
)]
public class SetTimeoutAction : CoreAction
{
    [JsonInclude]
    public TimeSpan WaitTime;

    public SetTimeoutAction(string name)
        : base(name)
    {
    }

    /// <markdown-doc>
    /// <parent-name>Logic</parent-name>
    /// <path>Api/Logic</path>
    /// </markdown-doc>
    /// <summary>
    /// Timeout for the specified duration in milliseconds, then execute the callback
    /// </summary>
    /// <markdown-example>
    /// Shows how to count down from 3 and execute a command using Javascript.
    /// <code language="js">
    /// <![CDATA[
    /// setTimeout(function () {
    ///   Print("Aborting in 3 second...")
    ///
    ///   setTimeout(function () {
    ///     Print("Three")
    ///
    ///     setTimeout(function () {
    ///       Print("Two")
    ///
    ///       setTimeout(function () {
    ///         Print("One")
    ///
    ///         setTimeout(function () {
    ///           App.Command("Abort")
    ///         }, 1000)
    ///       }, 1000)
    ///     }, 1000)
    ///   }, 1000)
    /// }, 1000)
    /// ]]>
    /// </code>
    /// </markdown-example>
    /// <markdown-example>
    /// Shows how to count down from 3 each second and execute a command using Lua.
    /// <code language="lua">
    /// <![CDATA[
    /// SetTimeout(function ()
    ///    Print("Aborting in 3 second...")
    ///
    ///    SetTimeout(function ()
    ///       Print("Three")
    ///
    ///       SetTimeout(function ()
    ///          Print("Two")
    ///
    ///          SetTimeout(function ()
    ///             Print("One")
    ///
    ///             SetTimeout(function ()
    ///                App.Command("Abort")
    ///             end, 1000)
    ///          end, 1000)
    ///       end, 1000)
    ///    end, 1000)
    /// end, 1000)
    /// ]]>
    /// </code>
    /// </markdown-example>
    /// <param name="callback">Function to execute after the wait</param>
    /// <param name="waitTime">Time to wait (in milliseconds)</param>
    /// <param name="arguments">Zero or more extra parameters to pass to the function</param>
    /// <returns>A timeout id that can be removed with clearTimeout</returns>
    /// <name>SetTimeout</name>
    [ExposesScriptingMethod("SetTimeout")]
    [ExposesScriptingMethod("setTimeout")] // Alias to conform to JS standard
    public IdPool.TimeoutId ExecuteForScript(CallbackAction callback, long waitTime, params object[] arguments)
    {
        this.WaitTime = TimeSpan.FromMilliseconds(waitTime);

        CancellationTokenSource cancellation = new();
        var token = cancellation.Token;

        Task.Run(async () =>
        {
            token.ThrowIfCancellationRequested();

            await Task.Delay(this.WaitTime);

            token.ThrowIfCancellationRequested();

            lock (BaseScriptAction.LockObject)
            {
                callback.Invoke(arguments);
            }
        }, token);

        return IdPool.CreateNewId<IdPool.TimeoutId>(cancellation);
    }

    public override Task Execute(AbstractInputBag inputBag = null) =>
        // Irrelevant because only scripts should use this function
        Task.Delay(this.WaitTime);

    public override string GetNameDisplay() =>
        // Irrelevant because only scripts should use this function
        this.Name.Replace("{0}", this.WaitTime.TotalMilliseconds.ToString());
}
