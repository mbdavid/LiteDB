using LiteDB;
using LiteDB.Engine;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Contracts;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Reflection;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LiteDB.Demo
{
    public class HtmlPageDump
    {
        private const int BLOCK_WIDTH = 30;

        private readonly BsonDocument _page;
        private readonly uint _pageID;
        private readonly PageType _pageType;
        private readonly byte[] _buffer;
        private readonly StringBuilder _writer = new StringBuilder();
        private readonly List<PageItem> _items = new List<PageItem>();
        private string[] _colors = new string[] { "#B2EBF2", "#FFECB3" };

        public HtmlPageDump(BsonDocument page)
        {
            _page = page;
            _buffer = page["buffer"].AsBinary;
            _pageID = BitConverter.ToUInt32(_buffer, 0);
            _pageType = (PageType)_buffer[4];
            page.Remove("buffer");

            this.LoadItems();
            this.SpanCaptionItems();
        }

        private void LoadItems()
        {
            for (var i = 0; i < _buffer.Length; i++)
            {
                _items.Add(new PageItem
                {
                    Index = i,
                    Value = _buffer[i],
                    Color = -1
                });
            }
        }

        private void SpanCaptionItems()
        {
            this.SpanPageHeader();
            this.SpanSegments();

            if (_pageType == PageType.Header)
            {
                this.SpanHeaderPage();
            }
            else if (_pageType == PageType.Collection)
            {
                this.SpanCollectionPage();
            }
        }

        private void SpanPageHeader()
        {
            var h = 0;

            // some span for header
            h += this.SpanPageID(h, "PageID", false, false);
            h += this.SpanItem<byte>(h, 0, null, "PageType", null);
            h += this.SpanPageID(h, "PrevPageID", false, false);
            h += this.SpanPageID(h, "NextPageID", false, true);
            h += this.SpanItem<byte>(h, 0, null, "Slot", null);

            h += this.SpanItem(h, 3, null, "TransactionID", BitConverter.ToUInt32);
            h += this.SpanItem<byte>(h, 0, null, "IsConf", null);
            h += this.SpanItem(h, 3, null, "ColID", BitConverter.ToUInt32);

            h += this.SpanItem<byte>(h, 0, null, "Items", null);
            h += this.SpanItem(h, 1, null, "UsedBytes", BitConverter.ToUInt16);
            h += this.SpanItem(h, 1, null, "FragmentedBytes", BitConverter.ToUInt16);
            h += this.SpanItem(h, 1, null, "NextFreePo", BitConverter.ToUInt16);
            h += this.SpanItem<byte>(h, 0, null, "HghIdx", null);
            h += this.SpanItem<byte>(h, 0, null, "Reserv", null);
        }

        private void SpanSegments()
        {
            // color segments
            var highestIndex = _buffer[30];

            if (highestIndex < byte.MaxValue)
            {
                var colorIndex = 0;

                for (var i = 0; i <= highestIndex; i++)
                {
                    var posAddr = _buffer.Length - ((i + 1) * 4) + 2;
                    var lenAddr = _buffer.Length - (i + 1) * 4;

                    var position = BitConverter.ToUInt16(_buffer, posAddr);
                    var length = BitConverter.ToUInt16(_buffer, lenAddr);

                    _items[lenAddr].Span = 1;
                    _items[lenAddr].Caption = i.ToString();
                    _items[lenAddr].Text = length.ToString();

                    _items[posAddr].Span = 1;
                    _items[posAddr].Caption = i.ToString();
                    _items[posAddr].Text = position.ToString();
                    _items[posAddr].Href = "#" + i;

                    if (position != 0)
                    {
                        _items[posAddr].Color = _items[lenAddr].Color = (colorIndex++ % _colors.Length);

                        _items[position].Id = i.ToString();

                        if (_pageType == PageType.Data)
                        {
                            this.SpanItem<byte>(position, 0, null, "Extend", null);
                            this.SpanPageID(position + 1, "NextBlockID", true, false);
                        }
                        else
                        {
                            this.SpanItem<byte>(position, 0, null, "Slot", null);
                            this.SpanItem<byte>(position + 1, 0, null, "Level", null);
                            this.SpanPageID(position + 2, "DataBlock", true, false);
                            this.SpanPageID(position + 7, "NextNode", true, false);

                            for (var j = 0; j < _buffer[position + 1]; j++)
                            {
                                this.SpanPageID(position + 12 + (j * 5 * 2), "Prev #" + j, true, false);
                                this.SpanPageID(position + 12 + (j * 5 * 2) + 5, "Next #" + j, true, false);
                            }

                            var p = position + 12 + (_buffer[position + 1] * 5 * 2);

                            this.SpanItem<byte>(p, 0, null, "Type", null);

                            if (_buffer[p] == 6 || _buffer[p] == 9)
                            {
                                this.SpanItem<byte>(++p, 0, null, "Len", null);
                            }

                            //TODO key??
                        }

                        for (var j = position; j < position + length; j++)
                        {
                            _items[j].Color = colorIndex - 1;
                        }
                    }
                }
            }

            // fixing zebra segment colors
            var current = 0;
            var color = 0;

            for (var i = 0; i < _buffer.Length; i++)
            {
                if (_items[i].Color != -1)
                {
                    if (_items[i].Color != current)
                    {
                        color++;
                    }

                    current = _items[i].Color;
                    _items[i].Color = color % _colors.Length;
                }
            }
        }

        private void SpanHeaderPage()
        {
            var h = 32;
            var color = 0;

            h += this.SpanItem(h, 26, null, "HeaderInfo", (byte[] b, int i) => Encoding.UTF8.GetString(b, i, 27));
            h += this.SpanItem<byte>(h, 0, null, "FileVersion", null);
            h += this.SpanPageID(h, "FreeEmptyPageList", false, true);
            h += this.SpanPageID(h, "LastPageID", false, false);
            h += this.SpanItem(h, 7, null, "CreationTime", (byte[] b, int i) => new DateTime(BitConverter.ToInt64(b, i)).ToString("o"));
            h += this.SpanItem(h, 3, null, "UserVersion", BitConverter.ToInt32);

            var collectionPosition = 128;

            this.SpanItem(collectionPosition, 3, null, "Length", BitConverter.ToInt32);
            
            var p = collectionPosition + 4;

            while (p < collectionPosition + 4 + BitConverter.ToInt32(_buffer, collectionPosition) - 5)
            {
                var initial = p;

                p++; // dataType
                p += this.SpanCString(p, "Name");
                p += this.SpanPageID(p, "PageID", false, false);

                for (var k = initial; k <= p; k++)
                {
                    _items[k].Color = (color % _colors.Length);
                }

                color++;
            }
        }

        private void SpanCollectionPage()
        {
            var color = 0;

            for (var i = 0; i < 5; i++)
            {
                this.SpanPageID(32 + (i * 4), "DataPageList #" + i, false, true);
            }

            var h = 96;
            var indexes = _buffer[h];

            h += this.SpanItem<byte>(h, 0, null, "Indexes", null);

            for (var i = 0; i < indexes; i++)
            {
                var initial = h;

                h += this.SpanItem<byte>(h, 0, null, "Slot", null);
                h += this.SpanItem<byte>(h, 0, null, "Type", null);
                h += this.SpanCString(h, "Name");
                h += this.SpanCString(h, "Expr");
                h += this.SpanItem<byte>(h, 0, null, "Unique", null);
                h += this.SpanPageID(h, "Head", true, false);
                h += this.SpanPageID(h, "Tail", true, false);
                h += this.SpanItem<byte>(h, 0, null, "MaxLevel", null);
                h += this.SpanPageID(h, "IndexPageList", false, true);

                for (var k = initial; k <= h; k++)
                {
                    _items[k].Color = (color % _colors.Length);
                }

                color++;
            }
        }

        #region SpanItems Method Helpers

        private int SpanItem<T>(int index, int span, string href, string caption, Func<byte[], int, T> convert)
        {
            _items[index].Span = span;
            _items[index].Caption = caption;
            _items[index].Text = convert == null ? _items[index].Value.ToString() : convert(_buffer, index).ToString();
            _items[index].Href = href?.Replace("{text}", _items[index].Text);

            return span + 1;
        }

        private int SpanPageID(int index, string caption, bool pageAddress, bool pageList)
        {
            var pageID = BitConverter.ToUInt32(_buffer, index);

            _items[index].Span = 3;
            _items[index].Caption = caption;
            _items[index].Text = pageID == uint.MaxValue ? "-" : pageID.ToString();
            _items[index].Href = pageID == uint.MaxValue || index == 0 ? 
                null : 
                "/" + (pageList ? "list/" : "") + pageID + (pageAddress ? "#" + _buffer[index + 4] : "");

            if (pageAddress)
            {
                _items[index + 4].Caption = "Index";
                _items[index + 4].Text = _items[index + 4].Value == byte.MaxValue ? "-" : _items[index + 4].Value.ToString();
            }

            if (pageList)
            {
                _items[index].Target = "list";
            }

            return 4 + (pageAddress ? 1 : 0);
        }

        private int SpanCString(int index, string caption)
        {
            var p = index;

            while (_buffer[p] != 0)
            {
                p++;
            }

            var length = p - index;

            if (length > 0)
            {
                _items[index].Text = Encoding.UTF8.GetString(_buffer, index, length);
                _items[index].Span = length - 1; // include /0
                _items[index].Caption = caption;
            }

            return length + 1;
        }

        #endregion

        public string Render()
        {
            if (_page == null) return "Page not found";

            this.RenderHeader();
            this.RenderInfo();
            this.RenderConvert();
            this.RenderPage();
            this.RenderFooter();

            return _writer.ToString();
        }

        private void RenderHeader()
        {
            _writer.AppendLine("<html>");
            _writer.AppendLine("<head>");
            _writer.AppendLine($"<title>LiteDB Debugger: #{_pageID.ToString("0000")} - {_pageType}</title>");
            _writer.AppendLine("<style>");
            _writer.AppendLine("* { box-sizing: border-box; }");
            _writer.AppendLine("body { font-family: monospace; }");
            _writer.AppendLine("h1 { border-bottom: 2px solid #545454; color: #545454; margin: 0; }");
            _writer.AppendLine("textarea { margin: 0px; width: 1000px; height: 61px; vertical-align: top; }");
            _writer.AppendLine(".page { display: flex; min-width: 1245px; }");
            _writer.AppendLine($".rules > div {{ padding: 9px 0 0; height: 31px; width: {BLOCK_WIDTH}px; color: gray; background-color: #f1f1f1; margin: 1px; text-align: center; position: relative; }}");
            _writer.AppendLine(".line { min-width: 1024px; }");
            _writer.AppendLine($".line > a {{ background-color: #d1d1d1; margin: 1px; min-width: {BLOCK_WIDTH}px; height: 30px; display: inline-block; text-align: center; padding: 10px 0 0; position: relative; }}");
            _writer.AppendLine(".line:first-child > a { background-color: #a1a1a1; }");
            _writer.AppendLine(".line > a[href] { color: blue; }");
            _writer.AppendLine(".line > a:before { background-color: white; font-size: 7px; top: -1; left: 0; color: black; position: absolute; content: attr(st); }");
            _writer.AppendLine("iframe { border: none; flex: 1; min-width: 50px; }");

            foreach (var color in _items.Select(x => x.Color).Where(x => x != -1).Distinct())
            {
                _writer.AppendLine($".c{color} {{ background-color: {_colors[color % _colors.Length]} !important; }}");
            }

            _writer.AppendLine("</style>");
            _writer.AppendLine("</head>");
            _writer.AppendLine("<body>");
            _writer.AppendLine($"<h1>#{_pageID.ToString("0000")} :: {_pageType} Page</h1>");
        }

        private void RenderInfo()
        {
            if (_page.ContainsKey("pageID"))
            {
                _writer.AppendLine("<div style='text-align: center; margin: 5px 0; width: 1245px'>");
                _writer.AppendLine($"Origin: [{_page["_origin"].AsString}] - Position: {_page["_position"].AsInt64} - Free bytes: {_page["freeBytes"]}");
                _writer.AppendLine("</div>");
            }
        }

        private void RenderConvert()
        {
            _writer.AppendLine($"<form method='post' action='/{_pageID}'>");
            _writer.AppendLine("<textarea placeholder='Paste hex page body content here' name='b'></textarea>");
            _writer.AppendLine("<button type='submit'>View</button>");
            _writer.AppendLine("</form>");
        }

        private void RenderPage()
        {
            _writer.AppendLine("<div class='page'>");

            this.RenderRules();
            this.RenderBlocks();

            _writer.AppendLine("<iframe name='list' id='list'></iframe>");

            _writer.AppendLine("</div>");
        }

        private void RenderRules()
        {
            _writer.AppendLine("<div class='rules'>");

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                if (i % 32 == 0)
                {
                    _writer.AppendLine($"<div>{i}</div>");
                }
            }

            _writer.AppendLine("</div>");
        }

        private void RenderBlocks()
        {
            var span = 32;

            _writer.AppendLine("<div class='blocks'>");
            _writer.AppendLine("<div class='line'>");

            for (var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                span -= (item.Span + 1);

                var renderText = this.RenderItem(item, span);

                if (span <= 0)
                {
                    _writer.AppendLine("</div><div class='line'>");

                    if (span < 0)
                    {
                        var overflow = new PageItem { Color = item.Color, Span = Math.Abs(span + 1), Text = "&nbsp;" };

                        if (renderText == false) overflow.Text = item.Text;

                        this.RenderItem(overflow, 0);
                    }

                    span = 32 + span;
                }

                i += item.Span;
            }

            _writer.AppendLine("</div>");
            _writer.AppendLine("</div>");
        }

        private bool RenderItem(PageItem item, int span)
        {
            var renderText = true;

            _writer.Append($"<a title='{item.Index}'");

            if (!string.IsNullOrEmpty(item.Href))
            {
                _writer.Append($" href='{item.Href}'");
            }

            if (!string.IsNullOrEmpty(item.Target))
            {
                _writer.Append($" target='{item.Target}'");
            }

            if (item.Color >= 0)
            {
                _writer.Append($" class='c{item.Color}'");
            }

            if (item.Span > 0)
            {
                var s = item.Span + (span < 0 ? span : 0);

                if (s < item.Span && Math.Abs(span) > s)
                {
                    renderText = false;
                }

                _writer.Append($" style='min-width: {BLOCK_WIDTH * (s + 1) + (s * 2)}px'");
            }

            if (!string.IsNullOrEmpty(item.Caption))
            {
                _writer.Append($" st='{item.Caption}'");
            }

            if (!string.IsNullOrEmpty(item.Id))
            {
                _writer.Append($" id='{item.Id}'");
            }

            _writer.Append(">");
            _writer.Append(renderText ? (item.Text ?? item.Value.ToString()) : "&#8594;");
            _writer.Append("</a>");

            return renderText;
        }

        private void RenderFooter()
        {
            _writer.AppendLine("</body>");
            _writer.AppendLine("</html>");
        }

        public class PageItem
        {
            public int Index { get; set; }
            public string Id { get; set; }
            public string Text { get; set; }
            public byte Value { get; set; }
            public int Span { get; set; }
            public string Caption { get; set; }
            public int Color { get; set; }
            public string Href { get; set; }
            public string Target { get; set; }
        }
    }
}
