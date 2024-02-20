namespace ZipPla
{
    partial class MouseGestureSettingForm
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
            this.components = new System.ComponentModel.Container();
            this.btnOK = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbEnabled = new System.Windows.Forms.CheckBox();
            this.gbMouseGestureRegion = new System.Windows.Forms.GroupBox();
            this.pnlMouseGestureRegion = new System.Windows.Forms.Panel();
            this.gbAppearance = new System.Windows.Forms.GroupBox();
            this.btnColor = new System.Windows.Forms.Button();
            this.btnWidth = new System.Windows.Forms.Button();
            this.pnlAppearance = new System.Windows.Forms.Panel();
            this.cmsWidth = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem3 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem4 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem5 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem6 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem7 = new System.Windows.Forms.ToolStripMenuItem();
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.tbcGesture = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cbcCommand = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.cmsDelete = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnDelete = new System.Windows.Forms.Button();
            this.gbMouseGestureRegion.SuspendLayout();
            this.gbAppearance.SuspendLayout();
            this.cmsWidth.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.cmsDelete.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(256, 338);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(337, 338);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cbEnabled
            // 
            this.cbEnabled.AutoSize = true;
            this.cbEnabled.Checked = true;
            this.cbEnabled.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbEnabled.Location = new System.Drawing.Point(15, 12);
            this.cbEnabled.Name = "cbEnabled";
            this.cbEnabled.Size = new System.Drawing.Size(64, 16);
            this.cbEnabled.TabIndex = 2;
            this.cbEnabled.Text = "Enabled";
            this.cbEnabled.UseVisualStyleBackColor = true;
            this.cbEnabled.CheckedChanged += new System.EventHandler(this.cbEnabled_CheckedChanged);
            // 
            // gbMouseGestureRegion
            // 
            this.gbMouseGestureRegion.BackColor = System.Drawing.SystemColors.Control;
            this.gbMouseGestureRegion.Controls.Add(this.pnlMouseGestureRegion);
            this.gbMouseGestureRegion.Location = new System.Drawing.Point(12, 34);
            this.gbMouseGestureRegion.Name = "gbMouseGestureRegion";
            this.gbMouseGestureRegion.Size = new System.Drawing.Size(200, 200);
            this.gbMouseGestureRegion.TabIndex = 3;
            this.gbMouseGestureRegion.TabStop = false;
            this.gbMouseGestureRegion.Text = "Gesture to add a command";
            // 
            // pnlMouseGestureRegion
            // 
            this.pnlMouseGestureRegion.BackColor = System.Drawing.Color.White;
            this.pnlMouseGestureRegion.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlMouseGestureRegion.Location = new System.Drawing.Point(3, 15);
            this.pnlMouseGestureRegion.Name = "pnlMouseGestureRegion";
            this.pnlMouseGestureRegion.Size = new System.Drawing.Size(194, 182);
            this.pnlMouseGestureRegion.TabIndex = 0;
            // 
            // gbAppearance
            // 
            this.gbAppearance.Controls.Add(this.btnColor);
            this.gbAppearance.Controls.Add(this.btnWidth);
            this.gbAppearance.Controls.Add(this.pnlAppearance);
            this.gbAppearance.Location = new System.Drawing.Point(12, 240);
            this.gbAppearance.Name = "gbAppearance";
            this.gbAppearance.Size = new System.Drawing.Size(197, 83);
            this.gbAppearance.TabIndex = 4;
            this.gbAppearance.TabStop = false;
            this.gbAppearance.Text = "Appearance";
            // 
            // btnColor
            // 
            this.btnColor.Location = new System.Drawing.Point(110, 48);
            this.btnColor.Name = "btnColor";
            this.btnColor.Size = new System.Drawing.Size(75, 23);
            this.btnColor.TabIndex = 5;
            this.btnColor.Text = "Color";
            this.btnColor.UseVisualStyleBackColor = true;
            this.btnColor.Click += new System.EventHandler(this.btnColor_Click);
            // 
            // btnWidth
            // 
            this.btnWidth.Location = new System.Drawing.Point(110, 18);
            this.btnWidth.Name = "btnWidth";
            this.btnWidth.Size = new System.Drawing.Size(75, 23);
            this.btnWidth.TabIndex = 1;
            this.btnWidth.Text = "Width";
            this.btnWidth.UseVisualStyleBackColor = true;
            this.btnWidth.Click += new System.EventHandler(this.btnWidth_Click);
            // 
            // pnlAppearance
            // 
            this.pnlAppearance.BackColor = System.Drawing.Color.White;
            this.pnlAppearance.Location = new System.Drawing.Point(14, 29);
            this.pnlAppearance.Name = "pnlAppearance";
            this.pnlAppearance.Size = new System.Drawing.Size(82, 31);
            this.pnlAppearance.TabIndex = 0;
            this.pnlAppearance.Paint += new System.Windows.Forms.PaintEventHandler(this.pnlAppearance_Paint);
            // 
            // cmsWidth
            // 
            this.cmsWidth.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem2,
            this.toolStripMenuItem3,
            this.toolStripMenuItem4,
            this.toolStripMenuItem5,
            this.toolStripMenuItem6,
            this.toolStripMenuItem7});
            this.cmsWidth.Name = "cmsWidth";
            this.cmsWidth.Size = new System.Drawing.Size(81, 136);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(80, 22);
            this.toolStripMenuItem2.Text = "0";
            // 
            // toolStripMenuItem3
            // 
            this.toolStripMenuItem3.Name = "toolStripMenuItem3";
            this.toolStripMenuItem3.Size = new System.Drawing.Size(80, 22);
            this.toolStripMenuItem3.Text = "1";
            // 
            // toolStripMenuItem4
            // 
            this.toolStripMenuItem4.Name = "toolStripMenuItem4";
            this.toolStripMenuItem4.Size = new System.Drawing.Size(80, 22);
            this.toolStripMenuItem4.Text = "2";
            // 
            // toolStripMenuItem5
            // 
            this.toolStripMenuItem5.Name = "toolStripMenuItem5";
            this.toolStripMenuItem5.Size = new System.Drawing.Size(80, 22);
            this.toolStripMenuItem5.Text = "3";
            // 
            // toolStripMenuItem6
            // 
            this.toolStripMenuItem6.Name = "toolStripMenuItem6";
            this.toolStripMenuItem6.Size = new System.Drawing.Size(80, 22);
            this.toolStripMenuItem6.Text = "4";
            // 
            // toolStripMenuItem7
            // 
            this.toolStripMenuItem7.Name = "toolStripMenuItem7";
            this.toolStripMenuItem7.Size = new System.Drawing.Size(80, 22);
            this.toolStripMenuItem7.Text = "5";
            // 
            // dataGridView
            // 
            this.dataGridView.AllowUserToAddRows = false;
            this.dataGridView.AllowUserToResizeColumns = false;
            this.dataGridView.AllowUserToResizeRows = false;
            this.dataGridView.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dataGridView.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dataGridView.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.tbcGesture,
            this.cbcCommand});
            this.dataGridView.Location = new System.Drawing.Point(218, 12);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.RowTemplate.Height = 21;
            this.dataGridView.SelectionMode = System.Windows.Forms.DataGridViewSelectionMode.FullRowSelect;
            this.dataGridView.Size = new System.Drawing.Size(194, 282);
            this.dataGridView.TabIndex = 6;
            this.dataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_CellClick);
            this.dataGridView.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dataGridView_EditingControlShowing);
            this.dataGridView.Scroll += new System.Windows.Forms.ScrollEventHandler(this.dataGridView_Scroll);
            this.dataGridView.SelectionChanged += new System.EventHandler(this.dataGridView_SelectionChanged);
            this.dataGridView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataGridView_MouseDown);
            // 
            // tbcGesture
            // 
            this.tbcGesture.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.tbcGesture.HeaderText = "Gesture ";
            this.tbcGesture.Name = "tbcGesture";
            this.tbcGesture.ReadOnly = true;
            this.tbcGesture.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.tbcGesture.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.tbcGesture.Width = 55;
            // 
            // cbcCommand
            // 
            this.cbcCommand.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.cbcCommand.HeaderText = "Command";
            this.cbcCommand.Name = "cbcCommand";
            // 
            // cmsDelete
            // 
            this.cmsDelete.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem});
            this.cmsDelete.Name = "cmsDelete";
            this.cmsDelete.Size = new System.Drawing.Size(108, 26);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(107, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.Enabled = false;
            this.btnDelete.Location = new System.Drawing.Point(337, 300);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 8;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // MouseGestureSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(424, 373);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.dataGridView);
            this.Controls.Add(this.gbAppearance);
            this.Controls.Add(this.gbMouseGestureRegion);
            this.Controls.Add(this.cbEnabled);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(440, 412);
            this.Name = "MouseGestureSettingForm";
            this.Text = "MouseGestureSettingForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.MouseGestureSettingForm_FormClosing);
            this.Shown += new System.EventHandler(this.MouseGestureSettingForm_Shown);
            this.gbMouseGestureRegion.ResumeLayout(false);
            this.gbAppearance.ResumeLayout(false);
            this.cmsWidth.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.cmsDelete.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox cbEnabled;
        private System.Windows.Forms.GroupBox gbMouseGestureRegion;
        private System.Windows.Forms.Panel pnlMouseGestureRegion;
        private System.Windows.Forms.GroupBox gbAppearance;
        private System.Windows.Forms.Button btnColor;
        private System.Windows.Forms.Button btnWidth;
        private System.Windows.Forms.Panel pnlAppearance;
        private System.Windows.Forms.ContextMenuStrip cmsWidth;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem3;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem4;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem5;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem6;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem7;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.ContextMenuStrip cmsDelete;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcGesture;
        private System.Windows.Forms.DataGridViewComboBoxColumn cbcCommand;
    }
}