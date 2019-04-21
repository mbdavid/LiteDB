using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// When a method are decorated with this attribute means that this method are not immutable
    /// </summary>
    internal class VolatileAttribute: Attribute
    {
    }

    /// <summary>
    /// When a method are decorated with this attributes means that intput parameter support multiple values
    /// Methods return a single value
    /// </summary>
    internal class AggregateAttribute : Attribute
    {
    }
}
