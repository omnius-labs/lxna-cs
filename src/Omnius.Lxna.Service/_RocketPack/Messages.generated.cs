
#nullable enable

namespace Omnius.Lxna.Service
{
    public enum ThumbnailResizeType : byte
    {
        Pad = 0,
        Crop = 1,
    }

    public enum ThumbnailFormatType : byte
    {
        Png = 0,
    }

    public sealed partial class ThumbnailContent : global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>, global::System.IDisposable
    {
        public static global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<ThumbnailContent> Formatter => global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>.Formatter;
        public static ThumbnailContent Empty => global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>.Empty;

        static ThumbnailContent()
        {
            global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>.Empty = new ThumbnailContent(global::Omnius.Core.SimpleMemoryOwner<byte>.Empty);
        }

        private readonly global::System.Lazy<int> ___hashCode;

        public static readonly int MaxImageLength = 33554432;

        public ThumbnailContent(global::System.Buffers.IMemoryOwner<byte> image)
        {
            if (image is null) throw new global::System.ArgumentNullException("image");
            if (image.Memory.Length > 33554432) throw new global::System.ArgumentOutOfRangeException("image");

            _image = image;

            ___hashCode = new global::System.Lazy<int>(() =>
            {
                var ___h = new global::System.HashCode();
                if (!image.Memory.IsEmpty) ___h.Add(global::Omnius.Core.Helpers.ObjectHelper.GetHashCode(image.Memory.Span));
                return ___h.ToHashCode();
            });
        }

        private readonly global::System.Buffers.IMemoryOwner<byte> _image;
        public global::System.ReadOnlyMemory<byte> Image => _image.Memory;

        public static ThumbnailContent Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.Serialization.RocketPack.RocketPackReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.Serialization.RocketPack.RocketPackWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(ThumbnailContent? left, ThumbnailContent? right)
        {
            return (right is null) ? (left is null) : right.Equals(left);
        }
        public static bool operator !=(ThumbnailContent? left, ThumbnailContent? right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is ThumbnailContent)) return false;
            return this.Equals((ThumbnailContent)other);
        }
        public bool Equals(ThumbnailContent? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (!global::Omnius.Core.BytesOperations.Equals(this.Image.Span, target.Image.Span)) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode.Value;

        public void Dispose()
        {
            _image?.Dispose();
        }

        private sealed class ___CustomFormatter : global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<ThumbnailContent>
        {
            public void Serialize(ref global::Omnius.Core.Serialization.RocketPack.RocketPackWriter w, in ThumbnailContent value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                {
                    uint propertyCount = 0;
                    if (!value.Image.IsEmpty)
                    {
                        propertyCount++;
                    }
                    w.Write(propertyCount);
                }

                if (!value.Image.IsEmpty)
                {
                    w.Write((uint)0);
                    w.Write(value.Image.Span);
                }
            }

            public ThumbnailContent Deserialize(ref global::Omnius.Core.Serialization.RocketPack.RocketPackReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                uint propertyCount = r.GetUInt32();

                global::System.Buffers.IMemoryOwner<byte> p_image = global::Omnius.Core.SimpleMemoryOwner<byte>.Empty;

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0:
                            {
                                p_image = r.GetRecyclableMemory(33554432);
                                break;
                            }
                    }
                }

                return new ThumbnailContent(p_image);
            }
        }
    }

}
