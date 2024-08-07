// <auto-generated/>
#nullable enable

namespace Omnius.Lxna.Components.Image;

public sealed partial class ThumbnailContent : global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Image.ThumbnailContent>, global::System.IDisposable
{
    public static global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Image.ThumbnailContent> Formatter => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Image.ThumbnailContent>.Formatter;
    public static global::Omnius.Lxna.Components.Image.ThumbnailContent Empty => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Image.ThumbnailContent>.Empty;

    static ThumbnailContent()
    {
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Image.ThumbnailContent>.Formatter = new ___CustomFormatter();
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Image.ThumbnailContent>.Empty = new global::Omnius.Lxna.Components.Image.ThumbnailContent(global::Omnius.Core.Base.MemoryOwner<byte>.Empty);
    }

    private readonly global::System.Lazy<int> ___hashCode;

    public static readonly int MaxImageLength = 33554432;

    public ThumbnailContent(global::System.Buffers.IMemoryOwner<byte> image)
    {
        if (image is null) throw new global::System.ArgumentNullException("image");
        if (image.Memory.Length > 33554432) throw new global::System.ArgumentOutOfRangeException("image");

        this.Image = image;

        ___hashCode = new global::System.Lazy<int>(() =>
        {
            var ___h = new global::System.HashCode();
            if (!image.Memory.IsEmpty) ___h.Add(global::Omnius.Core.Base.Helpers.ObjectHelper.GetHashCode(image.Memory.Span));
            return ___h.ToHashCode();
        });
    }

    public void Dispose()
    {
        this.Image.Dispose();
    }

    public global::System.Buffers.IMemoryOwner<byte> Image { get; }

    public static global::Omnius.Lxna.Components.Image.ThumbnailContent Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.Base.IBytesPool bytesPool)
    {
        var reader = new global::Omnius.Core.RocketPack.RocketMessageReader(sequence, bytesPool);
        return Formatter.Deserialize(ref reader, 0);
    }
    public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.Base.IBytesPool bytesPool)
    {
        var writer = new global::Omnius.Core.RocketPack.RocketMessageWriter(bufferWriter, bytesPool);
        Formatter.Serialize(ref writer, this, 0);
    }

    public static bool operator ==(global::Omnius.Lxna.Components.Image.ThumbnailContent? left, global::Omnius.Lxna.Components.Image.ThumbnailContent? right)
    {
        return (right is null) ? (left is null) : right.Equals(left);
    }
    public static bool operator !=(global::Omnius.Lxna.Components.Image.ThumbnailContent? left, global::Omnius.Lxna.Components.Image.ThumbnailContent? right)
    {
        return !(left == right);
    }
    public override bool Equals(object? other)
    {
        if (other is not global::Omnius.Lxna.Components.Image.ThumbnailContent) return false;
        return this.Equals((global::Omnius.Lxna.Components.Image.ThumbnailContent)other);
    }
    public bool Equals(global::Omnius.Lxna.Components.Image.ThumbnailContent? target)
    {
        if (target is null) return false;
        if (object.ReferenceEquals(this, target)) return true;
        if (!global::Omnius.Core.Base.BytesOperations.Equals(this.Image.Memory.Span, target.Image.Memory.Span)) return false;

        return true;
    }
    public override int GetHashCode() => ___hashCode.Value;

    private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Image.ThumbnailContent>
    {
        public void Serialize(ref global::Omnius.Core.RocketPack.RocketMessageWriter w, scoped in global::Omnius.Lxna.Components.Image.ThumbnailContent value, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            if (!value.Image.Memory.IsEmpty)
            {
                w.Write((uint)1);
                w.Write(value.Image.Memory.Span);
            }
            w.Write((uint)0);
        }
        public global::Omnius.Lxna.Components.Image.ThumbnailContent Deserialize(ref global::Omnius.Core.RocketPack.RocketMessageReader r, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            global::System.Buffers.IMemoryOwner<byte> p_image = global::Omnius.Core.Base.MemoryOwner<byte>.Empty;

            for (; ; )
            {
                uint id = r.GetUInt32();
                if (id == 0) break;
                switch (id)
                {
                    case 1:
                        {
                            p_image = r.GetRecyclableMemory(33554432);
                            break;
                        }
                }
            }

            return new global::Omnius.Lxna.Components.Image.ThumbnailContent(p_image);
        }
    }
}
