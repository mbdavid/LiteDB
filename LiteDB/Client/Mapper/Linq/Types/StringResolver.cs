using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class StringResolver : ITypeResolver
    {
        public bool HasSpecialMember => true;

        public string ResolveMethod(MethodInfo method)
        {
            var qtParams = method.GetParameters().Length;

            switch (method.Name)
            {
                case "Count": return "LENGTH(#)";
                case "Trim": return "TRIM(#)";
                case "TrimStart": return "LTRIM(#)";
                case "TrimEnd": return "RTRIM(#)";
                case "ToUpper": return "UPPER(#)";
                case "ToLower": return "LOWER(#)";
                case "Replace": return "REPLACE(#, @0, @1)";
                case "PadLeft": return "LPAD(#, @0, @1)";
                case "RightLeft": return "RPAD(#, @0, @1)";
                case "IndexOf": return qtParams == 1 ? "INDEXOF(#, @0)" : "INDEXOF(#, @0, @1)";
                case "Substring": return qtParams == 1 ? "SUBSTRING(#, @0)" : "SUBSTRING(#, @0, @1)";
                case "StartsWith": return "# LIKE (@0 + '%')";
                case "Contains": return "# LIKE ('%' + @0 + '%')";
                case "EndsWith": return "# LIKE ('%' + @0)";

                // static methods
                case "IsNullOrEmpty": return "(@0 = null OR LENGTH(@0) = 0)";
                case "IsNullOrWhiteSpace": return "(@0 = null OR LENGTH(TRIM(@0)) = 0)";
                case "Format": throw new NotImplementedException(); //TODO implement format
                case "Join": throw new NotImplementedException(); //TODO implement join
            };

            throw new NotSupportedException($"Method {method.Name} are not supported when convert to BsonExpression.");
        }

        public string ResolveMember(MemberInfo member)
        {
            switch (member.Name)
            {
                case "Length": return "LENGTH(#)";
                case "Empty": return "''";
            }

            throw new NotSupportedException($"Member {member.Name} not supported when convert to BsonExpression.");
        }

    }
}