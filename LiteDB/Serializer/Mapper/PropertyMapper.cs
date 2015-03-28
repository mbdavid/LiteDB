using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal delegate object GenericSetter(object target, object value);
    internal delegate object GenericGetter(object obj);

    /// <summary>
    /// Internal representation for a .NET Property mapped to BsonDocument
    /// </summary>
    internal class PropertyMapper
    {
        public bool AutoId { get; set; }
        public string PropertyName { get; set; }
        public Type PropertyType { get; set; }
        public string FieldName { get; set; }
        public GenericGetter Getter { get; set; }
        public GenericSetter Setter { get; set; }

        // if this field has a [BsonIndex] store indexoptions
        public IndexOptions IndexOptions { get; set; }
    }
}
