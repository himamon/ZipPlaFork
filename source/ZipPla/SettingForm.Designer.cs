namespace ZipPla
{
    partial class SettingForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(SettingForm));
            this.openFileDialogExecutableFile = new System.Windows.Forms.OpenFileDialog();
            this.btnOK = new System.Windows.Forms.Button();
            this.cbLanguage = new System.Windows.Forms.ComboBox();
            this.gbApplications = new System.Windows.Forms.GroupBox();
            this.btnAccessKeyHint = new System.Windows.Forms.Button();
            this.btnDefault = new System.Windows.Forms.Button();
            this.btnDelete = new System.Windows.Forms.Button();
            this.btnAdd = new System.Windows.Forms.Button();
            this.dgvApplications = new System.Windows.Forms.DataGridView();
            this.tbcName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tbcCommandLineParameter = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.cbcMulti = new System.Windows.Forms.DataGridViewCheckBoxColumn();
            this.tbcOpenByClicking = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tbcShowInContextMenu = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gbLanguage = new System.Windows.Forms.GroupBox();
            this.btnLanguageEdit = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.gbHistory = new System.Windows.Forms.GroupBox();
            this.cbHistory = new System.Windows.Forms.ComboBox();
            this.cmsLanguageEdit = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.openlanguageFolderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.updateAndEditLanguageFileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsApplicationAdd = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.externalApplicationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.moveFilesfoldersToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.sendTheItemInTheClipboardToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripSeparator();
            this.builtinBookReaderToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.associatedApplicationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.moveLocationToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.explorerToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gbHint = new System.Windows.Forms.GroupBox();
            this.cbNgen = new System.Windows.Forms.CheckBox();
            this.cbHint = new System.Windows.Forms.CheckBox();
            this.cmsDelete = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.gbDragAndDrop = new System.Windows.Forms.GroupBox();
            this.rbMoveOrCopy = new System.Windows.Forms.RadioButton();
            this.rbShowThumbnails = new System.Windows.Forms.RadioButton();
            this.gbDynamicStringSelection = new System.Windows.Forms.GroupBox();
            this.cbDynamicStringSelectionAllowUserToRename = new System.Windows.Forms.CheckBox();
            this.dgvDynamicStringSelection = new System.Windows.Forms.DataGridView();
            this.tbcDisplayName = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tbcDisplayNameForEmptyString = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.tbcCommand = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.btnDynamicStringSelectionDelete = new System.Windows.Forms.Button();
            this.btnDynamicStringSelectionAdd = new System.Windows.Forms.Button();
            this.btnDynamicStringSelectionDefault = new System.Windows.Forms.Button();
            this.cbDynamicStringSelectionEnabled = new System.Windows.Forms.CheckBox();
            this.cmsDDSAdd = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.filterWithSelectedTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.copySelectedTextToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.searchSelectedTextOnGoogleToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.newCustomCommandToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.cmsDynamicStringSelection = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.deleteToolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.folderBrowserDialogMoveFilesFolders = new System.Windows.Forms.FolderBrowserDialog();
            this.gbViewerMemoryUsage = new System.Windows.Forms.GroupBox();
            this.btnViewerMemoryUsageHelp = new System.Windows.Forms.Button();
            this.cbViewerMemoryUsage = new System.Windows.Forms.ComboBox();
            this.gbSusiePlugins = new System.Windows.Forms.GroupBox();
            this.cbSearchAlsoSusieInstallationFolder = new System.Windows.Forms.CheckBox();
            this.cbAllowUntestedPlugins = new System.Windows.Forms.CheckBox();
            this.dataGridViewTextBoxColumn1 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn2 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn3 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn4 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn5 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn6 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.dataGridViewTextBoxColumn7 = new System.Windows.Forms.DataGridViewTextBoxColumn();
            this.gbZipPlaUpdateCheck = new System.Windows.Forms.GroupBox();
            this.btnCheckNow = new System.Windows.Forms.Button();
            this.cbAskPermissionBeforeConnection = new System.Windows.Forms.CheckBox();
            this.gbApplications.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvApplications)).BeginInit();
            this.gbLanguage.SuspendLayout();
            this.gbHistory.SuspendLayout();
            this.cmsLanguageEdit.SuspendLayout();
            this.cmsApplicationAdd.SuspendLayout();
            this.gbHint.SuspendLayout();
            this.cmsDelete.SuspendLayout();
            this.gbDragAndDrop.SuspendLayout();
            this.gbDynamicStringSelection.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDynamicStringSelection)).BeginInit();
            this.cmsDDSAdd.SuspendLayout();
            this.cmsDynamicStringSelection.SuspendLayout();
            this.gbViewerMemoryUsage.SuspendLayout();
            this.gbSusiePlugins.SuspendLayout();
            this.gbZipPlaUpdateCheck.SuspendLayout();
            this.SuspendLayout();
            // 
            // openFileDialogExecutableFile
            // 
            this.openFileDialogExecutableFile.DereferenceLinks = false;
            this.openFileDialogExecutableFile.Filter = "*.exe;*.bat;*.cmd;*.vbs;*.js;*.wsf;*.lnk|*.exe;*.bat;*.cmd;*.vbs;*.js;*.wsf;*.lnk" +
    "";
            // 
            // btnOK
            // 
            this.btnOK.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnOK.Location = new System.Drawing.Point(456, 528);
            this.btnOK.Name = "btnOK";
            this.btnOK.Size = new System.Drawing.Size(75, 23);
            this.btnOK.TabIndex = 0;
            this.btnOK.Text = "OK";
            this.btnOK.UseVisualStyleBackColor = true;
            this.btnOK.Click += new System.EventHandler(this.btnOK_Click);
            // 
            // cbLanguage
            // 
            this.cbLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbLanguage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbLanguage.FormattingEnabled = true;
            this.cbLanguage.Location = new System.Drawing.Point(16, 19);
            this.cbLanguage.Name = "cbLanguage";
            this.cbLanguage.Size = new System.Drawing.Size(164, 20);
            this.cbLanguage.TabIndex = 0;
            this.cbLanguage.DropDown += new System.EventHandler(this.cbLanguage_DropDown);
            this.cbLanguage.SelectedValueChanged += new System.EventHandler(this.cbLanguage_SelectedValueChanged);
            this.cbLanguage.MouseEnter += new System.EventHandler(this.cbLanguage_MouseEnter);
            // 
            // gbApplications
            // 
            this.gbApplications.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbApplications.Controls.Add(this.btnAccessKeyHint);
            this.gbApplications.Controls.Add(this.btnDefault);
            this.gbApplications.Controls.Add(this.btnDelete);
            this.gbApplications.Controls.Add(this.btnAdd);
            this.gbApplications.Controls.Add(this.dgvApplications);
            this.gbApplications.Location = new System.Drawing.Point(12, 351);
            this.gbApplications.Name = "gbApplications";
            this.gbApplications.Size = new System.Drawing.Size(600, 171);
            this.gbApplications.TabIndex = 10;
            this.gbApplications.TabStop = false;
            this.gbApplications.Text = "Applications";
            // 
            // btnAccessKeyHint
            // 
            this.btnAccessKeyHint.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnAccessKeyHint.AutoSize = true;
            this.btnAccessKeyHint.Location = new System.Drawing.Point(99, 137);
            this.btnAccessKeyHint.MinimumSize = new System.Drawing.Size(75, 23);
            this.btnAccessKeyHint.Name = "btnAccessKeyHint";
            this.btnAccessKeyHint.Size = new System.Drawing.Size(75, 23);
            this.btnAccessKeyHint.TabIndex = 3;
            this.btnAccessKeyHint.Text = "?";
            this.btnAccessKeyHint.UseVisualStyleBackColor = true;
            this.btnAccessKeyHint.Click += new System.EventHandler(this.btnAccessKeyHint_Click);
            // 
            // btnDefault
            // 
            this.btnDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDefault.Location = new System.Drawing.Point(18, 137);
            this.btnDefault.Name = "btnDefault";
            this.btnDefault.Size = new System.Drawing.Size(75, 23);
            this.btnDefault.TabIndex = 1;
            this.btnDefault.Text = "Default";
            this.btnDefault.UseVisualStyleBackColor = true;
            this.btnDefault.Click += new System.EventHandler(this.btnDefault_Click);
            // 
            // btnDelete
            // 
            this.btnDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDelete.Location = new System.Drawing.Point(507, 137);
            this.btnDelete.Name = "btnDelete";
            this.btnDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDelete.TabIndex = 5;
            this.btnDelete.Text = "Delete";
            this.btnDelete.UseVisualStyleBackColor = true;
            this.btnDelete.Click += new System.EventHandler(this.btnDelete_Click);
            // 
            // btnAdd
            // 
            this.btnAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnAdd.Location = new System.Drawing.Point(426, 137);
            this.btnAdd.Name = "btnAdd";
            this.btnAdd.Size = new System.Drawing.Size(75, 23);
            this.btnAdd.TabIndex = 4;
            this.btnAdd.Text = "Add";
            this.btnAdd.UseVisualStyleBackColor = true;
            this.btnAdd.Click += new System.EventHandler(this.btnAdd_Click);
            // 
            // dgvApplications
            // 
            this.dgvApplications.AllowUserToAddRows = false;
            this.dgvApplications.AllowUserToResizeColumns = false;
            this.dgvApplications.AllowUserToResizeRows = false;
            this.dgvApplications.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvApplications.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
            this.dgvApplications.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvApplications.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.tbcName,
            this.tbcCommandLineParameter,
            this.cbcMulti,
            this.tbcOpenByClicking,
            this.tbcShowInContextMenu});
            this.dgvApplications.Location = new System.Drawing.Point(18, 26);
            this.dgvApplications.Name = "dgvApplications";
            this.dgvApplications.RowHeadersVisible = false;
            this.dgvApplications.RowTemplate.Height = 21;
            this.dgvApplications.Size = new System.Drawing.Size(564, 105);
            this.dgvApplications.TabIndex = 0;
            this.dgvApplications.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvApplications_CellValidating);
            this.dgvApplications.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvApplications_CellValueChanged);
            this.dgvApplications.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvApplications_RowsAdded);
            this.dgvApplications.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.dgvApplications_RowsRemoved);
            this.dgvApplications.SelectionChanged += new System.EventHandler(this.dgvApplications_SelectionChanged);
            this.dgvApplications.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dgvApplications_MouseDown);
            // 
            // tbcName
            // 
            this.tbcName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.tbcName.HeaderText = "Name";
            this.tbcName.MinimumWidth = 60;
            this.tbcName.Name = "tbcName";
            this.tbcName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // tbcCommandLineParameter
            // 
            this.tbcCommandLineParameter.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.tbcCommandLineParameter.HeaderText = "Params";
            this.tbcCommandLineParameter.Name = "tbcCommandLineParameter";
            this.tbcCommandLineParameter.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.tbcCommandLineParameter.Width = 49;
            // 
            // cbcMulti
            // 
            this.cbcMulti.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.cbcMulti.HeaderText = "Multi";
            this.cbcMulti.Name = "cbcMulti";
            this.cbcMulti.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.cbcMulti.Width = 36;
            // 
            // tbcOpenByClicking
            // 
            this.tbcOpenByClicking.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.tbcOpenByClicking.HeaderText = "Open on click";
            this.tbcOpenByClicking.Name = "tbcOpenByClicking";
            this.tbcOpenByClicking.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.tbcOpenByClicking.Width = 81;
            // 
            // tbcShowInContextMenu
            // 
            this.tbcShowInContextMenu.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.tbcShowInContextMenu.HeaderText = "Context menu";
            this.tbcShowInContextMenu.Name = "tbcShowInContextMenu";
            this.tbcShowInContextMenu.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.tbcShowInContextMenu.Width = 82;
            // 
            // gbLanguage
            // 
            this.gbLanguage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbLanguage.Controls.Add(this.btnLanguageEdit);
            this.gbLanguage.Controls.Add(this.cbLanguage);
            this.gbLanguage.Location = new System.Drawing.Point(12, 12);
            this.gbLanguage.Name = "gbLanguage";
            this.gbLanguage.Size = new System.Drawing.Size(267, 50);
            this.gbLanguage.TabIndex = 2;
            this.gbLanguage.TabStop = false;
            this.gbLanguage.Text = "Language";
            // 
            // btnLanguageEdit
            // 
            this.btnLanguageEdit.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnLanguageEdit.Location = new System.Drawing.Point(186, 17);
            this.btnLanguageEdit.Name = "btnLanguageEdit";
            this.btnLanguageEdit.Size = new System.Drawing.Size(63, 23);
            this.btnLanguageEdit.TabIndex = 1;
            this.btnLanguageEdit.Text = "Edit";
            this.btnLanguageEdit.UseVisualStyleBackColor = true;
            this.btnLanguageEdit.Click += new System.EventHandler(this.btnLanguageEdit_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnCancel.Location = new System.Drawing.Point(537, 528);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 1;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // gbHistory
            // 
            this.gbHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbHistory.Controls.Add(this.cbHistory);
            this.gbHistory.Location = new System.Drawing.Point(285, 12);
            this.gbHistory.Name = "gbHistory";
            this.gbHistory.Size = new System.Drawing.Size(136, 50);
            this.gbHistory.TabIndex = 3;
            this.gbHistory.TabStop = false;
            this.gbHistory.Text = "Max num of histories";
            // 
            // cbHistory
            // 
            this.cbHistory.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbHistory.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbHistory.FormattingEnabled = true;
            this.cbHistory.Items.AddRange(new object[] {
            "10",
            "30",
            "100",
            "300",
            "1000",
            "3000",
            "10000"});
            this.cbHistory.Location = new System.Drawing.Point(16, 19);
            this.cbHistory.Name = "cbHistory";
            this.cbHistory.Size = new System.Drawing.Size(102, 20);
            this.cbHistory.TabIndex = 0;
            this.cbHistory.MouseEnter += new System.EventHandler(this.cbHistory_MouseEnter);
            // 
            // cmsLanguageEdit
            // 
            this.cmsLanguageEdit.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.openlanguageFolderToolStripMenuItem,
            this.updateAndEditLanguageFileToolStripMenuItem});
            this.cmsLanguageEdit.Name = "cmsLanguageEdit";
            this.cmsLanguageEdit.Size = new System.Drawing.Size(230, 48);
            // 
            // openlanguageFolderToolStripMenuItem
            // 
            this.openlanguageFolderToolStripMenuItem.Name = "openlanguageFolderToolStripMenuItem";
            this.openlanguageFolderToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this.openlanguageFolderToolStripMenuItem.Text = "Open \"language\" folder";
            this.openlanguageFolderToolStripMenuItem.Click += new System.EventHandler(this.openlanguageFolderToolStripMenuItem_Click);
            // 
            // updateAndEditLanguageFileToolStripMenuItem
            // 
            this.updateAndEditLanguageFileToolStripMenuItem.Name = "updateAndEditLanguageFileToolStripMenuItem";
            this.updateAndEditLanguageFileToolStripMenuItem.Size = new System.Drawing.Size(229, 22);
            this.updateAndEditLanguageFileToolStripMenuItem.Text = "Update and edit language file";
            this.updateAndEditLanguageFileToolStripMenuItem.Click += new System.EventHandler(this.updateAndEditLanguageFileToolStripMenuItem_Click);
            // 
            // cmsApplicationAdd
            // 
            this.cmsApplicationAdd.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.externalApplicationToolStripMenuItem,
            this.moveFilesfoldersToolStripMenuItem,
            this.sendTheItemInTheClipboardToolStripMenuItem,
            this.toolStripMenuItem1,
            this.builtinBookReaderToolStripMenuItem,
            this.associatedApplicationToolStripMenuItem,
            this.moveLocationToolStripMenuItem,
            this.explorerToolStripMenuItem});
            this.cmsApplicationAdd.Name = "cmsApplicationAdd";
            this.cmsApplicationAdd.Size = new System.Drawing.Size(247, 164);
            // 
            // externalApplicationToolStripMenuItem
            // 
            this.externalApplicationToolStripMenuItem.Name = "externalApplicationToolStripMenuItem";
            this.externalApplicationToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.externalApplicationToolStripMenuItem.Text = "External application";
            this.externalApplicationToolStripMenuItem.Click += new System.EventHandler(this.externalApplicationToolStripMenuItem_Click);
            // 
            // moveFilesfoldersToolStripMenuItem
            // 
            this.moveFilesfoldersToolStripMenuItem.Name = "moveFilesfoldersToolStripMenuItem";
            this.moveFilesfoldersToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.moveFilesfoldersToolStripMenuItem.Text = "Move files/folders";
            this.moveFilesfoldersToolStripMenuItem.Click += new System.EventHandler(this.moveFilesfoldersToolStripMenuItem_Click);
            // 
            // sendTheItemInTheClipboardToolStripMenuItem
            // 
            this.sendTheItemInTheClipboardToolStripMenuItem.Name = "sendTheItemInTheClipboardToolStripMenuItem";
            this.sendTheItemInTheClipboardToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.sendTheItemInTheClipboardToolStripMenuItem.Text = "Send to the item in the clipboard";
            this.sendTheItemInTheClipboardToolStripMenuItem.Click += new System.EventHandler(this.sendTheItemInTheClipboardToolStripMenuItem_Click);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(243, 6);
            // 
            // builtinBookReaderToolStripMenuItem
            // 
            this.builtinBookReaderToolStripMenuItem.Name = "builtinBookReaderToolStripMenuItem";
            this.builtinBookReaderToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.builtinBookReaderToolStripMenuItem.Text = "Built-in book reader";
            this.builtinBookReaderToolStripMenuItem.Click += new System.EventHandler(this.builtinBookReaderToolStripMenuItem_Click);
            // 
            // associatedApplicationToolStripMenuItem
            // 
            this.associatedApplicationToolStripMenuItem.Name = "associatedApplicationToolStripMenuItem";
            this.associatedApplicationToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.associatedApplicationToolStripMenuItem.Text = "Associated application";
            this.associatedApplicationToolStripMenuItem.Click += new System.EventHandler(this.associatedApplicationToolStripMenuItem_Click);
            // 
            // moveLocationToolStripMenuItem
            // 
            this.moveLocationToolStripMenuItem.Name = "moveLocationToolStripMenuItem";
            this.moveLocationToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.moveLocationToolStripMenuItem.Text = "Move location";
            this.moveLocationToolStripMenuItem.Click += new System.EventHandler(this.moveLocationToolStripMenuItem_Click);
            // 
            // explorerToolStripMenuItem
            // 
            this.explorerToolStripMenuItem.Name = "explorerToolStripMenuItem";
            this.explorerToolStripMenuItem.Size = new System.Drawing.Size(246, 22);
            this.explorerToolStripMenuItem.Text = "Explorer";
            this.explorerToolStripMenuItem.Click += new System.EventHandler(this.explorerToolStripMenuItem_Click);
            // 
            // gbHint
            // 
            this.gbHint.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbHint.Controls.Add(this.cbNgen);
            this.gbHint.Controls.Add(this.cbHint);
            this.gbHint.Location = new System.Drawing.Point(12, 124);
            this.gbHint.Name = "gbHint";
            this.gbHint.Size = new System.Drawing.Size(211, 44);
            this.gbHint.TabIndex = 6;
            this.gbHint.TabStop = false;
            this.gbHint.Text = "Hint message";
            // 
            // cbNgen
            // 
            this.cbNgen.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbNgen.AutoSize = true;
            this.cbNgen.Location = new System.Drawing.Point(121, 19);
            this.cbNgen.Name = "cbNgen";
            this.cbNgen.Size = new System.Drawing.Size(68, 16);
            this.cbNgen.TabIndex = 10;
            this.cbNgen.Text = "ngen.exe";
            this.cbNgen.UseVisualStyleBackColor = true;
            this.cbNgen.CheckedChanged += new System.EventHandler(this.cbNgen_CheckedChanged);
            // 
            // cbHint
            // 
            this.cbHint.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbHint.AutoSize = true;
            this.cbHint.Location = new System.Drawing.Point(16, 19);
            this.cbHint.Name = "cbHint";
            this.cbHint.Size = new System.Drawing.Size(79, 16);
            this.cbHint.TabIndex = 9;
            this.cbHint.Text = "ffmpeg.exe";
            this.cbHint.UseVisualStyleBackColor = true;
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
            // gbDragAndDrop
            // 
            this.gbDragAndDrop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbDragAndDrop.Controls.Add(this.rbMoveOrCopy);
            this.gbDragAndDrop.Controls.Add(this.rbShowThumbnails);
            this.gbDragAndDrop.Location = new System.Drawing.Point(229, 68);
            this.gbDragAndDrop.Name = "gbDragAndDrop";
            this.gbDragAndDrop.Size = new System.Drawing.Size(164, 100);
            this.gbDragAndDrop.TabIndex = 7;
            this.gbDragAndDrop.TabStop = false;
            this.gbDragAndDrop.Text = "Drag and drop";
            // 
            // rbMoveOrCopy
            // 
            this.rbMoveOrCopy.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.rbMoveOrCopy.AutoSize = true;
            this.rbMoveOrCopy.BackColor = System.Drawing.Color.Transparent;
            this.rbMoveOrCopy.Location = new System.Drawing.Point(16, 65);
            this.rbMoveOrCopy.Name = "rbMoveOrCopy";
            this.rbMoveOrCopy.Size = new System.Drawing.Size(106, 16);
            this.rbMoveOrCopy.TabIndex = 0;
            this.rbMoveOrCopy.Text = "Move/copy files";
            this.rbMoveOrCopy.UseVisualStyleBackColor = false;
            // 
            // rbShowThumbnails
            // 
            this.rbShowThumbnails.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.rbShowThumbnails.AutoSize = true;
            this.rbShowThumbnails.Checked = true;
            this.rbShowThumbnails.Location = new System.Drawing.Point(16, 35);
            this.rbShowThumbnails.Name = "rbShowThumbnails";
            this.rbShowThumbnails.Size = new System.Drawing.Size(109, 16);
            this.rbShowThumbnails.TabIndex = 0;
            this.rbShowThumbnails.TabStop = true;
            this.rbShowThumbnails.Text = "Show thumbnails";
            this.rbShowThumbnails.UseVisualStyleBackColor = true;
            // 
            // gbDynamicStringSelection
            // 
            this.gbDynamicStringSelection.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbDynamicStringSelection.Controls.Add(this.cbDynamicStringSelectionAllowUserToRename);
            this.gbDynamicStringSelection.Controls.Add(this.dgvDynamicStringSelection);
            this.gbDynamicStringSelection.Controls.Add(this.btnDynamicStringSelectionDelete);
            this.gbDynamicStringSelection.Controls.Add(this.btnDynamicStringSelectionAdd);
            this.gbDynamicStringSelection.Controls.Add(this.btnDynamicStringSelectionDefault);
            this.gbDynamicStringSelection.Controls.Add(this.cbDynamicStringSelectionEnabled);
            this.gbDynamicStringSelection.Location = new System.Drawing.Point(12, 174);
            this.gbDynamicStringSelection.Name = "gbDynamicStringSelection";
            this.gbDynamicStringSelection.Size = new System.Drawing.Size(600, 171);
            this.gbDynamicStringSelection.TabIndex = 9;
            this.gbDynamicStringSelection.TabStop = false;
            this.gbDynamicStringSelection.Text = "Dynamic string selection";
            // 
            // cbDynamicStringSelectionAllowUserToRename
            // 
            this.cbDynamicStringSelectionAllowUserToRename.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbDynamicStringSelectionAllowUserToRename.AutoSize = true;
            this.cbDynamicStringSelectionAllowUserToRename.Location = new System.Drawing.Point(169, 141);
            this.cbDynamicStringSelectionAllowUserToRename.Name = "cbDynamicStringSelectionAllowUserToRename";
            this.cbDynamicStringSelectionAllowUserToRename.Size = new System.Drawing.Size(211, 16);
            this.cbDynamicStringSelectionAllowUserToRename.TabIndex = 3;
            this.cbDynamicStringSelectionAllowUserToRename.Text = "Rename item by editing the text box";
            this.cbDynamicStringSelectionAllowUserToRename.UseVisualStyleBackColor = true;
            // 
            // dgvDynamicStringSelection
            // 
            this.dgvDynamicStringSelection.AllowUserToAddRows = false;
            this.dgvDynamicStringSelection.AllowUserToResizeColumns = false;
            this.dgvDynamicStringSelection.AllowUserToResizeRows = false;
            this.dgvDynamicStringSelection.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.dgvDynamicStringSelection.ClipboardCopyMode = System.Windows.Forms.DataGridViewClipboardCopyMode.Disable;
            this.dgvDynamicStringSelection.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.dgvDynamicStringSelection.Columns.AddRange(new System.Windows.Forms.DataGridViewColumn[] {
            this.tbcDisplayName,
            this.tbcDisplayNameForEmptyString,
            this.tbcCommand});
            this.dgvDynamicStringSelection.Location = new System.Drawing.Point(18, 26);
            this.dgvDynamicStringSelection.Name = "dgvDynamicStringSelection";
            this.dgvDynamicStringSelection.RowHeadersVisible = false;
            this.dgvDynamicStringSelection.RowTemplate.Height = 21;
            this.dgvDynamicStringSelection.Size = new System.Drawing.Size(564, 105);
            this.dgvDynamicStringSelection.TabIndex = 0;
            this.dgvDynamicStringSelection.CellValidating += new System.Windows.Forms.DataGridViewCellValidatingEventHandler(this.dgvDynamicStringSelection_CellValidating);
            this.dgvDynamicStringSelection.CellValueChanged += new System.Windows.Forms.DataGridViewCellEventHandler(this.dgvDynamicStringSelection_CellValueChanged);
            this.dgvDynamicStringSelection.RowsAdded += new System.Windows.Forms.DataGridViewRowsAddedEventHandler(this.dgvDynamicStringSelection_RowsAdded);
            this.dgvDynamicStringSelection.RowsRemoved += new System.Windows.Forms.DataGridViewRowsRemovedEventHandler(this.dgvDynamicStringSelection_RowsRemoved);
            this.dgvDynamicStringSelection.SelectionChanged += new System.EventHandler(this.dgvDynamicStringSelection_SelectionChanged);
            this.dgvDynamicStringSelection.MouseDown += new System.Windows.Forms.MouseEventHandler(this.dgvDynamicStringSelection_MouseDown);
            // 
            // tbcDisplayName
            // 
            this.tbcDisplayName.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.tbcDisplayName.HeaderText = "Display name";
            this.tbcDisplayName.Name = "tbcDisplayName";
            this.tbcDisplayName.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.tbcDisplayName.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.tbcDisplayName.Width = 72;
            // 
            // tbcDisplayNameForEmptyString
            // 
            this.tbcDisplayNameForEmptyString.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.AllCells;
            this.tbcDisplayNameForEmptyString.HeaderText = "For empty string";
            this.tbcDisplayNameForEmptyString.Name = "tbcDisplayNameForEmptyString";
            this.tbcDisplayNameForEmptyString.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.tbcDisplayNameForEmptyString.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            this.tbcDisplayNameForEmptyString.Width = 86;
            // 
            // tbcCommand
            // 
            this.tbcCommand.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.tbcCommand.HeaderText = "Command";
            this.tbcCommand.Name = "tbcCommand";
            this.tbcCommand.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.tbcCommand.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // btnDynamicStringSelectionDelete
            // 
            this.btnDynamicStringSelectionDelete.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDynamicStringSelectionDelete.Location = new System.Drawing.Point(507, 137);
            this.btnDynamicStringSelectionDelete.Name = "btnDynamicStringSelectionDelete";
            this.btnDynamicStringSelectionDelete.Size = new System.Drawing.Size(75, 23);
            this.btnDynamicStringSelectionDelete.TabIndex = 5;
            this.btnDynamicStringSelectionDelete.Text = "Delete";
            this.btnDynamicStringSelectionDelete.UseVisualStyleBackColor = true;
            this.btnDynamicStringSelectionDelete.Click += new System.EventHandler(this.btnDynamicStringSelectionDelete_Click);
            // 
            // btnDynamicStringSelectionAdd
            // 
            this.btnDynamicStringSelectionAdd.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
            this.btnDynamicStringSelectionAdd.Location = new System.Drawing.Point(426, 137);
            this.btnDynamicStringSelectionAdd.Name = "btnDynamicStringSelectionAdd";
            this.btnDynamicStringSelectionAdd.Size = new System.Drawing.Size(75, 23);
            this.btnDynamicStringSelectionAdd.TabIndex = 4;
            this.btnDynamicStringSelectionAdd.Text = "Add";
            this.btnDynamicStringSelectionAdd.UseVisualStyleBackColor = true;
            this.btnDynamicStringSelectionAdd.Click += new System.EventHandler(this.btnDynamicStringSelectionAdd_Click);
            // 
            // btnDynamicStringSelectionDefault
            // 
            this.btnDynamicStringSelectionDefault.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.btnDynamicStringSelectionDefault.Location = new System.Drawing.Point(18, 137);
            this.btnDynamicStringSelectionDefault.Name = "btnDynamicStringSelectionDefault";
            this.btnDynamicStringSelectionDefault.Size = new System.Drawing.Size(75, 23);
            this.btnDynamicStringSelectionDefault.TabIndex = 1;
            this.btnDynamicStringSelectionDefault.Text = "Default";
            this.btnDynamicStringSelectionDefault.UseVisualStyleBackColor = true;
            this.btnDynamicStringSelectionDefault.Click += new System.EventHandler(this.btnDynamicStringSelectionDefault_Click);
            // 
            // cbDynamicStringSelectionEnabled
            // 
            this.cbDynamicStringSelectionEnabled.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
            this.cbDynamicStringSelectionEnabled.AutoSize = true;
            this.cbDynamicStringSelectionEnabled.Location = new System.Drawing.Point(99, 141);
            this.cbDynamicStringSelectionEnabled.Name = "cbDynamicStringSelectionEnabled";
            this.cbDynamicStringSelectionEnabled.Size = new System.Drawing.Size(64, 16);
            this.cbDynamicStringSelectionEnabled.TabIndex = 2;
            this.cbDynamicStringSelectionEnabled.Text = "Enabled";
            this.cbDynamicStringSelectionEnabled.UseVisualStyleBackColor = true;
            this.cbDynamicStringSelectionEnabled.CheckedChanged += new System.EventHandler(this.cbDynamicStringSelectionEnabled_CheckedChanged);
            // 
            // cmsDDSAdd
            // 
            this.cmsDDSAdd.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.filterWithSelectedTextToolStripMenuItem,
            this.copySelectedTextToolStripMenuItem,
            this.searchSelectedTextOnGoogleToolStripMenuItem,
            this.newCustomCommandToolStripMenuItem});
            this.cmsDDSAdd.Name = "cmsDDSAdd";
            this.cmsDDSAdd.Size = new System.Drawing.Size(237, 92);
            // 
            // filterWithSelectedTextToolStripMenuItem
            // 
            this.filterWithSelectedTextToolStripMenuItem.Name = "filterWithSelectedTextToolStripMenuItem";
            this.filterWithSelectedTextToolStripMenuItem.Size = new System.Drawing.Size(236, 22);
            this.filterWithSelectedTextToolStripMenuItem.Text = "Filter with selected text";
            this.filterWithSelectedTextToolStripMenuItem.Click += new System.EventHandler(this.filterWithSelectedTextToolStripMenuItem_Click);
            // 
            // copySelectedTextToolStripMenuItem
            // 
            this.copySelectedTextToolStripMenuItem.Name = "copySelectedTextToolStripMenuItem";
            this.copySelectedTextToolStripMenuItem.Size = new System.Drawing.Size(236, 22);
            this.copySelectedTextToolStripMenuItem.Text = "Copy selected text";
            this.copySelectedTextToolStripMenuItem.Click += new System.EventHandler(this.copySelectedTextToolStripMenuItem_Click);
            // 
            // searchSelectedTextOnGoogleToolStripMenuItem
            // 
            this.searchSelectedTextOnGoogleToolStripMenuItem.Name = "searchSelectedTextOnGoogleToolStripMenuItem";
            this.searchSelectedTextOnGoogleToolStripMenuItem.Size = new System.Drawing.Size(236, 22);
            this.searchSelectedTextOnGoogleToolStripMenuItem.Text = "Search selected text on Google";
            this.searchSelectedTextOnGoogleToolStripMenuItem.Click += new System.EventHandler(this.searchSelectedTextOnGoogleToolStripMenuItem_Click);
            // 
            // newCustomCommandToolStripMenuItem
            // 
            this.newCustomCommandToolStripMenuItem.Name = "newCustomCommandToolStripMenuItem";
            this.newCustomCommandToolStripMenuItem.Size = new System.Drawing.Size(236, 22);
            this.newCustomCommandToolStripMenuItem.Text = "New custom command";
            this.newCustomCommandToolStripMenuItem.Click += new System.EventHandler(this.newCustomCommandToolStripMenuItem_Click);
            // 
            // cmsDynamicStringSelection
            // 
            this.cmsDynamicStringSelection.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.deleteToolStripMenuItem1});
            this.cmsDynamicStringSelection.Name = "cmsDynamicStringSelection";
            this.cmsDynamicStringSelection.Size = new System.Drawing.Size(132, 26);
            // 
            // deleteToolStripMenuItem1
            // 
            this.deleteToolStripMenuItem1.Name = "deleteToolStripMenuItem1";
            this.deleteToolStripMenuItem1.ShortcutKeys = System.Windows.Forms.Keys.Delete;
            this.deleteToolStripMenuItem1.Size = new System.Drawing.Size(131, 22);
            this.deleteToolStripMenuItem1.Text = "Delete";
            this.deleteToolStripMenuItem1.Click += new System.EventHandler(this.deleteToolStripMenuItem1_Click);
            // 
            // folderBrowserDialogMoveFilesFolders
            // 
            this.folderBrowserDialogMoveFilesFolders.ShowNewFolderButton = false;
            // 
            // gbViewerMemoryUsage
            // 
            this.gbViewerMemoryUsage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbViewerMemoryUsage.Controls.Add(this.btnViewerMemoryUsageHelp);
            this.gbViewerMemoryUsage.Controls.Add(this.cbViewerMemoryUsage);
            this.gbViewerMemoryUsage.Location = new System.Drawing.Point(427, 12);
            this.gbViewerMemoryUsage.Name = "gbViewerMemoryUsage";
            this.gbViewerMemoryUsage.Size = new System.Drawing.Size(185, 50);
            this.gbViewerMemoryUsage.TabIndex = 4;
            this.gbViewerMemoryUsage.TabStop = false;
            this.gbViewerMemoryUsage.Text = "Viewer memory usage";
            // 
            // btnViewerMemoryUsageHelp
            // 
            this.btnViewerMemoryUsageHelp.Anchor = System.Windows.Forms.AnchorStyles.Right;
            this.btnViewerMemoryUsageHelp.Location = new System.Drawing.Point(144, 17);
            this.btnViewerMemoryUsageHelp.Name = "btnViewerMemoryUsageHelp";
            this.btnViewerMemoryUsageHelp.Size = new System.Drawing.Size(23, 23);
            this.btnViewerMemoryUsageHelp.TabIndex = 1;
            this.btnViewerMemoryUsageHelp.Text = "?";
            this.btnViewerMemoryUsageHelp.UseVisualStyleBackColor = true;
            this.btnViewerMemoryUsageHelp.Click += new System.EventHandler(this.btnViewerMemoryUsageHelp_Click);
            // 
            // cbViewerMemoryUsage
            // 
            this.cbViewerMemoryUsage.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right)));
            this.cbViewerMemoryUsage.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbViewerMemoryUsage.FormattingEnabled = true;
            this.cbViewerMemoryUsage.Items.AddRange(new object[] {
            "Minimum",
            "Around 256 MB",
            "Around 512 MB",
            "Around 1 GB",
            "Around 2 GB",
            "Around 4 GB",
            "Around 8 GB",
            "Around 16 GB",
            "Around 32 GB",
            "Automatic"});
            this.cbViewerMemoryUsage.Location = new System.Drawing.Point(16, 19);
            this.cbViewerMemoryUsage.Name = "cbViewerMemoryUsage";
            this.cbViewerMemoryUsage.Size = new System.Drawing.Size(122, 20);
            this.cbViewerMemoryUsage.TabIndex = 0;
            this.cbViewerMemoryUsage.MouseEnter += new System.EventHandler(this.cbViewerMemoryUsage_MouseEnter);
            // 
            // gbSusiePlugins
            // 
            this.gbSusiePlugins.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.gbSusiePlugins.Controls.Add(this.cbSearchAlsoSusieInstallationFolder);
            this.gbSusiePlugins.Controls.Add(this.cbAllowUntestedPlugins);
            this.gbSusiePlugins.Location = new System.Drawing.Point(399, 68);
            this.gbSusiePlugins.Name = "gbSusiePlugins";
            this.gbSusiePlugins.Size = new System.Drawing.Size(213, 100);
            this.gbSusiePlugins.TabIndex = 8;
            this.gbSusiePlugins.TabStop = false;
            this.gbSusiePlugins.Text = "Susie Plug-ins";
            // 
            // cbSearchAlsoSusieInstallationFolder
            // 
            this.cbSearchAlsoSusieInstallationFolder.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbSearchAlsoSusieInstallationFolder.AutoSize = true;
            this.cbSearchAlsoSusieInstallationFolder.Location = new System.Drawing.Point(16, 65);
            this.cbSearchAlsoSusieInstallationFolder.Name = "cbSearchAlsoSusieInstallationFolder";
            this.cbSearchAlsoSusieInstallationFolder.Size = new System.Drawing.Size(184, 16);
            this.cbSearchAlsoSusieInstallationFolder.TabIndex = 1;
            this.cbSearchAlsoSusieInstallationFolder.Text = "Search also Susie install folder";
            this.cbSearchAlsoSusieInstallationFolder.UseVisualStyleBackColor = true;
            // 
            // cbAllowUntestedPlugins
            // 
            this.cbAllowUntestedPlugins.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbAllowUntestedPlugins.AutoSize = true;
            this.cbAllowUntestedPlugins.Location = new System.Drawing.Point(16, 35);
            this.cbAllowUntestedPlugins.Name = "cbAllowUntestedPlugins";
            this.cbAllowUntestedPlugins.Size = new System.Drawing.Size(146, 16);
            this.cbAllowUntestedPlugins.TabIndex = 0;
            this.cbAllowUntestedPlugins.Text = "Allow untested plug-ins";
            this.cbAllowUntestedPlugins.UseVisualStyleBackColor = true;
            // 
            // dataGridViewTextBoxColumn1
            // 
            this.dataGridViewTextBoxColumn1.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn1.HeaderText = "Name";
            this.dataGridViewTextBoxColumn1.MinimumWidth = 60;
            this.dataGridViewTextBoxColumn1.Name = "dataGridViewTextBoxColumn1";
            this.dataGridViewTextBoxColumn1.ReadOnly = true;
            this.dataGridViewTextBoxColumn1.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn2
            // 
            this.dataGridViewTextBoxColumn2.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn2.HeaderText = "Params";
            this.dataGridViewTextBoxColumn2.Name = "dataGridViewTextBoxColumn2";
            this.dataGridViewTextBoxColumn2.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn3
            // 
            this.dataGridViewTextBoxColumn3.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn3.HeaderText = "Open on click";
            this.dataGridViewTextBoxColumn3.Name = "dataGridViewTextBoxColumn3";
            this.dataGridViewTextBoxColumn3.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn4
            // 
            this.dataGridViewTextBoxColumn4.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn4.HeaderText = "Context menu";
            this.dataGridViewTextBoxColumn4.Name = "dataGridViewTextBoxColumn4";
            this.dataGridViewTextBoxColumn4.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn5
            // 
            this.dataGridViewTextBoxColumn5.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn5.HeaderText = "Display name";
            this.dataGridViewTextBoxColumn5.Name = "dataGridViewTextBoxColumn5";
            this.dataGridViewTextBoxColumn5.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewTextBoxColumn5.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn6
            // 
            this.dataGridViewTextBoxColumn6.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.DisplayedCells;
            this.dataGridViewTextBoxColumn6.HeaderText = "For empty string";
            this.dataGridViewTextBoxColumn6.Name = "dataGridViewTextBoxColumn6";
            this.dataGridViewTextBoxColumn6.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewTextBoxColumn6.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // dataGridViewTextBoxColumn7
            // 
            this.dataGridViewTextBoxColumn7.AutoSizeMode = System.Windows.Forms.DataGridViewAutoSizeColumnMode.Fill;
            this.dataGridViewTextBoxColumn7.HeaderText = "Command";
            this.dataGridViewTextBoxColumn7.Name = "dataGridViewTextBoxColumn7";
            this.dataGridViewTextBoxColumn7.Resizable = System.Windows.Forms.DataGridViewTriState.False;
            this.dataGridViewTextBoxColumn7.SortMode = System.Windows.Forms.DataGridViewColumnSortMode.NotSortable;
            // 
            // gbZipPlaUpdateCheck
            // 
            this.gbZipPlaUpdateCheck.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.gbZipPlaUpdateCheck.Controls.Add(this.btnCheckNow);
            this.gbZipPlaUpdateCheck.Controls.Add(this.cbAskPermissionBeforeConnection);
            this.gbZipPlaUpdateCheck.Location = new System.Drawing.Point(12, 68);
            this.gbZipPlaUpdateCheck.Name = "gbZipPlaUpdateCheck";
            this.gbZipPlaUpdateCheck.Size = new System.Drawing.Size(211, 50);
            this.gbZipPlaUpdateCheck.TabIndex = 5;
            this.gbZipPlaUpdateCheck.TabStop = false;
            this.gbZipPlaUpdateCheck.Text = "ZipPla update check";
            // 
            // btnCheckNow
            // 
            this.btnCheckNow.Location = new System.Drawing.Point(123, 19);
            this.btnCheckNow.Name = "btnCheckNow";
            this.btnCheckNow.Size = new System.Drawing.Size(75, 23);
            this.btnCheckNow.TabIndex = 1;
            this.btnCheckNow.Text = "Check now";
            this.btnCheckNow.UseVisualStyleBackColor = true;
            this.btnCheckNow.Click += new System.EventHandler(this.btnCheckNow_Click);
            // 
            // cbAskPermissionBeforeConnection
            // 
            this.cbAskPermissionBeforeConnection.Anchor = System.Windows.Forms.AnchorStyles.Left;
            this.cbAskPermissionBeforeConnection.AutoSize = true;
            this.cbAskPermissionBeforeConnection.Location = new System.Drawing.Point(16, 23);
            this.cbAskPermissionBeforeConnection.Name = "cbAskPermissionBeforeConnection";
            this.cbAskPermissionBeforeConnection.Size = new System.Drawing.Size(151, 16);
            this.cbAskPermissionBeforeConnection.TabIndex = 0;
            this.cbAskPermissionBeforeConnection.Text = "Dialog before connection";
            this.cbAskPermissionBeforeConnection.UseVisualStyleBackColor = true;
            // 
            // SettingForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(96F, 96F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Dpi;
            this.ClientSize = new System.Drawing.Size(624, 563);
            this.Controls.Add(this.gbZipPlaUpdateCheck);
            this.Controls.Add(this.gbSusiePlugins);
            this.Controls.Add(this.gbViewerMemoryUsage);
            this.Controls.Add(this.gbHint);
            this.Controls.Add(this.gbDynamicStringSelection);
            this.Controls.Add(this.gbDragAndDrop);
            this.Controls.Add(this.gbHistory);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.gbLanguage);
            this.Controls.Add(this.gbApplications);
            this.Controls.Add(this.btnOK);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MinimizeBox = false;
            this.MinimumSize = new System.Drawing.Size(640, 400);
            this.Name = "SettingForm";
            this.Text = "SettingForm";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.SettingForm_FormClosing);
            this.Shown += new System.EventHandler(this.SettingForm_Shown);
            this.SizeChanged += new System.EventHandler(this.SettingForm_SizeChanged);
            this.gbApplications.ResumeLayout(false);
            this.gbApplications.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvApplications)).EndInit();
            this.gbLanguage.ResumeLayout(false);
            this.gbHistory.ResumeLayout(false);
            this.cmsLanguageEdit.ResumeLayout(false);
            this.cmsApplicationAdd.ResumeLayout(false);
            this.gbHint.ResumeLayout(false);
            this.gbHint.PerformLayout();
            this.cmsDelete.ResumeLayout(false);
            this.gbDragAndDrop.ResumeLayout(false);
            this.gbDragAndDrop.PerformLayout();
            this.gbDynamicStringSelection.ResumeLayout(false);
            this.gbDynamicStringSelection.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.dgvDynamicStringSelection)).EndInit();
            this.cmsDDSAdd.ResumeLayout(false);
            this.cmsDynamicStringSelection.ResumeLayout(false);
            this.gbViewerMemoryUsage.ResumeLayout(false);
            this.gbSusiePlugins.ResumeLayout(false);
            this.gbSusiePlugins.PerformLayout();
            this.gbZipPlaUpdateCheck.ResumeLayout(false);
            this.gbZipPlaUpdateCheck.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.OpenFileDialog openFileDialogExecutableFile;
        private System.Windows.Forms.Button btnOK;
        private System.Windows.Forms.ComboBox cbLanguage;
        private System.Windows.Forms.GroupBox gbApplications;
        private System.Windows.Forms.GroupBox gbLanguage;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.GroupBox gbHistory;
        private System.Windows.Forms.ComboBox cbHistory;
        private System.Windows.Forms.Button btnLanguageEdit;
        private System.Windows.Forms.ContextMenuStrip cmsLanguageEdit;
        private System.Windows.Forms.ToolStripMenuItem openlanguageFolderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem updateAndEditLanguageFileToolStripMenuItem;
        private System.Windows.Forms.DataGridView dgvApplications;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn1;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn2;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn3;
        private System.Windows.Forms.Button btnAdd;
        private System.Windows.Forms.ContextMenuStrip cmsApplicationAdd;
        private System.Windows.Forms.ToolStripMenuItem builtinBookReaderToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem associatedApplicationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem externalApplicationToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem explorerToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem moveLocationToolStripMenuItem;
        private System.Windows.Forms.Button btnDelete;
        private System.Windows.Forms.Button btnDefault;
        private System.Windows.Forms.ToolStripSeparator toolStripMenuItem1;
        private System.Windows.Forms.GroupBox gbHint;
        private System.Windows.Forms.CheckBox cbHint;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn4;
        private System.Windows.Forms.ContextMenuStrip cmsDelete;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem;
        private System.Windows.Forms.GroupBox gbDragAndDrop;
        private System.Windows.Forms.RadioButton rbMoveOrCopy;
        private System.Windows.Forms.RadioButton rbShowThumbnails;
        private System.Windows.Forms.GroupBox gbDynamicStringSelection;
        private System.Windows.Forms.Button btnDynamicStringSelectionDelete;
        private System.Windows.Forms.Button btnDynamicStringSelectionAdd;
        private System.Windows.Forms.Button btnDynamicStringSelectionDefault;
        private System.Windows.Forms.CheckBox cbDynamicStringSelectionEnabled;
        private System.Windows.Forms.DataGridView dgvDynamicStringSelection;
        private System.Windows.Forms.ContextMenuStrip cmsDDSAdd;
        private System.Windows.Forms.ContextMenuStrip cmsDynamicStringSelection;
        private System.Windows.Forms.ToolStripMenuItem deleteToolStripMenuItem1;
        private System.Windows.Forms.ToolStripMenuItem filterWithSelectedTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem copySelectedTextToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem searchSelectedTextOnGoogleToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem newCustomCommandToolStripMenuItem;
        private System.Windows.Forms.CheckBox cbDynamicStringSelectionAllowUserToRename;
        private System.Windows.Forms.ToolStripMenuItem moveFilesfoldersToolStripMenuItem;
        private System.Windows.Forms.FolderBrowserDialog folderBrowserDialogMoveFilesFolders;
        private System.Windows.Forms.ToolStripMenuItem sendTheItemInTheClipboardToolStripMenuItem;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn5;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn6;
        private System.Windows.Forms.DataGridViewTextBoxColumn dataGridViewTextBoxColumn7;
        private System.Windows.Forms.GroupBox gbViewerMemoryUsage;
        private System.Windows.Forms.ComboBox cbViewerMemoryUsage;
        private System.Windows.Forms.GroupBox gbSusiePlugins;
        private System.Windows.Forms.CheckBox cbSearchAlsoSusieInstallationFolder;
        private System.Windows.Forms.CheckBox cbAllowUntestedPlugins;
        private System.Windows.Forms.Button btnViewerMemoryUsageHelp;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcDisplayName;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcDisplayNameForEmptyString;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcCommand;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcName;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcCommandLineParameter;
        private System.Windows.Forms.DataGridViewCheckBoxColumn cbcMulti;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcOpenByClicking;
        private System.Windows.Forms.DataGridViewTextBoxColumn tbcShowInContextMenu;
        private System.Windows.Forms.CheckBox cbNgen;
        private System.Windows.Forms.GroupBox gbZipPlaUpdateCheck;
        private System.Windows.Forms.Button btnCheckNow;
        private System.Windows.Forms.CheckBox cbAskPermissionBeforeConnection;
        private System.Windows.Forms.Button btnAccessKeyHint;
    }
}