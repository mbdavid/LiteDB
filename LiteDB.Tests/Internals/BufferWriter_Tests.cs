using System;
using System.Linq;
using System.Text;
using FluentAssertions;
using LiteDB.Engine;
using Xunit;

namespace LiteDB.Internals
{
    public class BufferWriter_Tests
    {
        [Fact]
        public void Buffer_Write_String()
        {
            var source = new BufferSlice(new byte[1000], 0, 1000);

            // direct string into byte[]

            using (var w = new BufferWriter(source))
            {
                w.WriteString("abc123", false);
                w.Position.Should().Be(6);
            }

            Encoding.UTF8.GetString(source.Array, 0, 6).Should().Be("abc123");

            source.Fill(0);

            // BSON string specs

            using (var w = new BufferWriter(source))
            {
                w.WriteString("abc123", true);
            }

            source.ReadInt32(0).Should().Be(7);
            source.ReadString(4, 6).Should().Be("abc123");
            ((char) source.ReadByte(10)).Should().Be('\0');
        }

        [Fact]
        public void Buffer_Write_Numbers()
        {
            var source = new BufferSlice(new byte[1000], 0, 1000);

            // numbers
            using (var w = new BufferWriter(source))
            {
                // max values
                w.Write(int.MaxValue);
                w.Write(uint.MaxValue);
                w.Write(long.MaxValue);
                w.Write(double.MaxValue);
                w.Write(decimal.MaxValue);

                // min values
                w.Write(int.MinValue);
                w.Write(uint.MinValue);
                w.Write(long.MinValue);
                w.Write(double.MinValue);
                w.Write(decimal.MinValue);

                // zero values
                w.Write(0); // int
                w.Write(0u); // uint
                w.Write(0L); // long
                w.Write(0d); // double
                w.Write(0m); // decimal

                // fixed values
                w.Write(1990); // int
                w.Write(1990u); // uint
                w.Write(1990L); // long
                w.Write(1990d); // double
                w.Write(1990m); // decimal
            }

            var p = 0;

            source.ReadInt32(p).Should().Be(int.MaxValue);
            p += 4;
            source.ReadUInt32(p).Should().Be(uint.MaxValue);
            p += 4;
            source.ReadInt64(p).Should().Be(long.MaxValue);
            p += 8;
            source.ReadDouble(p).Should().Be(double.MaxValue);
            p += 8;
            source.ReadDecimal(p).Should().Be(decimal.MaxValue);
            p += 16;

            source.ReadInt32(p).Should().Be(int.MinValue);
            p += 4;
            source.ReadUInt32(p).Should().Be(uint.MinValue);
            p += 4;
            source.ReadInt64(p).Should().Be(long.MinValue);
            p += 8;
            source.ReadDouble(p).Should().Be(double.MinValue);
            p += 8;
            source.ReadDecimal(p).Should().Be(decimal.MinValue);
            p += 16;

            source.ReadInt32(p).Should().Be(0);
            p += 4;
            source.ReadUInt32(p).Should().Be(0u);
            p += 4;
            source.ReadInt64(p).Should().Be(0L);
            p += 8;
            source.ReadDouble(p).Should().Be(0d);
            p += 8;
            source.ReadDecimal(p).Should().Be(0m);
            p += 16;

            source.ReadInt32(p).Should().Be(1990);
            p += 4;
            source.ReadUInt32(p).Should().Be(1990u);
            p += 4;
            source.ReadInt64(p).Should().Be(1990L);
            p += 8;
            source.ReadDouble(p).Should().Be(1990d);
            p += 8;
            source.ReadDecimal(p).Should().Be(1990m);
            p += 16;
        }

        [Fact]
        public void Buffer_Write_Types()
        {
            var source = new BufferSlice(new byte[1000], 0, 1000);

            var g = Guid.NewGuid();
            var d = DateTime.Now;
            var o = ObjectId.NewObjectId();

            using (var w = new BufferWriter(source))
            {
                w.Write(true);
                w.Write(false);
                w.Write(DateTime.MinValue);
                w.Write(DateTime.MaxValue);
                w.Write(d);
                w.Write(Guid.Empty);
                w.Write(g);
                w.Write(ObjectId.Empty);
                w.Write(o);
                w.Write(PageAddress.Empty);
                w.Write(new PageAddress(199, 0));
            }

            var p = 0;

            source.ReadBool(p).Should().BeTrue();
            p += 1;
            source.ReadBool(p).Should().BeFalse();
            p += 1;
            source.ReadDateTime(p).Should().Be(DateTime.MinValue);
            p += 8;
            source.ReadDateTime(p).Should().Be(DateTime.MaxValue);
            p += 8;
            source.ReadDateTime(p).ToLocalTime().Should().Be(d);
            p += 8;
            source.ReadGuid(p).Should().Be(Guid.Empty);
            p += 16;
            source.ReadGuid(p).Should().Be(g);
            p += 16;
            source.ReadObjectId(p).Should().Be(ObjectId.Empty);
            p += 12;
            source.ReadObjectId(p).Should().Be(o);
            p += 12;
            source.ReadPageAddress(p).Should().Be(PageAddress.Empty);
            p += PageAddress.SIZE;
            source.ReadPageAddress(p).Should().Be(new PageAddress(199, 0));
            p += PageAddress.SIZE;
        }

        [Fact]
        public void Buffer_Write_Overflow()
        {
            var data = new byte[50];
            var source = new BufferSlice[]
            {
                new BufferSlice(data, 0, 10),
                new BufferSlice(data, 10, 10),
                new BufferSlice(data, 20, 10),
                new BufferSlice(data, 30, 10),
                new BufferSlice(data, 40, 10)
            };

            using (var w = new BufferWriter(source))
            {
                w.Write(new byte[50].Fill(99, 0, 50), 0, 50);
            }

            data.All(x => x == 99).Should().BeTrue();
        }

        [Fact]
        public void Buffer_Bson()
        {
            var source = new BufferSlice(new byte[1000], 0, 1000);

            var doc = new BsonDocument
            {
                ["minValue"] = BsonValue.MinValue,
                ["null"] = BsonValue.Null,
                ["int"] = int.MaxValue,
                ["long"] = long.MaxValue,
                ["double"] = double.MaxValue,
                ["decimal"] = decimal.MaxValue,
                ["string"] = "String",
                ["document"] = new BsonDocument {["_id"] = 1},
                ["array"] = new BsonArray {1, 2, 3},
                ["binary"] = new byte[50].Fill(255, 0, 49),
                ["objectId"] = ObjectId.NewObjectId(),
                ["guid"] = Guid.NewGuid(),
                ["boolean"] = true,
                ["date"] = DateTime.UtcNow,
                ["maxValue"] = BsonValue.MaxValue
            };

            using (var w = new BufferWriter(source))
            {
                w.WriteDocument(doc, true);

                w.Position.Should().Be(307);
            }

            using (var r = new BufferReader(source, true))
            {
                var reader = r.ReadDocument();

                r.Position.Should().Be(307);

                JsonSerializer.Serialize(reader).Should().Be(JsonSerializer.Serialize(doc));
            }
        }
    }
}