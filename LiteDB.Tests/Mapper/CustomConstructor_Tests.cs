using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Xunit;

namespace LiteDB.Tests.Mapper
{
    public class CustomConstructor_Tests
    {
        class CustomCollection<T> : IEnumerable<T>
        {
            private List<T> _collection;

            public CustomCollection(IEnumerable collection)
            {
                _collection = new List<T>(collection.Cast<T>());
            }

            public IEnumerator<T> GetEnumerator()
            {
                return _collection.GetEnumerator();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return _collection.GetEnumerator();
            }
        }

        class CustomDictionary<TKey, TValue> : IDictionary
        {
            private Dictionary<TKey, TValue> _dictionary;

            public CustomDictionary(IDictionary dictionary)
            {
                _dictionary = dictionary.Keys.Cast<TKey>().ToDictionary(key => key, key => (TValue)dictionary[key]);
            }

            public void Add(object key, object? value) => throw new NotImplementedException();

            public void Clear() => throw new NotImplementedException();

            public bool Contains(object key) => throw new NotImplementedException();

            public IDictionaryEnumerator GetEnumerator() => _dictionary.GetEnumerator();

            public void Remove(object key) => throw new NotImplementedException();

            public bool IsFixedSize => false;
            public bool IsReadOnly => false;

            public object? this[object key]
            {
                get => key is TKey typedKey ? (object?)_dictionary[typedKey] : null;
                set => throw new NotImplementedException();
            }

            public ICollection Keys => _dictionary.Keys;
            public ICollection Values => _dictionary.Values;

            IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

            public void CopyTo(Array array, int index) => throw new NotImplementedException();

            public int Count => _dictionary.Count;
            public bool IsSynchronized => false;
            public object SyncRoot => throw new NotImplementedException();
        }

        [Fact]
        public void Custom_Collection()
        {
            var mapper = new BsonMapper();

            var collection = new CustomCollection<int>(new [] { 1, 23, 65436, 24334 });

            Action action = () =>
            {
                var bson = mapper.Serialize(collection);
                mapper.Deserialize<CustomCollection<int>>(bson);
            };
            action.Should().Throw<LiteException>("because CustomCollection<> is an unsupported collection type");


            mapper.RegisterCollectionType<CustomCollection<object>>((t, objs) =>
            {
                var c = Activator.CreateInstance(typeof(CustomCollection<>).MakeGenericType(t), objs);
                return (IEnumerable)c;
            });

            {
                var bson = mapper.Serialize(collection);
                var deserializedList = mapper.Deserialize<CustomCollection<int>>(bson);
                deserializedList.Should().BeEquivalentTo(collection);
            }
        }

        [Fact]
        public void Custom_Dictionary()
        {
            var mapper = new BsonMapper();

            var dictionary = new CustomDictionary<string, int>(new Dictionary<string, int>
            {
                { "foo", 1 },
                { "bar", 23 },
                { "baz", 65436 },
                { "qux", 24334 }
            });

            Action action = () =>
            {
                var bson = mapper.Serialize(dictionary);
                mapper.Deserialize<CustomDictionary<string, int>>(bson);
            };
            action.Should().Throw<LiteException>("because CustomDictionary<,> is an unsupported collection type");


            mapper.RegisterDictionaryType<CustomDictionary<string, int>>((tkey, tvalue, d) =>
            {
                var c = Activator.CreateInstance(typeof(CustomDictionary<,>).MakeGenericType(tkey, tvalue), d);
                return (IDictionary)c;
            });

            {
                var bson = mapper.Serialize(dictionary); 
                var deserializedDict = mapper.Deserialize<CustomDictionary<string, int>>(bson);
                deserializedDict.Should().BeEquivalentTo(dictionary);
            }
        }
    }
}