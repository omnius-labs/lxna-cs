using Omnius.Core.Helpers;
using Omnius.Core.RocketPack;
using Omnius.Lxna.Components.Storage.Internal.Helpers;

namespace Omnius.Lxna.Components.Storage;

public readonly partial struct NestedPath : IComparable<NestedPath>
{
    public NestedPath(params string[] values) : this(values.Select(path => new Utf8String(PathHelper.Normalize(path))).ToArray())
    {
    }

    public int Depth => this.Values.Count;

    public string GetLastPath()
    {
        return this.Values.LastOrDefault() ?? string.Empty;
    }

    public string GetName()
    {
        var lastPath = this.Values.Where(n => !string.IsNullOrEmpty(n)).LastOrDefault() ?? Utf8String.Empty;
        return lastPath.ToString().TrimEnd('/').Split('/')[^1];
    }

    public string GetExtension()
    {
        var lastPath = this.Values.Where(n => !string.IsNullOrEmpty(n)).LastOrDefault() ?? Utf8String.Empty;
        return System.IO.Path.GetExtension(lastPath);
    }

    public int CompareTo(NestedPath other)
    {
        return CollectionHelper.Compare(this.Values, other.Values);
    }

    public static NestedPath Union(NestedPath path1, NestedPath path2)
    {
        return new NestedPath(path1.Values.Union(path2.Values).ToArray());
    }

    public static NestedPath Combine(NestedPath originalPath, string relativePath)
    {
        var lastPath = PathHelper.Combine(originalPath.GetLastPath(), relativePath);
        return new NestedPath(originalPath.Values[..^1].Append(new Utf8String(lastPath)).ToArray());
    }

    public override string ToString() => string.Join('\n', this.Values.Select(n => $"\"{n}\""));
}
