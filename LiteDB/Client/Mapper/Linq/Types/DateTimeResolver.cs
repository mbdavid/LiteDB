using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class DateTimeResolver : ITypeResolver
    {
        public string ResolveMethod(MethodInfo method)
        {
            switch (method.Name)
            {
                case "AddYears": return "DATEADD('y', @0, #)";
                case "AddMonths": return "DATEADD('M', @0, #)";
                case "AddDays": return "DATEADD('d', @0, #)";
                case "AddHours": return "DATEADD('h', @0, #)";
                case "AddMinutes": return "DATEADD('m', @0, #)";
                case "AddSeconds": return "DATEADD('s', @0, #)";

                // static methods
                case "Parse": return "TO_DATETIME(@0)";
            };

            throw new NotSupportedException($"Method {method.Name} are not supported when convert to BsonExpression.");
        }

        public bool HasSpecialMember => true;

        public string ResolveMember(MemberInfo member)
        {
            switch (member.Name)
            {
                // static properties
                case "Now": return "NOW()";
                case "UtcNow": return "NOW_UTC()";
                case "Today": return "TODAY()";

                // instance properties
                case "Year": return "YEAR(#)";
                case "Month": return "MONTH(#)";
                case "Day": return "DAY(#)";
                case "Hour": return "HOUR(#)";
                case "Minute": return "MINUTE(#)";
                case "Second": return "SECOND(#)";
            }

            throw new NotSupportedException($"Member {member.Name} not supported when convert to BsonExpression.");
        }
    }
}