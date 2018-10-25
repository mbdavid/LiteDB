using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using static LiteDB.Constants;

namespace LiteDB.Engine
{
    public class ArraySlice<T>
    {
        public readonly int Offset;
        public readonly int Count;
        public readonly T[] Array;

        public ArraySlice(T[] array, int offset, int count)
        {
            this.Array = array;
            this.Offset = offset;
            this.Count = count;
        }

        public T this[int index]
        {
            get => this.Array[this.Offset + index];
            set => this.Array[this.Offset + index] = value;
        }
    }
}