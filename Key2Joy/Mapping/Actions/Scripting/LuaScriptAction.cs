﻿using Key2Joy.Contracts.Mapping;
using Key2Joy.Plugins;
using NLua;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Key2Joy.Mapping
{
    [Action(
        Description = "Lua Script Action",
        NameFormat = "Lua Script: {0}",
        GroupName = "Scripting",
        GroupImage = "script_code"
    )]
    public class LuaScriptAction : BaseScriptActionWithEnvironment<Lua>
    {
        public LuaScriptAction(string name)
            : base(name)
        {
            ImageResource = "Lua";
        }

        public override async Task Execute(AbstractInputBag inputBag)
        {
            try
            {
                var source = "Key2Joy.Script.Inline";
                if (IsScriptPath)
                    source = Script;

                lock (LockObject)
                    environment.DoString(GetExecutableScript(), Script);
            }
            catch (NLua.Exceptions.LuaScriptException e)
            {
                Output.WriteLine(e);
            }
        }
        
        public override void RegisterScriptingEnum(ExposedEnumeration enumeration)
        {
            environment.NewTable(enumeration.Name);

            foreach (var kvp in enumeration.KeyValues)
            {
                var enumKey = kvp.Key;
                var enumValue = kvp.Value;

                string path = enumeration.Name + "." + enumKey;
                environment.SetObjectToPath(path, enumValue);
            }
        }

        public override void RegisterScriptingMethod(ExposedMethod exposedMethod, AbstractAction instance)
        {
            var functionName = exposedMethod.FunctionName;
            var parents = functionName.Split('.');

            if (parents.Length > 1)
            {
                var currentPath = new StringBuilder();
                
                for (int i = 0; i < parents.Length - 1; i++)
                {
                    if (i > 0)
                        currentPath.Append('.');

                    currentPath.Append(parents[i]);
                }

                var path = currentPath.ToString();

                if(environment.GetTable(path) == null)
                    environment.NewTable(path);
            }
            
            environment.RegisterFunction(
                functionName,
                instance,
                instance.GetType().GetMethod(exposedMethod.MethodName));
        }

        public override Lua MakeEnvironment()
        {
            return new Lua();
        }

        public override void RegisterEnvironmentObjects()
        {
            environment.RegisterFunction("print", this, typeof(LuaScriptAction).GetMethod(nameof(Print), new[] { typeof(object[]) }));
            environment.RegisterFunction("Print", this, typeof(LuaScriptAction).GetMethod(nameof(Print), new[] { typeof(object[]) }));

            base.RegisterEnvironmentObjects();
        }

        public override void OnStartListening(AbstractTriggerListener listener, ref IList<AbstractAction> otherActions)
        {
            base.OnStartListening(listener, ref otherActions);
        }

        public override void OnStopListening(AbstractTriggerListener listener)
        {
            base.OnStopListening(listener);

            environment.Dispose();
        }

        public override bool Equals(object obj)
        {
            if (!(obj is LuaScriptAction action))
                return false;

            return action.Name == Name
                && action.Script == Script;
        }
    }
}
