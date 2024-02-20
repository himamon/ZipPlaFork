namespace ZipPla
{
    partial class ThumbnailCacheSettingForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(ThumbnailCacheSettingForm));
            this.btnCancel = new System.Windows.Forms.Button();
            this.tbFile = new System.Windows.Forms.TextBox();
            this.btnOK = new System.Windows.Forms.Button();
            this.rbADS = new System.Windows.Forms.RadioButton();
            this.rbFile = new System.Windows.Forms.RadioButton();
            this.rbNoCache = new System.Windows.Forms.RadioButton();
            this.lbFirstDescription = new System.Windows.Forms.Label();
            this.lbAdsDescription = new System.Windows.Forms.Label();
            this.lbFileDescription = new System.Windows.Forms.Label();
            this.btnBrowse = new System.Windows.Forms.Button();
            this.btnOriginal = new System.Windows.Forms.Button();
            this.btnZipPla = new System.Windows.Forms.Button();
            this.pictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(417, 426);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 0;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // tbFile
            // 
            this.tbFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tbFile.Location = new System.Drawing.Point(45, 200);
            this.tbFile.Name = "tbFile";
            this.tbFile.Size = new System.Drawing.Size(429, 19);
            this.tbFile.TabIndex = 1;
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(336, 426);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 2;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // rbADS
            // 
            this.rbADS.AutoSize = true;
            this.rbADS.Location = new System.Drawing.Point(31, 61);
            this.rbADS.Name = "rbADS";
            this.rbADS.Size = new System.Drawing.Size(304, 16);
            this.rbADS.TabIndex = 3;
            this.rbADS.TabStop = true;
            this.rbADS.Text = "Store caches in Alternate Data Stream (recommended)";
            this.rbADS.UseVisualStyleBackColor = true;
            this.rbADS.CheckedChanged += new System.EventHandler(this.rbADS_CheckedChanged);
            // 
            // rbFile
            // 
            this.rbFile.AutoSize = true;
            this.rbFile.Location = new System.Drawing.Point(31, 176);
            this.rbFile.Name = "rbFile";
            this.rbFile.Size = new System.Drawing.Size(206, 16);
            this.rbFile.TabIndex = 4;
            this.rbFile.TabStop = true;
            this.rbFile.Text = "Store caches in the specified folder";
            this.rbFile.UseVisualStyleBackColor = true;
            this.rbFile.CheckedChanged += new System.EventHandler(this.rbFile_CheckedChanged);
            // 
            // rbNoCache
            // 
            this.rbNoCache.AutoSize = true;
            this.rbNoCache.Location = new System.Drawing.Point(31, 388);
            this.rbNoCache.Name = "rbNoCache";
            this.rbNoCache.Size = new System.Drawing.Size(170, 16);
            this.rbNoCache.TabIndex = 5;
            this.rbNoCache.TabStop = true;
            this.rbNoCache.Text = "Not use the thumbnail cache";
            this.rbNoCache.UseVisualStyleBackColor = true;
            this.rbNoCache.CheckedChanged += new System.EventHandler(this.rbNoCache_CheckedChanged);
            // 
            // lbFirstDescription
            // 
            this.lbFirstDescription.AutoSize = true;
            this.lbFirstDescription.Location = new System.Drawing.Point(29, 24);
            this.lbFirstDescription.Name = "lbFirstDescription";
            this.lbFirstDescription.Size = new System.Drawing.Size(373, 24);
            this.lbFirstDescription.TabIndex = 6;
            this.lbFirstDescription.Text = "By using the thumbnail cache,\r\nthumbnails you view are stored in some location fo" +
    "r quick viewing later.";
            // 
            // lbAdsDescription
            // 
            this.lbAdsDescription.AutoSize = true;
            this.lbAdsDescription.Location = new System.Drawing.Point(57, 85);
            this.lbAdsDescription.Name = "lbAdsDescription";
            this.lbAdsDescription.Size = new System.Drawing.Size(363, 48);
            this.lbAdsDescription.TabIndex = 7;
            this.lbAdsDescription.Text = resources.GetString("lbAdsDescription.Text");
            this.lbAdsDescription.Click += new System.EventHandler(this.lbAdsDescription_Click);
            // 
            // lbFileDescription
            // 
            this.lbFileDescription.AutoSize = true;
            this.lbFileDescription.Location = new System.Drawing.Point(57, 261);
            this.lbFileDescription.Name = "lbFileDescription";
            this.lbFileDescription.Size = new System.Drawing.Size(359, 84);
            this.lbFileDescription.TabIndex = 8;
            this.lbFileDescription.Text = resources.GetString("lbFileDescription.Text");
            this.lbFileDescription.Click += new System.EventHandler(this.lbFileDescription_Click);
            // 
            // btnBrowse
            // 
            this.btnBrowse.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowse.AutoSize = true;
            this.btnBrowse.Location = new System.Drawing.Point(399, 225);
            this.btnBrowse.Name = "btnBrowse";
            this.btnBrowse.Size = new System.Drawing.Size(75, 23);
            this.btnBrowse.TabIndex = 9;
            this.btnBrowse.Text = "Browse";
            this.btnBrowse.UseVisualStyleBackColor = true;
            this.btnBrowse.Click += new System.EventHandler(this.btnBrowse_Click);
            // 
            // btnOriginal
            // 
            this.btnOriginal.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOriginal.AutoSize = true;
            this.btnOriginal.Location = new System.Drawing.Point(318, 225);
            this.btnOriginal.Name = "btnOriginal";
            this.btnOriginal.Size = new System.Drawing.Size(75, 23);
            this.btnOriginal.TabIndex = 11;
            this.btnOriginal.Text = "Original";
            this.btnOriginal.UseVisualStyleBackColor = true;
            this.btnOriginal.Click += new System.EventHandler(this.btnOriginal_Click);
            // 
            // btnZipPla
            // 
            this.btnZipPla.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnZipPla.AutoSize = true;
            this.btnZipPla.Location = new System.Drawing.Point(237, 225);
            this.btnZipPla.Name = "btnZipPla";
            this.btnZipPla.Size = new System.Drawing.Size(75, 23);
            this.btnZipPla.TabIndex = 12;
            this.btnZipPla.Text = "ZipPla.exe";
            this.btnZipPla.UseVisualStyleBackColor = true;
            this.btnZipPla.Click += new System.EventHandler(this.btnZipPla_Click);
            // 
            // pictureBox
            // 
            this.pictureBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.pictureBox.Location = new System.Drawing.Point(12, 428);
            this.pictureBox.Name = "pictureBox";
            this.pictureBox.Size = new System.Drawing.Size(21, 21);
            this.pictureBox.TabIndex = 13;
            this.pictureBox.TabStop = false;
            // 
            // ThumbnailCacheSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(504, 461);
            this.Controls.Add(this.pictureBox);
            this.Controls.Add(this.btnZipPla);
            this.Controls.Add(this.btnOriginal);
            this.Controls.Add(this.btnBrowse);
            this.Controls.Add(this.lbFileDescription);
            this.Controls.Add(this.lbAdsDescription);
            this.Controls.Add(this.lbFirstDescription);
            this.Controls.Add(this.rbNoCache);
            this.Controls.Add(this.rbFile);
            this.Controls.Add(this.rbADS);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.tbFile);
            this.Controls.Add(this.btnCancel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MaximumSize = new System.Drawing.Size(100000, 500);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(520, 500);
            this.Name = "ThumbnailCacheSettingForm";
            this.Text = "ThumbnailCacheSettingForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.ThumbnailCacheSettingForm_FormClosing);
            this.MouseDown += new System.Windows.Forms.MouseEventHandler(this.ThumbnailCacheSettingForm_MouseDown);
            ((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.TextBox tbFile;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.RadioButton rbADS;
        private System.Windows.Forms.RadioButton rbFile;
        private System.Windows.Forms.RadioButton rbNoCache;
        private System.Windows.Forms.Label lbFirstDescription;
        private System.Windows.Forms.Label lbAdsDescription;
        private System.Windows.Forms.Label lbFileDescription;
        private System.Windows.Forms.Button btnBrowse;
        private System.Windows.Forms.Button btnOriginal;
        private System.Windows.Forms.Button btnZipPla;
        private System.Windows.Forms.PictureBox pictureBox;
    }
}