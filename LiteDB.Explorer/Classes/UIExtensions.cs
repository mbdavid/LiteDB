using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LiteDB.Explorer
{
    static class UIExtensions
    {
        public static void BindBsonData(this DataGridView grd, TaskData data)
        {
            grd.Clear();

            if (data.Result == null) return;

            foreach (var value in data.Result)
            {
                var row = new DataGridViewRow();

                var doc = value.IsDocument ?
                    value.AsDocument :
                    new BsonDocument { ["[value]"] = value };

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
        }

        public static void SetBsonValue(this DataGridViewCell cell, BsonValue value)
        {
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
                    cell.Value = value.RawValue.ToString();
                    break;
                default:
                    cell.Value = JsonSerializer.Serialize(value, false, false);
                    break;
            }

            cell.ToolTipText = value.Type.ToString();
            cell.Tag = value;
        }

        public static void BindBsonData(this RichTextBox txt, TaskData data)
        {
            var index = 0;

            txt.Text = "";

            if (data.Result?.Count > 0)
            {
                foreach (var value in data.Result)
                {
                    txt.AppendText($"[{index++ + 1}]:" + Environment.NewLine, Color.DarkGreen);
                    txt.AppendText(JsonSerializer.Serialize(value, true, true) + Environment.NewLine, Color.Black);

                }

                if (data.LimitExceeded)
                {
                    txt.AppendText("Limit exceeded", Color.OrangeRed);
                }
            }
            else
            {
                txt.AppendText("no result", Color.Gray);
            }

            txt.SelectionStart = 0;
        }

        public static void DoubleBuffered(this DataGridView dgv, bool setting)
        {
            var dgvType = dgv.GetType();
            var pi = dgvType.GetProperty("DoubleBuffered", BindingFlags.Instance | BindingFlags.NonPublic);
            pi.SetValue(dgv, setting, null);
        }

        public static void AppendText(this RichTextBox box, string text, Color color)
        {
            box.SelectionStart = box.TextLength;
            box.SelectionLength = 0;

            box.SelectionColor = color;
            box.AppendText(text);
            box.SelectionColor = box.ForeColor;
        }

        public static void Clear(this DataGridView grd)
        {
            grd.Columns.Clear();
            grd.DataSource = null;
        }

        public static void BindErrorMessage(this RichTextBox txt, string sql, Exception ex)
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

                    var str = sql.Substring(start, length);
                    var t = length - (end - p);

                    sb.AppendLine();
                    sb.AppendLine(str);
                    sb.AppendLine("".PadLeft(t, '-') + "^");
                }
            }

            txt.Text = "";
            txt.ForeColor = Color.Red;
            txt.Text = sb.ToString();
        }
    }
}
