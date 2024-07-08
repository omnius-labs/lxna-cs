using Omnius.Lxna.Components.Storage;

namespace Omnius.Lxna.Components.Thumbnail.Internal.Repositories.Entities;

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
