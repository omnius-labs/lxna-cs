// <auto-generated/>
#nullable enable

namespace Omnius.Lxna.Components.Thumbnail.Internal;

internal sealed partial class FileMeta : global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta>
{
    public static global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta> Formatter => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta>.Formatter;
    public static global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta Empty => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta>.Empty;

    static FileMeta()
    {
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta>.Formatter = new ___CustomFormatter();
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta>.Empty = new global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta(global::Omnius.Lxna.Components.Storage.NestedPath.Empty, 0, global::Omnius.Core.RocketPack.Timestamp64.Zero);
    }

    private readonly global::System.Lazy<int> ___hashCode;

    public FileMeta(global::Omnius.Lxna.Components.Storage.NestedPath path, ulong length, global::Omnius.Core.RocketPack.Timestamp64 lastWriteTime)
    {
        this.Path = path;
        this.Length = length;
        this.LastWriteTime = lastWriteTime;

        ___hashCode = new global::System.Lazy<int>(() =>
        {
            var ___h = new global::System.HashCode();
            if (path != default) ___h.Add(path.GetHashCode());
            if (length != default) ___h.Add(length.GetHashCode());
            if (lastWriteTime != default) ___h.Add(lastWriteTime.GetHashCode());
            return ___h.ToHashCode();
        });
    }

    public global::Omnius.Lxna.Components.Storage.NestedPath Path { get; }
    public ulong Length { get; }
    public global::Omnius.Core.RocketPack.Timestamp64 LastWriteTime { get; }

    public static global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
    {
        var reader = new global::Omnius.Core.RocketPack.RocketMessageReader(sequence, bytesPool);
        return Formatter.Deserialize(ref reader, 0);
    }
    public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
    {
        var writer = new global::Omnius.Core.RocketPack.RocketMessageWriter(bufferWriter, bytesPool);
        Formatter.Serialize(ref writer, this, 0);
    }

    public static bool operator ==(global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta? left, global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta? right)
    {
        return (right is null) ? (left is null) : right.Equals(left);
    }
    public static bool operator !=(global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta? left, global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta? right)
    {
        return !(left == right);
    }
    public override bool Equals(object? other)
    {
        if (other is not global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta) return false;
        return this.Equals((global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta)other);
    }
    public bool Equals(global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta? target)
    {
        if (target is null) return false;
        if (object.ReferenceEquals(this, target)) return true;
        if (this.Path != target.Path) return false;
        if (this.Length != target.Length) return false;
        if (this.LastWriteTime != target.LastWriteTime) return false;

        return true;
    }
    public override int GetHashCode() => ___hashCode.Value;

    private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta>
    {
        public void Serialize(ref global::Omnius.Core.RocketPack.RocketMessageWriter w, scoped in global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta value, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            if (value.Path != global::Omnius.Lxna.Components.Storage.NestedPath.Empty)
            {
                w.Write((uint)1);
                global::Omnius.Lxna.Components.Storage.NestedPath.Formatter.Serialize(ref w, value.Path, rank + 1);
            }
            if (value.Length != 0)
            {
                w.Write((uint)2);
                w.Write(value.Length);
            }
            if (value.LastWriteTime != global::Omnius.Core.RocketPack.Timestamp64.Zero)
            {
                w.Write((uint)3);
                w.Write(value.LastWriteTime);
            }
            w.Write((uint)0);
        }
        public global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta Deserialize(ref global::Omnius.Core.RocketPack.RocketMessageReader r, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            global::Omnius.Lxna.Components.Storage.NestedPath p_path = global::Omnius.Lxna.Components.Storage.NestedPath.Empty;
            ulong p_length = 0;
            global::Omnius.Core.RocketPack.Timestamp64 p_lastWriteTime = global::Omnius.Core.RocketPack.Timestamp64.Zero;

            for (; ; )
            {
                uint id = r.GetUInt32();
                if (id == 0) break;
                switch (id)
                {
                    case 1:
                        {
                            p_path = global::Omnius.Lxna.Components.Storage.NestedPath.Formatter.Deserialize(ref r, rank + 1);
                            break;
                        }
                    case 2:
                        {
                            p_length = r.GetUInt64();
                            break;
                        }
                    case 3:
                        {
                            p_lastWriteTime = r.GetTimestamp64();
                            break;
                        }
                }
            }

            return new global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta(p_path, p_length, p_lastWriteTime);
        }
    }
}
internal sealed partial class ThumbnailMeta : global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta>
{
    public static global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta> Formatter => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta>.Formatter;
    public static global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta Empty => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta>.Empty;

    static ThumbnailMeta()
    {
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta>.Formatter = new ___CustomFormatter();
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta>.Empty = new global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta((global::Omnius.Lxna.Components.Thumbnail.ThumbnailResizeType)0, (global::Omnius.Lxna.Components.Thumbnail.ThumbnailFormatType)0, 0, 0);
    }

    private readonly global::System.Lazy<int> ___hashCode;

    public ThumbnailMeta(global::Omnius.Lxna.Components.Thumbnail.ThumbnailResizeType resizeType, global::Omnius.Lxna.Components.Thumbnail.ThumbnailFormatType formatType, uint width, uint height)
    {
        this.ResizeType = resizeType;
        this.FormatType = formatType;
        this.Width = width;
        this.Height = height;

        ___hashCode = new global::System.Lazy<int>(() =>
        {
            var ___h = new global::System.HashCode();
            if (resizeType != default) ___h.Add(resizeType.GetHashCode());
            if (formatType != default) ___h.Add(formatType.GetHashCode());
            if (width != default) ___h.Add(width.GetHashCode());
            if (height != default) ___h.Add(height.GetHashCode());
            return ___h.ToHashCode();
        });
    }

    public global::Omnius.Lxna.Components.Thumbnail.ThumbnailResizeType ResizeType { get; }
    public global::Omnius.Lxna.Components.Thumbnail.ThumbnailFormatType FormatType { get; }
    public uint Width { get; }
    public uint Height { get; }

    public static global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
    {
        var reader = new global::Omnius.Core.RocketPack.RocketMessageReader(sequence, bytesPool);
        return Formatter.Deserialize(ref reader, 0);
    }
    public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
    {
        var writer = new global::Omnius.Core.RocketPack.RocketMessageWriter(bufferWriter, bytesPool);
        Formatter.Serialize(ref writer, this, 0);
    }

    public static bool operator ==(global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta? left, global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta? right)
    {
        return (right is null) ? (left is null) : right.Equals(left);
    }
    public static bool operator !=(global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta? left, global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta? right)
    {
        return !(left == right);
    }
    public override bool Equals(object? other)
    {
        if (other is not global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta) return false;
        return this.Equals((global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta)other);
    }
    public bool Equals(global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta? target)
    {
        if (target is null) return false;
        if (object.ReferenceEquals(this, target)) return true;
        if (this.ResizeType != target.ResizeType) return false;
        if (this.FormatType != target.FormatType) return false;
        if (this.Width != target.Width) return false;
        if (this.Height != target.Height) return false;

        return true;
    }
    public override int GetHashCode() => ___hashCode.Value;

    private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta>
    {
        public void Serialize(ref global::Omnius.Core.RocketPack.RocketMessageWriter w, scoped in global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta value, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            if (value.ResizeType != (global::Omnius.Lxna.Components.Thumbnail.ThumbnailResizeType)0)
            {
                w.Write((uint)1);
                w.Write((ulong)value.ResizeType);
            }
            if (value.FormatType != (global::Omnius.Lxna.Components.Thumbnail.ThumbnailFormatType)0)
            {
                w.Write((uint)2);
                w.Write((ulong)value.FormatType);
            }
            if (value.Width != 0)
            {
                w.Write((uint)3);
                w.Write(value.Width);
            }
            if (value.Height != 0)
            {
                w.Write((uint)4);
                w.Write(value.Height);
            }
            w.Write((uint)0);
        }
        public global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta Deserialize(ref global::Omnius.Core.RocketPack.RocketMessageReader r, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            global::Omnius.Lxna.Components.Thumbnail.ThumbnailResizeType p_resizeType = (global::Omnius.Lxna.Components.Thumbnail.ThumbnailResizeType)0;
            global::Omnius.Lxna.Components.Thumbnail.ThumbnailFormatType p_formatType = (global::Omnius.Lxna.Components.Thumbnail.ThumbnailFormatType)0;
            uint p_width = 0;
            uint p_height = 0;

            for (; ; )
            {
                uint id = r.GetUInt32();
                if (id == 0) break;
                switch (id)
                {
                    case 1:
                        {
                            p_resizeType = (global::Omnius.Lxna.Components.Thumbnail.ThumbnailResizeType)r.GetUInt64();
                            break;
                        }
                    case 2:
                        {
                            p_formatType = (global::Omnius.Lxna.Components.Thumbnail.ThumbnailFormatType)r.GetUInt64();
                            break;
                        }
                    case 3:
                        {
                            p_width = r.GetUInt32();
                            break;
                        }
                    case 4:
                        {
                            p_height = r.GetUInt32();
                            break;
                        }
                }
            }

            return new global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta(p_resizeType, p_formatType, p_width, p_height);
        }
    }
}
internal sealed partial class ThumbnailCache : global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache>
{
    public static global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache> Formatter => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache>.Formatter;
    public static global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache Empty => global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache>.Empty;

    static ThumbnailCache()
    {
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache>.Formatter = new ___CustomFormatter();
        global::Omnius.Core.RocketPack.IRocketMessage<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache>.Empty = new global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache(global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta.Empty, global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta.Empty, global::System.Array.Empty<global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent>());
    }

    private readonly global::System.Lazy<int> ___hashCode;

    public static readonly int MaxContentsCount = 8192;

    public ThumbnailCache(global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta fileMeta, global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta thumbnailMeta, global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent[] contents)
    {
        if (fileMeta is null) throw new global::System.ArgumentNullException("fileMeta");
        if (thumbnailMeta is null) throw new global::System.ArgumentNullException("thumbnailMeta");
        if (contents is null) throw new global::System.ArgumentNullException("contents");
        if (contents.Length > 8192) throw new global::System.ArgumentOutOfRangeException("contents");
        foreach (var n in contents)
        {
            if (n is null) throw new global::System.ArgumentNullException("n");
        }

        this.FileMeta = fileMeta;
        this.ThumbnailMeta = thumbnailMeta;
        this.Contents = new global::Omnius.Core.Collections.ReadOnlyListSlim<global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent>(contents);

        ___hashCode = new global::System.Lazy<int>(() =>
        {
            var ___h = new global::System.HashCode();
            if (fileMeta != default) ___h.Add(fileMeta.GetHashCode());
            if (thumbnailMeta != default) ___h.Add(thumbnailMeta.GetHashCode());
            foreach (var n in contents)
            {
                if (n != default) ___h.Add(n.GetHashCode());
            }
            return ___h.ToHashCode();
        });
    }

    public global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta FileMeta { get; }
    public global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta ThumbnailMeta { get; }
    public global::Omnius.Core.Collections.ReadOnlyListSlim<global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent> Contents { get; }

    public static global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
    {
        var reader = new global::Omnius.Core.RocketPack.RocketMessageReader(sequence, bytesPool);
        return Formatter.Deserialize(ref reader, 0);
    }
    public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
    {
        var writer = new global::Omnius.Core.RocketPack.RocketMessageWriter(bufferWriter, bytesPool);
        Formatter.Serialize(ref writer, this, 0);
    }

    public static bool operator ==(global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache? left, global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache? right)
    {
        return (right is null) ? (left is null) : right.Equals(left);
    }
    public static bool operator !=(global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache? left, global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache? right)
    {
        return !(left == right);
    }
    public override bool Equals(object? other)
    {
        if (other is not global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache) return false;
        return this.Equals((global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache)other);
    }
    public bool Equals(global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache? target)
    {
        if (target is null) return false;
        if (object.ReferenceEquals(this, target)) return true;
        if (this.FileMeta != target.FileMeta) return false;
        if (this.ThumbnailMeta != target.ThumbnailMeta) return false;
        if (!global::Omnius.Core.Helpers.CollectionHelper.Equals(this.Contents, target.Contents)) return false;

        return true;
    }
    public override int GetHashCode() => ___hashCode.Value;

    private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketMessageFormatter<global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache>
    {
        public void Serialize(ref global::Omnius.Core.RocketPack.RocketMessageWriter w, scoped in global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache value, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            if (value.FileMeta != global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta.Empty)
            {
                w.Write((uint)1);
                global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta.Formatter.Serialize(ref w, value.FileMeta, rank + 1);
            }
            if (value.ThumbnailMeta != global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta.Empty)
            {
                w.Write((uint)2);
                global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta.Formatter.Serialize(ref w, value.ThumbnailMeta, rank + 1);
            }
            if (value.Contents.Count != 0)
            {
                w.Write((uint)3);
                w.Write((uint)value.Contents.Count);
                foreach (var n in value.Contents)
                {
                    global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent.Formatter.Serialize(ref w, n, rank + 1);
                }
            }
            w.Write((uint)0);
        }
        public global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache Deserialize(ref global::Omnius.Core.RocketPack.RocketMessageReader r, scoped in int rank)
        {
            if (rank > 256) throw new global::System.FormatException();

            global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta p_fileMeta = global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta.Empty;
            global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta p_thumbnailMeta = global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta.Empty;
            global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent[] p_contents = global::System.Array.Empty<global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent>();

            for (; ; )
            {
                uint id = r.GetUInt32();
                if (id == 0) break;
                switch (id)
                {
                    case 1:
                        {
                            p_fileMeta = global::Omnius.Lxna.Components.Thumbnail.Internal.FileMeta.Formatter.Deserialize(ref r, rank + 1);
                            break;
                        }
                    case 2:
                        {
                            p_thumbnailMeta = global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailMeta.Formatter.Deserialize(ref r, rank + 1);
                            break;
                        }
                    case 3:
                        {
                            var length = r.GetUInt32();
                            p_contents = new global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent[length];
                            for (int i = 0; i < p_contents.Length; i++)
                            {
                                p_contents[i] = global::Omnius.Lxna.Components.Thumbnail.ThumbnailContent.Formatter.Deserialize(ref r, rank + 1);
                            }
                            break;
                        }
                }
            }

            return new global::Omnius.Lxna.Components.Thumbnail.Internal.ThumbnailCache(p_fileMeta, p_thumbnailMeta, p_contents);
        }
    }
}