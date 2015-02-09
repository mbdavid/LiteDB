<%@ WebHandler Language="C#" Class="WebShell" %>
using System;
using System.Web;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using LiteDB;
using LiteDB.Shell;
using LiteDB.Shell.Commands;

public class WebShell : IHttpHandler
{
    public void ProcessRequest (HttpContext context)
    {
        var db = context.Request.Form["db"];
        var command = context.Request.Form["cmd"];
        
        try
        {
            if (!Regex.IsMatch(db, @"^db_\d{13}$")) throw new ArgumentException("Invalid database name");

            var filename = context.Server.MapPath("~/App_Data/" + db + ".db");

            using (var shell = new LiteShell())
            {
                // accept only a subset commands
                shell.WebMode = true;
                
                shell.Display.Pretty = true;
                shell.Database = new LiteDB.LiteDatabase(filename);
                shell.Display.TextWriters.Add(context.Response.Output);

                shell.Run(command);
            }
        }
        catch(Exception ex)
        {
            context.Response.Clear();
            context.Response.Write("ERROR: " + ex.Message);
        }
    }
 
    public bool IsReusable { get { return false; } }

}