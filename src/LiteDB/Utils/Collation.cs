namespace LiteDB;

/// <summary>
/// Implement how database will compare to order by/find strings according defined culture/compare options
/// If not set, default is CurrentCulture with IgnoreCase
/// </summary>
public class Collation : IComparer<BsonValue>, IComparer<string>, IEqualityComparer<BsonValue>
{
    private const string BINARY = "binary";

    private readonly CompareInfo _compareInfo;

    /// <summary>
    /// Inicialize Collation instance using a collation string: language [/CompareOptions]
    /// "en-US"
    /// "en/IgnoreCase"
    /// </summary>
    public Collation(string collation)
    {
        var parts = collation.Split('/');
        var culture = parts[0];
        var compareOptions = parts.Length > 1 ? 
            (CompareOptions)Enum.Parse(typeof(CompareOptions), parts[1]) : 
            CompareOptions.None;

        this.Culture = culture.Equals(BINARY, StringComparison.OrdinalIgnoreCase) ? 
            CultureInfo.InvariantCulture :
            new CultureInfo(culture);

        this.CompareOptions = compareOptions;

        _compareInfo = this.Culture.CompareInfo;
    }

    public Collation(int lcid, CompareOptions compareOptions)
    {
        this.Culture = CultureInfo.GetCultureInfo(lcid);
        this.CompareOptions = compareOptions;

        _compareInfo = this.Culture.CompareInfo;
    }

    public static Collation Default = new (CultureInfo.CurrentCulture.LCID, CompareOptions.IgnoreCase);

    public static Collation Binary = new (CultureInfo.InvariantCulture.LCID, CompareOptions.None);

    /// <summary>
    /// Get database language culture
    /// </summary>
    public CultureInfo Culture { get; }

    /// <summary>
    /// Get options to how string should be compared in sort
    /// </summary>
    public CompareOptions CompareOptions { get; }

    /// <summary>
    /// Compare 2 string values using current culture/compare options
    /// </summary>
    public int Compare(string left, string right)
    {
        var result = _compareInfo.Compare(left, right, this.CompareOptions);

        return result < 0 ? -1 : result > 0 ? +1 : 0;
    }

    /// <summary>
    /// Compare 2 chars values using current culture/compare options
    /// </summary>
    public bool Equals(char left, char right)
    {
        if (this.CompareOptions.HasFlag(CompareOptions.IgnoreCase))
        {
            return char.ToUpper(left) == char.ToUpper(right);
        }
        else
        {
            return left == right;
        }
    }

    public bool StartsWith(string value, string target)
    {
        return value.StartsWith(target, this.CompareOptions.HasFlag(CompareOptions.IgnoreCase), this.Culture);
    }

    public int Compare(BsonValue left, BsonValue rigth)
    {
        return left.CompareTo(rigth, this);
    }

    public bool Equals(BsonValue x, BsonValue y)
    {
        return this.Compare(x, y) == 0;
    }

    public int GetHashCode(BsonValue obj)
    {
        return obj.GetHashCode();
    }

    public override string ToString()
    {
        var name = _compareInfo.LCID == CultureInfo.InvariantCulture.LCID ? BINARY : this.Culture.Name;

        return name + 
            (this.CompareOptions == CompareOptions.None ? "" : "/" + this.CompareOptions.ToString());
    }
}