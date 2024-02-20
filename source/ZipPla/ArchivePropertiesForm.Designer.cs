namespace ZipPla
{
    partial class ArchivePropertiesForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ArchivePropertiesForm));
            this.lbLocation = new System.Windows.Forms.Label();
            this.lbFileName = new System.Windows.Forms.Label();
            this.lbSize = new System.Windows.Forms.Label();
            this.lbType = new System.Windows.Forms.Label();
            this.lbNumberOfPages = new System.Windows.Forms.Label();
            this.tbLocation = new System.Windows.Forms.TextBox();
            this.tbFileName = new System.Windows.Forms.TextBox();
            this.lbType2 = new System.Windows.Forms.Label();
            this.lbSize2 = new System.Windows.Forms.Label();
            this.lbNumberOfPages2 = new System.Windows.Forms.Label();
            this.btnOK = new System.Windows.Forms.Button();
            this.tbExtension = new System.Windows.Forms.TextBox();
            this.lbLaccessDate2 = new System.Windows.Forms.Label();
            this.lbLaccessDate = new System.Windows.Forms.Label();
            this.lbModifiedDate2 = new System.Windows.Forms.Label();
            this.lbModifiedDate = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // lbLocation
            // 
            this.lbLocation.AutoSize = true;
            this.lbLocation.Location = new System.Drawing.Point(12, 16);
            this.lbLocation.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLocation.Name = "lbLocation";
            this.lbLocation.Size = new System.Drawing.Size(48, 12);
            this.lbLocation.TabIndex = 0;
            this.lbLocation.Text = "Location";
            // 
            // lbFileName
            // 
            this.lbFileName.AutoSize = true;
            this.lbFileName.Location = new System.Drawing.Point(12, 44);
            this.lbFileName.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbFileName.Name = "lbFileName";
            this.lbFileName.Size = new System.Drawing.Size(34, 12);
            this.lbFileName.TabIndex = 1;
            this.lbFileName.Text = "Name";
            // 
            // lbSize
            // 
            this.lbSize.AutoSize = true;
            this.lbSize.Location = new System.Drawing.Point(12, 100);
            this.lbSize.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbSize.Name = "lbSize";
            this.lbSize.Size = new System.Drawing.Size(26, 12);
            this.lbSize.TabIndex = 2;
            this.lbSize.Text = "Size";
            // 
            // lbType
            // 
            this.lbType.AutoSize = true;
            this.lbType.Location = new System.Drawing.Point(12, 72);
            this.lbType.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbType.Name = "lbType";
            this.lbType.Size = new System.Drawing.Size(30, 12);
            this.lbType.TabIndex = 3;
            this.lbType.Text = "Type";
            // 
            // lbNumberOfPages
            // 
            this.lbNumberOfPages.AutoSize = true;
            this.lbNumberOfPages.Location = new System.Drawing.Point(12, 184);
            this.lbNumberOfPages.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbNumberOfPages.Name = "lbNumberOfPages";
            this.lbNumberOfPages.Size = new System.Drawing.Size(92, 12);
            this.lbNumberOfPages.TabIndex = 4;
            this.lbNumberOfPages.Text = "Number of pages";
            // 
            // tbLocation
            // 
            this.tbLocation.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbLocation.Location = new System.Drawing.Point(124, 13);
            this.tbLocation.Margin = new System.Windows.Forms.Padding(4);
            this.tbLocation.Name = "tbLocation";
            this.tbLocation.ReadOnly = true;
            this.tbLocation.Size = new System.Drawing.Size(448, 19);
            this.tbLocation.TabIndex = 5;
            // 
            // tbFileName
            // 
            this.tbFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFileName.Location = new System.Drawing.Point(124, 41);
            this.tbFileName.Margin = new System.Windows.Forms.Padding(4);
            this.tbFileName.Name = "tbFileName";
            this.tbFileName.Size = new System.Drawing.Size(399, 19);
            this.tbFileName.TabIndex = 6;
            this.tbFileName.KeyDown += new System.Windows.Forms.KeyEventHandler(this.tbFileName_KeyDown);
            // 
            // lbType2
            // 
            this.lbType2.AutoSize = true;
            this.lbType2.Location = new System.Drawing.Point(122, 72);
            this.lbType2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbType2.Name = "lbType2";
            this.lbType2.Size = new System.Drawing.Size(30, 12);
            this.lbType2.TabIndex = 7;
            this.lbType2.Text = "Type";
            // 
            // lbSize2
            // 
            this.lbSize2.AutoSize = true;
            this.lbSize2.Location = new System.Drawing.Point(122, 100);
            this.lbSize2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbSize2.Name = "lbSize2";
            this.lbSize2.Size = new System.Drawing.Size(26, 12);
            this.lbSize2.TabIndex = 8;
            this.lbSize2.Text = "Size";
            // 
            // lbNumberOfPages2
            // 
            this.lbNumberOfPages2.AutoSize = true;
            this.lbNumberOfPages2.Location = new System.Drawing.Point(122, 184);
            this.lbNumberOfPages2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbNumberOfPages2.Name = "lbNumberOfPages2";
            this.lbNumberOfPages2.Size = new System.Drawing.Size(92, 12);
            this.lbNumberOfPages2.TabIndex = 9;
            this.lbNumberOfPages2.Text = "Number of pages";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = System.Windows.Forms.AnchorStyles.Top;
            this.btnOK.Location = new System.Drawing.Point(254, 206);
            this.btnOK.Margin = new System.Windows.Forms.Padding(4);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 10;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // tbExtension
            // 
            this.tbExtension.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.tbExtension.Location = new System.Drawing.Point(531, 41);
            this.tbExtension.Margin = new System.Windows.Forms.Padding(4);
            this.tbExtension.Name = "tbExtension";
            this.tbExtension.ReadOnly = true;
            this.tbExtension.Size = new System.Drawing.Size(41, 19);
            this.tbExtension.TabIndex = 11;
            // 
            // lbLaccessDate2
            // 
            this.lbLaccessDate2.AutoSize = true;
            this.lbLaccessDate2.Location = new System.Drawing.Point(122, 128);
            this.lbLaccessDate2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLaccessDate2.Name = "lbLaccessDate2";
            this.lbLaccessDate2.Size = new System.Drawing.Size(93, 12);
            this.lbLaccessDate2.TabIndex = 13;
            this.lbLaccessDate2.Text = "Last access date";
            // 
            // lbLaccessDate
            // 
            this.lbLaccessDate.AutoSize = true;
            this.lbLaccessDate.Location = new System.Drawing.Point(12, 128);
            this.lbLaccessDate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbLaccessDate.Name = "lbLaccessDate";
            this.lbLaccessDate.Size = new System.Drawing.Size(81, 12);
            this.lbLaccessDate.TabIndex = 12;
            this.lbLaccessDate.Text = "Date accessed";
            // 
            // lbModifiedDate2
            // 
            this.lbModifiedDate2.AutoSize = true;
            this.lbModifiedDate2.Location = new System.Drawing.Point(122, 156);
            this.lbModifiedDate2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbModifiedDate2.Name = "lbModifiedDate2";
            this.lbModifiedDate2.Size = new System.Drawing.Size(82, 12);
            this.lbModifiedDate2.TabIndex = 15;
            this.lbModifiedDate2.Text = "Last write date";
            // 
            // lbModifiedDate
            // 
            this.lbModifiedDate.AutoSize = true;
            this.lbModifiedDate.Location = new System.Drawing.Point(12, 156);
            this.lbModifiedDate.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.lbModifiedDate.Name = "lbModifiedDate";
            this.lbModifiedDate.Size = new System.Drawing.Size(73, 12);
            this.lbModifiedDate.TabIndex = 14;
            this.lbModifiedDate.Text = "Date midified";
            // 
            // ArchivePropertiesForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(584, 243);
            this.Controls.Add(this.lbModifiedDate2);
            this.Controls.Add(this.lbModifiedDate);
            this.Controls.Add(this.lbLaccessDate2);
            this.Controls.Add(this.lbLaccessDate);
            this.Controls.Add(this.tbExtension);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lbNumberOfPages2);
            this.Controls.Add(this.lbSize2);
            this.Controls.Add(this.lbType2);
            this.Controls.Add(this.tbFileName);
            this.Controls.Add(this.tbLocation);
            this.Controls.Add(this.lbNumberOfPages);
            this.Controls.Add(this.lbType);
            this.Controls.Add(this.lbSize);
            this.Controls.Add(this.lbFileName);
            this.Controls.Add(this.lbLocation);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(10000, 282);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(400, 282);
            this.Name = "ArchivePropertiesForm";
            this.Text = "ArchivePropertiesForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ArchivePropertiesForm_FormClosing);
            this.Shown += new System.EventHandler(this.ArchivePropertiesForm_Shown);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label lbLocation;
        private System.Windows.Forms.Label lbFileName;
        private System.Windows.Forms.Label lbSize;
        private System.Windows.Forms.Label lbType;
        private System.Windows.Forms.Label lbNumberOfPages;
        private System.Windows.Forms.TextBox tbLocation;
        private System.Windows.Forms.TextBox tbFileName;
        private System.Windows.Forms.Label lbType2;
        private System.Windows.Forms.Label lbSize2;
        private System.Windows.Forms.Label lbNumberOfPages2;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.TextBox tbExtension;
        private System.Windows.Forms.Label lbLaccessDate2;
        private System.Windows.Forms.Label lbLaccessDate;
        private System.Windows.Forms.Label lbModifiedDate2;
        private System.Windows.Forms.Label lbModifiedDate;
    }
}