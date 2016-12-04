using System;

namespace LiteDB
{
    internal class AutoId
    {
        /// <summary>
        /// Function to test if type is empty
        /// </summary>
        public Func<object, bool> IsEmpty { get; set; }

        /// <summary>
        /// Function that implements how generate a new Id for this type
        /// </summary>
        public Func<LiteEngine, string, object> NewId { get; set; }
    }
}
