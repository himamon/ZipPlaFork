namespace ZipPla
{
    partial class KeyboardShortcutSettingForm
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
            this.dataGridView = new System.Windows.Forms.DataGridView();
            this.tbcShortcut = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cbcCommand = new System.Windows.Forms.DataGridViewComboBoxColumn();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.btnDefault = new System.Windows.Forms.Button();
            this.lbHint = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cmsDelete = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.btnSpecials = new System.Windows.Forms.Button();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cmsAddSpecials = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.lButtonDoubleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnLeftSideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnRightSideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnTopSideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnBottomSideToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnCenterToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnTopLeftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnTopRightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnBottomLeftToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.lButtonOnBottomRightToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.nextPageFailedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.previousPageFailedToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).BeginInit();
            this.cmsDelete.SuspendLayout();
            this.cmsAddSpecials.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(276, 226);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
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
            this.tbcShortcut,
            this.cbcCommand});
            this.dataGridView.Location = new System.Drawing.Point(12, 12);
            this.dataGridView.Name = "dataGridView";
            this.dataGridView.RowHeadersVisible = false;
            this.dataGridView.RowTemplate.Height = 21;
            this.dataGridView.Size = new System.Drawing.Size(420, 170);
            this.dataGridView.TabIndex = 1;
            this.dataGridView.CellClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.dataGridView_CellClick);
            this.dataGridView.EditingControlShowing += new System.Windows.Forms.DataGridViewEditingControlShowingEventHandler(this.dataGridView_EditingControlShowing);
            this.dataGridView.Scroll += new System.Windows.Forms.ScrollEventHandler(this.dataGridView_Scroll);
            this.dataGridView.SelectionChanged += new System.EventHandler(this.dataGridView_SelectionChanged);
            this.dataGridView.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dataGridView_MouseDown);
            // 
            // tbcShortcut
            // 
            this.tbcShortcut.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.tbcShortcut.HeaderText = "Shortcut";
            this.tbcShortcut.Name = "tbcShortcut";
            this.tbcShortcut.ReadOnly = true;
            this.tbcShortcut.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.tbcShortcut.Width = 54;
            // 
            // cbcCommand
            // 
            this.cbcCommand.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.cbcCommand.HeaderText = "Command";
            this.cbcCommand.Name = "cbcCommand";
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.Location = new System.Drawing.Point(357, 188);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 2;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAdd.Location = new System.Drawing.Point(195, 188);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 3;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.KeyDown += new System.Windows.Forms.KeyEventHandler(this.btnAdd_KeyDown);
            this.btnAdd.MouseClick += new System.Windows.Forms.MouseEventHandler(this.btnAdd_MouseClick);
            // 
            // btnDefault
            // 
            this.btnDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDefault.Location = new System.Drawing.Point(12, 188);
            this.btnDefault.Name = "btnDefault";
            this.btnDefault.Size = new System.Drawing.Size(75, 23);
            this.btnDefault.TabIndex = 4;
            this.btnDefault.Text = "Default";
            this.btnDefault.UseVisualStyleBackColor = true;
            this.btnDefault.Click += new System.EventHandler(this.btnDefault_Click);
            // 
            // lbHint
            // 
            this.lbHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.lbHint.AutoSize = true;
            this.lbHint.Location = new System.Drawing.Point(12, 237);
            this.lbHint.Name = "lbHint";
            this.lbHint.Size = new System.Drawing.Size(0, 12);
            this.lbHint.TabIndex = 5;
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(357, 226);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 6;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // cmsDelete
            // 
            this.cmsDelete.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem});
            this.cmsDelete.Name = "cmsDelete";
            this.cmsDelete.Size = new System.Drawing.Size(132, 26);
            // 
            // deleteToolStripMenuItem
            // 
            this.deleteToolStripMenuItem.Name = "deleteToolStripMenuItem";
            this.deleteToolStripMenuItem.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteToolStripMenuItem.Size = new System.Drawing.Size(131, 22);
            this.deleteToolStripMenuItem.Text = "Delete";
            this.deleteToolStripMenuItem.Click += new System.EventHandler(this.deleteToolStripMenuItem_Click);
            // 
            // btnSpecials
            // 
            this.btnSpecials.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnSpecials.Location = new System.Drawing.Point(276, 188);
            this.btnSpecials.Name = "btnSpecials";
            this.btnSpecials.Size = new System.Drawing.Size(75, 23);
            this.btnSpecials.TabIndex = 7;
            this.btnSpecials.Text = "Specials";
            this.btnSpecials.UseVisualStyleBackColor = true;
            this.btnSpecials.Click += new System.EventHandler(this.btnSpecials_Click);
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn1.HeaderText = "Shortcut";
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // cmsAddSpecials
            // 
            this.cmsAddSpecials.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lButtonDoubleToolStripMenuItem,
            this.lButtonOnLeftSideToolStripMenuItem,
            this.lButtonOnRightSideToolStripMenuItem,
            this.lButtonOnTopSideToolStripMenuItem,
            this.lButtonOnBottomSideToolStripMenuItem,
            this.lButtonOnCenterToolStripMenuItem,
            this.lButtonOnTopLeftToolStripMenuItem,
            this.lButtonOnTopRightToolStripMenuItem,
            this.lButtonOnBottomLeftToolStripMenuItem,
            this.lButtonOnBottomRightToolStripMenuItem,
            this.toolStripMenuItem1,
            this.nextPageFailedToolStripMenuItem,
            this.previousPageFailedToolStripMenuItem});
            this.cmsAddSpecials.Name = "cmsAddSpecials";
            this.cmsAddSpecials.Size = new System.Drawing.Size(200, 274);
            // 
            // lButtonDoubleToolStripMenuItem
            // 
            this.lButtonDoubleToolStripMenuItem.Name = "lButtonDoubleToolStripMenuItem";
            this.lButtonDoubleToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonDoubleToolStripMenuItem.Text = "LButtonDouble";
            this.lButtonDoubleToolStripMenuItem.Click += new System.EventHandler(this.lButtonDoubleToolStripMenuItem_Click);
            // 
            // lButtonOnLeftSideToolStripMenuItem
            // 
            this.lButtonOnLeftSideToolStripMenuItem.Name = "lButtonOnLeftSideToolStripMenuItem";
            this.lButtonOnLeftSideToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnLeftSideToolStripMenuItem.Text = "LButtonOnLeftSide";
            this.lButtonOnLeftSideToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnLeftSideToolStripMenuItem_Click);
            // 
            // lButtonOnRightSideToolStripMenuItem
            // 
            this.lButtonOnRightSideToolStripMenuItem.Name = "lButtonOnRightSideToolStripMenuItem";
            this.lButtonOnRightSideToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnRightSideToolStripMenuItem.Text = "LButtonOnRightSide";
            this.lButtonOnRightSideToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnRightSideToolStripMenuItem_Click);
            // 
            // lButtonOnTopSideToolStripMenuItem
            // 
            this.lButtonOnTopSideToolStripMenuItem.Name = "lButtonOnTopSideToolStripMenuItem";
            this.lButtonOnTopSideToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnTopSideToolStripMenuItem.Text = "LButtonOnTopSide";
            this.lButtonOnTopSideToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnTopSideToolStripMenuItem_Click);
            // 
            // lButtonOnBottomSideToolStripMenuItem
            // 
            this.lButtonOnBottomSideToolStripMenuItem.Name = "lButtonOnBottomSideToolStripMenuItem";
            this.lButtonOnBottomSideToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnBottomSideToolStripMenuItem.Text = "LButtonOnBottomSide";
            this.lButtonOnBottomSideToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnBottomSideToolStripMenuItem_Click);
            // 
            // lButtonOnCenterToolStripMenuItem
            // 
            this.lButtonOnCenterToolStripMenuItem.Name = "lButtonOnCenterToolStripMenuItem";
            this.lButtonOnCenterToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnCenterToolStripMenuItem.Text = "LButtonOnCenter";
            this.lButtonOnCenterToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnCenterToolStripMenuItem_Click);
            // 
            // lButtonOnTopLeftToolStripMenuItem
            // 
            this.lButtonOnTopLeftToolStripMenuItem.Name = "lButtonOnTopLeftToolStripMenuItem";
            this.lButtonOnTopLeftToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnTopLeftToolStripMenuItem.Text = "LButtonOnTopLeft";
            this.lButtonOnTopLeftToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnTopLeftToolStripMenuItem_Click);
            // 
            // lButtonOnTopRightToolStripMenuItem
            // 
            this.lButtonOnTopRightToolStripMenuItem.Name = "lButtonOnTopRightToolStripMenuItem";
            this.lButtonOnTopRightToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnTopRightToolStripMenuItem.Text = "LButtonOnTopRight";
            this.lButtonOnTopRightToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnTopRightToolStripMenuItem_Click);
            // 
            // lButtonOnBottomLeftToolStripMenuItem
            // 
            this.lButtonOnBottomLeftToolStripMenuItem.Name = "lButtonOnBottomLeftToolStripMenuItem";
            this.lButtonOnBottomLeftToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnBottomLeftToolStripMenuItem.Text = "LButtonOnBottomLeft";
            this.lButtonOnBottomLeftToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnButtomLeftToolStripMenuItem_Click);
            // 
            // lButtonOnBottomRightToolStripMenuItem
            // 
            this.lButtonOnBottomRightToolStripMenuItem.Name = "lButtonOnBottomRightToolStripMenuItem";
            this.lButtonOnBottomRightToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.lButtonOnBottomRightToolStripMenuItem.Text = "LButtonOnBottomRight";
            this.lButtonOnBottomRightToolStripMenuItem.Click += new System.EventHandler(this.lButtonOnButtonRightToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(196, 6);
            // 
            // nextPageFailedToolStripMenuItem
            // 
            this.nextPageFailedToolStripMenuItem.Name = "nextPageFailedToolStripMenuItem";
            this.nextPageFailedToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.nextPageFailedToolStripMenuItem.Text = "\"Next page\" failed";
            this.nextPageFailedToolStripMenuItem.Click += new System.EventHandler(this.nextPageFailedToolStripMenuItem_Click);
            // 
            // previousPageFailedToolStripMenuItem
            // 
            this.previousPageFailedToolStripMenuItem.Name = "previousPageFailedToolStripMenuItem";
            this.previousPageFailedToolStripMenuItem.Size = new System.Drawing.Size(199, 22);
            this.previousPageFailedToolStripMenuItem.Text = "\"Previous page\" failed";
            this.previousPageFailedToolStripMenuItem.Click += new System.EventHandler(this.previousPageFailedToolStripMenuItem_Click);
            // 
            // KeyboardShortcutSettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 12F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(444, 261);
            this.Controls.Add(this.btnSpecials);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOK);
            this.Controls.Add(this.lbHint);
            this.Controls.Add(this.btnDefault);
            this.Controls.Add(this.btnAdd);
            this.Controls.Add(this.btnDelete);
            this.Controls.Add(this.dataGridView);
            this.KeyPreview = true;
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(360, 300);
            this.Name = "KeyboardShortcutSettingForm";
            this.Text = "KeyboardShortcutSettingForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.KeyboardShortcutSettingForm_FormClosing);
            this.Shown += new System.EventHandler(this.KeyboardShortcutSettingForm_Shown);
            ((System.ComponentModel.ISupportInitialize)(this.dataGridView)).EndInit();
            this.cmsDelete.ResumeLayout(false);
            this.cmsAddSpecials.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.DataGridView dataGridView;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.Button btnDefault;
        private System.Windows.Forms.Label lbHint;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.ContextMenuStrip cmsDelete;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.Button btnSpecials;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.ContextMenuStrip cmsAddSpecials;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnLeftSideToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnRightSideToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnTopSideToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnBottomSideToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnCenterToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnTopLeftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnTopRightToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnBottomLeftToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonOnBottomRightToolStripMenuItem;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem nextPageFailedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem previousPageFailedToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem lButtonDoubleToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcShortcut;
        private System.Windows.Forms.DataGridViewComboBoxColumn cbcCommand;
    }
}