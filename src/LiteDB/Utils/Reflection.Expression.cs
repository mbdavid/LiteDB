namespace LiteDB;

/// <summary>
/// Using Expressions is the easy and fast way to create classes, structs, get/set fields/properties. But it not works in NET35
/// </summary>
internal partial class Reflection
{
    public static CreateObject CreateClass(Type type)
    {
        var pDoc = Expression.Parameter(typeof(BsonDocument), "_doc");

        return Expression.Lambda<CreateObject>(Expression.New(type), pDoc).Compile();
    }

    public static CreateObject CreateStruct(Type type)
    {
        var pDoc = Expression.Parameter(typeof(BsonDocument), "_doc");
        var newType = Expression.New(type);
        var convert = Expression.Convert(newType, typeof(object));

        return Expression.Lambda<CreateObject>(convert, pDoc).Compile();
    }

    public static GenericGetter? CreateGenericGetter(Type type, MemberInfo memberInfo)
    {
        if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));

        // if has no read
        if (memberInfo is PropertyInfo propertyInfo && propertyInfo.CanRead == false) return null;

        var obj = Expression.Parameter(typeof(object), "o");
        var accessor = Expression.MakeMemberAccess(Expression.Convert(obj, memberInfo.DeclaringType), memberInfo);

        return Expression.Lambda<GenericGetter>(Expression.Convert(accessor, typeof(object)), obj).Compile();
    }

    public static GenericSetter? CreateGenericSetter(Type type, MemberInfo memberInfo)
    {
        if (memberInfo == null) throw new ArgumentNullException(nameof(memberInfo));
        
        // if is property and has no write
        if (memberInfo is PropertyInfo propertyInfo && propertyInfo.CanWrite == false) return null;

        // if *Structs*, use direct reflection - net35 has no Expression.Unbox to cast target
        if (type.GetTypeInfo().IsValueType)
        {
            return 
                memberInfo is FieldInfo fieldInfo ? fieldInfo.SetValue :
                memberInfo is PropertyInfo propInfo ? ((t, v) => propInfo.SetValue(t, v, null)) :
                null;
        }

        var dataType =
            memberInfo is PropertyInfo propInf ? propInf.PropertyType :
            memberInfo is FieldInfo fieldInf ? fieldInf.FieldType :
            throw new NotSupportedException();

        var target = Expression.Parameter(typeof(object), "obj");
        var value = Expression.Parameter(typeof(object), "val");

        var castTarget = Expression.Convert(target, type);
        var castValue = Expression.ConvertChecked(value, dataType);

        var accessor =
            memberInfo is PropertyInfo p ? Expression.Property(castTarget, p) :
            memberInfo is FieldInfo f ? Expression.Field(castTarget, f) :
            throw new NotSupportedException();

        var assign = Expression.Assign(accessor, castValue);
        var conv = Expression.Convert(assign, typeof(object));
        
        return Expression.Lambda<GenericSetter>(conv, target, value).Compile();
    }
}