using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;

namespace LiteDB.Stress
{
    public class TestFile
    {
        public TimeSpan Timeout { get; }
        public string Filename { get; }
        public bool Delete { get; }
        public List<ITestItem> Tasks { get; }

        public TestFile(string filename)
        {
            var doc = new XmlDocument();
            doc.Load(filename);

            var root = doc.DocumentElement;
            var children = doc.SelectNodes("*");

            this.Timeout = TimeSpanEx.Parse(root["timeout"].Value);
            this.Filename = root["filename"].Value;
            this.Delete = bool.Parse(root["delete"].Value);

            this.Tasks = new List<ITestItem>();

            foreach(XmlElement el in children)
            {
                var item = el.Name == "insert" ?
                    (ITestItem)new InsertTaskItem(el) :
                    (ITestItem)new SqlTaskItem(el);

                this.Tasks.Add(item);
            }
        }
    }
}
