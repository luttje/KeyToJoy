using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using Key2Joy.Contracts.Mapping;
using Key2Joy.Contracts.Mapping.Actions;
using Key2Joy.Contracts.Mapping.Triggers;
using Key2Joy.Gui.Mapping;
using Key2Joy.Mapping;
using Key2Joy.Plugins;

namespace Key2Joy.Gui;

public partial class MappingForm : Form
{
    private const string TEXT_CREATE_REVERSE = "Also create a child mapping that is a useful reverse of this";

    public MappedOption MappedOption { get; private set; } = null;
    public MappedOption MappedOptionReverse { get; private set; } = null;

    private bool dominantReverseCheckedState;

    public MappingForm()
    {
        this.InitializeComponent();
        this.DialogResult = DialogResult.Cancel;

        this.actionControl.IsTopLevel = true;
        this.triggerControl.IsTopLevel = true;

        FormClosed += (s, e) => this.Dispose();
    }

    public MappingForm(MappedOption mappedOption)
        : this()
    {
        // Don't get in the user's way: we only automatically check if we're initializing or making a new mapping
        this.dominantReverseCheckedState = mappedOption == null || mappedOption.Children.Any();
        this.RefreshCreateReverseMappingOption();

        this.triggerControl.TriggerChanged += this.TriggerControl_TriggerChanged;
        this.actionControl.ActionChanged += this.ActionControl_ActionChanged;

        if (mappedOption == null)
        {
            this.chkCreateOrUpdateReverseMapping.Text = TEXT_CREATE_REVERSE;
            return;
        }
        else if (!mappedOption.Children.Any())
        {
            this.chkCreateOrUpdateReverseMapping.Text = TEXT_CREATE_REVERSE;
        }

        this.MappedOption = mappedOption;

        this.triggerControl.SelectTrigger(mappedOption.Trigger);
        this.actionControl.SelectAction(mappedOption.Action);
    }

    /// <summary>
    /// When the user clicks we override the dominant state, so we dont get in their way.
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void ChkCreateOrUpdateReverseMapping_Click(object sender, EventArgs e)
        => this.dominantReverseCheckedState = this.chkCreateOrUpdateReverseMapping.Checked;

    private void ActionControl_ActionChanged(object sender, ActionChangedEventArgs e)
        => this.RefreshCreateReverseMappingOption();

    private void TriggerControl_TriggerChanged(object sender, TriggerChangedEventArgs e)
        => this.RefreshCreateReverseMappingOption();

    private void RefreshCreateReverseMappingOption()
    {
        if (this.triggerControl.Trigger is IProvideReverseAspect
            && this.actionControl.Action is IProvideReverseAspect)
        {
            this.chkCreateOrUpdateReverseMapping.Checked = this.dominantReverseCheckedState;
            this.chkCreateOrUpdateReverseMapping.Enabled = true;
        }
        else
        {
            this.chkCreateOrUpdateReverseMapping.Checked = false;
            this.chkCreateOrUpdateReverseMapping.Enabled = false;
        }
    }

    public static Control BuildOptionsForComboBox<TAttribute, TAspect>(ComboBox comboBox, Panel optionsPanel)
        where TAttribute : MappingAttribute
        where TAspect : AbstractMappingAspect
    {
        var optionsUserControl = optionsPanel.Controls.OfType<Control>().FirstOrDefault();

        if (optionsUserControl != null)
        {
            optionsPanel.Controls.Remove(optionsUserControl);
            optionsUserControl.Dispose();
        }

        if (comboBox.SelectedItem == null)
        {
            return null;
        }

        var selected = ((ImageComboBoxItem<KeyValuePair<TAttribute, MappingTypeFactory<TAspect>>>)comboBox.SelectedItem).ItemValue;
        var selectedTypeFactory = selected.Value;

        optionsUserControl = CreateOptionsControl(selectedTypeFactory.FullTypeName);

        if (optionsUserControl == null)
        {
            MessageBox.Show("Could not create options control for " + selectedTypeFactory.FullTypeName + ". \n\nPlease report this issue to the developer.\n\nThe app will now crash.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            throw new NotImplementedException("Could not create options control for " + selectedTypeFactory.FullTypeName);
        }

        if (optionsUserControl is ElementHostProxy pluginUserControl)
        {
            optionsUserControl = new ActionPluginHostControl(pluginUserControl);
        }

        optionsPanel.Controls.Add(optionsUserControl);
        optionsUserControl.Dock = DockStyle.Top;

        return optionsUserControl;
    }

    private static Control CreateOptionsControl(string selectedTypeName)
    {
        var mappingControlFactory = MappingControlRepository.GetMappingControlFactory(selectedTypeName);

        if (mappingControlFactory == null)
        {
            return null;
        }

        return mappingControlFactory.CreateInstance<Control>();
    }

    private void BtnSaveMapping_Click(object sender, EventArgs e)
    {
        var trigger = this.triggerControl.Trigger;
        var action = this.actionControl.Action;

        if (trigger == null)
        {
            MessageBox.Show("You must select a trigger!", "No trigger selected!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        if (action == null)
        {
            MessageBox.Show("You must select an action!", "No action selected!", MessageBoxButtons.OK, MessageBoxIcon.Error);
            return;
        }

        this.MappedOption ??= new MappedOption();
        this.MappedOption.Trigger = trigger;
        this.MappedOption.Action = action;

        if (!this.triggerControl.CanMappingSave(this.MappedOption))
        {
            return;
        }

        if (!this.actionControl.CanMappingSave(this.MappedOption))
        {
            return;
        }

        if (this.chkCreateOrUpdateReverseMapping.Checked)
        {
            var reverse = MappedOption.GenerateReverseMapping(this.MappedOption, true);

            if (!this.MappedOption.Children.Any())
            {
                this.MappedOptionReverse = reverse;
                this.MappedOptionReverse.SetParent(this.MappedOption);
            }
            else
            {
                // Update the existing reverse mapping
                var existingReverse = this.MappedOption.Children.First();
                existingReverse.Trigger = reverse.Trigger;
                existingReverse.Action = reverse.Action;

                if (this.MappedOption.Children.Count > 1)
                {
                    // TODO: What do we do if there's multiple children?
                    MessageBox.Show(
                        $"There are {this.MappedOption.Children.Count} children of this mapping. Only the first one was updated with the reverse mapping.",
                        "Multiple children",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning
                    );
                }
            }
        }

        this.DialogResult = DialogResult.OK;

        this.Close();
    }
}
