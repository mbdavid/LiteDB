namespace LiteDB.Stress;

using System;
using System.Collections.Generic;
using System.IO;
using System.Xml;

public class TestFile
{
    public TimeSpan Timeout { get; }
    public string Filename { get; }
    public string Output { get; }
    public bool Delete { get; }
    public List<string> Setup { get; }
    public List<ITestItem> Tasks { get; }

    public TestFile(string filename)
    {
        var doc = new XmlDocument();
        doc.Load(filename);

        var root = doc.DocumentElement;
        var children = root.SelectNodes("*");

        Timeout = TimeSpanEx.Parse(root.GetAttribute("timeout"));
        Filename = root.GetAttribute("filename");
        Delete = bool.Parse(root.GetAttribute("delete"));
        Output = Path.Combine(Path.GetDirectoryName(Filename), Path.GetFileNameWithoutExtension(Filename) + ".log");

        Setup = new List<string>();
        Tasks = new List<ITestItem>();

        foreach (XmlElement el in children)
        {
            if (el.Name == "setup")
            {
                Setup.Add(el.InnerText);
            }
            else
            {
                var item = el.Name == "insert" ? new InsertTaskItem(el) : (ITestItem) new SqlTaskItem(el);

                Tasks.Add(item);
            }
        }
    }
}