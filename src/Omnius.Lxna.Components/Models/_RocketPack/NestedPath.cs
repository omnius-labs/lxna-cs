using System;
using System.Linq;
using Omnius.Core.Helpers;

namespace Omnius.Lxna.Components.Models
{
    public sealed partial class NestedPath : IComparable<NestedPath>
    {
        public string GetName()
        {
            var values = this.Values.Where(n => !string.IsNullOrEmpty(n)).ToArray();
            return values[^1].Split('/')[^1];
        }

        public string GetExtension()
        {
            var values = this.Values.Where(n => !string.IsNullOrEmpty(n)).ToArray();
            return System.IO.Path.GetExtension(values[^1]);
        }

        public int CompareTo(NestedPath? other)
        {
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
