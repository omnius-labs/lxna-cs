using Omnius.Lxna.Components.Models;

#nullable enable

namespace Omnius.Lxna.Components.Internal
{

    internal sealed partial class ThumbnailMetadata : global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailMetadata>
    {
        public static global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Components.Internal.ThumbnailMetadata> Formatter => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailMetadata>.Formatter;
        public static global::Omnius.Lxna.Components.Internal.ThumbnailMetadata Empty => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailMetadata>.Empty;

        static ThumbnailMetadata()
        {
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailMetadata>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailMetadata>.Empty = new global::Omnius.Lxna.Components.Internal.ThumbnailMetadata(0, global::Omnius.Core.RocketPack.Timestamp.Zero);
        }

        private readonly global::System.Lazy<int> ___hashCode;

        public ThumbnailMetadata(ulong fileLength, global::Omnius.Core.RocketPack.Timestamp fileLastWriteTime)
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
        public global::Omnius.Core.RocketPack.Timestamp FileLastWriteTime { get; }

        public static global::Omnius.Lxna.Components.Internal.ThumbnailMetadata Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.RocketPack.RocketPackObjectReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.RocketPack.RocketPackObjectWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(global::Omnius.Lxna.Components.Internal.ThumbnailMetadata? left, global::Omnius.Lxna.Components.Internal.ThumbnailMetadata? right)
        {
            return (right is null) ? (left is null) : right.Equals(left);
        }
        public static bool operator !=(global::Omnius.Lxna.Components.Internal.ThumbnailMetadata? left, global::Omnius.Lxna.Components.Internal.ThumbnailMetadata? right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is global::Omnius.Lxna.Components.Internal.ThumbnailMetadata)) return false;
            return this.Equals((global::Omnius.Lxna.Components.Internal.ThumbnailMetadata)other);
        }
        public bool Equals(global::Omnius.Lxna.Components.Internal.ThumbnailMetadata? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (this.FileLength != target.FileLength) return false;
            if (this.FileLastWriteTime != target.FileLastWriteTime) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode.Value;

        private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Components.Internal.ThumbnailMetadata>
        {
            public void Serialize(ref global::Omnius.Core.RocketPack.RocketPackObjectWriter w, in global::Omnius.Lxna.Components.Internal.ThumbnailMetadata value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                if (value.FileLength != 0)
                {
                    w.Write((uint)1);
                    w.Write(value.FileLength);
                }
                if (value.FileLastWriteTime != global::Omnius.Core.RocketPack.Timestamp.Zero)
                {
                    w.Write((uint)2);
                    w.Write(value.FileLastWriteTime);
                }
                w.Write((uint)0);
            }

            public global::Omnius.Lxna.Components.Internal.ThumbnailMetadata Deserialize(ref global::Omnius.Core.RocketPack.RocketPackObjectReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                ulong p_fileLength = 0;
                global::Omnius.Core.RocketPack.Timestamp p_fileLastWriteTime = global::Omnius.Core.RocketPack.Timestamp.Zero;

                for (;;)
                {
                    uint id = r.GetUInt32();
                    if (id == 0) break;
                    switch (id)
                    {
                        case 1:
                            {
                                p_fileLength = r.GetUInt64();
                                break;
                            }
                        case 2:
                            {
                                p_fileLastWriteTime = r.GetTimestamp();
                                break;
                            }
                    }
                }

                return new global::Omnius.Lxna.Components.Internal.ThumbnailMetadata(p_fileLength, p_fileLastWriteTime);
            }
        }
    }
    internal sealed partial class ThumbnailEntity : global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailEntity>
    {
        public static global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Components.Internal.ThumbnailEntity> Formatter => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailEntity>.Formatter;
        public static global::Omnius.Lxna.Components.Internal.ThumbnailEntity Empty => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailEntity>.Empty;

        static ThumbnailEntity()
        {
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailEntity>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Internal.ThumbnailEntity>.Empty = new global::Omnius.Lxna.Components.Internal.ThumbnailEntity(ThumbnailMetadata.Empty, global::System.Array.Empty<ThumbnailContent>());
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

        public static global::Omnius.Lxna.Components.Internal.ThumbnailEntity Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.RocketPack.RocketPackObjectReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.RocketPack.RocketPackObjectWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(global::Omnius.Lxna.Components.Internal.ThumbnailEntity? left, global::Omnius.Lxna.Components.Internal.ThumbnailEntity? right)
        {
            return (right is null) ? (left is null) : right.Equals(left);
        }
        public static bool operator !=(global::Omnius.Lxna.Components.Internal.ThumbnailEntity? left, global::Omnius.Lxna.Components.Internal.ThumbnailEntity? right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is global::Omnius.Lxna.Components.Internal.ThumbnailEntity)) return false;
            return this.Equals((global::Omnius.Lxna.Components.Internal.ThumbnailEntity)other);
        }
        public bool Equals(global::Omnius.Lxna.Components.Internal.ThumbnailEntity? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (this.Metadata != target.Metadata) return false;
            if (!global::Omnius.Core.Helpers.CollectionHelper.Equals(this.Contents, target.Contents)) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode.Value;

        private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Components.Internal.ThumbnailEntity>
        {
            public void Serialize(ref global::Omnius.Core.RocketPack.RocketPackObjectWriter w, in global::Omnius.Lxna.Components.Internal.ThumbnailEntity value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                if (value.Metadata != ThumbnailMetadata.Empty)
                {
                    w.Write((uint)1);
                    global::Omnius.Lxna.Components.Internal.ThumbnailMetadata.Formatter.Serialize(ref w, value.Metadata, rank + 1);
                }
                if (value.Contents.Count != 0)
                {
                    w.Write((uint)2);
                    w.Write((uint)value.Contents.Count);
                    foreach (var n in value.Contents)
                    {
                        global::Omnius.Lxna.Components.Models.ThumbnailContent.Formatter.Serialize(ref w, n, rank + 1);
                    }
                }
                w.Write((uint)0);
            }

            public global::Omnius.Lxna.Components.Internal.ThumbnailEntity Deserialize(ref global::Omnius.Core.RocketPack.RocketPackObjectReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                ThumbnailMetadata p_metadata = ThumbnailMetadata.Empty;
                ThumbnailContent[] p_contents = global::System.Array.Empty<ThumbnailContent>();

                for (;;)
                {
                    uint id = r.GetUInt32();
                    if (id == 0) break;
                    switch (id)
                    {
                        case 1:
                            {
                                p_metadata = global::Omnius.Lxna.Components.Internal.ThumbnailMetadata.Formatter.Deserialize(ref r, rank + 1);
                                break;
                            }
                        case 2:
                            {
                                var length = r.GetUInt32();
                                p_contents = new ThumbnailContent[length];
                                for (int i = 0; i < p_contents.Length; i++)
                                {
                                    p_contents[i] = global::Omnius.Lxna.Components.Models.ThumbnailContent.Formatter.Deserialize(ref r, rank + 1);
                                }
                                break;
                            }
                    }
                }

                return new global::Omnius.Lxna.Components.Internal.ThumbnailEntity(p_metadata, p_contents);
            }
        }
    }


}
