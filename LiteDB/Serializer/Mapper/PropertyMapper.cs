using System;

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

        // used when a property has a custom serialization/deserialization (like DbRef)
        public Func<object, BsonMapper, BsonValue> Serialize { get; set; }

        public Func<BsonValue, BsonMapper, object> Deserialize { get; set; }

        // if this field has a [BsonIndex] store indexoptions
        public IndexOptions IndexOptions { get; set; }

        // if this property is an DbRef to another class
        public bool IsDbRef { get; set; }
    }
}