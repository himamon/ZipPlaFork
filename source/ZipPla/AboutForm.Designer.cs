namespace ZipPla
{
    partial class AboutForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutForm));
            this.llLicense = new System.Windows.Forms.LinkLabel();
            this.btnOK = new System.Windows.Forms.Button();
            this.pbLight = new System.Windows.Forms.PictureBox();
            this.pbDark = new System.Windows.Forms.PictureBox();
            this.lbZipPla = new System.Windows.Forms.Label();
            this.lbVersion = new System.Windows.Forms.Label();
            ((System.ComponentModel.ISupportInitialize)(this.pbLight)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDark)).BeginInit();
            this.SuspendLayout();
            // 
            // llLicense
            // 
            this.llLicense.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.llLicense.AutoSize = true;
            this.llLicense.Location = new System.Drawing.Point(28, 125);
            this.llLicense.Name = "llLicense";
            this.llLicense.Size = new System.Drawing.Size(473, 192);
            this.llLicense.TabIndex = 0;
            this.llLicense.TabStop = true;
            this.llLicense.Text = resources.GetString("llLicense.Text");
            this.llLicense.LinkClicked += new System.Windows.Forms.LinkLabelLinkClickedEventHandler(this.llLicense_LinkClicked);
            // 
            // btnOK
            // 
            this.btnOK.Anchor = System.Windows.Forms.AnchorStyles.Bottom;
            this.btnOK.Location = new System.Drawing.Point(228, 342);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(76, 24);
            this.btnOK.TabIndex = 1;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // pbLight
            // 
            this.pbLight.Location = new System.Drawing.Point(29, 31);
            this.pbLight.Name = "pbLight";
            this.pbLight.Size = new System.Drawing.Size(64, 64);
            this.pbLight.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbLight.TabIndex = 1;
            this.pbLight.TabStop = false;
            // 
            // pbDark
            // 
            this.pbDark.Location = new System.Drawing.Point(99, 31);
            this.pbDark.Name = "pbDark";
            this.pbDark.Size = new System.Drawing.Size(64, 64);
            this.pbDark.SizeMode = System.Windows.Forms.PictureBoxSizeMode.Zoom;
            this.pbDark.TabIndex = 0;
            this.pbDark.TabStop = false;
            // 
            // lbZipPla
            // 
            this.lbZipPla.AutoSize = true;
            this.lbZipPla.Font = new System.Drawing.Font("MS UI Gothic", 27.75F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lbZipPla.Location = new System.Drawing.Point(169, 58);
            this.lbZipPla.Name = "lbZipPla";
            this.lbZipPla.Size = new System.Drawing.Size(119, 37);
            this.lbZipPla.TabIndex = 2;
            this.lbZipPla.Text = "ZipPla";
            // 
            // lbVersion
            // 
            this.lbVersion.AutoSize = true;
            this.lbVersion.Font = new System.Drawing.Font("MS UI Gothic", 12F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(128)));
            this.lbVersion.Location = new System.Drawing.Point(298, 76);
            this.lbVersion.Name = "lbVersion";
            this.lbVersion.Size = new System.Drawing.Size(160, 16);
            this.lbVersion.TabIndex = 3;
            this.lbVersion.Text = "Version %VERSION%";
            // 
            // AboutForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(524, 378);
            this.Controls.Add(this.lbVersion);
            this.Controls.Add(this.lbZipPla);
            this.Controls.Add(this.pbDark);
            this.Controls.Add(this.pbLight);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.llLicense);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "AboutForm";
            this.Text = "AboutForm";
            ((System.ComponentModel.ISupportInitialize)(this.pbLight)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.pbDark)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.LinkLabel llLicense;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.PictureBox pbLight;
        private System.Windows.Forms.PictureBox pbDark;
        private System.Windows.Forms.Label lbZipPla;
        private System.Windows.Forms.Label lbVersion;
    }
}