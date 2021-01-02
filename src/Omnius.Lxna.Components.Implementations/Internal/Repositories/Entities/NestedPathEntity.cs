using System.Linq;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components.Internal.Repositories.Entities
{
    public class NestedPathEntity
    {
        public string[]? Values { get; init; }

        public static NestedPathEntity Import(NestedPath value)
        {
            return new NestedPathEntity() { Values = value.Values.ToArray() };
        }

        public NestedPath Export()
        {
            return new NestedPath(this.Values);
        }
    }
}
