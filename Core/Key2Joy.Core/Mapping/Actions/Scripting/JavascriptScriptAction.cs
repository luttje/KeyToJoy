﻿using Jint;
using Jint.Native;
using Jint.Native.Object;
using Jint.Runtime.Descriptors;
using Jint.Runtime.Interop;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Plugins;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Key2Joy.Mapping
{
    [Action(
        Description = "Javascript Action",
        NameFormat = "Javascript Script: {0}",
        GroupName = "Scripting",
        GroupImage = "script_code"
    )]
    public class JavascriptAction : BaseScriptActionWithEnvironment<Engine>
    {
        public JavascriptAction(string name)
            : base(name)
        {
            ImageResource = "JS";
        }

        public override async Task Execute(AbstractInputBag inputBag)
        {
            try
            {
                lock (LockObject)
                {
                    environment.Execute(GetExecutableScript());
                }
            }
            catch (Jint.Runtime.JavaScriptException e)
            {
                Output.WriteLine(e);
            }
        }

        public override void RegisterScriptingEnum(ExposedEnumeration enumeration)
        {
            StringBuilder enumInjectScript = new();
            enumInjectScript.Append(enumeration.Name + " = {");

            foreach (var kvp in enumeration.KeyValues)
            {
                var enumKey = kvp.Key;
                var enumValue = kvp.Value;

                enumInjectScript.Append(enumKey);
                enumInjectScript.Append(": ");
                enumInjectScript.Append(enumValue);

                enumInjectScript.Append(",\n");
            }

            enumInjectScript.Append("};");

            var enumInjection = enumInjectScript.ToString();
            environment.Execute(enumInjection);

            environment.Execute($"Print(JSON.stringify({enumInjection}))");
        }

        public override void RegisterScriptingMethod(ExposedMethod exposedMethod, AbstractAction instance)
        {
            var functionName = exposedMethod.FunctionName;
            var parents = functionName.Split('.');
            DelegateWrapper @delegate = new(environment, exposedMethod.CreateDelegate(instance));

            if (parents.Length > 1)
            {
                var currentObject = environment.Realm.GlobalObject;

                for (var i = 0; i < parents.Length; i++)
                {
                    if (i != parents.Length - 1)
                    {
                        if (!currentObject.TryGetValue(parents[i], out var child))
                        {
                            child = new JsObject(environment);
                        }

                        if (child is not ObjectInstance childObject)
                        {
                            throw new NotImplementedException($"Tried using a non object({parents[i]}) as object parent while registering function: {functionName}!");
                        }

                        currentObject.FastSetProperty(parents[i], new PropertyDescriptor(childObject, false, true, true));
                        currentObject = childObject;
                    }
                    else
                    {
                        currentObject.FastSetProperty(parents[i], new PropertyDescriptor(@delegate, false, true, true));
                    }
                }

                return;
            }

            environment.SetValue(
                functionName,
                @delegate);
        }

        public override Engine MakeEnvironment()
        {
            return new Engine();
        }

        public override void RegisterEnvironmentObjects()
        {
            environment.SetValue("Print", new Action<object[]>(Print));

            base.RegisterEnvironmentObjects();
        }

        public override void OnStartListening(AbstractTriggerListener listener, ref IList<AbstractAction> otherActions)
        {
            base.OnStartListening(listener, ref otherActions);
        }

        public override void OnStopListening(AbstractTriggerListener listener)
        {
            base.OnStopListening(listener);

            environment = null;
        }

        public override bool Equals(object obj)
        {
            if (obj is not JavascriptAction action)
            {
                return false;
            }

            return action.Name == Name
                && action.Script == Script;
        }
    }
}
