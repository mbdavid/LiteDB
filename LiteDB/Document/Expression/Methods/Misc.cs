using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;
using static LiteDB.ZipExtensions;

namespace LiteDB
{
    internal partial class BsonExpressionMethods
    {
        /// <summary>
        /// Parse a JSON string into a new BsonValue
        /// JSON('{a:1}') = {a:1}
        /// </summary>
        public static BsonValue JSON(BsonValue json)
        {
            if (json.IsString)
            {
                BsonValue value = null;
                var isJson = false;

                try
                {
                    value = JsonSerializer.Deserialize(json.AsString);
                    isJson = true;
                }
                catch (LiteException ex) when (ex.ErrorCode == LiteException.UNEXPECTED_TOKEN)
                {
                }

                if (isJson) return value;
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Extend source document with other document. Copy all field from extend to source. Source document will be modified.
        /// EXTEND($, {a: 2}) = {_id:1, a: 2}
        /// </summary>
        public static BsonValue EXTEND(BsonValue source, BsonValue extend)
        {
            if (source.IsDocument && extend.IsDocument)
            {
                extend.AsDocument.CopyTo(source.AsDocument);

                return source.AsDocument;
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Convert an array into IEnuemrable of values.
        /// ITEMS([1, 2, null]) = 1, 2, null
        /// </summary>
        public static IEnumerable<BsonValue> ITEMS(BsonValue array)
        {
            if (array.IsArray)
            {
                foreach (var value in array.AsArray)
                {
                    yield return value;
                }
            }
        }

        /// <summary>
        /// Return document raw id (position in datapage). Works only for root document 
        /// </summary>
        public static BsonValue RAW_ID(BsonValue document)
        {
            if (document.IsDocument)
            {
                var doc = document.AsDocument;

                return doc.RawId.IsEmpty ? null : doc.RawId.ToString();
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Get all KEYS names from a document
        /// </summary>
        public static IEnumerable<BsonValue> KEYS(BsonValue document)
        {
            if (document.IsDocument)
            { 
                foreach(var key in document.AsDocument.Keys)
                {
                    yield return key;
                }
            }
        }

        /// <summary>
        /// Conditional IF statment. If condition are true, returns TRUE value, otherwise, FALSE value
        /// </summary>
        public static BsonValue IIF(BsonValue condition, BsonValue ifTrue, BsonValue ifFalse)
        {
            if (condition.IsBoolean)
            {
                return condition.AsBoolean ? ifTrue : ifFalse;
            }

            return BsonValue.Null;
        }

        /// <summary>
        /// Return first values if not null. If null, returns second value.
        /// </summary>
        public static BsonValue COALESCE(BsonValue left, BsonValue right)
        {
            return left.IsNull ? right : left;
        }

        /// <summary>
        /// Return length of variant value (valid only for String, Binary, Array or Document [keys])
        /// </summary>
        public static BsonValue LENGTH(BsonValue value)
        {
            if (value.IsString) return value.AsString.Length;
            else if (value.IsBinary) return value.AsBinary.Length;
            else if (value.IsArray) return value.AsArray.Count;
            else if (value.IsDocument) return value.AsDocument.Keys.Count;

            return BsonValue.Null;
        }
    }
}
