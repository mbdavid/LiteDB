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
    public class HtmlPageList
    {
        private readonly IEnumerable<BsonDocument> _pages;
        private readonly StringBuilder _writer = new StringBuilder();

        public HtmlPageList(IEnumerable<BsonDocument> pages)
        {
            _pages = pages;
        }

        public string Render()
        {
            this.RenderHeader();
            this.RenderInfo();
            this.RenderFooter();

            return _writer.ToString();
        }

        private void RenderHeader()
        {
            _writer.AppendLine("<html>");
            _writer.AppendLine("<head>");
            _writer.AppendLine("<title>LiteDB Explorer Debugger</title>");
            _writer.AppendLine("<style>");
            _writer.AppendLine("* { box-sizing: border-box; }");
            _writer.AppendLine("body { font-family: monospace; margin: 0; }");
            _writer.AppendLine("table { border-collapse: collapse; width: 100%; }");
            _writer.AppendLine("th { font-weight: bold; background-color: #a1a1a1; }");
            _writer.AppendLine("td, th { height: 32px; }");
            _writer.AppendLine("a[href] { color: blue; }");
            _writer.AppendLine("</style>");
            _writer.AppendLine("</head>");
            _writer.AppendLine("<body>");
        }

        private void RenderInfo()
        {
            _writer.AppendLine("<table border='1'>");

            _writer.AppendLine("<tr>");
            _writer.AppendLine("<th>PageID</th>");
            _writer.AppendLine("<th>PageType</th>");
            _writer.AppendLine("<th>Slot</th>");
            //_writer.AppendLine("<th>Collection</th>");
            //_writer.AppendLine("<th>Index</th>");
            _writer.AppendLine("<th>FreeSpace</th>");
            _writer.AppendLine("<th>Items</th>");
            _writer.AppendLine("</tr>");

            var count = 0;

            foreach (var page in _pages)
            {
                _writer.AppendLine("<tr>");
                _writer.AppendLine($"<td style='text-align: center'><a target='_top' href='/{page["pageID"].AsInt32}'>{page["pageID"].AsInt32}</a></td>");
                _writer.AppendLine($"<td style='text-align: center'>{page["pageType"].AsString}</td>");
                _writer.AppendLine($"<td style='text-align: center'>{page["slot"].AsInt32}</td>");
                //_writer.AppendLine($"<td>{page["collection"].AsString}</td>");
                //_writer.AppendLine($"<td>{page["index"].AsString}</td>");
                _writer.AppendLine($"<td style='text-align: right'>{page["freeBytes"].AsInt32}</td>");
                _writer.AppendLine($"<td style='text-align: center'>{page["itemsCount"].AsInt32}</td>");
                _writer.AppendLine("</tr>");

                count++;
            }

            _writer.AppendLine("</table>");

            _writer.AppendLine($"</div>Total: {count} pages</div>");
        }

        private void RenderFooter()
        {
            _writer.AppendLine("</body>");
            _writer.AppendLine("</html>");
        }
    }
}
