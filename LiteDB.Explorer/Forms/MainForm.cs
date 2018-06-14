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

namespace LiteDB.Explorer
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

            txtFileName.Text = filename ?? "";

            _synchronizationContext = SynchronizationContext.Current;

            if (txtFileName.Text.Length > 0)
            {
                this.Connect();
            }

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
                if (txtFileName.Enabled)
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
            _db = new LiteEngine(txtFileName.Text);
            _running = true;
            btnConnect.Text = "Disconnect";
            txtFileName.Enabled = false;
            splitRight.Visible = btnRefresh.Enabled = tabSql.Enabled = btnRun.Enabled = true;

            tabSql.TabPages.Add("+", "+");

            this.LoadTreeView();
            this.AddNewTab();
        }

        private void Disconnect()
        {
            _db?.Dispose();
            _running = false;
            btnConnect.Text = "Connect";
            txtFileName.Enabled = true;

            splitRight.Visible = btnRefresh.Enabled = tabSql.Enabled = btnRun.Enabled = false;

            foreach (var tab in tabSql.TabPages.Cast<TabPage>().Where(x => x.Name    != "+").ToArray())
            {
                var task = tab.Tag as TaskData;
                task.Thread.Abort();
            }

            tabSql.TabPages.Clear();

            tvwDatabase.Nodes.Clear();
        }

        private void AddNewTab()
        {
            var tab = tabSql.TabPages.Cast<TabPage>().Where(x => x.Text == "+").Single();
            var last = tabSql.TabPages.Cast<TabPage>().Where(x => x.Text != "+").Select(x => x.Tag as TaskData).LastOrDefault();

            var task = new TaskData()
            {
                Id = ((last?.Id) ?? 0) + 1,
            };

            task.Thread = new Thread(new ThreadStart(() => CreateThread(task)));
            task.Thread.Start();

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

            if (e.TabPage.Name == "+")
            {
                this.AddNewTab();
            }
            else
            {
                _active.Sql = txtSql.Text;
                _active = e.TabPage.Tag as TaskData;
                txtSql.Text = _active.Sql;
                this.LoadResult(_active);
            }
        }

        private void BtnRun_Click(object sender, EventArgs e)
        {
            _active.Sql = txtSql.SelectedText.Length > 0 ? txtSql.SelectedText : txtSql.Text;
            _active.Thread.Interrupt();
        }

        private void LoadTreeView()
        {
            tvwDatabase.Nodes.Clear();

            var root = tvwDatabase.Nodes.Add(Path.GetFileNameWithoutExtension(_db.Settings.Filename));
            var system = root.Nodes.Add("System");

            root.ImageKey = "database";
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
                col.Tag = $"SELECT $ FROM {key}";
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

                    using (var reader = _db.Execute(task.Sql))
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
                txtSql.Text = sql;
            }
            else
            {
                txtSql.Text += "\n\n" + sql;
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
            var cell = grdResult.Rows[e.RowIndex].Cells[e.ColumnIndex];

            cell.SetBsonValue(cell.Tag as BsonValue);
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

                var task = tab.Tag as TaskData;
                task.Thread.Abort();

                tabs.Remove(tab);
            }
        }
    }
}
