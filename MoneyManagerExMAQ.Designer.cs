namespace MoneyManagerExMAQ
{
    partial class MoneyManagerExMAQ
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
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

        // Layout: a docked table (header, toolbar, grids, write buttons). The two grids share a
        // draggable splitter, and both grow with the window; their columns fill the width.
        private void InitializeComponent()
        {
            tlpMain = new TableLayoutPanel();
            tlpHeader = new TableLayoutPanel();
            lblDb = new Label();
            btnSettings = new Button();
            flpToolbar = new FlowLayoutPanel();
            btnScanDownloads = new Button();
            btnAddFile = new Button();
            splitGrids = new SplitContainer();
            lblFiles = new Label();
            dgvFiles = new DataGridView();
            colFileName = new DataGridViewTextBoxColumn();
            colBankLabel = new DataGridViewTextBoxColumn();
            colMmexAccount = new DataGridViewComboBoxColumn();
            colRows = new DataGridViewTextBoxColumn();
            colNew = new DataGridViewTextBoxColumn();
            colIssues = new DataGridViewTextBoxColumn();
            colStatus = new DataGridViewTextBoxColumn();
            lblTransactions = new Label();
            dgvTransactions = new DataGridView();
            colDate = new DataGridViewTextBoxColumn();
            colDetails = new DataGridViewTextBoxColumn();
            colCategory = new DataGridViewTextBoxColumn();
            colSubcategory = new DataGridViewTextBoxColumn();
            colDebit = new DataGridViewTextBoxColumn();
            colCredit = new DataGridViewTextBoxColumn();
            colType = new DataGridViewTextBoxColumn();
            colOriginal = new DataGridViewTextBoxColumn();
            flpWriteButtons = new FlowLayoutPanel();
            btnWriteSelected = new Button();
            btnWriteAll = new Button();
            tlpMain.SuspendLayout();
            tlpHeader.SuspendLayout();
            flpToolbar.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)splitGrids).BeginInit();
            splitGrids.Panel1.SuspendLayout();
            splitGrids.Panel2.SuspendLayout();
            splitGrids.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)dgvFiles).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTransactions).BeginInit();
            flpWriteButtons.SuspendLayout();
            SuspendLayout();
            //
            // tlpMain
            //
            tlpMain.ColumnCount = 1;
            tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMain.Controls.Add(tlpHeader, 0, 0);
            tlpMain.Controls.Add(flpToolbar, 0, 1);
            tlpMain.Controls.Add(splitGrids, 0, 2);
            tlpMain.Controls.Add(flpWriteButtons, 0, 3);
            tlpMain.Dock = DockStyle.Fill;
            tlpMain.Name = "tlpMain";
            tlpMain.Padding = new Padding(12);
            tlpMain.RowCount = 4;
            tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpMain.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMain.TabIndex = 0;
            //
            // tlpHeader — database path on the left, Settings on the right
            //
            tlpHeader.AutoSize = true;
            tlpHeader.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            tlpHeader.ColumnCount = 2;
            tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpHeader.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpHeader.Controls.Add(lblDb, 0, 0);
            tlpHeader.Controls.Add(btnSettings, 1, 0);
            tlpHeader.Dock = DockStyle.Fill;
            tlpHeader.Margin = new Padding(0, 0, 0, 6);
            tlpHeader.Name = "tlpHeader";
            tlpHeader.RowCount = 1;
            tlpHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpHeader.TabIndex = 0;
            //
            // lblDb
            //
            lblDb.Anchor = AnchorStyles.Left | AnchorStyles.Right;
            lblDb.AutoEllipsis = true;
            lblDb.Margin = new Padding(0, 0, 12, 0);
            lblDb.Name = "lblDb";
            lblDb.Size = new Size(400, 24);
            lblDb.TabIndex = 0;
            lblDb.Text = "MMEX Database:";
            lblDb.TextAlign = ContentAlignment.MiddleLeft;
            //
            // btnSettings
            //
            btnSettings.AutoSize = true;
            btnSettings.Margin = new Padding(0);
            btnSettings.MinimumSize = new Size(110, 30);
            btnSettings.Name = "btnSettings";
            btnSettings.TabIndex = 1;
            btnSettings.Text = "Settings...";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += btnSettings_Click;
            //
            // flpToolbar
            //
            flpToolbar.AutoSize = true;
            flpToolbar.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flpToolbar.Controls.Add(btnScanDownloads);
            flpToolbar.Controls.Add(btnAddFile);
            flpToolbar.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            flpToolbar.WrapContents = false;
            flpToolbar.Margin = new Padding(0, 0, 0, 6);
            flpToolbar.Name = "flpToolbar";
            flpToolbar.TabIndex = 1;
            //
            // btnScanDownloads
            //
            btnScanDownloads.AutoSize = true;
            btnScanDownloads.Margin = new Padding(0, 0, 6, 0);
            btnScanDownloads.MinimumSize = new Size(140, 34);
            btnScanDownloads.Name = "btnScanDownloads";
            btnScanDownloads.Padding = new Padding(6, 0, 6, 0);
            btnScanDownloads.TabIndex = 0;
            btnScanDownloads.Text = "Scan Downloads";
            btnScanDownloads.UseVisualStyleBackColor = true;
            btnScanDownloads.Click += btnScanDownloads_Click;
            //
            // btnAddFile
            //
            btnAddFile.AutoSize = true;
            btnAddFile.Margin = new Padding(0);
            btnAddFile.MinimumSize = new Size(110, 34);
            btnAddFile.Name = "btnAddFile";
            btnAddFile.Padding = new Padding(6, 0, 6, 0);
            btnAddFile.TabIndex = 1;
            btnAddFile.Text = "Add File...";
            btnAddFile.UseVisualStyleBackColor = true;
            btnAddFile.Click += btnAddFile_Click;
            //
            // splitGrids — files on top, the selected file's transactions below
            //
            splitGrids.Dock = DockStyle.Fill;
            splitGrids.FixedPanel = FixedPanel.Panel1;   // extra height goes to the transactions grid
            splitGrids.Margin = new Padding(0);
            splitGrids.Name = "splitGrids";
            splitGrids.Orientation = Orientation.Horizontal;
            splitGrids.Panel1.Controls.Add(dgvFiles);
            splitGrids.Panel1.Controls.Add(lblFiles);
            splitGrids.Panel1MinSize = 100;
            splitGrids.Panel2.Controls.Add(dgvTransactions);
            splitGrids.Panel2.Controls.Add(lblTransactions);
            splitGrids.Panel2MinSize = 120;
            splitGrids.Size = new Size(960, 520);
            splitGrids.SplitterDistance = 250;
            splitGrids.SplitterWidth = 8;
            splitGrids.TabIndex = 2;
            //
            // lblFiles
            //
            lblFiles.Dock = DockStyle.Top;
            lblFiles.Name = "lblFiles";
            lblFiles.Size = new Size(960, 26);
            lblFiles.TabIndex = 0;
            lblFiles.Text = "Import files:";
            lblFiles.TextAlign = ContentAlignment.MiddleLeft;
            //
            // dgvFiles
            //
            dgvFiles.AllowUserToAddRows = false;
            dgvFiles.AllowUserToDeleteRows = false;
            dgvFiles.AllowUserToResizeRows = false;
            dgvFiles.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvFiles.BackgroundColor = SystemColors.Window;
            dgvFiles.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvFiles.Columns.AddRange(new DataGridViewColumn[] { colFileName, colBankLabel, colMmexAccount, colRows, colNew, colIssues, colStatus });
            dgvFiles.Dock = DockStyle.Fill;
            dgvFiles.MultiSelect = false;
            dgvFiles.Name = "dgvFiles";
            dgvFiles.ReadOnly = false;
            dgvFiles.RowHeadersWidth = 30;
            dgvFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFiles.TabIndex = 1;
            dgvFiles.SelectionChanged += dgvFiles_SelectionChanged;
            dgvFiles.CellValueChanged += dgvFiles_CellValueChanged;
            dgvFiles.CurrentCellDirtyStateChanged += dgvFiles_CurrentCellDirtyStateChanged;
            dgvFiles.DataError += dgvFiles_DataError;
            //
            // colFileName
            //
            colFileName.FillWeight = 20F;
            colFileName.HeaderText = "File";
            colFileName.Name = "colFileName";
            colFileName.ReadOnly = true;
            //
            // colBankLabel
            //
            colBankLabel.FillWeight = 18F;
            colBankLabel.HeaderText = "Bank Account";
            colBankLabel.Name = "colBankLabel";
            colBankLabel.ReadOnly = true;
            //
            // colMmexAccount
            //
            colMmexAccount.DisplayStyle = DataGridViewComboBoxDisplayStyle.DropDownButton;
            colMmexAccount.FillWeight = 22F;
            colMmexAccount.FlatStyle = FlatStyle.Flat;
            colMmexAccount.HeaderText = "MMEX Account";
            colMmexAccount.Name = "colMmexAccount";
            colMmexAccount.ReadOnly = false;
            //
            // colRows
            //
            colRows.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colRows.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colRows.HeaderText = "Rows";
            colRows.Name = "colRows";
            colRows.ReadOnly = true;
            colRows.Width = 58;
            //
            // colNew
            //
            colNew.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colNew.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colNew.HeaderText = "New";
            colNew.Name = "colNew";
            colNew.ReadOnly = true;
            colNew.Width = 52;
            //
            // colIssues
            //
            colIssues.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colIssues.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colIssues.HeaderText = "Issues";
            colIssues.Name = "colIssues";
            colIssues.ReadOnly = true;
            colIssues.Width = 58;
            //
            // colStatus
            //
            colStatus.FillWeight = 18F;
            colStatus.HeaderText = "Status";
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            //
            // lblTransactions
            //
            lblTransactions.AutoEllipsis = true;
            lblTransactions.Dock = DockStyle.Top;
            lblTransactions.Name = "lblTransactions";
            lblTransactions.Size = new Size(960, 26);
            lblTransactions.TabIndex = 0;
            lblTransactions.Text = "New transactions:";
            lblTransactions.TextAlign = ContentAlignment.MiddleLeft;
            //
            // dgvTransactions
            //
            dgvTransactions.AllowUserToAddRows = false;
            dgvTransactions.AllowUserToDeleteRows = false;
            dgvTransactions.AllowUserToResizeRows = false;
            dgvTransactions.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
            dgvTransactions.BackgroundColor = SystemColors.Window;
            dgvTransactions.ColumnHeadersDefaultCellStyle.WrapMode = DataGridViewTriState.False;
            dgvTransactions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTransactions.Columns.AddRange(new DataGridViewColumn[] { colDate, colDetails, colCategory, colSubcategory, colDebit, colCredit, colType, colOriginal });
            dgvTransactions.Dock = DockStyle.Fill;
            dgvTransactions.Name = "dgvTransactions";
            dgvTransactions.ReadOnly = true;
            dgvTransactions.RowHeadersWidth = 30;
            dgvTransactions.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvTransactions.TabIndex = 1;
            //
            // colDate
            //
            colDate.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colDate.HeaderText = "Date";
            colDate.Name = "colDate";
            colDate.ReadOnly = true;
            colDate.Width = 100;
            //
            // colDetails
            //
            colDetails.FillWeight = 20F;
            colDetails.HeaderText = "Payee / Details";
            colDetails.Name = "colDetails";
            colDetails.ReadOnly = true;
            //
            // colCategory
            //
            colCategory.FillWeight = 20F;
            colCategory.HeaderText = "Category";
            colCategory.Name = "colCategory";
            colCategory.ReadOnly = true;
            //
            // colSubcategory
            //
            colSubcategory.FillWeight = 10F;
            colSubcategory.HeaderText = "Subcategory";
            colSubcategory.Name = "colSubcategory";
            colSubcategory.ReadOnly = true;
            //
            // colDebit
            //
            colDebit.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colDebit.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colDebit.HeaderText = "Debit";
            colDebit.Name = "colDebit";
            colDebit.ReadOnly = true;
            colDebit.Width = 85;
            //
            // colCredit
            //
            colCredit.AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
            colCredit.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleRight;
            colCredit.HeaderText = "Credit";
            colCredit.Name = "colCredit";
            colCredit.ReadOnly = true;
            colCredit.Width = 85;
            //
            // colType
            //
            colType.FillWeight = 14F;
            colType.HeaderText = "Type";
            colType.Name = "colType";
            colType.ReadOnly = true;
            //
            // colOriginal
            //
            colOriginal.FillWeight = 36F;
            colOriginal.HeaderText = "Original Description";
            colOriginal.Name = "colOriginal";
            colOriginal.ReadOnly = true;
            //
            // flpWriteButtons
            //
            flpWriteButtons.AutoSize = true;
            flpWriteButtons.AutoSizeMode = AutoSizeMode.GrowAndShrink;
            flpWriteButtons.Controls.Add(btnWriteSelected);
            flpWriteButtons.Controls.Add(btnWriteAll);
            flpWriteButtons.Anchor = AnchorStyles.Top | AnchorStyles.Left;
            flpWriteButtons.WrapContents = false;
            flpWriteButtons.Margin = new Padding(0, 8, 0, 0);
            flpWriteButtons.Name = "flpWriteButtons";
            flpWriteButtons.TabIndex = 3;
            //
            // btnWriteSelected
            //
            btnWriteSelected.AutoSize = true;
            btnWriteSelected.Margin = new Padding(0, 0, 6, 0);
            btnWriteSelected.MinimumSize = new Size(180, 34);
            btnWriteSelected.Name = "btnWriteSelected";
            btnWriteSelected.Padding = new Padding(6, 0, 6, 0);
            btnWriteSelected.TabIndex = 0;
            btnWriteSelected.Text = "Write Selected to MMEX";
            btnWriteSelected.UseVisualStyleBackColor = true;
            btnWriteSelected.Click += btnWriteSelected_Click;
            //
            // btnWriteAll
            //
            btnWriteAll.AutoSize = true;
            btnWriteAll.Margin = new Padding(0);
            btnWriteAll.MinimumSize = new Size(160, 34);
            btnWriteAll.Name = "btnWriteAll";
            btnWriteAll.Padding = new Padding(6, 0, 6, 0);
            btnWriteAll.TabIndex = 1;
            btnWriteAll.Text = "Write All to MMEX";
            btnWriteAll.UseVisualStyleBackColor = true;
            btnWriteAll.Click += btnWriteAll_Click;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1100, 700);
            Controls.Add(tlpMain);
            MinimumSize = new Size(760, 520);
            Name = "Form1";
            Text = "MMEX Bank Import";
            tlpMain.ResumeLayout(false);
            tlpMain.PerformLayout();
            tlpHeader.ResumeLayout(false);
            tlpHeader.PerformLayout();
            flpToolbar.ResumeLayout(false);
            flpToolbar.PerformLayout();
            splitGrids.Panel1.ResumeLayout(false);
            splitGrids.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)splitGrids).EndInit();
            splitGrids.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)dgvFiles).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTransactions).EndInit();
            flpWriteButtons.ResumeLayout(false);
            flpWriteButtons.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private TableLayoutPanel tlpMain;
        private TableLayoutPanel tlpHeader;
        private Label lblDb;
        private Button btnSettings;
        private FlowLayoutPanel flpToolbar;
        private Button btnScanDownloads;
        private Button btnAddFile;
        private SplitContainer splitGrids;
        private Label lblFiles;
        private DataGridView dgvFiles;
        private DataGridViewTextBoxColumn colFileName;
        private DataGridViewTextBoxColumn colBankLabel;
        private DataGridViewComboBoxColumn colMmexAccount;
        private DataGridViewTextBoxColumn colRows;
        private DataGridViewTextBoxColumn colNew;
        private DataGridViewTextBoxColumn colIssues;
        private DataGridViewTextBoxColumn colStatus;
        private Label lblTransactions;
        private DataGridView dgvTransactions;
        private DataGridViewTextBoxColumn colDate;
        private DataGridViewTextBoxColumn colDetails;
        private DataGridViewTextBoxColumn colCategory;
        private DataGridViewTextBoxColumn colSubcategory;
        private DataGridViewTextBoxColumn colDebit;
        private DataGridViewTextBoxColumn colCredit;
        private DataGridViewTextBoxColumn colType;
        private DataGridViewTextBoxColumn colOriginal;
        private FlowLayoutPanel flpWriteButtons;
        private Button btnWriteSelected;
        private Button btnWriteAll;
    }
}
