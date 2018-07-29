using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using System.Text;

namespace LiteDB
{
    internal class EnumerableResolver : ITypeResolver
    {
        public string ResolveMethod(MethodInfo method)
        {
            switch (method.Name)
            {
                // works only with single-result extension method
                case "get_Item": return "#[@0]";
                case "ElementAt": return "@0[@1]";
                case "Single":
                case "First":
                case "SingleOrDefault":
                case "FirstOrDefault": return "@0[0]";
                case "Last":
                case "LastOrDefault": return "@0[-1]";

                // COUNT() works for IEnumerable<BsonValue> 
                // LENGTH() for Array/String/...
                case "Count": return "LENGTH(@0)";

                // not supported (recommend use Sql extension method)
                case "Select":
                case "Any":
                case "Where": throw new NotSupportedException($"Method {method.Name} are not supported. Try use `Items()` extension method to access sub documents fields. Eg: `x => x.Phones.Items(z => z.Type == 'Mobile').Number`");

                // not supported (recommend use Sql extension method)
                case "Sum":
                case "Average":
                case "Max":
                case "Min": throw new NotSupportedException($"Method {method.Name} are not supported. Try use `Sql` static methods. Eg: `x => Sql.Sum(x.Details.Items().Price)`");

                // not supported (recommend use Sql extension method)
                case "ToList": 
                case "ToArray": throw new NotSupportedException($"Method {method.Name} are not supported. Try use `Sql` static methods. Eg: `x => Sql.ToArray(x.Details.Items().Price)`");
            };

            return null;
        }

        public string ResolveMember(MemberInfo member) => null;
        public string ResolveCtor(ConstructorInfo ctor) => null;
    }
}