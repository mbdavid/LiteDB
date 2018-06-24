using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace LiteDB.Tests
{
    /// <summary>
    /// Data generation with fixed order. Always returns same value according "seed" instance
    /// </summary>
    public class DataGen
    {
        private Dictionary<string, Func<int, BsonValue>> _fields = new Dictionary<string, Func<int, BsonValue>>();
        private Sequencial _seq;

        private string[] _lorem = new[]
        {
            "lorem", "ipsum", "dolor", "sit", "amet", "consectetuer", "adipiscing", "elit", "sed", "diam", "nonummy", "nibh", "euismod", "tincidunt", "ut", "laoreet", "dolore", "magna", "aliquam", "erat"
        };

        private string[] _names = new[]
        {
            "james", "john", "robert", "michael", "mary", "william", "david", "richard", "charles", "joseph", "thomas", "patricia", "christopher", "linda", "barbara", "daniel", "paul", "mark", "elizabeth", "donald", "jennifer", "george", "maria", "kenneth", "susan", "steven", "edward", "margaret", "brian", "ronald", "dorothy", "anthony", "lisa", "kevin",
            "nancy", "karen", "betty", "helen", "jason", "matthew", "gary", "timothy", "sandra", "jose", "larry", "jeffrey", "frank", "donna", "carol", "ruth", "scott", "eric", "stephen", "andrew", "sharon", "michelle", "laura", "sarah", "kimberly", "deborah", "jessica", "raymond", "shirley", "cynthia", "angela", "melissa", "brenda", "amy", "jerry", "gregory",
            "anna", "joshua", "virginia", "rebecca", "kathleen", "dennis", "pamela", "martha", "debra", "amanda", "walter", "stephanie", "willie", "patrick", "terry", "carolyn", "peter", "christine", "marie", "janet", "frances", "catherine", "harold", "henry", "douglas", "joyce", "ann", "diane", "alice", "jean", "julie", "carl", "kelly", "heather", "arthur", "teresa",
            "gloria", "doris", "ryan", "joe", "roger", "evelyn", "juan", "ashley", "jack", "cheryl", "albert", "joan", "mildred", "katherine", "justin", "jonathan", "gerald", "keith", "samuel", "judith", "rose", "janice", "lawrence", "ralph", "nicole", "judy", "nicholas", "christina", "roy", "kathy", "theresa", "benjamin", "beverly", "denise", "bruce", "brandon", "adam", "tammy",
            "irene", "fred", "billy", "harry", "jane", "wayne", "louis", "lori", "steve", "tracy", "jeremy", "rachel", "andrea", "aaron", "marilyn", "robin", "randy", "leslie", "kathryn", "eugene", "bobby", "howard", "carlos", "sara", "louise", "jacqueline", "anne", "wanda", "russell", "shawn", "victor", "julia", "bonnie", "ruby", "chris", "tina", "lois", "phyllis", "jamie", "norma",
            "martin", "paula", "jesse", "diana", "annie", "shannon", "ernest", "todd", "phillip", "lee", "lillian", "peggy", "emily", "crystal", "kim", "craig", "carmen", "gladys", "connie", "rita", "alan", "dawn", "florence", "dale", "sean", "francis", "johnny", "clarence", "philip", "edna", "tiffany", "tony", "rosa", "jimmy", "earl", "cindy", "antonio", "luis", "mike", "danny", "bryan",
            "grace", "stanley", "leonard", "wendy", "nathan", "manuel", "curtis", "victoria", "rodney", "norman", "edith", "sherry", "sylvia", "josephine", "allen", "thelma", "sheila", "ethel", "marjorie", "lynn", "ellen", "elaine", "marvin", "carrie", "marion", "charlotte", "vincent", "glenn", "travis", "monica", "jeffery", "jeff", "esther", "pauline", "jacob", "emma", "chad", "kyle", "juanita"
        };

        public DataGen(int seed)
        {
            _seq = new Sequencial(seed);
        }

        public DataGen Sequencial(string field, int initial = 1)
        {
            _fields[field] = i => i + (initial - 1);
            return this;
        }

        public DataGen Random(string field, int min = 1, int max = 100)
        {
            _fields[field] = i => _seq.Next(min, max);
            return this;
        }

        public DataGen Random(string field, params BsonValue[] values)
        {
            _fields[field] = i => values[_seq.Next(0, values.Length - 1)];
            return this;
        }

        public DataGen Guid(string field)
        {
            _fields[field] = i => System.Guid.NewGuid();
            return this;
        }

        public DataGen ObjectId(string field)
        {
            _fields[field] = i => LiteDB.ObjectId.NewObjectId();
            return this;
        }

        public DataGen Bool(string field)
        {
            _fields[field] = i => _seq.Next(0, 100) > 50;
            return this;
        }

        public DataGen Lorem(string field, int words = 10)
        {
            _fields[field] = i => string.Join(" ", Enumerable.Range(0, words).Select(x => _lorem[_seq.Next(0, _lorem.Length - 1)]));
            return this;
        }

        public DataGen Name(string field)
        {
            _fields[field] = i => _names[_seq.Next(0, _names.Length - 1)];
            return this;
        }

        public DataGen FullName(string field)
        {
            _fields[field] = i => _names[_seq.Next(0, _names.Length - 1)] + " " + _names[_seq.Next(0, _names.Length - 1)];
            return this;
        }

        public DataGen Fixed(string field, BsonValue value)
        {
            _fields[field] = i => value;
            return this;
        }

        public DataGen Date(string field)
        {
            _fields[field] = i => new DateTime(_seq.Next(1990, 2020), _seq.Next(1, 12), _seq.Next(1, 28));
            return this;
        }

        public DataGen Array(string field, DataGen item, int minItems = 1, int maxItems = 10)
        {
            _fields[field] = i => new BsonArray(item.Run(_seq.Next(minItems, maxItems)));
            return this;
        }

        public IEnumerable<BsonDocument> Run(int count)
        {
            for(var i = 1; i <= count; i++)
            {
                var doc = new BsonDocument();

                foreach(var field in _fields)
                {
                    doc[field.Key] = field.Value(i);
                }

                yield return doc;
            }
        }

        /// <summary>
        /// Generate Person document { _id: [int], name: [string], age: [int], address: [string], active: [bool] }
        /// </summary>
        public static IEnumerable<BsonDocument> Person(int seed, int count)
        {
            return new DataGen(seed)
                .Sequencial("_id")
                .Name("name")
                .Random("age", 8, 55)
                .Lorem("address")
                .Bool("active")
                .Run(count);
        }

        /// <summary>
        /// Return fixed 29353 documents like this 
        /// { "_id" : "99950", "city" : "KETCHIKAN", "loc" : [ -133.18479, 55.942471 ], "pop" : 422, "state" : "AK" }
        /// </summary>
        public static IEnumerable<BsonDocument> Zip()
        {
            using (var stream = typeof(DataGen).Assembly.GetManifestResourceStream("LiteDB.Tests.Utils.Data.zip.json"))
            {
                var reader = new StreamReader(stream);

                return JsonSerializer.DeserializeArray(reader)
                    .Select(x => x.AsDocument);
            }
        }
    }
}