﻿using KeyToJoy.Mapping;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace KeyToJoy
{
    public partial class MappingForm : Form
    {
        private bool isLoaded = false;
        
        public MappingForm()
        {
            InitializeComponent();

            LoadTriggers();
        }

        private void LoadTriggers()
        {
            var triggerTypes = Assembly.GetExecutingAssembly()
                .GetTypes()
                .Where(t => t.GetCustomAttribute(typeof(TriggerAttribute), false) != null)
                .ToDictionary(t => t, t => t.GetCustomAttribute(typeof(TriggerAttribute), false) as MappingAttribute);

            cmbTrigger.DataSource = new BindingSource(triggerTypes, null);
            cmbTrigger.DisplayMember = "Value";
            cmbTrigger.ValueMember = "Key";
            cmbTrigger.SelectedIndex = -1;

            isLoaded = true;
        }

        private void HandleComboBox(ComboBox comboBox, Panel optionsPanel)
        {
            optionsPanel.Controls.Clear();

            if (comboBox.SelectedItem == null)
                return;

            var selected = (KeyValuePair<Type, MappingAttribute>)comboBox.SelectedItem;
            var selectedType = selected.Key;
            var attribute = selected.Value;

            if (attribute.OptionsUserControl != null)
            {
                var optionsUserControl = (UserControl)Activator.CreateInstance(attribute.OptionsUserControl);
                optionsPanel.Controls.Add(optionsUserControl);
                optionsUserControl.Dock = DockStyle.Top;
            }

            PerformLayout();
        }

        private void cmbTrigger_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isLoaded)
                return;
            
            HandleComboBox(cmbTrigger, pnlTriggerOptions);
        }
    }
}
