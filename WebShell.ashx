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
    private string[] CMDS = { "ShowCollections", "CollectionInsert", "CollectionUpdate", "CollectionDelete", "CollectionDrop", "CollectionFind", "CollectionMin", "CollectionMax", "CollectionCount", "CollectionEnsureIndex" };

    public void ProcessRequest (HttpContext context)
    {
        var dbname = context.Request.Form["db"];
        var command = context.Request.Form["cmd"];

        context.Response.AppendHeader("Access-Control-Allow-Origin", "litedb.org");

        try
        {
            if (!Regex.IsMatch(dbname, @"^db_\d{13}$")) throw new ArgumentException("Invalid database name");

            var filename = context.Server.MapPath("~/App_Data/" + dbname + ".db");

            using (var db = new LiteDatabase("filename=" + filename + ";journal=false"))
            {
                var shell = new LiteShell(db);

                foreach(var cmd in shell.Commands.Keys.Except(CMDS).ToArray())
                {
                    shell.Commands.Remove(cmd);
                }

                var result = shell.Run(command);

                if(result.IsNull) return;

                if(result.IsString)
                {
                    context.Response.Write(result.AsString);
                }
                else
                {
                    var json = JsonSerializer.Serialize(result, true, false);
                    context.Response.Write(json);
                }
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
