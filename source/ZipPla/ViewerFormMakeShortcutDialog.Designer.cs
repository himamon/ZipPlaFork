namespace ZipPla
{
    partial class ViewerFormMakeShortcutDialog
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

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ViewerFormMakeShortcutDialog));
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbStartInPreviousImageFilterSetting = new System.Windows.Forms.CheckBox();
            this.rbStartInPreviousFullscreenMode = new System.Windows.Forms.RadioButton();
            this.rbAlwaysStartInWindowMode = new System.Windows.Forms.RadioButton();
            this.rbAlwaysStartInFullscreenMode = new System.Windows.Forms.RadioButton();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(82, 116);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "Save";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(163, 116);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cbStartInPreviousImageFilterSetting
            // 
            this.cbStartInPreviousImageFilterSetting.AutoSize = true;
            this.cbStartInPreviousImageFilterSetting.Checked = true;
            this.cbStartInPreviousImageFilterSetting.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbStartInPreviousImageFilterSetting.Location = new System.Drawing.Point(12, 12);
            this.cbStartInPreviousImageFilterSetting.Name = "cbStartInPreviousImageFilterSetting";
            this.cbStartInPreviousImageFilterSetting.Size = new System.Drawing.Size(210, 16);
            this.cbStartInPreviousImageFilterSetting.TabIndex = 3;
            this.cbStartInPreviousImageFilterSetting.Text = "Start in previous image filter setting";
            this.cbStartInPreviousImageFilterSetting.UseVisualStyleBackColor = true;
            // 
            // rbStartInPreviousFullscreenMode
            // 
            this.rbStartInPreviousFullscreenMode.AutoSize = true;
            this.rbStartInPreviousFullscreenMode.Location = new System.Drawing.Point(12, 43);
            this.rbStartInPreviousFullscreenMode.Name = "rbStartInPreviousFullscreenMode";
            this.rbStartInPreviousFullscreenMode.Size = new System.Drawing.Size(193, 16);
            this.rbStartInPreviousFullscreenMode.TabIndex = 4;
            this.rbStartInPreviousFullscreenMode.TabStop = true;
            this.rbStartInPreviousFullscreenMode.Text = "Start in previous fullscreen mode";
            this.rbStartInPreviousFullscreenMode.UseVisualStyleBackColor = true;
            // 
            // rbAlwaysStartInWindowMode
            // 
            this.rbAlwaysStartInWindowMode.AutoSize = true;
            this.rbAlwaysStartInWindowMode.Location = new System.Drawing.Point(12, 65);
            this.rbAlwaysStartInWindowMode.Name = "rbAlwaysStartInWindowMode";
            this.rbAlwaysStartInWindowMode.Size = new System.Drawing.Size(173, 16);
            this.rbAlwaysStartInWindowMode.TabIndex = 5;
            this.rbAlwaysStartInWindowMode.TabStop = true;
            this.rbAlwaysStartInWindowMode.Text = "Always start in window mode";
            this.rbAlwaysStartInWindowMode.UseVisualStyleBackColor = true;
            // 
            // rbAlwaysStartInFullscreenMode
            // 
            this.rbAlwaysStartInFullscreenMode.AutoSize = true;
            this.rbAlwaysStartInFullscreenMode.Location = new System.Drawing.Point(12, 87);
            this.rbAlwaysStartInFullscreenMode.Name = "rbAlwaysStartInFullscreenMode";
            this.rbAlwaysStartInFullscreenMode.Size = new System.Drawing.Size(186, 16);
            this.rbAlwaysStartInFullscreenMode.TabIndex = 6;
            this.rbAlwaysStartInFullscreenMode.TabStop = true;
            this.rbAlwaysStartInFullscreenMode.Text = "Always start in fullscreen mode";
            this.rbAlwaysStartInFullscreenMode.UseVisualStyleBackColor = true;
            // 
            // ViewerFormMakeShortcutDialog
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(250, 151);
            this.Controls.Add(this.rbAlwaysStartInFullscreenMode);
            this.Controls.Add(this.rbAlwaysStartInWindowMode);
            this.Controls.Add(this.rbStartInPreviousFullscreenMode);
            this.Controls.Add(this.cbStartInPreviousImageFilterSetting);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "ViewerFormMakeShortcutDialog";
            this.Text = "ViewerFormMakeShortcutDialog";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox cbStartInPreviousImageFilterSetting;
        private System.Windows.Forms.RadioButton rbStartInPreviousFullscreenMode;
        private System.Windows.Forms.RadioButton rbAlwaysStartInWindowMode;
        private System.Windows.Forms.RadioButton rbAlwaysStartInFullscreenMode;
    }
}