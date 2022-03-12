using Omnius.Lxna.Components.Storages.Models;

namespace Omnius.Lxna.Components.ThumbnailGenerators.Internal.Common.Repositories.Entities;

public class NestedPathEntity
{
    public string[]? Values { get; set; }

    public static NestedPathEntity Import(NestedPath value)
    {
        return new NestedPathEntity() { Values = value.Values.ToArray() };
    }

    public NestedPath Export()
    {
        return new NestedPath(this.Values ?? Array.Empty<string>());
    }
}
