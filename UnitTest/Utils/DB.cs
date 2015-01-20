using System;
using System.Collections.Generic;
using LiteDB;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTest
{
    public class DB
    {
        public static string Path(bool delete = true, string name = "test.db", string connStr = "")
        {
            var path = System.IO.Path.GetFullPath(
                System.IO.Directory.GetCurrentDirectory() + 
                "../../../../TestResults/" + name);

            if(System.IO.File.Exists(path) && delete)
                System.IO.File.Delete(path);

            var connectionString = connStr.Length > 0 ?"filename=" + path + ";" + connStr : path;

            return connectionString;
        }
    }
}
