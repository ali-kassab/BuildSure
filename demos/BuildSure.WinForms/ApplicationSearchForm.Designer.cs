namespace BuildSure.WinForms;

partial class ApplicationSearchForm
{
    /// <summary>Required designer variable.</summary>
    private System.ComponentModel.IContainer components = null;

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
    /// Creates and positions the controls. Visual Studio's designer normally writes this method
    /// when you drag controls onto the form.
    /// </summary>
    private void InitializeComponent()
    {
        lblSearch = new Label();
        txtSearch = new TextBox();
        btnSearch = new Button();
        gridApplications = new DataGridView();
        lblStatus = new Label();
        ((System.ComponentModel.ISupportInitialize)gridApplications).BeginInit();
        SuspendLayout();
        //
        // lblSearch
        //
        lblSearch.AutoSize = true;
        lblSearch.Location = new Point(12, 17);
        lblSearch.Name = "lblSearch";
        lblSearch.Text = "Application number, project or customer:";
        //
        // txtSearch
        //
        txtSearch.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
        txtSearch.Location = new Point(12, 40);
        txtSearch.Name = "txtSearch";
        txtSearch.PlaceholderText = "e.g. APP-SEED or Omar";
        txtSearch.Size = new Size(640, 27);
        txtSearch.TabIndex = 0;
        //
        // btnSearch
        //
        btnSearch.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        btnSearch.Location = new Point(664, 38);
        btnSearch.Name = "btnSearch";
        btnSearch.Size = new Size(110, 31);
        btnSearch.TabIndex = 1;
        btnSearch.Text = "Search";
        btnSearch.UseVisualStyleBackColor = true;
        btnSearch.Click += btnSearch_Click;
        //
        // gridApplications
        //
        gridApplications.AllowUserToAddRows = false;
        gridApplications.AllowUserToDeleteRows = false;
        gridApplications.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        gridApplications.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.AllCells;
        gridApplications.Location = new Point(12, 80);
        gridApplications.Name = "gridApplications";
        gridApplications.ReadOnly = true;
        gridApplications.RowHeadersVisible = false;
        gridApplications.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        gridApplications.Size = new Size(762, 330);
        gridApplications.TabIndex = 2;
        //
        // lblStatus
        //
        lblStatus.Anchor = AnchorStyles.Bottom | AnchorStyles.Left;
        lblStatus.AutoSize = true;
        lblStatus.Location = new Point(12, 420);
        lblStatus.Name = "lblStatus";
        lblStatus.Text = "Ready.";
        //
        // ApplicationSearchForm
        //
        AcceptButton = btnSearch; // pressing Enter in the TextBox clicks Search
        AutoScaleDimensions = new SizeF(8F, 20F);
        AutoScaleMode = AutoScaleMode.Font;
        ClientSize = new Size(786, 450);
        Controls.Add(lblSearch);
        Controls.Add(txtSearch);
        Controls.Add(btnSearch);
        Controls.Add(gridApplications);
        Controls.Add(lblStatus);
        MinimumSize = new Size(600, 350);
        Name = "ApplicationSearchForm";
        StartPosition = FormStartPosition.CenterScreen;
        Text = "BuildSure - Application Search (Windows Forms demo)";
        Load += ApplicationSearchForm_Load;
        ((System.ComponentModel.ISupportInitialize)gridApplications).EndInit();
        ResumeLayout(false);
        PerformLayout();
    }

    #endregion

    private Label lblSearch;
    private TextBox txtSearch;
    private Button btnSearch;
    private DataGridView gridApplications;
    private Label lblStatus;
}
