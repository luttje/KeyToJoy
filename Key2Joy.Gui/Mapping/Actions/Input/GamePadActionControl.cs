﻿using System;
using System.Windows.Forms;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.LowLevelInput;
using Key2Joy.Mapping.Actions.Input;

namespace Key2Joy.Gui.Mapping;

[MappingControl(
    ForType = typeof(GamePadAction),
    ImageResourceName = "joystick"
)]
public partial class GamePadActionControl : UserControl, IActionOptionsControl
{
    public event EventHandler OptionsChanged;

    public GamePadActionControl()
    {
        this.InitializeComponent();

        this.cmbGamePad.DataSource = GamePadAction.GetAllButtons();
        this.cmbPressState.DataSource = PressStates.ALL;
        this.cmbPressState.SelectedIndex = 0;
        this.cmbGamePadIndex.DataSource = GamePadManager.Instance.GetAllGamePadIndices();
        this.cmbGamePadIndex.SelectedIndex = 0;
    }

    public void Select(object action)
    {
        var thisAction = (GamePadAction)action;

        this.cmbGamePad.SelectedItem = thisAction.Control;
        this.cmbPressState.SelectedItem = thisAction.PressState;
        this.cmbGamePadIndex.SelectedItem = thisAction.GamePadIndex;
    }

    public void Setup(object action)
    {
        var thisAction = (GamePadAction)action;

        thisAction.Control = (SimWinInput.GamePadControl)this.cmbGamePad.SelectedItem;
        thisAction.PressState = (PressState)this.cmbPressState.SelectedItem;
        thisAction.GamePadIndex = (int)this.cmbGamePadIndex.SelectedItem;
    }

    public bool CanMappingSave(object action) => true;

    private void CmbGamePad_SelectedIndexChanged(object sender, EventArgs e) => OptionsChanged?.Invoke(this, EventArgs.Empty);

    private void CmbPressState_SelectedIndexChanged(object sender, EventArgs e) => OptionsChanged?.Invoke(this, EventArgs.Empty);

    private void CmbGamePadIndex_SelectedIndexChanged(object sender, EventArgs e) => OptionsChanged?.Invoke(this, EventArgs.Empty);
}
