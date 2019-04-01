using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.Text.RegularExpressions;
using LiteDB.Engine;
using System.Threading;

namespace LiteDB.Internals
{
    [TestClass]
    public class BufferRW_Tests
    {
        [TestMethod]
        public void Buffer_Write_String()
        {
            var source = new BufferSlice(new byte[1000], 0, 1000);

            // direct string into byte[]

            using (var w = new BufferWriter(source))
            {
                w.WriteString("abc123", false);
                Assert.AreEqual(6, w.Position);
            }

            Assert.AreEqual("abc123", Encoding.UTF8.GetString(source.Array, 0, 6));

            source.Fill(0);

            // BSON string specs

            using (var w = new BufferWriter(source))
            {
                w.WriteString("abc123", true);
            }

            Assert.AreEqual(7, source.ReadInt32(0));
            Assert.AreEqual("abc123", source.ReadString(4, 6));
            Assert.AreEqual('\0', (char)source.ReadByte(10));
        }

        [TestMethod]
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

            Assert.AreEqual(int.MaxValue, source.ReadInt32(p)); p += 4;
            Assert.AreEqual(uint.MaxValue, source.ReadUInt32(p)); p += 4;
            Assert.AreEqual(long.MaxValue, source.ReadInt64(p)); p += 8;
            Assert.AreEqual(double.MaxValue, source.ReadDouble(p)); p += 8;
            Assert.AreEqual(decimal.MaxValue, source.ReadDecimal(p)); p += 16;

            Assert.AreEqual(int.MinValue, source.ReadInt32(p)); p += 4;
            Assert.AreEqual(uint.MinValue, source.ReadUInt32(p)); p += 4;
            Assert.AreEqual(long.MinValue, source.ReadInt64(p)); p += 8;
            Assert.AreEqual(double.MinValue, source.ReadDouble(p)); p += 8;
            Assert.AreEqual(decimal.MinValue, source.ReadDecimal(p)); p += 16;

            Assert.AreEqual(0, source.ReadInt32(p)); p += 4;
            Assert.AreEqual(0u, source.ReadUInt32(p)); p += 4;
            Assert.AreEqual(0L, source.ReadInt64(p)); p += 8;
            Assert.AreEqual(0d, source.ReadDouble(p)); p += 8;
            Assert.AreEqual(0m, source.ReadDecimal(p)); p += 16;

            Assert.AreEqual(1990, source.ReadInt32(p)); p += 4;
            Assert.AreEqual(1990u, source.ReadUInt32(p)); p += 4;
            Assert.AreEqual(1990L, source.ReadInt64(p)); p += 8;
            Assert.AreEqual(1990d, source.ReadDouble(p)); p += 8;
            Assert.AreEqual(1990m, source.ReadDecimal(p)); p += 16;

        }

        [TestMethod]
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

            Assert.AreEqual(true, source.ReadBool(p)); p += 1;
            Assert.AreEqual(false, source.ReadBool(p)); p += 1;
            Assert.AreEqual(DateTime.MinValue, source.ReadDateTime(p)); p += 8;
            Assert.AreEqual(DateTime.MaxValue, source.ReadDateTime(p)); p += 8;
            Assert.AreEqual(d, source.ReadDateTime(p)); p += 8;
            Assert.AreEqual(Guid.Empty, source.ReadGuid(p)); p += 16;
            Assert.AreEqual(g, source.ReadGuid(p)); p += 16;
            Assert.AreEqual(ObjectId.Empty, source.ReadObjectId(p)); p += 12;
            Assert.AreEqual(o, source.ReadObjectId(p)); p += 12;
            Assert.AreEqual(PageAddress.Empty, source.ReadPageAddress(p)); p += PageAddress.SIZE;
            Assert.AreEqual(new PageAddress(199, 0), source.ReadPageAddress(p)); p += PageAddress.SIZE;

        }
    }
}