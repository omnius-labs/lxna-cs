
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

    public sealed partial class ThumbnailMetadata : global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailMetadata>
    {
        public static global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<ThumbnailMetadata> Formatter => global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailMetadata>.Formatter;
        public static ThumbnailMetadata Empty => global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailMetadata>.Empty;

        static ThumbnailMetadata()
        {
            global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailMetadata>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailMetadata>.Empty = new ThumbnailMetadata(0, global::Omnius.Core.Serialization.RocketPack.Timestamp.Zero);
        }

        private readonly global::System.Lazy<int> ___hashCode;

        public ThumbnailMetadata(ulong fileLength, global::Omnius.Core.Serialization.RocketPack.Timestamp fileLastWriteTime)
        {
            this.FileLength = fileLength;
            this.FileLastWriteTime = fileLastWriteTime;

            ___hashCode = new global::System.Lazy<int>(() =>
            {
                var ___h = new global::System.HashCode();
                if (fileLength != default) ___h.Add(fileLength.GetHashCode());
                if (fileLastWriteTime != default) ___h.Add(fileLastWriteTime.GetHashCode());
                return ___h.ToHashCode();
            });
        }

        public ulong FileLength { get; }
        public global::Omnius.Core.Serialization.RocketPack.Timestamp FileLastWriteTime { get; }

        public static ThumbnailMetadata Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.Serialization.RocketPack.RocketPackReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.Serialization.RocketPack.RocketPackWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(ThumbnailMetadata? left, ThumbnailMetadata? right)
        {
            return (right is null) ? (left is null) : right.Equals(left);
        }
        public static bool operator !=(ThumbnailMetadata? left, ThumbnailMetadata? right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is ThumbnailMetadata)) return false;
            return this.Equals((ThumbnailMetadata)other);
        }
        public bool Equals(ThumbnailMetadata? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (this.FileLength != target.FileLength) return false;
            if (this.FileLastWriteTime != target.FileLastWriteTime) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode.Value;

        private sealed class ___CustomFormatter : global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<ThumbnailMetadata>
        {
            public void Serialize(ref global::Omnius.Core.Serialization.RocketPack.RocketPackWriter w, in ThumbnailMetadata value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                {
                    uint propertyCount = 0;
                    if (value.FileLength != 0)
                    {
                        propertyCount++;
                    }
                    if (value.FileLastWriteTime != global::Omnius.Core.Serialization.RocketPack.Timestamp.Zero)
                    {
                        propertyCount++;
                    }
                    w.Write(propertyCount);
                }

                if (value.FileLength != 0)
                {
                    w.Write((uint)0);
                    w.Write(value.FileLength);
                }
                if (value.FileLastWriteTime != global::Omnius.Core.Serialization.RocketPack.Timestamp.Zero)
                {
                    w.Write((uint)1);
                    w.Write(value.FileLastWriteTime);
                }
            }

            public ThumbnailMetadata Deserialize(ref global::Omnius.Core.Serialization.RocketPack.RocketPackReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                uint propertyCount = r.GetUInt32();

                ulong p_fileLength = 0;
                global::Omnius.Core.Serialization.RocketPack.Timestamp p_fileLastWriteTime = global::Omnius.Core.Serialization.RocketPack.Timestamp.Zero;

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0:
                            {
                                p_fileLength = r.GetUInt64();
                                break;
                            }
                        case 1:
                            {
                                p_fileLastWriteTime = r.GetTimestamp();
                                break;
                            }
                    }
                }

                return new ThumbnailMetadata(p_fileLength, p_fileLastWriteTime);
            }
        }
    }

    public sealed partial class ThumbnailContent : global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>
    {
        public static global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<ThumbnailContent> Formatter => global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>.Formatter;
        public static ThumbnailContent Empty => global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>.Empty;

        static ThumbnailContent()
        {
            global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailContent>.Empty = new ThumbnailContent(global::System.ReadOnlyMemory<byte>.Empty);
        }

        private readonly global::System.Lazy<int> ___hashCode;

        public static readonly int MaxImageLength = 33554432;

        public ThumbnailContent(global::System.ReadOnlyMemory<byte> image)
        {
            if (image.Length > 33554432) throw new global::System.ArgumentOutOfRangeException("image");

            this.Image = image;

            ___hashCode = new global::System.Lazy<int>(() =>
            {
                var ___h = new global::System.HashCode();
                if (!image.IsEmpty) ___h.Add(global::Omnius.Core.Helpers.ObjectHelper.GetHashCode(image.Span));
                return ___h.ToHashCode();
            });
        }

        public global::System.ReadOnlyMemory<byte> Image { get; }

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

                global::System.ReadOnlyMemory<byte> p_image = global::System.ReadOnlyMemory<byte>.Empty;

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0:
                            {
                                p_image = r.GetMemory(33554432);
                                break;
                            }
                    }
                }

                return new ThumbnailContent(p_image);
            }
        }
    }

}
