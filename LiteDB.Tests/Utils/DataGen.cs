using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    public class DataGen
    {
        /// <summary>
        /// Return fixed 1000 Person instances
        /// </summary>
        public static IEnumerable<Person> Person()
        {
            using (var stream = typeof(DataGen).Assembly.GetManifestResourceStream("LiteDB.Tests.Utils.Json.person.json"))
            {
                var reader = new StreamReader(stream);

                var s = reader.ReadToEnd();

                return JsonSerializer.DeserializeArray(s)
                    .Select(x => x.AsDocument)
                    .Select(x => BsonMapper.Global.ToObject<Person>(x));
            }
        }

        /// <summary>
        /// Return Person instances
        /// </summary>
        public static IEnumerable<Person> Person(int start, int end)
        {
            foreach(var p in Person().Take(end - start + 1).Skip(start - 1))
            {
                p.Id = start++;

                yield return p;
            }
        }

        /// <summary>
        /// Return fixed 29353 Zips instances
        /// </summary>
        public static IEnumerable<Zip> Zip()
        {
            using (var stream = typeof(DataGen).Assembly.GetManifestResourceStream("LiteDB.Tests.Utils.Json.zip.json"))
            {
                var reader = new StreamReader(stream);

                var s = reader.ReadToEnd();

                return JsonSerializer.DeserializeArray(s)
                    .Select(x => x.AsDocument)
                    .Select(x => BsonMapper.Global.ToObject<Zip>(x));
            }
        }
    }
}