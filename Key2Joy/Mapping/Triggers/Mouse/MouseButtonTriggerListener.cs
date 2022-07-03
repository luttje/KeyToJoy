﻿using Key2Joy.LowLevelInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Key2Joy.Mapping
{
    internal class MouseButtonTriggerListener : TriggerListener
    {
        internal static MouseButtonTriggerListener instance;
        internal static MouseButtonTriggerListener Instance
        {
            get
            {
                if (instance == null)
                    instance = new MouseButtonTriggerListener();
                
                return instance;
            }
        }

        private GlobalInputHook globalMouseButtonHook;
        private Dictionary<Mouse.Buttons, MappedOption> lookupDown;
        private Dictionary<Mouse.Buttons, MappedOption> lookupRelease;
        
        private MouseButtonTriggerListener()
        {
            lookupDown = new Dictionary<Mouse.Buttons, MappedOption>();
            lookupRelease = new Dictionary<Mouse.Buttons, MappedOption>();
        }

        protected override void Start()
        {
            // This captures global mouse input and blocks default behaviour by setting e.Handled
            globalMouseButtonHook = new GlobalInputHook();
            globalMouseButtonHook.MouseInputEvent += OnMouseButtonInputEvent;

            base.Start();
        }

        protected override void Stop()
        {
            instance = null;
            globalMouseButtonHook.MouseInputEvent -= OnMouseButtonInputEvent;
            globalMouseButtonHook.Dispose();
            globalMouseButtonHook = null;

            base.Stop();
        }

        internal override void AddMappedOption(MappedOption mappedOption)
        {
            var trigger = mappedOption.Trigger as MouseButtonTrigger;

            if (trigger.PressState == PressState.Press)
                lookupDown.Add(trigger.MouseButtons, mappedOption);
            if (trigger.PressState == PressState.Release)
                lookupRelease.Add(trigger.MouseButtons, mappedOption);
        }

        private void OnMouseButtonInputEvent(object sender, GlobalMouseHookEventArgs e)
        {
            if (!IsActive)
                return;

            // Mouse movement is handled through WndProc and TryOverrideMouseMoveInput in MouseMoveTriggerListener
            if (e.MouseState == MouseState.Move)
                return;

            var buttons = Mouse.Buttons.None;
            var isDown = false;
                
            try
            {
                buttons = Mouse.ButtonsFromState(e.MouseState, out isDown);
            }
            catch (NotImplementedException) { }

            var dictionary = lookupRelease;

            if (isDown)
                dictionary = lookupDown;
                
            // Test if this is a bound mouse button, if so halt default input behaviour
            if (!dictionary.TryGetValue(buttons, out var mappedOption))
                return;
                
            if (!TryOverrideMouseButtonInput(mappedOption.Action, new MouseButtonInputBag
            {
                Trigger = mappedOption.Trigger,
                State = e.MouseState,
                IsDown = isDown,
                LastX = e.MouseData.Position.X,
                LastY = e.MouseData.Position.Y,
            }))
                return;

            e.Handled = true;
        }
        
        private bool TryOverrideMouseButtonInput(BaseAction action, MouseButtonInputBag inputBag)
        {
            action.Execute(inputBag);

            return true;
        }
    }
}
