using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LiteDB.Engine
{
    /// <summary>
    /// Represent an abstraction of any custom collection based on external data
    /// </summary>
    public interface IFileCollection
    {
        /// <summary>
        /// Get fake collection name
        /// </summary>
        string Name { get; }

        /// <summary>
        /// Define an data input used on any query
        /// </summary>
        IEnumerable<BsonDocument> Input();

        /// <summary>
        /// Define an data output to be saved when run query "Into"
        /// </summary>
        int Output(IEnumerable<BsonValue> source);
    }
}
