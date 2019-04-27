using ICSharpCode.TextEditor;
using LiteDB.Engine;
using LiteDB.Studio.Forms;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiteDB.Studio
{
    public partial class MainForm : Form
    {
        private readonly SynchronizationContext _synchronizationContext;

        private LiteDatabase _db = null;
        private ConnectionString _connectionString = null;
        private SqlCodeCompletion _codeCompletion;

        public MainForm(string filename)
        {
            InitializeComponent();

            // For performance https://stackoverflow.com/questions/4255148/how-to-improve-painting-performance-of-datagridview
            grdResult.DoubleBuffered(true);

            _synchronizationContext = SynchronizationContext.Current;

            _codeCompletion = new SqlCodeCompletion(txtSql, imgCodeCompletion);

            if (string.IsNullOrWhiteSpace(filename))
            {
                this.Disconnect();
            }
            else
            {
                this.Connect(new ConnectionString(filename));
            }

            txtSql.ActiveTextAreaControl.TextArea.Caret.PositionChanged += (s, e) =>
            {
                if (this.ActiveTask == null) return;

                this.ActiveTask.EditorContent = txtSql.Text;
                this.ActiveTask.SelectedTab = tabResult.SelectedTab.Name;
                this.ActiveTask.Position = new Tuple<int, int>(txtSql.ActiveTextAreaControl.TextArea.Caret.Line, txtSql.ActiveTextAreaControl.TextArea.Caret.Column);

                lblCursor.Text = $"Line: {(txtSql.ActiveTextAreaControl.Caret.Line + 1)} - Column: {(txtSql.ActiveTextAreaControl.Caret.Column + 1)}";
            };

            // stop all threads
            this.FormClosing += (s, e) =>
            {
                this.Disconnect();
            };
        }

        public void Connect(ConnectionString connectionString)
        {
            _db = new LiteDatabase(connectionString);

            _connectionString = connectionString;

            _codeCompletion.UpdateCodeCompletion(_db);

            btnConnect.Text = "Disconnect";

            this.UIState(true);

            tabSql.TabPages.Add("+", "+");

            this.LoadTreeView();
            this.AddNewTab("");

            txtSql.Focus();
        }

        private void Disconnect()
        {
            btnConnect.Text = "Connect";

            this.UIState(false);

            foreach (var tab in tabSql.TabPages.Cast<TabPage>().Where(x => x.Name != "+").ToArray())
            {
                var task = tab.Tag as TaskData;
                task.ThreadRunning = false;
                task.WaitHandle.Set();
            }

            // clear all tabs and controls
            tabSql.TabPages.Clear();

            txtSql.Clear();
            grdResult.Clear();
            txtResult.Clear();
            txtParameters.Clear();

            tvwDatabase.Nodes.Clear();
            tvwDatabase.Focus();

            _db?.Dispose();
            _db = null;
        }

        private void UIState(bool enabled)
        {
            splitRight.Visible = enabled;
            tabSql.Visible = enabled;

            btnRefresh.Enabled = enabled;
            tabSql.Enabled = enabled;
            btnRun.Enabled = enabled;

            btnBegin.Enabled = enabled;
            btnCommit.Enabled = enabled;
            btnRollback.Enabled = enabled;
            btnCheckpoint.Enabled = enabled;
        }

        private TaskData ActiveTask => tabSql.SelectedTab?.Tag as TaskData;

        private void AddNewTab(string content)
        {
            // find + tab
            var tab = tabSql.TabPages.Cast<TabPage>().Where(x => x.Text == "+").Single();

            var task = new TaskData();

            task.EditorContent = content;
            task.Thread = new Thread(new ThreadStart(() => CreateThread(task)));
            task.Thread.Start();

            task.Id = task.Thread.ManagedThreadId;

            tab.Text = tab.Name = task.Id.ToString();
            tab.Tag = task;

            if (tabSql.SelectedTab != tab)
            {
                tabSql.SelectTab(tab);
            }

            // adding new + tab at end
            tabSql.TabPages.Add("+", "+");

            tabResult.SelectTab("tabGrid");
        }

        private void ExecuteSql(string sql)
        {
            if (this.ActiveTask?.Executing == false)
            {
                this.ActiveTask.Sql = sql;
                this.ActiveTask.WaitHandle.Set();
            }
        }

        private void LoadTreeView()
        {
            tvwDatabase.Nodes.Clear();

            var root = tvwDatabase.Nodes.Add(Path.GetFileName(_connectionString.Filename));
            var system = root.Nodes.Add("System");

            root.ImageKey = "database";
            root.ContextMenuStrip = ctxMenuRoot;

            system.ImageKey = system.SelectedImageKey = "folder";

            var sc = _db.GetCollection("$cols")
                .Query()
                .Where("type = 'system'")
                .OrderBy("name")
                .ToDocuments();

            foreach (var doc in sc)
            {
                var col = system.Nodes.Add(doc["name"].AsString);
                col.Tag = $"SELECT $ FROM {doc["name"].AsString}";
                col.ImageKey = col.SelectedImageKey = "table_gear";
            }

            root.ExpandAll();
            system.Toggle();

            foreach (var key in _db.GetCollectionNames().OrderBy(x => x))
            {
                var col = root.Nodes.Add(key);
                col.Tag = $"SELECT $ FROM {key};";
                col.ImageKey = col.SelectedImageKey = "table";
                col.ContextMenuStrip = ctxMenu;
            }
        }

        private void CreateThread(TaskData task)
        {
            while (true)
            {
                task.WaitHandle.Wait();

                if (task.ThreadRunning == false) break;

                if (task.Sql.Trim() == "")
                {
                    task.WaitHandle.Reset();
                    continue;
                }

                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    task.Executing = true;
                    task.IsGridLoaded = task.IsTextLoaded = task.IsParametersLoaded = false;

                    _synchronizationContext.Post(new SendOrPostCallback(o =>
                    {
                        this.LoadResult(task);
                    }), task);

                    task.Parameters = new BsonDocument();

                    var sql = new StringReader(task.Sql.Trim());

                    while(sql.Peek() >= 0 && _db != null)
                    {
                        using (var reader = _db.Execute(sql, task.Parameters))
                        {
                            task.ReadResult(reader);
                        }
                    }

                    task.Elapsed = sw.Elapsed;
                    task.Exception = null;
                    task.Executing = false;

                    // update form button selected
                    _synchronizationContext.Post(new SendOrPostCallback(o =>
                    {
                        var t = o as TaskData;

                        if (this.ActiveTask?.Id == t.Id)
                        {
                            this.LoadResult(o as TaskData);
                        }

                    }), task);
                }
                catch (Exception ex)
                {
                    task.Executing = false;
                    task.Result = null;
                    task.Elapsed = sw.Elapsed;
                    task.Exception = ex;

                    _synchronizationContext.Post(new SendOrPostCallback(o =>
                    {
                        var t = o as TaskData;

                        if (this.ActiveTask?.Id == t.Id)
                        {
                            tabResult.SelectedTab = tabText;
                            this.LoadResult(o as TaskData);
                        }

                    }), task);
                }

                // put thread in wait mode
                task.WaitHandle.Reset();
            }

            task.WaitHandle.Dispose();
        }

        private void LoadResult(TaskData data)
        {
            if (data == null) return;

            btnRun.Enabled = !data.Executing;

            if (data.Executing)
            {
                grdResult.Clear();
                txtResult.Clear();
                txtParameters.Clear();

                lblResultCount.Visible = false;
                lblElapsed.Text = "Running";
                prgRunning.Style = ProgressBarStyle.Marquee;
                txtParameters.Clear();
            }
            else
            {
                lblResultCount.Visible = true;
                lblElapsed.Text = data.Elapsed.ToString();
                prgRunning.Style = ProgressBarStyle.Blocks;
                lblResultCount.Text = 
                    data.Result == null ? "" :
                    data.Result.Count == 0 ? "no documents" :
                    data.Result.Count  == 1 ? "1 document" : 
                    data.Result.Count + (data.LimitExceeded ? "+" : "") + " documents";

                if (data.Exception != null)
                {
                    txtResult.BindErrorMessage(data.Sql, data.Exception);
                    txtParameters.BindErrorMessage(data.Sql, data.Exception);
                    grdResult.BindErrorMessage(data.Sql, data.Exception);
                }
                else if(data.Result != null)
                {
                    if (tabResult.SelectedTab == tabGrid && data.IsGridLoaded == false)
                    {
                        grdResult.BindBsonData(data);
                        data.IsGridLoaded = true;
                    }
                    else if(tabResult.SelectedTab == tabText && data.IsTextLoaded == false)
                    {
                        txtResult.BindBsonData(data);
                        data.IsTextLoaded = true;
                    }
                    else if(tabResult.SelectedTab == tabParameters && data.IsParametersLoaded == false)
                    {
                        txtParameters.BindParameter(data);
                        data.IsParametersLoaded = true;
                    }
                }
            }
        }

        private void AddSqlSnippet(string sql)
        {
            if (txtSql.Text.Trim().Length == 0)
            {
                txtSql.Text = sql.Replace("\\n", "\n");
            }
            else
            {
                AddNewTab(sql.Replace("\\n", "\n"));
            }
        }

        #region Grid Edit

        private void GrdResult_CellBeginEdit(object sender, DataGridViewCellCancelEventArgs e)
        {
            var cell = grdResult.Rows[e.RowIndex].Cells[e.ColumnIndex];

            cell.Value = JsonSerializer.Serialize(cell.Tag as BsonValue);
        }

        private void GrdResult_CellEndEdit(object sender, DataGridViewCellEventArgs e)
        {
            var field = grdResult.Columns[e.ColumnIndex].HeaderText;
            var id = grdResult.Rows[e.RowIndex].Cells["_id"].Tag as BsonValue;
            var cell = grdResult.Rows[e.RowIndex].Cells[e.ColumnIndex];
            var current = cell.Tag as BsonValue;
            var text = cell.Value.ToString();

            // try run update collection using current/new value
            var value = this.UpdateCellGrid(id, field, current, text);

            if (value != current)
            {
                cell.Style.BackColor = Color.LightGreen;
            }

            cell.SetBsonValue(value);
        }

        private void GrdResult_RowPostPaint(object sender, DataGridViewRowPostPaintEventArgs e)
        {
            var grid = sender as DataGridView;
            var rowIdx = (e.RowIndex + 1).ToString();

            var centerFormat = new StringFormat()
            {
                // right alignment might actually make more sense for numbers
                Alignment = StringAlignment.Center,
                LineAlignment = StringAlignment.Center
            };

            var headerBounds = new Rectangle(e.RowBounds.Left, e.RowBounds.Top, grid.RowHeadersWidth, e.RowBounds.Height);
            e.Graphics.DrawString(rowIdx, this.Font, SystemBrushes.ControlText, headerBounds, centerFormat);
        }

        public BsonValue UpdateCellGrid(BsonValue id, string field, BsonValue current, string json)
        {
            try
            {
                var value = JsonSerializer.Deserialize(json);

                if (current == value) return current;

                var r = _db.Execute($"UPDATE {this.ActiveTask.Collection} SET {{ {field}: @0 }} WHERE _id = @1 AND {field} = @2",
                    new BsonDocument
                    {
                        ["0"] = value,
                        ["1"] = id,
                        ["2"] = current
                    });

                if (r.Current == 1) return value;

                throw new Exception("Current document was not found. Try run your query again");
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return current;
            }
        }

        #endregion

        #region Toolbar

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            this.LoadTreeView();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5 && btnRun.Enabled)
            {
                BtnRun_Click(btnRun, EventArgs.Empty);
            }
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            var sql = txtSql.ActiveTextAreaControl.SelectionManager.SelectedText.Length > 0 ?
                txtSql.ActiveTextAreaControl.SelectionManager.SelectedText :
                txtSql.Text;

            this.ExecuteSql(sql);
        }

        private void BtnBegin_Click(object sender, EventArgs e)
        {
            this.ExecuteSql("BEGIN");
        }

        private void BtnCommit_Click(object sender, EventArgs e)
        {
            this.ExecuteSql("COMMIT");
        }

        private void BtnRollback_Click(object sender, EventArgs e)
        {
            this.ExecuteSql("ROLLBACK");
        }

        private void BtnCheckpoint_Click(object sender, EventArgs e)
        {
            this.ExecuteSql("CHECKPOINT");
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (_db == null)
                {
                    var dialog = new ConnectionForm(_connectionString ?? new ConnectionString());

                    dialog.ShowDialog();

                    if (dialog.DialogResult == DialogResult.OK)
                    {
                        this.Connect(dialog.ConnectionString);
                    }
                }
                else
                {
                    this.Disconnect();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        #endregion

        #region ContextMenu

        private void CtxMenu_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var colname = tvwDatabase.SelectedNode.Text;

            var sql = string.Format(e.ClickedItem.Tag.ToString(), colname);
            this.AddSqlSnippet(sql);
        }

        private void CtxMenuRoot_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            var sql = e.ClickedItem.Tag.ToString();
            this.AddSqlSnippet(sql);
        }

        #endregion

        #region TreeView

        private void TvwCols_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                tvwDatabase.SelectedNode = tvwDatabase.GetNodeAt(e.X, e.Y);
            }
        }

        private void TvwCols_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is string cmd)
            {
                this.AddSqlSnippet(cmd);
            }
        }

        #endregion

        #region Editor Tabs

        private void TabResult_Selected(object sender, TabControlEventArgs e)
        {
            if (tabSql.TabPages.Count == 0) return;

            this.LoadResult(this.ActiveTask);

            // set focus to result
            this.ActiveControl =
                tabResult.SelectedTab == tabGrid ? (Control)grdResult :
                tabResult.SelectedTab == tabText ? (Control)txtResult : (Control)txtParameters; 
        }

        private void TabSql_MouseClick(object sender, MouseEventArgs e)
        {
            var tabControl = sender as TabControl;
            var tabs = tabControl.TabPages;

            if (tabs.Count <= 1) return;

            if (e.Button == MouseButtons.Middle)
            {
                var tab = tabs.Cast<TabPage>()
                        .Where((t, i) => tabControl.GetTabRect(i).Contains(e.Location))
                        .First();

                if (tab.Tag is TaskData task)
                {
                    task.ThreadRunning = false;
                    task.WaitHandle.Set();
                    tabs.Remove(tab);
                }
            }
        }

        private void TabSql_SelectedIndexChanged(object sender, EventArgs e)
        {
            // this event occurs after tab already selected
            Application.DoEvents();

            txtSql.ActiveTextAreaControl.TextArea.Focus();
        }

        private void TabSql_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == null) return;

            txtSql.Clear();
            grdResult.Clear();
            txtResult.Clear();
            txtParameters.Clear();

            lblResultCount.Visible = false;
            lblElapsed.Text = "";
            prgRunning.Style = ProgressBarStyle.Blocks;
            lblResultCount.Text = "";

            Application.DoEvents();

            if (e.TabPage.Name == "+")
            {
                this.AddNewTab("");
            }
            else
            {
                // restore data
                this.ActiveTask.IsGridLoaded = this.ActiveTask.IsTextLoaded = this.ActiveTask.IsParametersLoaded = false;

                txtSql.Text = this.ActiveTask.EditorContent;

                if (this.ActiveTask.Position != null)
                {
                    txtSql.ActiveTextAreaControl.TextArea.Caret.Line = this.ActiveTask.Position.Item1;
                    txtSql.ActiveTextAreaControl.TextArea.Caret.Column = this.ActiveTask.Position.Item2;
                }

                if (tabResult.SelectedTab.Name != this.ActiveTask.SelectedTab && this.ActiveTask.SelectedTab != "")
                {
                    tabResult.SelectTab(this.ActiveTask.SelectedTab); // fire LoadResult from TabResult_IndexChanged
                }
                else
                {
                    this.LoadResult(this.ActiveTask);
                }
            }
        }

        #endregion
    }
}
