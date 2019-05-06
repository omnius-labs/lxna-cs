using Lxna.Messages;

#nullable enable

namespace Lxna.Core.Contents
{
    public sealed partial class FileId : Omnix.Serialization.RocketPack.RocketPackMessageBase<FileId>
    {
        static FileId()
        {
            FileId.Formatter = new CustomFormatter();
            FileId.Empty = new FileId(string.Empty, 0, Omnix.Serialization.RocketPack.Timestamp.Zero);
        }

        private readonly int __hashCode;

        public static readonly int MaxPathLength = 1024;

        public FileId(string path, ulong length, Omnix.Serialization.RocketPack.Timestamp lastWriteTime)
        {
            if (path is null) throw new System.ArgumentNullException("path");
            if (path.Length > 1024) throw new System.ArgumentOutOfRangeException("path");
            this.Path = path;
            this.Length = length;
            this.LastWriteTime = lastWriteTime;

            {
                var __h = new System.HashCode();
                if (this.Path != default) __h.Add(this.Path.GetHashCode());
                if (this.Length != default) __h.Add(this.Length.GetHashCode());
                if (this.LastWriteTime != default) __h.Add(this.LastWriteTime.GetHashCode());
                __hashCode = __h.ToHashCode();
            }
        }

        public string Path { get; }
        public ulong Length { get; }
        public Omnix.Serialization.RocketPack.Timestamp LastWriteTime { get; }

        public override bool Equals(FileId? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (this.Path != target.Path) return false;
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
                    if (value.Path != string.Empty)
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

                if (value.Path != string.Empty)
                {
                    w.Write((uint)0);
                    w.Write(value.Path);
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

                // Read property count
                uint propertyCount = r.GetUInt32();

                string p_path = string.Empty;
                ulong p_length = 0;
                Omnix.Serialization.RocketPack.Timestamp p_lastWriteTime = Omnix.Serialization.RocketPack.Timestamp.Zero;

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0: // Path
                            {
                                p_path = r.GetString(1024);
                                break;
                            }
                        case 1: // Length
                            {
                                p_length = r.GetUInt64();
                                break;
                            }
                        case 2: // LastWriteTime
                            {
                                p_lastWriteTime = r.GetTimestamp();
                                break;
                            }
                    }
                }

                return new FileId(p_path, p_length, p_lastWriteTime);
            }
        }
    }

    public sealed partial class ThumbnailsCache : Omnix.Serialization.RocketPack.RocketPackMessageBase<ThumbnailsCache>
    {
        static ThumbnailsCache()
        {
            ThumbnailsCache.Formatter = new CustomFormatter();
            ThumbnailsCache.Empty = new ThumbnailsCache(System.Array.Empty<Thumbnail>());
        }

        private readonly int __hashCode;

        public static readonly int MaxThumbnailsCount = 1024;

        public ThumbnailsCache(Thumbnail[] thumbnails)
        {
            if (thumbnails is null) throw new System.ArgumentNullException("thumbnails");
            if (thumbnails.Length > 1024) throw new System.ArgumentOutOfRangeException("thumbnails");
            foreach (var n in thumbnails)
            {
                if (n is null) throw new System.ArgumentNullException("n");
            }

            this.Thumbnails = new Omnix.Collections.ReadOnlyListSlim<Thumbnail>(thumbnails);

            {
                var __h = new System.HashCode();
                foreach (var n in this.Thumbnails)
                {
                    if (n != default) __h.Add(n.GetHashCode());
                }
                __hashCode = __h.ToHashCode();
            }
        }

        public Omnix.Collections.ReadOnlyListSlim<Thumbnail> Thumbnails { get; }

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
                        Thumbnail.Formatter.Serialize(w, n, rank + 1);
                    }
                }
            }

            public ThumbnailsCache Deserialize(Omnix.Serialization.RocketPack.RocketPackReader r, int rank)
            {
                if (rank > 256) throw new System.FormatException();

                // Read property count
                uint propertyCount = r.GetUInt32();

                Thumbnail[] p_thumbnails = System.Array.Empty<Thumbnail>();

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0: // Thumbnails
                            {
                                var length = r.GetUInt32();
                                p_thumbnails = new Thumbnail[length];
                                for (int i = 0; i < p_thumbnails.Length; i++)
                                {
                                    p_thumbnails[i] = Thumbnail.Formatter.Deserialize(r, rank + 1);
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
