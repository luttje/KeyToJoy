﻿using Key2Joy.Contracts.Mapping;
using System.Threading.Tasks;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Text.Json.Serialization;
using System.Windows.Forms;
using System.Diagnostics;

namespace Key2Joy.Plugin.Midi.Mapping
{
    [Action(
        Description = "Get Hello World",
        NameFormat = "Demonstrates plugin creation by greeting {0}",
        GroupName = "Logic",
        GroupImage = "application_xp_terminal"
    )]
    public class GetHelloWorldAction : AbstractAction
    {
        public string Target { get; set; } = "World";

        public GetHelloWorldAction(string name)
            : base(name)
        {

        }

        public override async Task Execute(AbstractInputBag inputBag = null)
        {
            MessageBox.Show($"Hello {Target}!");
            Debug.WriteLine($"Hello {Target}!");
            //Output.WriteLine(Output.OutputModes.Verbose, $"Hello {Target}!");
        }

        [ExposesScriptingMethod("Hello.World")]
        public static void ExecuteForScript(string target)
        {
            MessageBox.Show($"Hello {target}!");
        }

        public override string GetNameDisplay()
        {
            return Name.Replace("{0}", Target.ToString());
        }
    }
}
