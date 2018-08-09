using LiteDB.Engine;
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

        private LiteEngine _db;
        private TaskData _active = null;
        private bool _running = true;

        public MainForm(string filename)
        {
            InitializeComponent();

            // For performance https://stackoverflow.com/questions/4255148/how-to-improve-painting-performance-of-datagridview
            grdResult.DoubleBuffered(true);

            txtFilename.Text = filename ?? "";

            _synchronizationContext = SynchronizationContext.Current;

            if (txtFilename.Text.Length > 0)
            {
                this.Connect();
            }
            else
            {
                this.Disconnect();
            }

            UIExtensions.DefineSyntaxHighlighter(txtSql);

            // stop all threads
            this.FormClosing += (s, e) =>
            {
                this.Disconnect();
            };
        }

        private void BtnConnect_Click(object sender, EventArgs e)
        {
            try
            {
                if (txtFilename.Enabled)
                {
                    this.Connect();
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

        private void Connect()
        {
            var settings = new EngineSettings
            {
                Filename = txtFilename.Text,
                Log = new Logger(Logger.FULL, this.DoLog)
            };

            _db = new LiteEngine(settings);
            _running = true;
            btnConnect.Text = "Disconnect";
            txtFilename.Enabled = btnFileOpen.Enabled = false;
            splitRight.Visible = btnRefresh.Enabled = tabSql.Enabled = btnRun.Enabled = btnBegin.Enabled = btnCommit.Enabled = btnRollback.Enabled = true;

            tabSql.TabPages.Add("+", "+");

            this.LoadTreeView();
            this.AddNewTab();
        }

        private void Disconnect()
        {
            _db?.Dispose();
            _running = false;
            btnConnect.Text = "Connect";
            txtFilename.Enabled = btnFileOpen.Enabled = true;

            splitRight.Visible = btnRefresh.Enabled = tabSql.Enabled = btnRun.Enabled = btnBegin.Enabled = btnCommit.Enabled = btnRollback.Enabled = false;

            foreach (var tab in tabSql.TabPages.Cast<TabPage>().Where(x => x.Name    != "+").ToArray())
            {
                var task = tab.Tag as TaskData;
                task.Thread.Abort();
            }

            tabSql.TabPages.Clear();

            tvwDatabase.Nodes.Clear();
        }

        private void DoLog(LogEntry log)
        {
            _synchronizationContext.Post(new SendOrPostCallback(o =>
            {
                try
                {
                    var l = (LogEntry)o;

                    txtLog.AppendText(
                        string.Format("{0}: {1:HH:mm:ss} [{2}] - {3}\n",
                        l.ThreadID,
                        l.Time,
                        l.Level,
                        l.Message.Replace('\r', ';').Replace('\n', ' ')));

                    txtLog.ScrollToCaret();
                }
                catch
                {
                }

            }), log);

        }

        private void AddNewTab()
        {
            var tab = tabSql.TabPages.Cast<TabPage>().Where(x => x.Text == "+").Single();

            var task = new TaskData();

            task.Thread = new Thread(new ThreadStart(() => CreateThread(task)));
            task.Thread.Start();

            task.Id = task.Thread.ManagedThreadId;

            tab.Text = tab.Name = task.Id.ToString();
            tab.Tag = task;

            txtSql.Text = "";

            // adding new + tab at end
            tabSql.TabPages.Add("+", "+");

            _active = task;

            this.LoadResult(task);
        }

        private void TabSql_Selected(object sender, TabControlEventArgs e)
        {
            if (e.TabPage == null) return;

            if (_active != null) _active.Sql = txtSql.Text;

            if (e.TabPage.Name == "+")
            {
                this.AddNewTab();
            }
            else
            {
                _active = e.TabPage.Tag as TaskData;
                txtSql.Text = _active.Sql;
                Application.DoEvents();
                this.LoadResult(_active);
            }
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            this.ExecuteSql(txtSql.SelectedText.Length > 0 ? txtSql.SelectedText : txtSql.Text);
        }

        private void ExecuteSql(string sql)
        {
            if (_active.Running == false)
            {
                _active.Sql = sql;
                _active.Thread.Interrupt();
            }
        }

        private void LoadTreeView()
        {
            tvwDatabase.Nodes.Clear();

            var root = tvwDatabase.Nodes.Add(Path.GetFileNameWithoutExtension(_db.Filename));
            var system = root.Nodes.Add("System");

            root.ImageKey = "database";
            root.ContextMenuStrip = ctxMenuRoot;

            system.ImageKey = system.SelectedImageKey = "folder";

            foreach (var key in _db.GetSystemCollections().OrderBy(x => x))
            {
                var col = system.Nodes.Add(key);
                col.Tag = $"SELECT $ FROM {key}";
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
            while(_running)
            {
                try
                {
                    Thread.Sleep(Timeout.Infinite);
                }
                catch (ThreadInterruptedException)
                {
                }

                if (task.Sql.Trim() == "") continue;

                var sw = new Stopwatch();
                sw.Start();

                try
                {
                    task.Running = true;

                    _synchronizationContext.Post(new SendOrPostCallback(o =>
                    {
                        this.LoadResult(task);
                    }), task);

                    task.Parameters = new BsonDocument();

                    using (var reader = _db.Execute(task.Sql, task.Parameters))
                    {
                        task.ReadResult(reader);
                    }

                    task.Elapsed = sw.Elapsed;
                    task.Exception = null;
                    task.Running = false;

                    // update form button selected
                    if (_active.Id == task.Id)
                    {
                        _synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            this.LoadResult(o as TaskData);
                        }), task);
                    }
                }
                catch (Exception ex)
                {
                    task.Running = false;
                    task.Result = null;
                    task.Elapsed = sw.Elapsed;
                    task.Exception = ex;

                    if (_active.Id == task.Id)
                    {
                        _synchronizationContext.Post(new SendOrPostCallback(o =>
                        {
                            tabResult.SelectedTab = tabText;
                            this.LoadResult(o as TaskData);
                        }), task);
                    }
                }
            }
        }

        private void LoadResult(TaskData data)
        {
            btnRun.Enabled = !data.Running;
            txtParameters.Text = JsonSerializer.Serialize(data.Parameters, true);

            if (data.Running)
            {
                grdResult.Clear();
                txtResult.Clear();
                lblResultCount.Visible = false;
                lblElapsed.Text = "Running";
                prgRunning.Style = ProgressBarStyle.Marquee;
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
                    grdResult.Clear();
                }
                else
                {
                    if (tabResult.SelectedTab.Text == "Grid")
                    {
                        grdResult.BindBsonData(data);
                        txtResult.Text = "";
                    }
                    else
                    {
                        txtResult.BindBsonData(data);
                        grdResult.Clear();
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
                txtSql.Text += "\n\n";
                var start = txtSql.TextLength;
                txtSql.Text += sql.Replace("\\n", "\n");
                txtSql.Select(start, sql.Length);
            }
        }

        public BsonValue UpdateCellGrid(BsonValue id, string field, BsonValue current, string json)
        {
            try
            {
                var value = JsonSerializer.Deserialize(json);

                if (current == value) return current;

                var r = _db.Execute($"UPDATE {_active.Collection} SET {{ {field}: @0 }} WHERE _id = @1 AND {field} = @2",
                    new BsonDocument
                    {
                        ["0"] = value,
                        ["1"] = id,
                        ["2"] = current
                    });

                if (r.Current == 1) return value;

                throw new Exception("Current document was not found. Try run your query again");
            }
            catch(Exception ex)
            {
                MessageBox.Show(ex.Message, "Update error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return current;
            }
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F5 && btnRun.Enabled)
            {
                BtnRun_Click(btnRun, EventArgs.Empty);
            }
        }

        private void TvwCols_NodeMouseDoubleClick(object sender, TreeNodeMouseClickEventArgs e)
        {
            if (e.Node.Tag is string cmd)
            {
                this.AddSqlSnippet(cmd);
            }
        }

        private void TabResult_Selected(object sender, TabControlEventArgs e)
        {
            this.LoadResult(_active);

            if (tabResult.SelectedTab == tabText) ActiveControl = txtResult;
            else if (tabResult.SelectedTab == tabGrid) ActiveControl =  grdResult;
        }

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

        private void BtnRefresh_Click(object sender, EventArgs e)
        {
            this.LoadTreeView();
        }

        private void TxtSql_SelectionChanged(object sender, EventArgs e)
        {
            lblCursor.Text = "Position: " + (txtSql.SelectionStart + 1) +
                (txtSql.SelectionLength > 0 ? $" (Selection: {txtSql.SelectionLength})" : "");
        }

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

        private void TvwCols_MouseUp(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                tvwDatabase.SelectedNode = tvwDatabase.GetNodeAt(e.X, e.Y);
            }
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
                    task.Thread.Abort();
                    tabs.Remove(tab);
                }
            }
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

        private void TxtSql_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == (char)Keys.Tab)
            {
                e.Handled = true;
                txtSql.SelectedText = new string(' ', 4);
            }
        }

        private void BtnFileOpen_Click(object sender, EventArgs e)
        {
            diaOpen.FileName = txtFilename.Text;

            if (diaOpen.ShowDialog() == DialogResult.OK)
            {
                txtFilename.Text = diaOpen.FileName;

                BtnConnect_Click(null, null);
            }
        }
    }
}
