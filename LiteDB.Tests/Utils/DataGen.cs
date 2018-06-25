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
        /// Return fixed 1000 persons:
        /// { "name": "Kelsey Garza", "age": 66, "phone": "624-744-6218", "email": "Kelly@suscipit.edu", "address": "62702 West Bosnia and Herzegovina Way", "city": "Wheaton", "state": "MO", "date": { "$date": "1950-08-07"}, "active": true }
        /// </summary>
        public static IEnumerable<BsonDocument> Person()
        {
            using (var stream = typeof(DataGen).Assembly.GetManifestResourceStream("LiteDB.Tests.Utils.Data.person.json"))
            {
                var reader = new StreamReader(stream);

                var s = reader.ReadToEnd();

                return JsonSerializer.DeserializeArray(s)
                    .Select(x => x.AsDocument);
            }
        }

        /// <summary>
        /// Return fixed up to 1000 persons:
        /// { "_id": 1, "name": "Kelsey Garza", "age": 66, "phone": "624-744-6218", "email": "Kelly@suscipit.edu", "address": "62702 West Bosnia and Herzegovina Way", "city": "Wheaton", "state": "MO", "date": { "$date": "1950-08-07"}, "active": true }
        /// </summary>
        public static IEnumerable<BsonDocument> Person(int start, int end)
        {
            foreach(var doc in Person().Take(end - start + 1).Skip(start - 1))
            {
                doc["_id"] = start++;

                yield return doc;
            }
        }

        /// <summary>
        /// Return fixed 29353 zips: 
        /// { "_id" : "99950", "city" : "KETCHIKAN", "loc" : [ -133.18479, 55.942471 ], "pop" : 422, "state" : "AK" }
        /// </summary>
        public static IEnumerable<BsonDocument> Zip()
        {
            using (var stream = typeof(DataGen).Assembly.GetManifestResourceStream("LiteDB.Tests.Utils.Data.zip.json"))
            {
                var reader = new StreamReader(stream);

                var s = reader.ReadToEnd();

                return JsonSerializer.DeserializeArray(s)
                    .Select(x => x.AsDocument);
            }
        }
    }
}