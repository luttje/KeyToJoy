﻿using Linearstar.Windows.RawInput;
using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Linearstar.Windows.RawInput.Native;
using Key2Joy.Mapping;
using Key2Joy.LowLevelInput;

namespace Key2Joy
{
    public partial class KeyboardTriggerOptionsControl : UserControl, ITriggerOptionsControl
    {
        const string TEXT_CHANGE = "(press any key to select it as the trigger)";
        const string TEXT_CHANGE_INSTRUCTION = "(click here, then press any key to set it as the trigger)";
        const int WM_INPUT = 0x00FF;

        public event EventHandler OptionsChanged;
        
        private Keys keys;
        private bool isTrapping;

        public KeyboardTriggerOptionsControl()
        {
            InitializeComponent();

            // This captures global keyboard input and blocks default behaviour by setting e.Handled
            var globalKeyboardHook = new GlobalInputHook();
            globalKeyboardHook.KeyboardInputEvent += OnKeyInputEvent;

            // Relieve input capturing by this mapping form
            Disposed += (s, e) =>
            {
                globalKeyboardHook.KeyboardInputEvent -= OnKeyInputEvent;
                globalKeyboardHook.Dispose();
                globalKeyboardHook = null;
            };
            ControlRemoved += (s, e) => Dispose();

            cmbPressState.DataSource = LegacyPressStateConverter.GetPressStatesWithoutLegacy();
            cmbPressState.SelectedIndex = 0;

            // Removed because this is annoying when you want to just edit code
            //StartTrapping();
        }

        private void OnKeyInputEvent(object sender, GlobalKeyboardHookEventArgs e)
        {
            if (!isTrapping)
                return;

            keys = VirtualKeyConverter.KeysFromVirtual(e.KeyboardData.VirtualCode);
            UpdateKeys();
            StopTrapping();
        }

        public void Select(BaseTrigger trigger)
        {
            var thisTrigger = (KeyboardTrigger)trigger;

            this.keys = thisTrigger.Keys;
            cmbPressState.SelectedItem = thisTrigger.PressState;
            UpdateKeys();
        }

        public void Setup(BaseTrigger trigger)
        {
            var thisTrigger = (KeyboardTrigger)trigger;

            thisTrigger.Keys = this.keys;
            thisTrigger.PressState = (PressState) cmbPressState.SelectedItem;
        }

        private void StartTrapping()
        {
            txtKeyBind.Text = TEXT_CHANGE;
            txtKeyBind.Focus();
            isTrapping = true;
        }
        
        private void StopTrapping()
        {
            isTrapping = false;
        }

        private void ResetTrapping()
        {
            txtKeyBind.Text = TEXT_CHANGE_INSTRUCTION;
            StopTrapping();
        }

        private void UpdateKeys()
        {
            txtKeyBind.Text = $"{keys} {TEXT_CHANGE_INSTRUCTION}";
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void cmbPressedState_SelectedIndexChanged(object sender, EventArgs e)
        {
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void txtKeyBind_MouseUp(object sender, MouseEventArgs e)
        {
            StartTrapping();
        }
    }
}
