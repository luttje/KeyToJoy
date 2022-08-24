﻿using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Key2Joy.Mapping
{
    [Action(
        Description = "Repeatedly calls a function or executes a code snippet, with a fixed time delay between each call",
        Visibility = MappingMenuVisibility.Never
    )]
    [Util.ObjectListViewGroup(
        Name = "Logic",
        Image = "script_code"
    )]
    internal class SetIntervalAction : BaseAction
    {
        public delegate void CallbackAction(params object[] arguments);
            
        [JsonProperty]
        public TimeSpan WaitTime;

        public SetIntervalAction(string name, string description)
            : base(name, description)
        {
        }

        /// <markdown-doc>
        /// <parent-name>Logic</parent-name>
        /// <path>Api/Logic</path>
        /// </markdown-doc>
        /// <summary>
        /// Repeatedly calls a function or executes a code snippet, with a fixed time delay between each call
        /// </summary>
        /// <markdown-example>
        /// Shows how to count up to 10 every second and then stop by using ClearInterval();
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
        ///           App.Command("abort")
        ///         }, 1000)
        ///       }, 1000)
        ///     }, 1000)
        ///   }, 1000)
        /// }, 1000)
        /// ]]>
        /// </code>
        /// </markdown-example>
        /// <param name="callback">Function to execute after each wait</param>
        /// <param name="waitTime">Time to wait (in milliseconds)</param>
        /// <param name="arguments">Zero or more extra parameters to pass to the function</param>
        /// <name>SetInterval</name>
        [ExposesScriptingMethod("SetInterval")]
        [ExposesScriptingMethod("setInterval")] // Alias to conform to JS standard
        public IdPool.IntervalId ExecuteForScript(CallbackAction callback, long waitTime, params object[] arguments)
        {
            WaitTime = TimeSpan.FromMilliseconds(waitTime);

            var cancellation = new CancellationTokenSource();
            var token = cancellation.Token;
            Task.Run(async () =>
            {
                while (true)
                {
                    token.ThrowIfCancellationRequested();

                    await Task.Delay(WaitTime);

                    token.ThrowIfCancellationRequested();

                    callback.Invoke(arguments);
                }
            }, token);

            return IdPool.CreateNewId<IdPool.IntervalId>(cancellation);
        }

        internal override Task Execute(IInputBag inputBag = null)
        {
            // Irrelevant because only scripts should use this function
            return Task.Delay(WaitTime);
        }

        public override string GetNameDisplay()
        {
            // Irrelevant because only scripts should use this function
            return Name.Replace("{0}", WaitTime.TotalMilliseconds.ToString());
        }

        public override object Clone()
        {
            return new SetIntervalAction(Name, description)
            {
                WaitTime = WaitTime,
                ImageResource = ImageResource,
                Name = Name,
            };
        }
    }
}
