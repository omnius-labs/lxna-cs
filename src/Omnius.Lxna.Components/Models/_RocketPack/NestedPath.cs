using System;
using System.Linq;
using Omnius.Core.Helpers;

namespace Omnius.Lxna.Components.Models
{
    public sealed partial class NestedPath : IComparable<NestedPath>
    {
        public string GetName()
        {
            var value = this.Values.Where(n => !string.IsNullOrEmpty(n)).LastOrDefault() ?? string.Empty;
            return value.Split('/')[^1];
        }

        public string GetExtension()
        {
            var value = this.Values.Where(n => !string.IsNullOrEmpty(n)).LastOrDefault() ?? string.Empty;
            return System.IO.Path.GetExtension(value);
        }

        public int CompareTo(NestedPath? other)
        {
            if (other is null) return 1;

            return CollectionHelper.Compare(this.Values, other.Values);
        }

        public static NestedPath Combine(NestedPath path1, NestedPath path2)
        {
            return new NestedPath(path1.Values.Union(path2.Values).ToArray());
        }

        public static NestedPath Combine(NestedPath path1, string path2)
        {
            return new NestedPath(path1.Values.Append(path2).ToArray());
        }

        public static NestedPath Combine(string path1, NestedPath path2)
        {
            return new NestedPath(new string[] { path1 }.Union(path2.Values).ToArray());
        }
    }
}
