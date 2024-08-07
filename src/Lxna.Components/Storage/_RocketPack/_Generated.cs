// <auto-generated/>
#nullable enable

namespace Omnius.Lxna.Components.Storage;

public readonly partial struct NestedPath : global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Storage.NestedPath>
{
    public static global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Storage.NestedPath> Formatter => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Storage.NestedPath>.Formatter;
    public static global::Omnius.Lxna.Components.Storage.NestedPath Empty => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Storage.NestedPath>.Empty;

    static NestedPath()
    {
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Storage.NestedPath>.Formatter = new ___CustomFormatter();
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Storage.NestedPath>.Empty = new global::Omnius.Lxna.Components.Storage.NestedPath(global::System.Array.Empty<global::Omnius.Core.RocketPack.Utf8String>());
    }

    private readonly int ___hashCode;

    public static readonly int MaxValuesCount = 32;

    public NestedPath(global::Omnius.Core.RocketPack.Utf8String[] values)
    {
        if (values is null) throw new global::System.ArgumentNullException("values");
        if (values.Length > 32) throw new global::System.ArgumentOutOfRangeException("values");
        foreach (var n in values)
        {
            if (n is null) throw new global::System.ArgumentNullException("n");
            if (n.Length > 8192) throw new global::System.ArgumentOutOfRangeException("n");
        }

        this.Values = new global::Omnius.Core.Collections.ReadOnlyListSlim<global::Omnius.Core.RocketPack.Utf8String>(values);

        {
            var ___h = new global::System.HashCode();
            foreach (var n in values)
            {
                if (!n.IsEmpty) ___h.Add(n.GetHashCode());
            }
            ___hashCode = ___h.ToHashCode();
        }
    }

    public global::Omnius.Core.Collections.ReadOnlyListSlim<global::Omnius.Core.RocketPack.Utf8String> Values { get; }

    public static global::Omnius.Lxna.Components.Storage.NestedPath Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.Base.IBytesPool bytesPool)
    {
        var reader = new global::Omnius.Core.RocketPack.RocketMessageReader(sequence, bytesPool);
        return Formatter.Deserialize(ref reader, 0);
    }
    public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.Base.IBytesPool bytesPool)
    {
        var writer = new global::Omnius.Core.RocketPack.RocketMessageWriter(bufferWriter, bytesPool);
        Formatter.Serialize(ref writer, this, 0);
    }

    public static bool operator ==(global::Omnius.Lxna.Components.Storage.NestedPath left, global::Omnius.Lxna.Components.Storage.NestedPath right)
    {
        return right.Equals(left);
    }
    public static bool operator !=(global::Omnius.Lxna.Components.Storage.NestedPath left, global::Omnius.Lxna.Components.Storage.NestedPath right)
    {
        return !(left == right);
    }
    public override bool Equals(object? other)
    {
        if (other is not global::Omnius.Lxna.Components.Storage.NestedPath) return false;
        return this.Equals((global::Omnius.Lxna.Components.Storage.NestedPath)other);
    }
    public bool Equals(global::Omnius.Lxna.Components.Storage.NestedPath target)
    {
        if (!global::Omnius.Core.Base.Helpers.CollectionHelper.Equals(this.Values, target.Values)) return false;

        return true;
    }
    public override int GetHashCode() => ___hashCode;

    private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Storage.NestedPath>
    {
        public void Serialize(ref global::Omnius.Core.RocketPack.RocketMessageWriter w, scoped in global::Omnius.Lxna.Components.Storage.NestedPath value, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            w.Write((uint)value.Values.Count);
            foreach (var n in value.Values)
            {
                w.Write(n);
            }
        }
        public global::Omnius.Lxna.Components.Storage.NestedPath Deserialize(ref global::Omnius.Core.RocketPack.RocketMessageReader r, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            global::Omnius.Core.RocketPack.Utf8String[] p_values = global::System.Array.Empty<global::Omnius.Core.RocketPack.Utf8String>();

            {
                var length = r.GetUInt32();
                p_values = new global::Omnius.Core.RocketPack.Utf8String[length];
                for (int i = 0; i < p_values.Length; i++)
                {
                    p_values[i] = r.GetString(8192);
                }
            }
            return new global::Omnius.Lxna.Components.Storage.NestedPath(p_values);
        }
    }
}
