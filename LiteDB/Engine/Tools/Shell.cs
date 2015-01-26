using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB
{
    public partial class LiteEngine
    {
        /// <summary>
        /// Run a shell command and write output on StringBuild sb paramter
        /// </summary>
        public void Run(string command, StringBuilder sb)
        {
            new LiteDB.Shell.LiteShell(this, sb, true).Run(command);
        }

        /// <summary>
        /// Run a shell command and return output as string
        /// </summary>
        public string Run(string command)
        {
            var sb = new StringBuilder();
            this.Run(command, sb);
            return sb.ToString();
        }
    }
}
