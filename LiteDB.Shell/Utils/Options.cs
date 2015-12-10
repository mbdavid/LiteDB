using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    public class OptionSet
    {
        public string Filename;
        public string Upgrade;
        public string Run;

        public OptionSet(string[] args)
        {
            var options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            var index = 0;

            foreach (var item in args)
            {
                var m = Regex.Match(item, @"^(--|-|\/)(\w+)[=:]?(.*)$");

                if (m.Success)
                {
                    var key = m.Groups[2].Value;
                    var value = m.Groups[3].Value;

                    options.Add(key, string.IsNullOrWhiteSpace(value) ? "true" : value);
                }
                else if (index == 0)
                {
                    this.Filename = item;
                }

                index++;
            }

            options.TryGetValue("upgrade", out this.Upgrade);
            options.TryGetValue("run", out this.Run);
        }
    }
}