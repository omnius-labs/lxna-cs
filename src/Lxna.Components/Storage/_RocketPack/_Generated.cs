// <auto-generated/>
#nullable enable

namespace Lxna.Components.Storage;

public readonly partial struct NestedPath : global::Core.RocketPack.IRocketMessage<global::Lxna.Components.Storage.NestedPath>
{
    public static global::Core.RocketPack.IRocketMessageFormatter<global::Lxna.Components.Storage.NestedPath> Formatter => global::Core.RocketPack.IRocketMessage<global::Lxna.Components.Storage.NestedPath>.Formatter;
    public static global::Lxna.Components.Storage.NestedPath Empty => global::Core.RocketPack.IRocketMessage<global::Lxna.Components.Storage.NestedPath>.Empty;

    static NestedPath()
    {
        global::Core.RocketPack.IRocketMessage<global::Lxna.Components.Storage.NestedPath>.Formatter = new ___CustomFormatter();
        global::Core.RocketPack.IRocketMessage<global::Lxna.Components.Storage.NestedPath>.Empty = new global::Lxna.Components.Storage.NestedPath(global::System.Array.Empty<global::Core.RocketPack.Utf8String>());
    }

    private readonly int ___hashCode;

    public static readonly int MaxValuesCount = 32;

    public NestedPath(global::Core.RocketPack.Utf8String[] values)
    {
        if (values is null) throw new global::System.ArgumentNullException("values");
        if (values.Length > 32) throw new global::System.ArgumentOutOfRangeException("values");
        foreach (var n in values)
        {
            if (n is null) throw new global::System.ArgumentNullException("n");
            if (n.Length > 8192) throw new global::System.ArgumentOutOfRangeException("n");
        }

        this.Values = new global::Core.Collections.ReadOnlyListSlim<global::Core.RocketPack.Utf8String>(values);

        {
            var ___h = new global::System.HashCode();
            foreach (var n in values)
            {
                if (!n.IsEmpty) ___h.Add(n.GetHashCode());
            }
            ___hashCode = ___h.ToHashCode();
        }
    }

    public global::Core.Collections.ReadOnlyListSlim<global::Core.RocketPack.Utf8String> Values { get; }

    public static global::Lxna.Components.Storage.NestedPath Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Core.Base.IBytesPool bytesPool)
    {
        var reader = new global::Core.RocketPack.RocketMessageReader(sequence, bytesPool);
        return Formatter.Deserialize(ref reader, 0);
    }
    public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Core.Base.IBytesPool bytesPool)
    {
        var writer = new global::Core.RocketPack.RocketMessageWriter(bufferWriter, bytesPool);
        Formatter.Serialize(ref writer, this, 0);
    }

    public static bool operator ==(global::Lxna.Components.Storage.NestedPath left, global::Lxna.Components.Storage.NestedPath right)
    {
        return right.Equals(left);
    }
    public static bool operator !=(global::Lxna.Components.Storage.NestedPath left, global::Lxna.Components.Storage.NestedPath right)
    {
        return !(left == right);
    }
    public override bool Equals(object? other)
    {
        if (other is not global::Lxna.Components.Storage.NestedPath) return false;
        return this.Equals((global::Lxna.Components.Storage.NestedPath)other);
    }
    public bool Equals(global::Lxna.Components.Storage.NestedPath target)
    {
        if (!global::Core.Base.Helpers.CollectionHelper.Equals(this.Values, target.Values)) return false;

        return true;
    }
    public override int GetHashCode() => ___hashCode;

    private sealed class ___CustomFormatter : global::Core.RocketPack.IRocketMessageFormatter<global::Lxna.Components.Storage.NestedPath>
    {
        public void Serialize(ref global::Core.RocketPack.RocketMessageWriter w, scoped in global::Lxna.Components.Storage.NestedPath value, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            w.Write((uint)value.Values.Count);
            foreach (var n in value.Values)
            {
                w.Write(n);
            }
        }
        public global::Lxna.Components.Storage.NestedPath Deserialize(ref global::Core.RocketPack.RocketMessageReader r, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            global::Core.RocketPack.Utf8String[] p_values = global::System.Array.Empty<global::Core.RocketPack.Utf8String>();

            {
                var length = r.GetUInt32();
                p_values = new global::Core.RocketPack.Utf8String[length];
                for (int i = 0; i < p_values.Length; i++)
                {
                    p_values[i] = r.GetString(8192);
                }
            }
            return new global::Lxna.Components.Storage.NestedPath(p_values);
        }
    }
}