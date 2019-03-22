using Lxna.Messages;
using Omnix.Base;
using Omnix.Base.Helpers;
using Omnix.Serialization;
using Omnix.Serialization.RocketPack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lxna.Core.Contents
{
    public sealed partial class ThumbnailId : RocketPackMessageBase<ThumbnailId>
    {
        static ThumbnailId()
        {
            ThumbnailId.Formatter = new CustomFormatter();
        }

        public static readonly int MaxPathLength = 1024;

        public ThumbnailId(string path, Timestamp lastWriteTime, ulong length)
        {
            if (path is null) throw new ArgumentNullException("path");
            if (path.Length > 1024) throw new ArgumentOutOfRangeException("path");
            this.Path = path;
            this.LastWriteTime = lastWriteTime;
            this.Length = length;

            {
                var hashCode = new HashCode();
                if (this.Path != default) hashCode.Add(this.Path.GetHashCode());
                if (this.LastWriteTime != default) hashCode.Add(this.LastWriteTime.GetHashCode());
                if (this.Length != default) hashCode.Add(this.Length.GetHashCode());
                _hashCode = hashCode.ToHashCode();
            }
        }

        public string Path { get; }
        public Timestamp LastWriteTime { get; }
        public ulong Length { get; }

        public override bool Equals(ThumbnailId target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Path != target.Path) return false;
            if (this.LastWriteTime != target.LastWriteTime) return false;
            if (this.Length != target.Length) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        private sealed class CustomFormatter : IRocketPackFormatter<ThumbnailId>
        {
            public void Serialize(RocketPackWriter w, ThumbnailId value, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Write property count
                {
                    int propertyCount = 0;
                    if (value.Path != default) propertyCount++;
                    if (value.LastWriteTime != default) propertyCount++;
                    if (value.Length != default) propertyCount++;
                    w.Write((ulong)propertyCount);
                }

                // Path
                if (value.Path != default)
                {
                    w.Write((ulong)0);
                    w.Write(value.Path);
                }
                // LastWriteTime
                if (value.LastWriteTime != default)
                {
                    w.Write((ulong)1);
                    w.Write(value.LastWriteTime);
                }
                // Length
                if (value.Length != default)
                {
                    w.Write((ulong)2);
                    w.Write((ulong)value.Length);
                }
            }

            public ThumbnailId Deserialize(RocketPackReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Read property count
                int propertyCount = (int)r.GetUInt64();

                string p_path = default;
                Timestamp p_lastWriteTime = default;
                ulong p_length = default;

                for (; propertyCount > 0; propertyCount--)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: // Path
                            {
                                p_path = r.GetString(1024);
                                break;
                            }
                        case 1: // LastWriteTime
                            {
                                p_lastWriteTime = r.GetTimestamp();
                                break;
                            }
                        case 2: // Length
                            {
                                p_length = (ulong)r.GetUInt64();
                                break;
                            }
                    }
                }

                return new ThumbnailId(p_path, p_lastWriteTime, p_length);
            }
        }
    }

    public sealed partial class ThumbnailInfo : RocketPackMessageBase<ThumbnailInfo>
    {
        static ThumbnailInfo()
        {
            ThumbnailInfo.Formatter = new CustomFormatter();
        }

        public static readonly int MaxImagesCount = 1024;

        public ThumbnailInfo(ThumbnailId id, IList<ThumbnailImage> images)
        {
            if (id is null) throw new ArgumentNullException("id");
            if (images is null) throw new ArgumentNullException("images");
            if (images.Count > 1024) throw new ArgumentOutOfRangeException("images");
            foreach (var n in images)
            {
                if (n is null) throw new ArgumentNullException("n");
            }

            this.Id = id;
            this.Images = new ReadOnlyCollection<ThumbnailImage>(images);

            {
                var hashCode = new HashCode();
                if (this.Id != default) hashCode.Add(this.Id.GetHashCode());
                foreach (var n in this.Images)
                {
                    if (n != default) hashCode.Add(n.GetHashCode());
                }
                _hashCode = hashCode.ToHashCode();
            }
        }

        public ThumbnailId Id { get; }
        public IReadOnlyList<ThumbnailImage> Images { get; }

        public override bool Equals(ThumbnailInfo target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Id != target.Id) return false;
            if ((this.Images is null) != (target.Images is null)) return false;
            if (!(this.Images is null) && !(target.Images is null) && !CollectionHelper.Equals(this.Images, target.Images)) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        private sealed class CustomFormatter : IRocketPackFormatter<ThumbnailInfo>
        {
            public void Serialize(RocketPackWriter w, ThumbnailInfo value, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Write property count
                {
                    int propertyCount = 0;
                    if (value.Id != default) propertyCount++;
                    if (value.Images.Count != 0) propertyCount++;
                    w.Write((ulong)propertyCount);
                }

                // Id
                if (value.Id != default)
                {
                    w.Write((ulong)0);
                    ThumbnailId.Formatter.Serialize(w, value.Id, rank + 1);
                }
                // Images
                if (value.Images.Count != 0)
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Images.Count);
                    foreach (var n in value.Images)
                    {
                        ThumbnailImage.Formatter.Serialize(w, n, rank + 1);
                    }
                }
            }

            public ThumbnailInfo Deserialize(RocketPackReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Read property count
                int propertyCount = (int)r.GetUInt64();

                ThumbnailId p_id = default;
                IList<ThumbnailImage> p_images = default;

                for (; propertyCount > 0; propertyCount--)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: // Id
                            {
                                p_id = ThumbnailId.Formatter.Deserialize(r, rank + 1);
                                break;
                            }
                        case 1: // Images
                            {
                                var length = (int)r.GetUInt64();
                                p_images = new ThumbnailImage[length];
                                for (int i = 0; i < p_images.Count; i++)
                                {
                                    p_images[i] = ThumbnailImage.Formatter.Deserialize(r, rank + 1);
                                }
                                break;
                            }
                    }
                }

                return new ThumbnailInfo(p_id, p_images);
            }
        }
    }

}
