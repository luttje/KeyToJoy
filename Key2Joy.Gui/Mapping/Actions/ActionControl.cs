﻿using Key2Joy.Mapping;
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

namespace Key2Joy.Gui.Mapping
{
    public partial class ActionControl : UserControl
    {
        public BaseAction Action { get; private set; }
        public event Action<BaseAction> ActionChanged;
        public bool IsTopLevel { get; set; }

        private bool isLoaded = false;
        private IActionOptionsControl options;
        private BaseAction selectedAction = null;

        public ActionControl()
        {
            InitializeComponent();
        }
        
        private void BuildAction()
        {
            if (cmbAction.SelectedItem == null) 
            {
                ActionChanged?.Invoke(null);
                return;
            }

            var selected = (ImageComboBoxItem<KeyValuePair<ActionAttribute, Type>>)cmbAction.SelectedItem;
            var selectedType = selected.ItemValue.Value;
            var attribute = selected.ItemValue.Key;

            if (Action == null || Action.GetType() != selectedType)
                Action = BaseAction.MakeAction(selectedType, attribute);

            if (options != null)
                options.Setup(Action);

            ActionChanged?.Invoke(Action);
        }

        public bool CanMappingSave(MappedOption mappedOption)
        {
            if (options != null)
                return options.CanMappingSave(mappedOption.Action);

            return false;
        }

        public void SelectAction(BaseAction action)
        {
            selectedAction = action;
            
            if (!isLoaded)
                return;

            var selected = cmbAction.Items.Cast<ImageComboBoxItem<KeyValuePair<ActionAttribute, Type>>>();
            var selectedType = selected.FirstOrDefault(x => x.ItemValue.Value == action.GetType());
            cmbAction.SelectedItem = selectedType;
        }

        private void LoadActions()
        {
            var actionTypes = ActionAttribute.GetAllActions(IsTopLevel);

            foreach (var keyValuePair in actionTypes)
            {
                var control = MappingControlAttribute.GetCorrespondingControlType(keyValuePair.Value, out _);
                var customImage = control?.GetCustomAttribute<MappingControlAttribute>()?.ImageResourceName;
                var image = Program.ResourceBitmapFromName(customImage ?? "error");
                var item = new ImageComboBoxItem<KeyValuePair<ActionAttribute, Type>>(keyValuePair, new Bitmap(image), "Key");

                cmbAction.Items.Add(item);
            }

            cmbAction.SelectedIndex = -1;

            isLoaded = true;

            if(selectedAction != null)
                SelectAction(selectedAction);
        }
        
        private void cmbAction_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (!isLoaded)
                return;
            
            var options = MappingForm.BuildOptionsForComboBox<ActionAttribute>(cmbAction, pnlActionOptions);

            if (options != null)
            {
                this.options = options as IActionOptionsControl;

                if (this.options != null)
                {
                    if(selectedAction != null)
                        this.options.Select(selectedAction);

                    this.options.OptionsChanged += (s, _) => BuildAction();
                }
            }
            
            BuildAction();
            
            selectedAction = null;
            PerformLayout();
        }

        private void ActionControl_Load(object sender, EventArgs e)
        {
            LoadActions();
        }
    }
}
