using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Class that converts POCO class to/from BsonDocument
    /// If you prefer use a new instance of BsonMapper (not Global), be sure cache this instance for better performance 
    /// Serialization rules:
    ///     - Classes must be "public"
    ///     - Properties must have public getter and setter
    ///     - Entity class must have Id property, [ClassName]Id property or [BsonId] attribute
    ///     - No circular references
    ///     - Fields are not valid
    ///     - IList, Array supports
    ///     - IDictionary supports (Key must be a simple datatype - converted by ChangeType)
    /// </summary>
    public partial class BsonMapper
    {
        private const int MAX_DEPTH = 20;

        /// <summary>
        /// Mapping cache between Class/BsonDocument
        /// </summary>
        private Dictionary<Type, Dictionary<string, PropertyMapper>> _mapper = new Dictionary<Type,Dictionary<string,PropertyMapper>>();

        /// <summary>
        /// A resolver name property
        /// </summary>
        public Func<string, string> ResolvePropertyName;

        /// <summary>
        /// Indicate that mapper do not serialize null values
        /// </summary>
        public bool SerializeNullValues { get; set; }

        public BsonMapper()
        {
            this.SerializeNullValues = false;
            this.ResolvePropertyName = (s) => s;
        }

        /// <summary>
        /// Global BsonMapper instance
        /// </summary>
        public static BsonMapper Global = new BsonMapper();

        #region Predefinded Property Resolvers

        public void UseCamelCase()
        {
            this.ResolvePropertyName = (s) => char.ToLower(s[0]) + s.Substring(1);
        }

        private Regex _lowerCaseDelimiter = new Regex("(?!(^[A-Z]))([A-Z])");

        public void UseLowerCaseDelimiter(char delimiter = '_')
        {
            this.ResolvePropertyName = (s) => _lowerCaseDelimiter.Replace(s, delimiter + "$2").ToLower();
        }

        #endregion

        /// <summary>
        /// Get property mapper between typed .NET class and BsonDocument - Cache results
        /// </summary>
        internal Dictionary<string, PropertyMapper> GetPropertyMapper(Type type)
        {
            Dictionary<string, PropertyMapper> props;

            if (_mapper.TryGetValue(type, out props))
            {
                return props;
            }

            _mapper[type] = Reflection.GetProperties(type, this.ResolvePropertyName);

            return _mapper[type];
        }
    }
}
