using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

namespace LiteDB.Shell
{
    /// <summary>
    /// Very simple class that parses command line arguments 
    /// </summary>
    internal class OptionSet
    {
        private Dictionary<string, string> _options = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public string Extra { get; private set; }

        public bool Has(string key)
        {
            return _options.ContainsKey(key);
        }

        public string Get(string key)
        {
            string str;
            if (_options.TryGetValue(key, out str)) return str;
            return null;
        }

        public OptionSet(string[] args)
        {
            var expr = new Regex(@"^(--|-|\/)(\w+)([=:]?)");

            for (var i = 0; i < args.Length; i++)
            {
                var arg = args[i];
                var m = expr.Match(arg);
                if (arg == ">" || arg == "|") break;

                if (m.Success)
                {
                    var key = m.Groups[2].Value;
                    var equals = m.Groups[3].Value;
                    var value = "";

                    if (equals.Length > 0)
                    {
                         value = arg.Substring(m.Value.Length);
                    }
                    else
                    {
                        if(i < args.Length - 1)
                        {
                            var next = args[i + 1];
                            if(!(expr.IsMatch(next) || next == ">" || next == "|"))
                            {
                                value = next;
                                i++;
                            }
                        }
                    }

                    _options.Add(key, string.IsNullOrWhiteSpace(value) ? "true" : value);
                }
                else
                {
                    this.Extra = arg;
                }
            }
        }
    }
}