namespace LiteDB.Studio
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.txtFileName = new System.Windows.Forms.TextBox();
            this.splitMain = new System.Windows.Forms.SplitContainer();
            this.tvwDatabase = new System.Windows.Forms.TreeView();
            this.imgList = new System.Windows.Forms.ImageList(this.components);
            this.splitRight = new System.Windows.Forms.SplitContainer();
            this.txtSql = new System.Windows.Forms.RichTextBox();
            this.tabResult = new System.Windows.Forms.TabControl();
            this.tabGrid = new System.Windows.Forms.TabPage();
            this.grdResult = new System.Windows.Forms.DataGridView();
            this.tabText = new System.Windows.Forms.TabPage();
            this.txtResult = new System.Windows.Forms.RichTextBox();
            this.tabLog = new System.Windows.Forms.TabPage();
            this.txtLog = new System.Windows.Forms.RichTextBox();
            this.tabSql = new System.Windows.Forms.TabControl();
            this.stbStatus = new System.Windows.Forms.StatusStrip();
            this.lblCursor = new System.Windows.Forms.ToolStripStatusLabel();
            this.lblResultCount = new System.Windows.Forms.ToolStripStatusLabel();
            this.prgRunning = new System.Windows.Forms.ToolStripProgressBar();
            this.lblElapsed = new System.Windows.Forms.ToolStripStatusLabel();
            this.tlbMain = new System.Windows.Forms.ToolStrip();
            this.btnConnect = new System.Windows.Forms.ToolStripButton();
            this.tlbSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnRefresh = new System.Windows.Forms.ToolStripButton();
            this.tlbSep2 = new System.Windows.Forms.ToolStripSeparator();
            this.btnRun = new System.Windows.Forms.ToolStripButton();
            this.toolStripSeparator1 = new System.Windows.Forms.ToolStripSeparator();
            this.btnBegin = new System.Windows.Forms.ToolStripButton();
            this.btnCommit = new System.Windows.Forms.ToolStripButton();
            this.btnRollback = new System.Windows.Forms.ToolStripButton();
            this.ctxMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.mnuQuery = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripSeparator2 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuAnalyze = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuIndexes = new System.Windows.Forms.ToolStripMenuItem();
            this.mnuSep1 = new System.Windows.Forms.ToolStripSeparator();
            this.mnuDropCollection = new System.Windows.Forms.ToolStripMenuItem();
            this.tabParameters = new System.Windows.Forms.TabPage();
            this.txtParameters = new System.Windows.Forms.RichTextBox();
            this.mnuExplanPlan = new System.Windows.Forms.ToolStripMenuItem();
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).BeginInit();
            this.splitMain.Panel1.SuspendLayout();
            this.splitMain.Panel2.SuspendLayout();
            this.splitMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).BeginInit();
            this.splitRight.Panel1.SuspendLayout();
            this.splitRight.Panel2.SuspendLayout();
            this.splitRight.SuspendLayout();
            this.tabResult.SuspendLayout();
            this.tabGrid.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grdResult)).BeginInit();
            this.tabText.SuspendLayout();
            this.tabLog.SuspendLayout();
            this.stbStatus.SuspendLayout();
            this.tlbMain.SuspendLayout();
            this.ctxMenu.SuspendLayout();
            this.tabParameters.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtFileName
            // 
            this.txtFileName.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtFileName.Location = new System.Drawing.Point(5, 37);
            this.txtFileName.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.txtFileName.Name = "txtFileName";
            this.txtFileName.Size = new System.Drawing.Size(1070, 23);
            this.txtFileName.TabIndex = 0;
            this.txtFileName.Text = ":memory:";
            // 
            // splitMain
            // 
            this.splitMain.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitMain.Location = new System.Drawing.Point(5, 69);
            this.splitMain.Name = "splitMain";
            // 
            // splitMain.Panel1
            // 
            this.splitMain.Panel1.Controls.Add(this.tvwDatabase);
            // 
            // splitMain.Panel2
            // 
            this.splitMain.Panel2.Controls.Add(this.splitRight);
            this.splitMain.Panel2.Controls.Add(this.tabSql);
            this.splitMain.Size = new System.Drawing.Size(1070, 566);
            this.splitMain.SplitterDistance = 233;
            this.splitMain.TabIndex = 10;
            this.splitMain.TabStop = false;
            // 
            // tvwDatabase
            // 
            this.tvwDatabase.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tvwDatabase.ImageIndex = 0;
            this.tvwDatabase.ImageList = this.imgList;
            this.tvwDatabase.Location = new System.Drawing.Point(0, 0);
            this.tvwDatabase.Margin = new System.Windows.Forms.Padding(0);
            this.tvwDatabase.Name = "tvwDatabase";
            this.tvwDatabase.SelectedImageIndex = 0;
            this.tvwDatabase.Size = new System.Drawing.Size(233, 566);
            this.tvwDatabase.TabIndex = 9;
            this.tvwDatabase.NodeMouseDoubleClick += new System.Windows.Forms.TreeNodeMouseClickEventHandler(this.TvwCols_NodeMouseDoubleClick);
            this.tvwDatabase.MouseUp += new System.Windows.Forms.MouseEventHandler(this.TvwCols_MouseUp);
            // 
            // imgList
            // 
            this.imgList.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("imgList.ImageStream")));
            this.imgList.TransparentColor = System.Drawing.Color.Transparent;
            this.imgList.Images.SetKeyName(0, "database");
            this.imgList.Images.SetKeyName(1, "folder");
            this.imgList.Images.SetKeyName(2, "table");
            this.imgList.Images.SetKeyName(3, "table_gear");
            // 
            // splitRight
            // 
            this.splitRight.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.splitRight.Location = new System.Drawing.Point(3, 26);
            this.splitRight.Name = "splitRight";
            this.splitRight.Orientation = System.Windows.Forms.Orientation.Horizontal;
            // 
            // splitRight.Panel1
            // 
            this.splitRight.Panel1.BackColor = System.Drawing.SystemColors.Control;
            this.splitRight.Panel1.Controls.Add(this.txtSql);
            // 
            // splitRight.Panel2
            // 
            this.splitRight.Panel2.Controls.Add(this.tabResult);
            this.splitRight.Size = new System.Drawing.Size(823, 537);
            this.splitRight.SplitterDistance = 164;
            this.splitRight.TabIndex = 8;
            // 
            // txtSql
            // 
            this.txtSql.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtSql.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtSql.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtSql.HideSelection = false;
            this.txtSql.Location = new System.Drawing.Point(3, 3);
            this.txtSql.Name = "txtSql";
            this.txtSql.Size = new System.Drawing.Size(816, 158);
            this.txtSql.TabIndex = 2;
            this.txtSql.Text = "";
            this.txtSql.SelectionChanged += new System.EventHandler(this.TxtSql_SelectionChanged);
            // 
            // tabResult
            // 
            this.tabResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.tabResult.Controls.Add(this.tabGrid);
            this.tabResult.Controls.Add(this.tabText);
            this.tabResult.Controls.Add(this.tabParameters);
            this.tabResult.Controls.Add(this.tabLog);
            this.tabResult.Location = new System.Drawing.Point(3, 3);
            this.tabResult.Name = "tabResult";
            this.tabResult.SelectedIndex = 0;
            this.tabResult.Size = new System.Drawing.Size(816, 361);
            this.tabResult.TabIndex = 0;
            this.tabResult.Selected += new System.Windows.Forms.TabControlEventHandler(this.TabResult_Selected);
            // 
            // tabGrid
            // 
            this.tabGrid.Controls.Add(this.grdResult);
            this.tabGrid.Location = new System.Drawing.Point(4, 24);
            this.tabGrid.Name = "tabGrid";
            this.tabGrid.Padding = new System.Windows.Forms.Padding(3);
            this.tabGrid.Size = new System.Drawing.Size(808, 333);
            this.tabGrid.TabIndex = 0;
            this.tabGrid.Text = "Grid";
            this.tabGrid.UseVisualStyleBackColor = true;
            // 
            // grdResult
            // 
            this.grdResult.AllowUserToAddRows = false;
            this.grdResult.AllowUserToDeleteRows = false;
            this.grdResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grdResult.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grdResult.Location = new System.Drawing.Point(6, 5);
            this.grdResult.Name = "grdResult";
            this.grdResult.Size = new System.Drawing.Size(796, 326);
            this.grdResult.TabIndex = 0;
            this.grdResult.CellBeginEdit += new System.Windows.Forms.DataGridViewCellCancelEventHandler(this.GrdResult_CellBeginEdit);
            this.grdResult.CellEndEdit += new System.Windows.Forms.DataGridViewCellEventHandler(this.GrdResult_CellEndEdit);
            this.grdResult.RowPostPaint += new System.Windows.Forms.DataGridViewRowPostPaintEventHandler(this.GrdResult_RowPostPaint);
            // 
            // tabText
            // 
            this.tabText.Controls.Add(this.txtResult);
            this.tabText.Location = new System.Drawing.Point(4, 24);
            this.tabText.Name = "tabText";
            this.tabText.Padding = new System.Windows.Forms.Padding(3);
            this.tabText.Size = new System.Drawing.Size(808, 333);
            this.tabText.TabIndex = 3;
            this.tabText.Text = "Text";
            this.tabText.UseVisualStyleBackColor = true;
            // 
            // txtResult
            // 
            this.txtResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtResult.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtResult.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtResult.Location = new System.Drawing.Point(6, 5);
            this.txtResult.Name = "txtResult";
            this.txtResult.ReadOnly = true;
            this.txtResult.Size = new System.Drawing.Size(796, 322);
            this.txtResult.TabIndex = 0;
            this.txtResult.Text = "";
            // 
            // tabLog
            // 
            this.tabLog.Controls.Add(this.txtLog);
            this.tabLog.Location = new System.Drawing.Point(4, 24);
            this.tabLog.Name = "tabLog";
            this.tabLog.Padding = new System.Windows.Forms.Padding(3);
            this.tabLog.Size = new System.Drawing.Size(808, 333);
            this.tabLog.TabIndex = 4;
            this.tabLog.Text = "Log";
            this.tabLog.UseVisualStyleBackColor = true;
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Font = new System.Drawing.Font("Consolas", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtLog.Location = new System.Drawing.Point(6, 6);
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.Size = new System.Drawing.Size(796, 315);
            this.txtLog.TabIndex = 0;
            this.txtLog.Text = "";
            this.txtLog.WordWrap = false;
            // 
            // tabSql
            // 
            this.tabSql.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabSql.Location = new System.Drawing.Point(0, 0);
            this.tabSql.Margin = new System.Windows.Forms.Padding(0);
            this.tabSql.Name = "tabSql";
            this.tabSql.SelectedIndex = 0;
            this.tabSql.Size = new System.Drawing.Size(833, 566);
            this.tabSql.TabIndex = 9;
            this.tabSql.Selected += new System.Windows.Forms.TabControlEventHandler(this.TabSql_Selected);
            this.tabSql.MouseClick += new System.Windows.Forms.MouseEventHandler(this.TabSql_MouseClick);
            // 
            // stbStatus
            // 
            this.stbStatus.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.lblCursor,
            this.lblResultCount,
            this.prgRunning,
            this.lblElapsed});
            this.stbStatus.Location = new System.Drawing.Point(0, 638);
            this.stbStatus.Name = "stbStatus";
            this.stbStatus.Size = new System.Drawing.Size(1080, 22);
            this.stbStatus.TabIndex = 11;
            this.stbStatus.Text = "statusStrip1";
            // 
            // lblCursor
            // 
            this.lblCursor.Name = "lblCursor";
            this.lblCursor.Size = new System.Drawing.Size(703, 17);
            this.lblCursor.Spring = true;
            this.lblCursor.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
            // 
            // lblResultCount
            // 
            this.lblResultCount.AutoSize = false;
            this.lblResultCount.Name = "lblResultCount";
            this.lblResultCount.Size = new System.Drawing.Size(150, 17);
            this.lblResultCount.Text = "0 documents";
            // 
            // prgRunning
            // 
            this.prgRunning.Name = "prgRunning";
            this.prgRunning.Size = new System.Drawing.Size(100, 16);
            // 
            // lblElapsed
            // 
            this.lblElapsed.AutoSize = false;
            this.lblElapsed.Name = "lblElapsed";
            this.lblElapsed.Size = new System.Drawing.Size(110, 17);
            this.lblElapsed.Text = "00:00:00.0000";
            this.lblElapsed.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
            // 
            // tlbMain
            // 
            this.tlbMain.GripMargin = new System.Windows.Forms.Padding(3);
            this.tlbMain.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.btnConnect,
            this.tlbSep1,
            this.btnRefresh,
            this.tlbSep2,
            this.btnRun,
            this.toolStripSeparator1,
            this.btnBegin,
            this.btnCommit,
            this.btnRollback});
            this.tlbMain.Location = new System.Drawing.Point(0, 0);
            this.tlbMain.Name = "tlbMain";
            this.tlbMain.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
            this.tlbMain.Size = new System.Drawing.Size(1080, 33);
            this.tlbMain.TabIndex = 12;
            this.tlbMain.Text = "toolStrip";
            // 
            // btnConnect
            // 
            this.btnConnect.Image = ((System.Drawing.Image)(resources.GetObject("btnConnect.Image")));
            this.btnConnect.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnConnect.Name = "btnConnect";
            this.btnConnect.Padding = new System.Windows.Forms.Padding(3);
            this.btnConnect.Size = new System.Drawing.Size(78, 26);
            this.btnConnect.Text = "Connect";
            this.btnConnect.Click += new System.EventHandler(this.BtnConnect_Click);
            // 
            // tlbSep1
            // 
            this.tlbSep1.Name = "tlbSep1";
            this.tlbSep1.Size = new System.Drawing.Size(6, 29);
            // 
            // btnRefresh
            // 
            this.btnRefresh.Image = ((System.Drawing.Image)(resources.GetObject("btnRefresh.Image")));
            this.btnRefresh.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Padding = new System.Windows.Forms.Padding(3);
            this.btnRefresh.Size = new System.Drawing.Size(72, 26);
            this.btnRefresh.Text = "Refresh";
            this.btnRefresh.Click += new System.EventHandler(this.BtnRefresh_Click);
            // 
            // tlbSep2
            // 
            this.tlbSep2.Name = "tlbSep2";
            this.tlbSep2.Size = new System.Drawing.Size(6, 29);
            // 
            // btnRun
            // 
            this.btnRun.Image = ((System.Drawing.Image)(resources.GetObject("btnRun.Image")));
            this.btnRun.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRun.Name = "btnRun";
            this.btnRun.Padding = new System.Windows.Forms.Padding(3);
            this.btnRun.Size = new System.Drawing.Size(54, 26);
            this.btnRun.Text = "Run";
            this.btnRun.Click += new System.EventHandler(this.BtnRun_Click);
            // 
            // toolStripSeparator1
            // 
            this.toolStripSeparator1.Name = "toolStripSeparator1";
            this.toolStripSeparator1.Size = new System.Drawing.Size(6, 29);
            // 
            // btnBegin
            // 
            this.btnBegin.Image = ((System.Drawing.Image)(resources.GetObject("btnBegin.Image")));
            this.btnBegin.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnBegin.Name = "btnBegin";
            this.btnBegin.Size = new System.Drawing.Size(57, 26);
            this.btnBegin.Text = "Begin";
            this.btnBegin.ToolTipText = "Begin Transaction";
            this.btnBegin.Click += new System.EventHandler(this.BtnBegin_Click);
            // 
            // btnCommit
            // 
            this.btnCommit.Image = ((System.Drawing.Image)(resources.GetObject("btnCommit.Image")));
            this.btnCommit.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnCommit.Name = "btnCommit";
            this.btnCommit.Size = new System.Drawing.Size(71, 26);
            this.btnCommit.Text = "Commit";
            this.btnCommit.Click += new System.EventHandler(this.BtnCommit_Click);
            // 
            // btnRollback
            // 
            this.btnRollback.Image = ((System.Drawing.Image)(resources.GetObject("btnRollback.Image")));
            this.btnRollback.ImageTransparentColor = System.Drawing.Color.Magenta;
            this.btnRollback.Name = "btnRollback";
            this.btnRollback.Size = new System.Drawing.Size(72, 26);
            this.btnRollback.Text = "Rollback";
            this.btnRollback.Click += new System.EventHandler(this.BtnRollback_Click);
            // 
            // ctxMenu
            // 
            this.ctxMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.mnuQuery,
            this.mnuExplanPlan,
            this.toolStripSeparator2,
            this.mnuAnalyze,
            this.mnuIndexes,
            this.mnuSep1,
            this.mnuDropCollection});
            this.ctxMenu.Name = "ctxMenu";
            this.ctxMenu.Size = new System.Drawing.Size(156, 148);
            this.ctxMenu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.CtxMenu_ItemClicked);
            // 
            // mnuQuery
            // 
            this.mnuQuery.Image = ((System.Drawing.Image)(resources.GetObject("mnuQuery.Image")));
            this.mnuQuery.Name = "mnuQuery";
            this.mnuQuery.Size = new System.Drawing.Size(155, 22);
            this.mnuQuery.Tag = "SELECT $ FROM {0};";
            this.mnuQuery.Text = "Query";
            // 
            // toolStripSeparator2
            // 
            this.toolStripSeparator2.Name = "toolStripSeparator2";
            this.toolStripSeparator2.Size = new System.Drawing.Size(152, 6);
            // 
            // mnuAnalyze
            // 
            this.mnuAnalyze.Image = ((System.Drawing.Image)(resources.GetObject("mnuAnalyze.Image")));
            this.mnuAnalyze.Name = "mnuAnalyze";
            this.mnuAnalyze.Size = new System.Drawing.Size(155, 22);
            this.mnuAnalyze.Tag = "ANALYZE {0};";
            this.mnuAnalyze.Text = "Analyze";
            // 
            // mnuIndexes
            // 
            this.mnuIndexes.Image = ((System.Drawing.Image)(resources.GetObject("mnuIndexes.Image")));
            this.mnuIndexes.Name = "mnuIndexes";
            this.mnuIndexes.Size = new System.Drawing.Size(155, 22);
            this.mnuIndexes.Tag = "SELECT $ FROM $indexes WHERE collection = \"{0}\";";
            this.mnuIndexes.Text = "Indexes";
            // 
            // mnuSep1
            // 
            this.mnuSep1.Name = "mnuSep1";
            this.mnuSep1.Size = new System.Drawing.Size(152, 6);
            // 
            // mnuDropCollection
            // 
            this.mnuDropCollection.Image = ((System.Drawing.Image)(resources.GetObject("mnuDropCollection.Image")));
            this.mnuDropCollection.Name = "mnuDropCollection";
            this.mnuDropCollection.Size = new System.Drawing.Size(155, 22);
            this.mnuDropCollection.Tag = "DROP COLLECTION {0};";
            this.mnuDropCollection.Text = "Drop collection";
            // 
            // tabParameters
            // 
            this.tabParameters.Controls.Add(this.txtParameters);
            this.tabParameters.Location = new System.Drawing.Point(4, 24);
            this.tabParameters.Name = "tabParameters";
            this.tabParameters.Padding = new System.Windows.Forms.Padding(3);
            this.tabParameters.Size = new System.Drawing.Size(808, 333);
            this.tabParameters.TabIndex = 5;
            this.tabParameters.Text = "Parameters";
            this.tabParameters.UseVisualStyleBackColor = true;
            // 
            // txtParameters
            // 
            this.txtParameters.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtParameters.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.txtParameters.Font = new System.Drawing.Font("Consolas", 9.75F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtParameters.HideSelection = false;
            this.txtParameters.Location = new System.Drawing.Point(6, 6);
            this.txtParameters.Name = "txtParameters";
            this.txtParameters.Size = new System.Drawing.Size(796, 321);
            this.txtParameters.TabIndex = 3;
            this.txtParameters.Text = "";
            // 
            // mnuExplanPlan
            // 
            this.mnuExplanPlan.Image = global::LiteDB.Studio.Properties.Resources.table_sort;
            this.mnuExplanPlan.Name = "mnuExplanPlan";
            this.mnuExplanPlan.Size = new System.Drawing.Size(155, 22);
            this.mnuExplanPlan.Tag = "EXPLAIN SELECT $ FROM {0};";
            this.mnuExplanPlan.Text = "Explain Plan";
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1080, 660);
            this.Controls.Add(this.tlbMain);
            this.Controls.Add(this.stbStatus);
            this.Controls.Add(this.splitMain);
            this.Controls.Add(this.txtFileName);
            this.Font = new System.Drawing.Font("Segoe UI", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.KeyPreview = true;
            this.Margin = new System.Windows.Forms.Padding(3, 4, 3, 4);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "LiteDB Studio";
            this.KeyDown += new System.Windows.Forms.KeyEventHandler(this.MainForm_KeyDown);
            this.splitMain.Panel1.ResumeLayout(false);
            this.splitMain.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitMain)).EndInit();
            this.splitMain.ResumeLayout(false);
            this.splitRight.Panel1.ResumeLayout(false);
            this.splitRight.Panel2.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.splitRight)).EndInit();
            this.splitRight.ResumeLayout(false);
            this.tabResult.ResumeLayout(false);
            this.tabGrid.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.grdResult)).EndInit();
            this.tabText.ResumeLayout(false);
            this.tabLog.ResumeLayout(false);
            this.stbStatus.ResumeLayout(false);
            this.stbStatus.PerformLayout();
            this.tlbMain.ResumeLayout(false);
            this.tlbMain.PerformLayout();
            this.ctxMenu.ResumeLayout(false);
            this.tabParameters.ResumeLayout(false);
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox txtFileName;
        private System.Windows.Forms.SplitContainer splitMain;
        private System.Windows.Forms.TreeView tvwDatabase;
        private System.Windows.Forms.StatusStrip stbStatus;
        private System.Windows.Forms.SplitContainer splitRight;
        private System.Windows.Forms.ToolStripStatusLabel lblElapsed;
        private System.Windows.Forms.TabControl tabResult;
        private System.Windows.Forms.TabPage tabGrid;
        private System.Windows.Forms.DataGridView grdResult;
        private System.Windows.Forms.TabPage tabText;
        private System.Windows.Forms.RichTextBox txtResult;
        private System.Windows.Forms.ToolStripStatusLabel lblResultCount;
        private System.Windows.Forms.ToolStrip tlbMain;
        private System.Windows.Forms.ToolStripButton btnConnect;
        private System.Windows.Forms.ToolStripButton btnRun;
        private System.Windows.Forms.ToolStripSeparator tlbSep1;
        private System.Windows.Forms.ToolStripButton btnRefresh;
        private System.Windows.Forms.ToolStripSeparator tlbSep2;
        private System.Windows.Forms.ToolStripStatusLabel lblCursor;
        private System.Windows.Forms.ToolStripProgressBar prgRunning;
        private System.Windows.Forms.ImageList imgList;
        private System.Windows.Forms.ContextMenuStrip ctxMenu;
        private System.Windows.Forms.ToolStripMenuItem mnuIndexes;
        private System.Windows.Forms.RichTextBox txtSql;
        private System.Windows.Forms.TabControl tabSql;
        private System.Windows.Forms.ToolStripMenuItem mnuDropCollection;
        private System.Windows.Forms.TabPage tabLog;
        private System.Windows.Forms.RichTextBox txtLog;
        private System.Windows.Forms.ToolStripMenuItem mnuAnalyze;
        private System.Windows.Forms.ToolStripSeparator mnuSep1;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator1;
        private System.Windows.Forms.ToolStripButton btnBegin;
        private System.Windows.Forms.ToolStripButton btnCommit;
        private System.Windows.Forms.ToolStripButton btnRollback;
        private System.Windows.Forms.ToolStripMenuItem mnuQuery;
        private System.Windows.Forms.ToolStripSeparator toolStripSeparator2;
        private System.Windows.Forms.TabPage tabParameters;
        private System.Windows.Forms.RichTextBox txtParameters;
        private System.Windows.Forms.ToolStripMenuItem mnuExplanPlan;
    }
}

