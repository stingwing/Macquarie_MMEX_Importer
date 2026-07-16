namespace MoneyManagerExMAQ
{
    partial class Form1
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

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            lblDb = new Label();
            btnSettings = new Button();
            btnScanDownloads = new Button();
            btnAddFile = new Button();
            lblFiles = new Label();
            dgvFiles = new DataGridView();
            colFileName = new DataGridViewTextBoxColumn();
            colBankLabel = new DataGridViewTextBoxColumn();
            colMmexAccount = new DataGridViewTextBoxColumn();
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
            btnWriteSelected = new Button();
            btnWriteAll = new Button();
            ((System.ComponentModel.ISupportInitialize)dgvFiles).BeginInit();
            ((System.ComponentModel.ISupportInitialize)dgvTransactions).BeginInit();
            SuspendLayout();
            //
            // lblDb
            //
            lblDb.AutoSize = true;
            lblDb.Location = new Point(20, 18);
            lblDb.Name = "lblDb";
            lblDb.Size = new Size(130, 20);
            lblDb.TabIndex = 0;
            lblDb.Text = "MMEX Database:";
            //
            // btnSettings
            //
            btnSettings.Anchor = AnchorStyles.Top | AnchorStyles.Right;
            btnSettings.Location = new Point(844, 14);
            btnSettings.Name = "btnSettings";
            btnSettings.Size = new Size(120, 30);
            btnSettings.TabIndex = 1;
            btnSettings.Text = "Settings...";
            btnSettings.UseVisualStyleBackColor = true;
            btnSettings.Click += btnSettings_Click;
            //
            // btnScanDownloads
            //
            btnScanDownloads.Location = new Point(20, 52);
            btnScanDownloads.Name = "btnScanDownloads";
            btnScanDownloads.Size = new Size(160, 35);
            btnScanDownloads.TabIndex = 2;
            btnScanDownloads.Text = "Scan Downloads";
            btnScanDownloads.UseVisualStyleBackColor = true;
            btnScanDownloads.Click += btnScanDownloads_Click;
            //
            // btnAddFile
            //
            btnAddFile.Location = new Point(190, 52);
            btnAddFile.Name = "btnAddFile";
            btnAddFile.Size = new Size(120, 35);
            btnAddFile.TabIndex = 3;
            btnAddFile.Text = "Add File...";
            btnAddFile.UseVisualStyleBackColor = true;
            btnAddFile.Click += btnAddFile_Click;
            //
            // lblFiles
            //
            lblFiles.AutoSize = true;
            lblFiles.Location = new Point(20, 98);
            lblFiles.Name = "lblFiles";
            lblFiles.Size = new Size(100, 20);
            lblFiles.TabIndex = 4;
            lblFiles.Text = "Import files:";
            //
            // dgvFiles
            //
            dgvFiles.AllowUserToAddRows = false;
            dgvFiles.AllowUserToDeleteRows = false;
            dgvFiles.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
            dgvFiles.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvFiles.Columns.AddRange(new DataGridViewColumn[] { colFileName, colBankLabel, colMmexAccount, colRows, colNew, colIssues, colStatus });
            dgvFiles.Location = new Point(20, 121);
            dgvFiles.MultiSelect = false;
            dgvFiles.Name = "dgvFiles";
            dgvFiles.ReadOnly = true;
            dgvFiles.RowHeadersWidth = 30;
            dgvFiles.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            dgvFiles.Size = new Size(944, 200);
            dgvFiles.TabIndex = 5;
            dgvFiles.SelectionChanged += dgvFiles_SelectionChanged;
            //
            // colFileName
            //
            colFileName.HeaderText = "File";
            colFileName.Name = "colFileName";
            colFileName.ReadOnly = true;
            colFileName.Width = 250;
            //
            // colBankLabel
            //
            colBankLabel.HeaderText = "Bank Account";
            colBankLabel.Name = "colBankLabel";
            colBankLabel.ReadOnly = true;
            colBankLabel.Width = 180;
            //
            // colMmexAccount
            //
            colMmexAccount.HeaderText = "MMEX Account";
            colMmexAccount.Name = "colMmexAccount";
            colMmexAccount.ReadOnly = true;
            colMmexAccount.Width = 140;
            //
            // colRows
            //
            colRows.HeaderText = "Rows";
            colRows.Name = "colRows";
            colRows.ReadOnly = true;
            colRows.Width = 65;
            //
            // colNew
            //
            colNew.HeaderText = "New";
            colNew.Name = "colNew";
            colNew.ReadOnly = true;
            colNew.Width = 65;
            //
            // colIssues
            //
            colIssues.HeaderText = "Issues";
            colIssues.Name = "colIssues";
            colIssues.ReadOnly = true;
            colIssues.Width = 70;
            //
            // colStatus
            //
            colStatus.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colStatus.HeaderText = "Status";
            colStatus.Name = "colStatus";
            colStatus.ReadOnly = true;
            //
            // lblTransactions
            //
            lblTransactions.AutoSize = true;
            lblTransactions.Location = new Point(20, 334);
            lblTransactions.Name = "lblTransactions";
            lblTransactions.Size = new Size(140, 20);
            lblTransactions.TabIndex = 6;
            lblTransactions.Text = "New transactions:";
            //
            // dgvTransactions
            //
            dgvTransactions.AllowUserToAddRows = false;
            dgvTransactions.AllowUserToDeleteRows = false;
            dgvTransactions.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            dgvTransactions.ColumnHeadersHeightSizeMode = DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            dgvTransactions.Columns.AddRange(new DataGridViewColumn[] { colDate, colDetails, colCategory, colSubcategory, colDebit, colCredit, colType, colOriginal });
            dgvTransactions.Location = new Point(20, 357);
            dgvTransactions.Name = "dgvTransactions";
            dgvTransactions.ReadOnly = true;
            dgvTransactions.RowHeadersWidth = 30;
            dgvTransactions.Size = new Size(944, 245);
            dgvTransactions.TabIndex = 7;
            //
            // colDate
            //
            colDate.HeaderText = "Date";
            colDate.Name = "colDate";
            colDate.ReadOnly = true;
            colDate.Width = 100;
            //
            // colDetails
            //
            colDetails.HeaderText = "Payee / Details";
            colDetails.Name = "colDetails";
            colDetails.ReadOnly = true;
            colDetails.Width = 230;
            //
            // colCategory
            //
            colCategory.HeaderText = "Category";
            colCategory.Name = "colCategory";
            colCategory.ReadOnly = true;
            colCategory.Width = 120;
            //
            // colSubcategory
            //
            colSubcategory.HeaderText = "Subcategory";
            colSubcategory.Name = "colSubcategory";
            colSubcategory.ReadOnly = true;
            colSubcategory.Width = 140;
            //
            // colDebit
            //
            colDebit.HeaderText = "Debit";
            colDebit.Name = "colDebit";
            colDebit.ReadOnly = true;
            colDebit.Width = 80;
            //
            // colCredit
            //
            colCredit.HeaderText = "Credit";
            colCredit.Name = "colCredit";
            colCredit.ReadOnly = true;
            colCredit.Width = 80;
            //
            // colType
            //
            colType.HeaderText = "Type";
            colType.Name = "colType";
            colType.ReadOnly = true;
            colType.Width = 190;
            //
            // colOriginal
            //
            colOriginal.AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            colOriginal.HeaderText = "Original Description";
            colOriginal.Name = "colOriginal";
            colOriginal.ReadOnly = true;
            //
            // btnWriteSelected
            //
            btnWriteSelected.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnWriteSelected.Location = new Point(20, 612);
            btnWriteSelected.Name = "btnWriteSelected";
            btnWriteSelected.Size = new Size(200, 35);
            btnWriteSelected.TabIndex = 8;
            btnWriteSelected.Text = "Write Selected to MMEX";
            btnWriteSelected.UseVisualStyleBackColor = true;
            btnWriteSelected.Click += btnWriteSelected_Click;
            //
            // btnWriteAll
            //
            btnWriteAll.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
            btnWriteAll.Location = new Point(230, 612);
            btnWriteAll.Name = "btnWriteAll";
            btnWriteAll.Size = new Size(170, 35);
            btnWriteAll.TabIndex = 9;
            btnWriteAll.Text = "Write All to MMEX";
            btnWriteAll.UseVisualStyleBackColor = true;
            btnWriteAll.Click += btnWriteAll_Click;
            //
            // Form1
            //
            AutoScaleDimensions = new SizeF(8F, 20F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(984, 661);
            Controls.Add(lblDb);
            Controls.Add(btnSettings);
            Controls.Add(btnScanDownloads);
            Controls.Add(btnAddFile);
            Controls.Add(lblFiles);
            Controls.Add(dgvFiles);
            Controls.Add(lblTransactions);
            Controls.Add(dgvTransactions);
            Controls.Add(btnWriteSelected);
            Controls.Add(btnWriteAll);
            Name = "Form1";
            Text = "MMEX Bank Import";
            ((System.ComponentModel.ISupportInitialize)dgvFiles).EndInit();
            ((System.ComponentModel.ISupportInitialize)dgvTransactions).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblDb;
        private Button btnSettings;
        private Button btnScanDownloads;
        private Button btnAddFile;
        private Label lblFiles;
        private DataGridView dgvFiles;
        private DataGridViewTextBoxColumn colFileName;
        private DataGridViewTextBoxColumn colBankLabel;
        private DataGridViewTextBoxColumn colMmexAccount;
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
        private Button btnWriteSelected;
        private Button btnWriteAll;
    }
}
