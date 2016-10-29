using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace LiteDB
{
    /// <summary>
    /// Class to map entity class to BsonDocument
    /// </summary>
    public class EntityMapper
    {
        /// <summary>
        /// List all type properties that will be mapped to/from BsonDocument
        /// </summary>
        public List<PropertyMapper> Props { get; set; }

        /// <summary>
        /// Indicate which property is _id
        /// </summary>
        public PropertyMapper Id { get { return Props.SingleOrDefault(x => x.FieldName == "_id"); } }

        /// <summary>
        /// Indicate which Type this entity mapper is
        /// </summary>
        public Type ForType { get; set; }
    }
}