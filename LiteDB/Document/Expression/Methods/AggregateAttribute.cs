using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Text;

namespace LiteDB
{
    /// <summary>
    /// When a method are decorated with this attribute means all enumetation will be visit BEFORE return any result
    /// </summary>
    internal class AggregateAttribute: Attribute
    {
    }
}
