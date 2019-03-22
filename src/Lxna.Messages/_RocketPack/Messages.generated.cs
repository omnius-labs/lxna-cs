using Omnix.Base;
using Omnix.Base.Helpers;
using Omnix.Serialization;
using Omnix.Serialization.RocketPack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lxna.Messages
{
    public enum ImageFormatType : byte
    {
        Png = 0,
    }

    public sealed partial class ErrorMessage : RocketPackMessageBase<ErrorMessage>
    {
        static ErrorMessage()
        {
            ErrorMessage.Formatter = new CustomFormatter();
        }

        public static readonly int MaxTypeLength = 8192;
        public static readonly int MaxMessageLength = 8192;
        public static readonly int MaxStackTraceLength = 8192;

        public ErrorMessage(string type, string message, string stackTrace)
        {
            if (type is null) throw new ArgumentNullException("type");
            if (type.Length > 8192) throw new ArgumentOutOfRangeException("type");
            if (message is null) throw new ArgumentNullException("message");
            if (message.Length > 8192) throw new ArgumentOutOfRangeException("message");
            if (stackTrace is null) throw new ArgumentNullException("stackTrace");
            if (stackTrace.Length > 8192) throw new ArgumentOutOfRangeException("stackTrace");

            this.Type = type;
            this.Message = message;
            this.StackTrace = stackTrace;

            {
                var hashCode = new HashCode();
                if (this.Type != default) hashCode.Add(this.Type.GetHashCode());
                if (this.Message != default) hashCode.Add(this.Message.GetHashCode());
                if (this.StackTrace != default) hashCode.Add(this.StackTrace.GetHashCode());
                _hashCode = hashCode.ToHashCode();
            }
        }

        public string Type { get; }
        public string Message { get; }
        public string StackTrace { get; }

        public override bool Equals(ErrorMessage target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Type != target.Type) return false;
            if (this.Message != target.Message) return false;
            if (this.StackTrace != target.StackTrace) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        private sealed class CustomFormatter : IRocketPackFormatter<ErrorMessage>
        {
            public void Serialize(RocketPackWriter w, ErrorMessage value, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Write property count
                {
                    int propertyCount = 0;
                    if (value.Type != default) propertyCount++;
                    if (value.Message != default) propertyCount++;
                    if (value.StackTrace != default) propertyCount++;
                    w.Write((ulong)propertyCount);
                }

                // Type
                if (value.Type != default)
                {
                    w.Write((ulong)0);
                    w.Write(value.Type);
                }
                // Message
                if (value.Message != default)
                {
                    w.Write((ulong)1);
                    w.Write(value.Message);
                }
                // StackTrace
                if (value.StackTrace != default)
                {
                    w.Write((ulong)2);
                    w.Write(value.StackTrace);
                }
            }

            public ErrorMessage Deserialize(RocketPackReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Read property count
                int propertyCount = (int)r.GetUInt64();

                string p_type = default;
                string p_message = default;
                string p_stackTrace = default;

                for (; propertyCount > 0; propertyCount--)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: // Type
                            {
                                p_type = r.GetString(8192);
                                break;
                            }
                        case 1: // Message
                            {
                                p_message = r.GetString(8192);
                                break;
                            }
                        case 2: // StackTrace
                            {
                                p_stackTrace = r.GetString(8192);
                                break;
                            }
                    }
                }

                return new ErrorMessage(p_type, p_message, p_stackTrace);
            }
        }
    }

    public sealed partial class FileMetadata : RocketPackMessageBase<FileMetadata>
    {
        static FileMetadata()
        {
            FileMetadata.Formatter = new CustomFormatter();
        }

        public static readonly int MaxPathLength = 1024;

        public FileMetadata(string path)
        {
            if (path is null) throw new ArgumentNullException("path");
            if (path.Length > 1024) throw new ArgumentOutOfRangeException("path");

            this.Path = path;

            {
                var hashCode = new HashCode();
                if (this.Path != default) hashCode.Add(this.Path.GetHashCode());
                _hashCode = hashCode.ToHashCode();
            }
        }

        public string Path { get; }

        public override bool Equals(FileMetadata target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Path != target.Path) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        private sealed class CustomFormatter : IRocketPackFormatter<FileMetadata>
        {
            public void Serialize(RocketPackWriter w, FileMetadata value, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Write property count
                {
                    int propertyCount = 0;
                    if (value.Path != default) propertyCount++;
                    w.Write((ulong)propertyCount);
                }

                // Path
                if (value.Path != default)
                {
                    w.Write((ulong)0);
                    w.Write(value.Path);
                }
            }

            public FileMetadata Deserialize(RocketPackReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Read property count
                int propertyCount = (int)r.GetUInt64();

                string p_path = default;

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
                    }
                }

                return new FileMetadata(p_path);
            }
        }
    }

    public sealed partial class ThumbnailImage : RocketPackMessageBase<ThumbnailImage>, IDisposable
    {
        static ThumbnailImage()
        {
            ThumbnailImage.Formatter = new CustomFormatter();
        }

        public static readonly int MaxValueLength = 33554432;

        public ThumbnailImage(ImageFormatType type, IMemoryOwner<byte> value)
        {
            if (value is null) throw new ArgumentNullException("value");
            if (value.Memory.Length > 33554432) throw new ArgumentOutOfRangeException("value");

            this.Type = type;
            _value = value;

            {
                var hashCode = new HashCode();
                if (this.Type != default) hashCode.Add(this.Type.GetHashCode());
                if (!this.Value.IsEmpty) hashCode.Add(ObjectHelper.GetHashCode(this.Value.Span));
                _hashCode = hashCode.ToHashCode();
            }
        }

        public ImageFormatType Type { get; }
        private readonly IMemoryOwner<byte> _value;
        public ReadOnlyMemory<byte> Value => _value.Memory;

        public override bool Equals(ThumbnailImage target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Type != target.Type) return false;
            if (!BytesOperations.SequenceEqual(this.Value.Span, target.Value.Span)) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        public void Dispose()
        {
            _value?.Dispose();
        }

        private sealed class CustomFormatter : IRocketPackFormatter<ThumbnailImage>
        {
            public void Serialize(RocketPackWriter w, ThumbnailImage value, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Write property count
                {
                    int propertyCount = 0;
                    if (value.Type != default) propertyCount++;
                    if (!value.Value.IsEmpty) propertyCount++;
                    w.Write((ulong)propertyCount);
                }

                // Type
                if (value.Type != default)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Type);
                }
                // Value
                if (!value.Value.IsEmpty)
                {
                    w.Write((ulong)1);
                    w.Write(value.Value.Span);
                }
            }

            public ThumbnailImage Deserialize(RocketPackReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Read property count
                int propertyCount = (int)r.GetUInt64();

                ImageFormatType p_type = default;
                IMemoryOwner<byte> p_value = default;

                for (; propertyCount > 0; propertyCount--)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: // Type
                            {
                                p_type = (ImageFormatType)r.GetUInt64();
                                break;
                            }
                        case 1: // Value
                            {
                                p_value = r.GetRecyclableMemory(33554432);
                                break;
                            }
                    }
                }

                return new ThumbnailImage(p_type, p_value);
            }
        }
    }

}
