using Omnius.Lxna.Components.Storages;

namespace Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common.Repositories.Entities;

public class NestedPathEntity
{
    public string[]? Values { get; set; }

    public static NestedPathEntity Import(NestedPath path)
    {
        return new NestedPathEntity() { Values = path.Values.Select(n => n.ToString()).ToArray() };
    }

    public NestedPath Export()
    {
        return new NestedPath(this.Values ?? Array.Empty<string>());
    }
}
