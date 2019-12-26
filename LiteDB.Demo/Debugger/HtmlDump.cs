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
    public class HtmlDump
    {
        private readonly BsonDocument _page;
        private readonly uint _pageID;
        private readonly PageType _pageType;
        private readonly byte[] _buffer;
        private readonly StringBuilder _writer = new StringBuilder();
        private readonly List<PageItem> _items = new List<PageItem>();
        private string[] _colors = new string[] {
            /* 400 */ "#B2EBF2", "#FFECB3"
        };

        public HtmlDump(BsonDocument page)
        {
            _page = page;
            _buffer = page["buffer"].AsBinary;
            _pageID = BitConverter.ToUInt32(_buffer, 0);
            _pageType = (PageType)_buffer[4];
            page.Remove("buffer");

            this.LoadItems();
            this.ReadItems();
        }

        private void LoadItems()
        {
            for (var i = 0; i < _buffer.Length; i++)
            {
                _items.Add(new PageItem
                {
                    Index = i,
                    Value = _buffer[i],
                    Title = "#" + i,
                    Color = -1
                });
            }
        }

        private void ReadItems()
        {
            // some span for header
            spanItem(0, 3, "/{text}", "PageID", BitConverter.ToUInt32);
            spanItem<byte>(4, 0, null, "PageType", null);
            spanItem(5, 3, "/{text}", "PrevPageID", BitConverter.ToUInt32);
            spanItem(9, 3, "/{text}", "NextPageID", BitConverter.ToUInt32);

            spanItem(13, 3, null, "TransactionID", BitConverter.ToUInt32);
            spanItem<byte>(17, 0, null, "IsConfirmed", null);
            spanItem(18, 3, null, "ColID", BitConverter.ToUInt32);

            spanItem<byte>(22, 0, null, "ItemsCount", null);
            spanItem(23, 1, null, "UsedBytes", BitConverter.ToUInt16);
            spanItem(25, 1, null, "FragmentedBytes", BitConverter.ToUInt16);
            spanItem(27, 1, null, "NextFreePosition", BitConverter.ToUInt16);
            spanItem<byte>(29, 0, null, "HighestIndex", null);
            spanItem<byte>(30, 0, null, "Reserved", null);
            spanItem<byte>(31, 0, null, "Reserved", null);

            // color segments
            var highestIndex = _buffer[29];

            if (highestIndex < byte.MaxValue)
            {
                var color = 0;

                for(var i = 0; i <= highestIndex; i++)
                {
                    var posAddr = 8192 - ((i + 1) * 4) + 2;
                    var lenAddr = 8192 - (i + 1) * 4;

                    var position = BitConverter.ToUInt16(_buffer, posAddr);
                    var length = BitConverter.ToUInt16(_buffer, lenAddr);

                    _items[posAddr].Span = 1;
                    _items[posAddr].Text = position.ToString();
                    _items[posAddr].Href = "#b" + position;
                    _items[posAddr].Title = $"Position #{i}";

                    _items[lenAddr].Span = 1;
                    _items[lenAddr].Text = length.ToString();
                    _items[lenAddr].Title = $"Length #{i}";

                    if (position != 0)
                    {
                        _items[posAddr].Color = (color % _colors.Length);
                        _items[lenAddr].Color = (color % _colors.Length);

                        _items[position].Id = "b" + position;
                        _items[position].Href = "/d/" + _pageID + "/" + position + "/" + length;

                        for(var j = position; j < position + length; j++)
                        {
                            _items[j].Color = (color % _colors.Length);
                        }

                        color++;
                    }
                }
            }

            if (_pageType == PageType.Header)
            {
                spanItem(32, 26, null, "HeaderInfo", (byte[] b, int i) => Encoding.UTF8.GetString(b, i, 27));
                spanItem<byte>(59, 0, null, "FileVersion", null);
                spanItem(60, 3, "/{text}", "FreeEmptyPageID", BitConverter.ToUInt32);
                spanItem(64, 3, "/{text}", "LastPageID", BitConverter.ToUInt32);
                spanItem<byte>(68, 7, null, "CreationTime", null);
                spanItem(76, 3, null, "UserVersion", BitConverter.ToInt32);
            }

            void spanItem<T>(int index, int span, string href, string title, Func<byte[], int, T> convert)
            {
                _items[index].Span = span;
                _items[index].Title = title;
                _items[index].Text = convert == null ? _items[index].Value.ToString() : convert(_buffer, index).ToString();
                _items[index].Href = href?.Replace("{text}", _items[index].Text);
            }
        }

        public string Render()
        {
            if (_page == null) return "Page not found";

            this.RenderHeader();
            this.RenderInfo();
            this.RenderPage();
            this.RenderFooter();

            return _writer.ToString();
        }

        private void RenderHeader()
        {
            _writer.AppendLine("<html>");
            _writer.AppendLine("<head>");
            _writer.AppendLine($"<title>LiteDB [Dump Page]: {_pageID}</title>");
            _writer.AppendLine("<style>");
            _writer.AppendLine(".page { display: flex; flex-wrap: wrap; width: 1205px; }");
            _writer.AppendLine(".page > a { font-family: monospace; background-color: #d1d1d1; margin: 1px; width: 60px; flex-basis: 35px; text-align: center; }");

            foreach (var color in _items.Select(x => x.Color).Where(x => x != -1).Distinct())
            {
                _writer.AppendLine($".c{color} {{ background-color: {_colors[color % _colors.Length]} !important; }}");
            }

            _writer.AppendLine("</style>");
            _writer.AppendLine("</head>");
            _writer.AppendLine("<body>");
            _writer.AppendLine($"<h1>{_pageType} - #{_pageID.ToString().PadLeft(4, '0')}</h1><hr/>");
        }

        private void RenderInfo()
        {
            _writer.AppendLine("<pre>");

            var json = new JsonWriter(new StringWriter(_writer));
            json.Pretty = true;
            json.Indent = 4;
            json.Serialize(_page);

            _writer.AppendLine("</pre>");
        }

        private void RenderPage()
        {
            _writer.AppendLine("<div class='page'>");

            for(var i = 0; i < _items.Count; i++)
            {
                var item = _items[i];

                _writer.Append("<a");

                if (!string.IsNullOrEmpty(item.Href))
                {
                    _writer.Append($" href='{item.Href}'");
                }

                if (item.Color >= 0)
                {
                    _writer.Append($" class='c{item.Color}'");
                }

                if (item.Span > 0)
                {
                    _writer.Append($" style='flex-basis: {35 * (item.Span + 1) + (item.Span * 2)}px'");
                }

                if (!string.IsNullOrEmpty(item.Title))
                {
                    _writer.Append($" title='{item.Title}'");
                }

                if (!string.IsNullOrEmpty(item.Id))
                {
                    _writer.Append($" id='{item.Id}'");
                }

                _writer.Append(">");
                _writer.Append(item.Text ?? item.Value.ToString());
                _writer.Append("</a>");

                i += item.Span;
            }

            _writer.AppendLine("</div>");
        }

        private void RenderFooter()
        {
            _writer.AppendLine("</body>");
            _writer.AppendLine("</html>");
        }

        public class PageItem
        {
            public int Index { get; set; }
            public string Href { get; set; }
            public string Id { get; set; }
            public string Title { get; set; }
            public string Text { get; set; }
            public byte Value { get; set; }
            public int Span { get; set; }
            public int Color { get; set; }
        }
    }
}
