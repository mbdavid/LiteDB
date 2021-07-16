using System;
using Xunit;

namespace LiteDB.Tests.Mapper
{
    public class Reflection_Tests
    {
        [Theory]
        [InlineData(typeof(string))]
        [InlineData(typeof(Boolean))]
        [InlineData(typeof(Boolean?))]
        [InlineData(typeof(Byte))]
        [InlineData(typeof(Byte?))]
        [InlineData(typeof(SByte))]
        [InlineData(typeof(SByte?))]
        [InlineData(typeof(Int16))]
        [InlineData(typeof(Int16?))]
        [InlineData(typeof(Int32))]
        [InlineData(typeof(Int32?))]
        [InlineData(typeof(Int64))]
        [InlineData(typeof(Int64?))]
        [InlineData(typeof(UInt16))]
        [InlineData(typeof(UInt16?))]
        [InlineData(typeof(UInt32))]
        [InlineData(typeof(UInt32?))]
        [InlineData(typeof(UInt64))]
        [InlineData(typeof(UInt64?))]
        [InlineData(typeof(Double))]
        [InlineData(typeof(Double?))]
        [InlineData(typeof(Single))]
        [InlineData(typeof(Single?))]
        [InlineData(typeof(Decimal))]
        [InlineData(typeof(Decimal?))]
        [InlineData(typeof(DateTime))]
        [InlineData(typeof(DateTime?))]
        [InlineData(typeof(DateTimeOffset))]
        [InlineData(typeof(DateTimeOffset?))]
        [InlineData(typeof(TimeSpan))]
        [InlineData(typeof(TimeSpan?))]
        [InlineData(typeof(Guid))]
        [InlineData(typeof(Guid?))]
        [InlineData(typeof(Uri))]
        [InlineData(typeof(ObjectId))]
        public void SimpleType(Type type)
        {
            var isSimpleType = Reflection.IsSimpleType(type);
            Assert.True(isSimpleType);
        }
    }
}