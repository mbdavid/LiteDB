using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace fastBinaryJSON
{
    internal sealed class BJsonParser
    {
        readonly byte[] json;
        int index;
        bool _useUTC = true;

        internal BJsonParser(byte[] json, bool useUTC)
        {
            this.json = json;
            _useUTC = useUTC;
        }

        public object Decode()
        {
            bool b = false;
            return ParseValue(out b);
        }

        private Dictionary<string, object> ParseObject()
        {
            Dictionary<string, object> dic = new Dictionary<string, object>();
            bool breakparse = false;
            while (!breakparse)
            {
                byte t = GetToken();
                if (t == TOKENS.COMMA)
                    continue;
                if (t == TOKENS.DOC_END)
                    break;
                string key = "";
                if (t != TOKENS.NAME)
                    throw new Exception("excpecting a name field");
                key = ParseName();
                t = GetToken();
                if (t != TOKENS.COLON)
                    throw new Exception("expecting a colon");
                object val = ParseValue(out breakparse);

                if (breakparse == false)
                {
                    dic.Add(key, val);
                }
            }
            return dic;
        }

        private string ParseName()
        {
            byte c = json[index++];
            string s = Reflection.Instance.utf8.GetString(json, index, c);
            index += c;
            return s;
        }

        private List<object> ParseArray()
        {
            List<object> array = new List<object>();

            bool breakparse = false;
            while (!breakparse)
            {
                object o = ParseValue(out breakparse);
                byte t = 0;
                if (breakparse == false)
                {
                    array.Add(o);
                    t = GetToken();
                }
                else t = (byte)o;

                if (t == TOKENS.COMMA)
                    continue;
                if (t == TOKENS.ARRAY_END)
                    break;
            }
            return array;
        }

        private object ParseValue(out bool breakparse)
        {
            byte t = GetToken();
            breakparse = false;
            switch (t)
            {
                case TOKENS.BYTE:
                    return ParseByte();
                case TOKENS.BYTEARRAY:
                    return ParseByteArray();
                case TOKENS.CHAR:
                    return ParseChar();
                case TOKENS.DATETIME:
                    return ParseDateTime();
                case TOKENS.DECIMAL:
                    return ParseDecimal();
                case TOKENS.DOUBLE:
                    return ParseDouble();
                case TOKENS.FLOAT:
                    return ParseFloat();
                case TOKENS.GUID:
                    return ParseGuid();
                case TOKENS.INT:
                    return ParseInt();
                case TOKENS.LONG:
                    return ParseLong();
                case TOKENS.SHORT:
                    return ParseShort();
                //case TOKENS.SINGLE:
                //    return ParseSingle();
                case TOKENS.UINT:
                    return ParseUint();
                case TOKENS.ULONG:
                    return ParseULong();
                case TOKENS.USHORT:
                    return ParseUShort();
                case TOKENS.UNICODE_STRING:
                    return ParseUnicodeString();
                case TOKENS.STRING:
                    return ParseString();
                case TOKENS.DOC_START:
                    return ParseObject();
                case TOKENS.ARRAY_START:
                    return ParseArray();
                case TOKENS.TRUE:
                    return true;
                case TOKENS.FALSE:
                    return false;
                case TOKENS.NULL:
                    return null;
                case TOKENS.ARRAY_END:
                    breakparse = true;
                    return TOKENS.ARRAY_END;
                case TOKENS.DOC_END:
                    breakparse = true;
                    return TOKENS.DOC_END;
                case TOKENS.COMMA:
                    breakparse = true;
                    return TOKENS.COMMA;
            }

            throw new Exception("Unrecognized token at index = " + index);
        }

        private object ParseChar()
        {
            throw new NotImplementedException();
        }

        private Guid ParseGuid()
        {
            byte[] b = new byte[16];
            Buffer.BlockCopy(json, index, b, 0, 16);
            index += 16;
            return new Guid(b);
        }

        private float ParseFloat()
        {
            float f = BitConverter.ToSingle(json, index);
            index += 4;
            return f;
        }

        private ushort ParseUShort()
        {
            ushort u = (ushort)Helper.ToInt16(json, index);
            index += 2;
            return u;
        }

        private ulong ParseULong()
        {
            ulong u = (ulong)Helper.ToInt64(json, index);
            index += 8;
            return u;
        }

        private uint ParseUint()
        {
            uint u = (uint)Helper.ToInt32(json, index);
            index += 4;
            return u;
        }

        private short ParseShort()
        {
            short u = (short)Helper.ToInt16(json, index);
            index += 2;
            return u;
        }

        private long ParseLong()
        {
            long u = (long)Helper.ToInt64(json, index);
            index += 8;
            return u;
        }

        private int ParseInt()
        {
            int u = (int)Helper.ToInt32(json, index);
            index += 4;
            return u;
        }

        private double ParseDouble()
        {
            double d = BitConverter.ToDouble(json, index);
            index += 8;
            return d;
        }

        private object ParseUnicodeString()
        {
            int c = Helper.ToInt32(json, index);
            index += 4;

            string s = Reflection.Instance.unicode.GetString(json, index, c);
            index += c;
            return s;
        }

        private string ParseString()
        {
            int c = Helper.ToInt32(json, index);
            index += 4;

            string s = Reflection.Instance.utf8.GetString(json, index, c);
            index += c;
            return s;
        }

        private decimal ParseDecimal()
        {
            int[] i = new int[4];
            i[0] = Helper.ToInt32(json, index);
            index += 4;
            i[1] = Helper.ToInt32(json, index);
            index += 4;
            i[2] = Helper.ToInt32(json, index);
            index += 4;
            i[3] = Helper.ToInt32(json, index);
            index += 4;

            return new decimal(i);
        }

        private DateTime ParseDateTime()
        {
            long l = Helper.ToInt64(json, index);
            index += 8;

            DateTime dt = new DateTime(l);
            if (_useUTC)
                dt = dt.ToLocalTime(); // to local time

            return dt;
        }

        private byte[] ParseByteArray()
        {
            int c = Helper.ToInt32(json, index);
            index += 4;
            byte[] b = new byte[c];
            Buffer.BlockCopy(json, index, b, 0, c);
            index += c;
            return b;
        }

        private byte ParseByte()
        {
            return json[index++];
        }

        private byte GetToken()
        {
            byte b = json[index++];
            return b;
        }
    }
}
