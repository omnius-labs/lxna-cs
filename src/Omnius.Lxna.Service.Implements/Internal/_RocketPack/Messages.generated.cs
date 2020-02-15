using Omnius.Lxna.Service;

#nullable enable

namespace Omnius.Lxna.Service.Internal
{
    internal sealed partial class ThumbnailEntity : global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<ThumbnailEntity>
    {
        public static global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<ThumbnailEntity> Formatter { get; }
        public static ThumbnailEntity Empty { get; }

        static ThumbnailEntity()
        {
            ThumbnailEntity.Formatter = new ___CustomFormatter();
            ThumbnailEntity.Empty = new ThumbnailEntity(ThumbnailMetadata.Empty, global::System.Array.Empty<ThumbnailContent>());
        }

        private readonly global::System.Lazy<int> ___hashCode;

        public static readonly int MaxContentsCount = 8192;

        public ThumbnailEntity(ThumbnailMetadata metadata, ThumbnailContent[] contents)
        {
            if (metadata is null) throw new global::System.ArgumentNullException("metadata");
            if (contents is null) throw new global::System.ArgumentNullException("contents");
            if (contents.Length > 8192) throw new global::System.ArgumentOutOfRangeException("contents");
            foreach (var n in contents)
            {
                if (n is null) throw new global::System.ArgumentNullException("n");
            }

            this.Metadata = metadata;
            this.Contents = new global::Omnius.Core.Collections.ReadOnlyListSlim<ThumbnailContent>(contents);

            ___hashCode = new global::System.Lazy<int>(() =>
            {
                var ___h = new global::System.HashCode();
                if (metadata != default) ___h.Add(metadata.GetHashCode());
                foreach (var n in contents)
                {
                    if (n != default) ___h.Add(n.GetHashCode());
                }
                return ___h.ToHashCode();
            });
        }

        public ThumbnailMetadata Metadata { get; }
        public global::Omnius.Core.Collections.ReadOnlyListSlim<ThumbnailContent> Contents { get; }

        public static ThumbnailEntity Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.Serialization.RocketPack.RocketPackReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.Serialization.RocketPack.RocketPackWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(ThumbnailEntity? left, ThumbnailEntity? right)
        {
            return (right is null) ? (left is null) : right.Equals(left);
        }
        public static bool operator !=(ThumbnailEntity? left, ThumbnailEntity? right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is ThumbnailEntity)) return false;
            return this.Equals((ThumbnailEntity)other);
        }
        public bool Equals(ThumbnailEntity? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (this.Metadata != target.Metadata) return false;
            if (!global::Omnius.Core.Helpers.CollectionHelper.Equals(this.Contents, target.Contents)) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode.Value;

        private sealed class ___CustomFormatter : global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<ThumbnailEntity>
        {
            public void Serialize(ref global::Omnius.Core.Serialization.RocketPack.RocketPackWriter w, in ThumbnailEntity value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                {
                    uint propertyCount = 0;
                    if (value.Metadata != ThumbnailMetadata.Empty)
                    {
                        propertyCount++;
                    }
                    if (value.Contents.Count != 0)
                    {
                        propertyCount++;
                    }
                    w.Write(propertyCount);
                }

                if (value.Metadata != ThumbnailMetadata.Empty)
                {
                    w.Write((uint)0);
                    ThumbnailMetadata.Formatter.Serialize(ref w, value.Metadata, rank + 1);
                }
                if (value.Contents.Count != 0)
                {
                    w.Write((uint)1);
                    w.Write((uint)value.Contents.Count);
                    foreach (var n in value.Contents)
                    {
                        ThumbnailContent.Formatter.Serialize(ref w, n, rank + 1);
                    }
                }
            }

            public ThumbnailEntity Deserialize(ref global::Omnius.Core.Serialization.RocketPack.RocketPackReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                uint propertyCount = r.GetUInt32();

                ThumbnailMetadata p_metadata = ThumbnailMetadata.Empty;
                ThumbnailContent[] p_contents = global::System.Array.Empty<ThumbnailContent>();

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0:
                            {
                                p_metadata = ThumbnailMetadata.Formatter.Deserialize(ref r, rank + 1);
                                break;
                            }
                        case 1:
                            {
                                var length = r.GetUInt32();
                                p_contents = new ThumbnailContent[length];
                                for (int i = 0; i < p_contents.Length; i++)
                                {
                                    p_contents[i] = ThumbnailContent.Formatter.Deserialize(ref r, rank + 1);
                                }
                                break;
                            }
                    }
                }

                return new ThumbnailEntity(p_metadata, p_contents);
            }
        }
    }

}
