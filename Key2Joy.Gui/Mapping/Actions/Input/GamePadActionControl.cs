﻿using Key2Joy.Contracts.Mapping;
using Key2Joy.LowLevelInput;
using Key2Joy.Mapping;
using System;
using System.Windows.Forms;

namespace Key2Joy.Gui.Mapping
{
    [MappingControl(
        ForType = typeof(Key2Joy.Mapping.GamePadAction),
        ImageResourceName = "joystick"
    )]
    public partial class GamePadActionControl : UserControl, IActionOptionsControl
    {
        public event EventHandler OptionsChanged;

        public GamePadActionControl()
        {
            InitializeComponent();

            cmbGamePad.DataSource = GamePadAction.GetAllButtons();
            cmbPressState.DataSource = PressStates.ALL;
            cmbPressState.SelectedIndex = 0;
            cmbGamePadIndex.DataSource = GamePadManager.Instance.GetAllGamePadIndices();
            cmbGamePadIndex.SelectedIndex = 0;
        }

        public void Select(object action)
        {
            var thisAction = (GamePadAction)action;

            cmbGamePad.SelectedItem = thisAction.Control;
            cmbPressState.SelectedItem = thisAction.PressState;
            cmbGamePadIndex.SelectedItem = thisAction.GamePadIndex;
        }

        public void Setup(object action)
        {
            var thisAction = (GamePadAction)action;

            thisAction.Control = (SimWinInput.GamePadControl)cmbGamePad.SelectedItem;
            thisAction.PressState = (PressState)cmbPressState.SelectedItem;
            thisAction.GamePadIndex = (int)cmbGamePadIndex.SelectedItem;
        }

        public bool CanMappingSave(object action)
        {
            return true;
        }

        private void CmbGamePad_SelectedIndexChanged(object sender, EventArgs e)
        {
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CmbPressState_SelectedIndexChanged(object sender, EventArgs e)
        {
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }

        private void CmbGamePadIndex_SelectedIndexChanged(object sender, EventArgs e)
        {
            OptionsChanged?.Invoke(this, EventArgs.Empty);
        }
    }
}
