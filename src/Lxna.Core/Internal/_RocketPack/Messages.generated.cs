using Lxna.Messages;
using Omnix.Network;

#nullable enable

namespace Lxna.Core.Internal
{
    internal sealed partial class FileId : Omnix.Serialization.RocketPack.RocketPackMessageBase<FileId>
    {
        static FileId()
        {
            FileId.Formatter = new CustomFormatter();
            FileId.Empty = new FileId(OmniAddress.Empty, 0, Omnix.Serialization.RocketPack.Timestamp.Zero);
        }

        private readonly int __hashCode;

        public FileId(OmniAddress address, ulong length, Omnix.Serialization.RocketPack.Timestamp lastWriteTime)
        {
            if (address is null) throw new System.ArgumentNullException("address");
            this.Address = address;
            this.Length = length;
            this.LastWriteTime = lastWriteTime;

            {
                var __h = new System.HashCode();
                if (this.Address != default) __h.Add(this.Address.GetHashCode());
                if (this.Length != default) __h.Add(this.Length.GetHashCode());
                if (this.LastWriteTime != default) __h.Add(this.LastWriteTime.GetHashCode());
                __hashCode = __h.ToHashCode();
            }
        }

        public OmniAddress Address { get; }
        public ulong Length { get; }
        public Omnix.Serialization.RocketPack.Timestamp LastWriteTime { get; }

        public override bool Equals(FileId? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (this.Address != target.Address) return false;
            if (this.Length != target.Length) return false;
            if (this.LastWriteTime != target.LastWriteTime) return false;

            return true;
        }

        public override int GetHashCode() => __hashCode;

        private sealed class CustomFormatter : Omnix.Serialization.RocketPack.IRocketPackFormatter<FileId>
        {
            public void Serialize(Omnix.Serialization.RocketPack.RocketPackWriter w, FileId value, int rank)
            {
                if (rank > 256) throw new System.FormatException();

                {
                    uint propertyCount = 0;
                    if (value.Address != OmniAddress.Empty)
                    {
                        propertyCount++;
                    }
                    if (value.Length != 0)
                    {
                        propertyCount++;
                    }
                    if (value.LastWriteTime != Omnix.Serialization.RocketPack.Timestamp.Zero)
                    {
                        propertyCount++;
                    }
                    w.Write(propertyCount);
                }

                if (value.Address != OmniAddress.Empty)
                {
                    w.Write((uint)0);
                    OmniAddress.Formatter.Serialize(w, value.Address, rank + 1);
                }
                if (value.Length != 0)
                {
                    w.Write((uint)1);
                    w.Write(value.Length);
                }
                if (value.LastWriteTime != Omnix.Serialization.RocketPack.Timestamp.Zero)
                {
                    w.Write((uint)2);
                    w.Write(value.LastWriteTime);
                }
            }

            public FileId Deserialize(Omnix.Serialization.RocketPack.RocketPackReader r, int rank)
            {
                if (rank > 256) throw new System.FormatException();

                uint propertyCount = r.GetUInt32();

                OmniAddress p_address = OmniAddress.Empty;
                ulong p_length = 0;
                Omnix.Serialization.RocketPack.Timestamp p_lastWriteTime = Omnix.Serialization.RocketPack.Timestamp.Zero;

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0:
                            {
                                p_address = OmniAddress.Formatter.Deserialize(r, rank + 1);
                                break;
                            }
                        case 1:
                            {
                                p_length = r.GetUInt64();
                                break;
                            }
                        case 2:
                            {
                                p_lastWriteTime = r.GetTimestamp();
                                break;
                            }
                    }
                }

                return new FileId(p_address, p_length, p_lastWriteTime);
            }
        }
    }

    internal sealed partial class ThumbnailsCache : Omnix.Serialization.RocketPack.RocketPackMessageBase<ThumbnailsCache>
    {
        static ThumbnailsCache()
        {
            ThumbnailsCache.Formatter = new CustomFormatter();
            ThumbnailsCache.Empty = new ThumbnailsCache(System.Array.Empty<LxnaThumbnail>());
        }

        private readonly int __hashCode;

        public static readonly int MaxThumbnailsCount = 1024;

        public ThumbnailsCache(LxnaThumbnail[] thumbnails)
        {
            if (thumbnails is null) throw new System.ArgumentNullException("thumbnails");
            if (thumbnails.Length > 1024) throw new System.ArgumentOutOfRangeException("thumbnails");
            foreach (var n in thumbnails)
            {
                if (n is null) throw new System.ArgumentNullException("n");
            }

            this.Thumbnails = new Omnix.Collections.ReadOnlyListSlim<LxnaThumbnail>(thumbnails);

            {
                var __h = new System.HashCode();
                foreach (var n in this.Thumbnails)
                {
                    if (n != default) __h.Add(n.GetHashCode());
                }
                __hashCode = __h.ToHashCode();
            }
        }

        public Omnix.Collections.ReadOnlyListSlim<LxnaThumbnail> Thumbnails { get; }

        public override bool Equals(ThumbnailsCache? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (!Omnix.Base.Helpers.CollectionHelper.Equals(this.Thumbnails, target.Thumbnails)) return false;

            return true;
        }

        public override int GetHashCode() => __hashCode;

        private sealed class CustomFormatter : Omnix.Serialization.RocketPack.IRocketPackFormatter<ThumbnailsCache>
        {
            public void Serialize(Omnix.Serialization.RocketPack.RocketPackWriter w, ThumbnailsCache value, int rank)
            {
                if (rank > 256) throw new System.FormatException();

                {
                    uint propertyCount = 0;
                    if (value.Thumbnails.Count != 0)
                    {
                        propertyCount++;
                    }
                    w.Write(propertyCount);
                }

                if (value.Thumbnails.Count != 0)
                {
                    w.Write((uint)0);
                    w.Write((uint)value.Thumbnails.Count);
                    foreach (var n in value.Thumbnails)
                    {
                        LxnaThumbnail.Formatter.Serialize(w, n, rank + 1);
                    }
                }
            }

            public ThumbnailsCache Deserialize(Omnix.Serialization.RocketPack.RocketPackReader r, int rank)
            {
                if (rank > 256) throw new System.FormatException();

                uint propertyCount = r.GetUInt32();

                LxnaThumbnail[] p_thumbnails = System.Array.Empty<LxnaThumbnail>();

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0:
                            {
                                var length = r.GetUInt32();
                                p_thumbnails = new LxnaThumbnail[length];
                                for (int i = 0; i < p_thumbnails.Length; i++)
                                {
                                    p_thumbnails[i] = LxnaThumbnail.Formatter.Deserialize(r, rank + 1);
                                }
                                break;
                            }
                    }
                }

                return new ThumbnailsCache(p_thumbnails);
            }
        }
    }

}
