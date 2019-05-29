using ICSharpCode.TextEditor.Gui.CompletionWindow;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiteDB.Studio
{
    static class UIExtensions
    {
        public static void BindBsonData(this DataGridView grd, TaskData data)
        {
            // hide grid if has more than 100 rows
            grd.Visible = data.Result.Count < 100;
            grd.Clear();

            foreach (var value in data.Result)
            {
                var row = new DataGridViewRow();

                var doc = value.IsDocument ?
                    value.AsDocument :
                    new BsonDocument { ["[value]"] = value };

                if (doc.Keys.Count == 0) doc["[root]"] = "{}";

                foreach (var key in doc.Keys)
                {
                    var col = grd.Columns[key];

                    if (col == null)
                    {
                        grd.Columns.Add(key, key);

                        col = grd.Columns[key];
                        col.AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;

                        col.ReadOnly = key == "_id";
                    }
                }

                row.DefaultCellStyle.BackColor = Color.Silver;
                row.CreateCells(grd);

                foreach (var key in doc.Keys)
                {
                    var col = grd.Columns[key];
                    var cell = row.Cells[col.Index];

                    cell.Style.BackColor = Color.White;
                    cell.SetBsonValue(doc[key]);

                    row.ReadOnly = key == "_id";
                }

                grd.Rows.Add(row);
            }

            if (data.LimitExceeded)
            {
                var limitRow = new DataGridViewRow();
                limitRow.CreateCells(grd);
                limitRow.DefaultCellStyle.ForeColor = Color.OrangeRed;
                var cell = limitRow.Cells[0];
                cell.Value = "Limit exceeded";
                cell.ReadOnly = true;
                grd.Rows.Add(limitRow);
            }

            for (int i = 0; i <= grd.Columns.Count - 1; i++)
            {
                var colw = grd.Columns[i].Width;
                grd.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;
                grd.Columns[i].Width = Math.Min(colw, 400);
            }

            if (grd.Rows.Count == 0)
            {
                grd.Columns.Add("no-data", "[no result]");
            }

            grd.ReadOnly = grd.Columns["_id"] == null;
            grd.Visible = true;
        }

        public static void SetBsonValue(this DataGridViewCell cell, BsonValue value)
        {
            if (value == null)
            {
                cell.Value = "";
                cell.Tag = null;
                return;
            }

            switch (value.Type)
            {
                case BsonType.MinValue:
                    cell.Value = "-∞";
                    break;
                case BsonType.MaxValue:
                    cell.Value = "+∞";
                    break;
                case BsonType.Boolean:
                    cell.Value = value.AsBoolean.ToString().ToLower();
                    break;
                case BsonType.DateTime:
                    cell.Value = value.AsDateTime.ToString();
                    break;
                case BsonType.Null:
                    cell.Value = "(null)";
                    cell.Style.ForeColor = Color.Silver;
                    break;
                case BsonType.Binary:
                    cell.Value = Convert.ToBase64String(value.AsBinary);
                    break;
                case BsonType.Int32:
                case BsonType.Int64:
                case BsonType.Double:
                case BsonType.Decimal:
                case BsonType.String:
                case BsonType.ObjectId:
                case BsonType.Guid:
                    cell.Value = value.ToString();
                    break;
                default:
                    cell.Value = JsonSerializer.Serialize(value);
                    break;
            }

            cell.ToolTipText = value.Type.ToString();
            cell.Tag = value;
        }

        public static void DoubleBuffered(this DataGridView dgv, bool setting)
        {
            var dgvType = dgv.GetType();
            var pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dgv, setting, null);
        }

        public static void Clear(this DataGridView grd)
        {
            grd.Columns.Clear();
            grd.DataSource = null;
        }

        public static void BindBsonData(this ICSharpCode.TextEditor.TextEditorControl txt, TaskData data)
        {
            var index = 0;
            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            {
                var json = new JsonWriter(writer)
                {
                    Pretty = true,
                    Indent = 2
                };

                if (data.Result.Count > 0)
                {
                    foreach (var value in data.Result)
                    {
                        if (data.Result?.Count > 1)
                        {
                            sb.AppendLine($"/* {index++ + 1} */");
                        }

                        json.Serialize(value);
                        sb.AppendLine();
                    }

                    if (data.LimitExceeded)
                    {
                        sb.AppendLine();
                        sb.AppendLine("/* Limit exceeded */");
                    }
                }
                else
                {
                    sb.AppendLine("no result");
                }
            }

            txt.SetHighlighting("JSON");
            txt.Text = sb.ToString();
        }

        public static void BindErrorMessage(this DataGridView grid, string sql, Exception ex)
        {
            grid.Clear();
            grid.Columns.Add("err", "Error");
            grid.Columns[0].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            grid.Rows.Add(ex.Message);
        }

        public static void BindErrorMessage(this ICSharpCode.TextEditor.TextEditorControl txt, string sql, Exception ex)
        {
            var sb = new StringBuilder();

            if (!(ex is LiteException))
            {
                sb.AppendLine(ex.Message);
                sb.AppendLine();
                sb.AppendLine("===================================================");
                sb.AppendLine(ex.StackTrace);
            }
            else if (ex is LiteException lex)
            {
                sb.AppendLine(ex.Message);

                if (lex.ErrorCode == LiteException.UNEXPECTED_TOKEN && sql != null)
                {
                    var p = (int)lex.Position;
                    var start = (int)Math.Max(p - 30, 1) - 1;
                    var end = Math.Min(p + 15, sql.Length);
                    var length = end - start;

                    var str = sql.Substring(start, length).Replace('\n', ' ').Replace('\r', ' ');
                    var t = length - (end - p);

                    sb.AppendLine();
                    sb.AppendLine(str);
                    sb.AppendLine("".PadLeft(t, '-') + "^");
                }
            }

            txt.Highlighting = null;
            txt.Clear();
            txt.Text = sb.ToString();
        }

        public static void BindParameter(this ICSharpCode.TextEditor.TextEditorControl txt, TaskData data)
        {
            txt.SuspendLayout();
            txt.Clear();
            txt.SetHighlighting("JSON");

            var sb = new StringBuilder();

            using (var writer = new StringWriter(sb))
            {
                var w = new JsonWriter(writer)
                {
                    Pretty = true,
                    Indent = 2
                };

                w.Serialize(data.Parameters ?? BsonValue.Null);
            }

            txt.Text = sb.ToString();
            txt.ResumeLayout();
        }
    }
}
