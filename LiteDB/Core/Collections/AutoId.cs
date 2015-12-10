using System;
using System.Linq;

namespace LiteDB
{
    public partial class LiteCollection<T>
    {
        /// <summary>
        /// Set new Id in entity class if entity needs one
        /// </summary>
        public void SetAutoId(T document)
        {
            // if object is BsonDocument, add _id as ObjectId
            if (document is BsonDocument)
            {
                var doc = document as BsonDocument;
                if (!doc.RawValue.ContainsKey("_id"))
                {
                    doc["_id"] = ObjectId.NewObjectId();
                }
                return;
            }

            // get fields mapper
            var mapper = _mapper.GetPropertyMapper(document.GetType());

            // it's not best way because is scan all properties - but Id propably is first field :)
            var id = mapper.Select(x => x.Value).FirstOrDefault(x => x.FieldName == "_id");

            // if not id or no autoId = true
            if (id == null || id.AutoId == false) return;

            // get id value
            var value = id.Getter(document);

            // test for ObjectId, Guid and Int32 types
            if (id.PropertyType == typeof(ObjectId) && (value == null || ObjectId.Empty.Equals((ObjectId)value)))
            {
                id.Setter(document, ObjectId.NewObjectId());
            }
            else if (id.PropertyType == typeof(Guid) && Guid.Empty.Equals((Guid)value))
            {
                id.Setter(document, Guid.NewGuid());
            }
            else if (id.PropertyType == typeof(Int32) && ((Int32)value) == 0)
            {
                var max = this.Max();
                id.Setter(document, max.IsMaxValue ? 1 : max + 1);
            }
        }
    }
}