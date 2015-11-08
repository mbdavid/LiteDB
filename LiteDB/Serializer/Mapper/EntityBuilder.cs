using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace LiteDB
{
    /// <summary>
    /// Helper class to modify your entity mapping to document. Can be used instead attribute decorates
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class EntityBuilder<T>
    {
        private Dictionary<string, PropertyMapper> _mapper;

        internal EntityBuilder(Dictionary<string, PropertyMapper> mapper)
        {
            _mapper = mapper;
        }

        /// <summary>
        /// Define which property will not be mapped to document
        /// </summary>
        public EntityBuilder<T> Ignore<K>(Expression<Func<T, K>> property)
        {
            _mapper.Remove(this.GetProperty(property));

            return this;
        }

        /// <summary>
        /// Define a custom name for a property when mapping to document
        /// </summary>
        public EntityBuilder<T> Map<K>(Expression<Func<T, K>> property, string field)
        {
            PropertyMapper prop;

            if (_mapper.TryGetValue(this.GetProperty(property), out prop))
            {
                prop.FieldName = field;
            }

            return this;
        }

        /// <summary>
        /// Define which property is your document id (primary key). Define if this property supports auto-id
        /// </summary>
        public EntityBuilder<T> Key<K>(Expression<Func<T, K>> property, bool autoId = true)
        {
            PropertyMapper prop;

            if(_mapper.TryGetValue(this.GetProperty(property), out prop))
            {
                prop.FieldName = "_id";
                prop.AutoId = autoId;
            }

            return this;
        }

        /// <summary>
        /// Define an index based in a field on entity
        /// </summary>
        public EntityBuilder<T> Index<K>(Expression<Func<T, K>> property, bool unique = false)
        {
            PropertyMapper prop;

            if (_mapper.TryGetValue(this.GetProperty(property), out prop))
            {
                prop.IndexOptions = new IndexOptions { Unique = unique };
            }

            return this;
        }

        /// <summary>
        /// Define an index based in a field on entity
        /// </summary>
        public EntityBuilder<T> Index<K>(Expression<Func<T, K>> property, IndexOptions options)
        {
            PropertyMapper prop;

            if (_mapper.TryGetValue(this.GetProperty(property), out prop))
            {
                prop.IndexOptions = options;
            }

            return this;
        }

        /// <summary>
        /// Get a property based on a expression. Eg.: 'x => x.UserId' return string "UserId"
        /// </summary>
        private string GetProperty<TK, K>(Expression<Func<TK, K>> expr)
        {
            var member = expr.Body as MemberExpression;

            if (member == null)
            {
                throw new ArgumentException(string.Format("Expression '{0}' refers to a method, not a property.", expr.ToString()));
            }

            return ((PropertyInfo)member.Member).Name;
        }
    }
}
