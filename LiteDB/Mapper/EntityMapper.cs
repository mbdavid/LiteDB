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
    internal class EntityMapper
    {
        /// <summary>
        /// List all type properties that will be mapped to/from BsonDocument
        /// </summary>
        public List<PropertyMapper> Props { get; set; }

        /// <summary>
        /// Indicate which property is _id
        /// </summary>
        public PropertyMapper Id { get; set; }

        /// <summary>
        /// Indicate which Type this entity mapper is
        /// </summary>
        public Type ForType { get; set; }

        public EntityMapper(Type type, Func<string, string> resolvePropertyName)
        {
            this.Props = new List<PropertyMapper>();
            this.ForType = type;

            var id = Reflection.GetIdProperty(type);
            var ignore = typeof(BsonIgnoreAttribute);
            var idAttr = typeof(BsonIdAttribute);
            var fieldAttr = typeof(BsonFieldAttribute);
            var indexAttr = typeof(BsonIndexAttribute);
#if NETFULL
            var props = type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.NonPublic);
#else
            var props = type.GetRuntimeProperties();
#endif
            foreach (var prop in props)
            {
                // ignore indexer property
                if (prop.GetIndexParameters().Length > 0) continue;

                // ignore write only
                if (!prop.CanRead) continue;

                // [BsonIgnore]
                if (prop.IsDefined(ignore, false)) continue;

                // check if property has [BsonField]
                var bsonField = prop.IsDefined(fieldAttr, false);

                // create getter/setter function
                var getter = Reflection.CreateGenericGetter(type, prop, bsonField);
                var setter = Reflection.CreateGenericSetter(type, prop, bsonField);

                // if not getter or setter - no mapping
                if (getter == null) continue;

                // if the property is already in the dictionary, it's probably an override - keep the first instance added
                if (this.Props.Any(x => x.PropertyName == prop.Name)) continue;

                // checks field name conversion
                var name = id != null && id.Equals(prop) ? "_id" : resolvePropertyName(prop.Name);

                // check if property has [BsonField] with a custom field name
                if (bsonField)
                {
                    var field = (BsonFieldAttribute)prop.GetCustomAttributes(fieldAttr, false).FirstOrDefault();
                    if (field != null && field.Name != null) name = field.Name;
                }

                // check if property has [BsonId] to get with was setted AutoId = true
                var autoId = (BsonIdAttribute)prop.GetCustomAttributes(idAttr, false).FirstOrDefault();

                // checks if this proerty has [BsonIndex]
                var index = (BsonIndexAttribute)prop.GetCustomAttributes(indexAttr, false).FirstOrDefault();

                // test if field name is OK (avoid to check in all instances) - do not test internal classes, like DbRef
                if (BsonDocument.IsValidFieldName(name) == false) throw LiteException.InvalidFormat(prop.Name, name);

                // create a property mapper
                var p = new PropertyMapper
                {
                    AutoId = autoId == null ? true : autoId.AutoId,
                    FieldName = name,
                    PropertyName = prop.Name,
                    PropertyType = prop.PropertyType,
                    IndexInfo = index == null ? null : (bool?)index.Unique,
                    Getter = getter,
                    Setter = setter
                };

                // add to props list
                this.Props.Add(p);

                // if prop is _id, update Id reference
                if (name == "_id")
                {
                    p.IndexInfo = true;
                    this.Id = p;
                }
            }
        }
    }
}