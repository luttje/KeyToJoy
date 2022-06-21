﻿namespace KeyToJoy.Mapping
{
    partial class GamePadActionControl
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.cmbGamePad = new System.Windows.Forms.ComboBox();
            this.lblInfo = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // cmbGamePad
            // 
            this.cmbGamePad.Dock = System.Windows.Forms.DockStyle.Fill;
            this.cmbGamePad.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cmbGamePad.FormattingEnabled = true;
            this.cmbGamePad.Location = new System.Drawing.Point(100, 5);
            this.cmbGamePad.Name = "cmbGamePad";
            this.cmbGamePad.Size = new System.Drawing.Size(317, 21);
            this.cmbGamePad.TabIndex = 9;
            // 
            // lblInfo
            // 
            this.lblInfo.Dock = System.Windows.Forms.DockStyle.Left;
            this.lblInfo.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.lblInfo.Location = new System.Drawing.Point(5, 5);
            this.lblInfo.Name = "lblInfo";
            this.lblInfo.Size = new System.Drawing.Size(95, 18);
            this.lblInfo.TabIndex = 10;
            this.lblInfo.Text = "GamePad Button:";
            this.lblInfo.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // GamePadActionControl
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.Color.Black;
            this.Controls.Add(this.cmbGamePad);
            this.Controls.Add(this.lblInfo);
            this.Name = "GamePadActionControl";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.Size = new System.Drawing.Size(422, 28);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ComboBox cmbGamePad;
        private System.Windows.Forms.Label lblInfo;
    }
}
