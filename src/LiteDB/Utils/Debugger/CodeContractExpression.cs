// ** This solution is works great, but it´s toooooooooo slow **

//namespace LiteDB;

///// <summary>
///// Static methods for test (in Debug mode) some parameters - ideal to debug database
///// </summary>
//[DebuggerStepThrough]
//internal static class CodeContract
//{
//    /// <summary>
//    /// Ensure condition is true, otherwise throw exception (check contract)
//    /// Works as ENSURE, but for non-expression tests (doesn't works with Span)
//    /// </summary>
//    [Conditional("DEBUG")]
//    public static void DEBUG(bool condition, string message)
//    {
//        if (condition == false)
//        {
//            ENSURE(Expression.Constant(message), null, null, null);
//        }
//    }

//    /// <summary>
//    /// If first test is true, ensure second condition to be true, otherwise throw exception (check contract)
//    /// </summary>
//    [Conditional("DEBUG")]
//    public static void ENSURE(bool ifTest, Expression<Func<bool>> condition, string? message = null)
//    {
//        if (ifTest)
//        {
//            var fn = condition.Compile();

//            if (fn() == false)
//            {
//                ENSURE(condition.Body, message, null, null);
//            }
//        }
//    }

//    /// <summary>
//    /// Ensure condition is true, otherwise throw exception (check contract)
//    /// </summary>
//    [Conditional("DEBUG")]
//    public static void ENSURE(Expression<Func<bool>> condition, string? message = null)
//    {
//        var fn = condition.Compile();

//        if (fn() == false)
//        {
//            ENSURE(condition.Body, message, null, null);
//        }
//    }

//    /// <summary>
//    /// Ensure condition is true, otherwise throw exception (check contract)
//    /// </summary>
//    [Conditional("DEBUG")]
//    public static void ENSURE<T>(T input, Expression<Func<T, bool>> condition, string? message = null)
//    {
//        var fn = condition.Compile();
//        var param = condition.Parameters[0];

//        if (fn(input) == false)
//        {
//            ENSURE(condition.Body, message, input, param);
//        }
//    }

//    /// <summary>
//    /// Build a pretty error message with debug informations. Used only for DEBUG
//    /// </summary>
//    private static void ENSURE(Expression expression, string? message = null, object? input = default, ParameterExpression? param = null)
//    {
//        var st = new StackTrace();
//        var frame = st.GetFrame(2);
//        var method = frame?.GetMethod();

//        // crazy way to detect name when async/sync
//        var location = $"{method?.DeclaringType?.DeclaringType?.CleanName()}.{method?.DeclaringType?.CleanName()}.{method?.Name}";

//        location = Regex.Replace(location, @"^\.", "");
//        location = Regex.Replace(location, @"\.MoveNext", "");

//        var expr = expression.Clean();
//        var sb = new StringBuilder();
//        var types = new List<Type>();

//        PrintValues(expression, sb, input, param, types);
//        FindObjects(expression, sb, input, param, types);

//        var err = new StringBuilder($"`{expr}` is false at '{location}'. ");

//        if (message is not null)
//        {
//            err.Append(message + ". ");
//        }

//        var msg = err.ToString() + sb.ToString();

//        if (Debugger.IsAttached)
//        {
//            Debug.Fail(msg);
//        }
//        else
//        {
//            throw ERR_ENSURE(msg);
//        }
//    }

//    /// <summary>
//    /// Print expressions results
//    /// </summary>
//    private static void PrintValues(Expression e, StringBuilder sb, object? input, ParameterExpression? param, List<Type> types)
//    {
//        if (e is BinaryExpression bin)
//        {
//            PrintValues(bin.Left, sb, input, param, types);
//            PrintValues(bin.Right, sb, input, param, types);
//        }
//        else if (e is UnaryExpression un)
//        {
//            PrintValues(un.Operand, sb, input, param, types);
//        }
//        else if (e is ConstantExpression)
//        {
//            //
//        }
//        else
//        {
//            object result;

//            if (param is not null)
//            {
//                var compiled = Expression.Lambda(e, param).Compile();

//                result = compiled.DynamicInvoke(input);
//            }
//            else
//            {
//                result = Expression.Lambda(e).Compile().DynamicInvoke();
//            }

//            if (sb.Length > 0) sb.Append(", ");

//            sb.Append($"`{e.Clean()}` = {result}");

//            if (result is not null)
//            {
//                types.Add(result.GetType());
//            }
//        }
//    }

//    /// <summary>
//    /// Find (recursively) looking for LiteDB objects/structs to print
//    /// </summary>
//    private static void FindObjects(Expression? e, StringBuilder sb, object? input, ParameterExpression? param, List<Type> types)
//    {
//        if (e is null) return;

//        if (e is BinaryExpression bin)
//        {
//            FindObjects(bin.Left, sb, input, param, types);
//            FindObjects(bin.Right, sb, input, param, types);
//        }
//        if (e is MethodCallExpression call)
//        {
//            FindObjects(call.Object, sb, input, param, types);

//            foreach (var arg in call.Arguments)
//            {
//                FindObjects(arg, sb, input, param, types);
//            }
//        }
//        else if (e is ConstantExpression con)
//        {
//            var value = con.Value;
//            var ns = value.GetType().Namespace ?? "";

//            if (ns.StartsWith("LiteDB"))
//            {
//                if (types.Contains(value.GetType())) return;

//                var key = con.Clean();
//                var dump = value.Dump();

//                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(dump))
//                {
//                    if (sb.Length > 0) sb.Append(", ");

//                    sb.Append($"{key}: {dump}");
//                }
//            }
//        }
//        else if (e is MemberExpression mem)
//        {
//            var ns = mem.Member.DeclaringType?.Namespace ?? "";

//            if (ns.StartsWith("LiteDB"))
//            {
//                object value;

//                if (param is not null)
//                {
//                    var compiled = Expression.Lambda(mem.Expression, param).Compile();

//                    value = compiled.DynamicInvoke(input);
//                }
//                else
//                {
//                    value = Expression.Lambda(mem.Expression).Compile().DynamicInvoke();
//                }

//                if (types.Contains(value.GetType())) return;

//                var key = mem.Expression.Clean();
//                var dump = value.Dump();

//                if (!string.IsNullOrEmpty(key) && !string.IsNullOrEmpty(dump))
//                {
//                    if (sb.Length > 0) sb.Append(", ");

//                    sb.Append($"{key}: {dump}");
//                }
//            }

//            FindObjects(mem.Expression!, sb, input, param, types);
//        }
//        else if (e is UnaryExpression un)
//        {
//            FindObjects(un.Operand, sb, input, param, types);
//        }
//    }
//}