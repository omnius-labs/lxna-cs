using Lxna.Messages;
using Omnix.Base;
using Omnix.Base.Helpers;
using Omnix.Serialization;
using Omnix.Serialization.RocketPack;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace Lxna.Rpc
{
    public enum LxnaRpcRequestType : byte
    {
        Exit = 0,
        Cancel = 1,
        Load = 2,
        Save = 3,
        GetState = 4,
        Start = 5,
        Stop = 6,
        GetFileMetadatas = 7,
        GetFileThumbnail = 8,
        ReadFileContent = 9,
    }

    public enum LxnaRpcResponseType : byte
    {
        Result = 0,
        Output = 1,
        Cancel = 2,
        Error = 3,
    }

    public sealed partial class LxnaRpcRequestHeader : RocketPackMessageBase<LxnaRpcRequestHeader>
    {
        static LxnaRpcRequestHeader()
        {
            LxnaRpcRequestHeader.Formatter = new CustomFormatter();
        }

        public LxnaRpcRequestHeader(LxnaRpcRequestType type, uint id)
        {
            this.Type = type;
            this.Id = id;

            {
                var hashCode = new HashCode();
                if (this.Type != default) hashCode.Add(this.Type.GetHashCode());
                if (this.Id != default) hashCode.Add(this.Id.GetHashCode());
                _hashCode = hashCode.ToHashCode();
            }
        }

        public LxnaRpcRequestType Type { get; }
        public uint Id { get; }

        public override bool Equals(LxnaRpcRequestHeader target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Type != target.Type) return false;
            if (this.Id != target.Id) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        private sealed class CustomFormatter : IRocketPackFormatter<LxnaRpcRequestHeader>
        {
            public void Serialize(RocketPackWriter w, LxnaRpcRequestHeader value, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Write property count
                {
                    int propertyCount = 0;
                    if (value.Type != default) propertyCount++;
                    if (value.Id != default) propertyCount++;
                    w.Write((ulong)propertyCount);
                }

                // Type
                if (value.Type != default)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Type);
                }
                // Id
                if (value.Id != default)
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Id);
                }
            }

            public LxnaRpcRequestHeader Deserialize(RocketPackReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Read property count
                int propertyCount = (int)r.GetUInt64();

                LxnaRpcRequestType p_type = default;
                uint p_id = default;

                for (; propertyCount > 0; propertyCount--)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: // Type
                            {
                                p_type = (LxnaRpcRequestType)r.GetUInt64();
                                break;
                            }
                        case 1: // Id
                            {
                                p_id = (uint)r.GetUInt64();
                                break;
                            }
                    }
                }

                return new LxnaRpcRequestHeader(p_type, p_id);
            }
        }
    }

    public sealed partial class LxnaRpcResponseHeader : RocketPackMessageBase<LxnaRpcResponseHeader>
    {
        static LxnaRpcResponseHeader()
        {
            LxnaRpcResponseHeader.Formatter = new CustomFormatter();
        }

        public LxnaRpcResponseHeader(LxnaRpcResponseType type, uint id)
        {
            this.Type = type;
            this.Id = id;

            {
                var hashCode = new HashCode();
                if (this.Type != default) hashCode.Add(this.Type.GetHashCode());
                if (this.Id != default) hashCode.Add(this.Id.GetHashCode());
                _hashCode = hashCode.ToHashCode();
            }
        }

        public LxnaRpcResponseType Type { get; }
        public uint Id { get; }

        public override bool Equals(LxnaRpcResponseHeader target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Type != target.Type) return false;
            if (this.Id != target.Id) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        private sealed class CustomFormatter : IRocketPackFormatter<LxnaRpcResponseHeader>
        {
            public void Serialize(RocketPackWriter w, LxnaRpcResponseHeader value, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Write property count
                {
                    int propertyCount = 0;
                    if (value.Type != default) propertyCount++;
                    if (value.Id != default) propertyCount++;
                    w.Write((ulong)propertyCount);
                }

                // Type
                if (value.Type != default)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.Type);
                }
                // Id
                if (value.Id != default)
                {
                    w.Write((ulong)1);
                    w.Write((ulong)value.Id);
                }
            }

            public LxnaRpcResponseHeader Deserialize(RocketPackReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Read property count
                int propertyCount = (int)r.GetUInt64();

                LxnaRpcResponseType p_type = default;
                uint p_id = default;

                for (; propertyCount > 0; propertyCount--)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: // Type
                            {
                                p_type = (LxnaRpcResponseType)r.GetUInt64();
                                break;
                            }
                        case 1: // Id
                            {
                                p_id = (uint)r.GetUInt64();
                                break;
                            }
                    }
                }

                return new LxnaRpcResponseHeader(p_type, p_id);
            }
        }
    }

    public sealed partial class GetFileMetadatasRequestBody : RocketPackMessageBase<GetFileMetadatasRequestBody>
    {
        static GetFileMetadatasRequestBody()
        {
            GetFileMetadatasRequestBody.Formatter = new CustomFormatter();
        }

        public static readonly int MaxPathLength = 1024;

        public GetFileMetadatasRequestBody(string path)
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

        public override bool Equals(GetFileMetadatasRequestBody target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if (this.Path != target.Path) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        private sealed class CustomFormatter : IRocketPackFormatter<GetFileMetadatasRequestBody>
        {
            public void Serialize(RocketPackWriter w, GetFileMetadatasRequestBody value, int rank)
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

            public GetFileMetadatasRequestBody Deserialize(RocketPackReader r, int rank)
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

                return new GetFileMetadatasRequestBody(p_path);
            }
        }
    }

    public sealed partial class GetFileMetadatasResponseBody : RocketPackMessageBase<GetFileMetadatasResponseBody>
    {
        static GetFileMetadatasResponseBody()
        {
            GetFileMetadatasResponseBody.Formatter = new CustomFormatter();
        }

        public static readonly int MaxFileMetadatasCount = 8192;

        public GetFileMetadatasResponseBody(IList<FileMetadata> fileMetadatas)
        {
            if (fileMetadatas is null) throw new ArgumentNullException("fileMetadatas");
            if (fileMetadatas.Count > 8192) throw new ArgumentOutOfRangeException("fileMetadatas");
            foreach (var n in fileMetadatas)
            {
                if (n is null) throw new ArgumentNullException("n");
            }

            this.FileMetadatas = new ReadOnlyCollection<FileMetadata>(fileMetadatas);

            {
                var hashCode = new HashCode();
                foreach (var n in this.FileMetadatas)
                {
                    if (n != default) hashCode.Add(n.GetHashCode());
                }
                _hashCode = hashCode.ToHashCode();
            }
        }

        public IReadOnlyList<FileMetadata> FileMetadatas { get; }

        public override bool Equals(GetFileMetadatasResponseBody target)
        {
            if ((object)target == null) return false;
            if (Object.ReferenceEquals(this, target)) return true;
            if ((this.FileMetadatas is null) != (target.FileMetadatas is null)) return false;
            if (!(this.FileMetadatas is null) && !(target.FileMetadatas is null) && !CollectionHelper.Equals(this.FileMetadatas, target.FileMetadatas)) return false;

            return true;
        }

        private readonly int _hashCode;
        public override int GetHashCode() => _hashCode;

        private sealed class CustomFormatter : IRocketPackFormatter<GetFileMetadatasResponseBody>
        {
            public void Serialize(RocketPackWriter w, GetFileMetadatasResponseBody value, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Write property count
                {
                    int propertyCount = 0;
                    if (value.FileMetadatas.Count != 0) propertyCount++;
                    w.Write((ulong)propertyCount);
                }

                // FileMetadatas
                if (value.FileMetadatas.Count != 0)
                {
                    w.Write((ulong)0);
                    w.Write((ulong)value.FileMetadatas.Count);
                    foreach (var n in value.FileMetadatas)
                    {
                        FileMetadata.Formatter.Serialize(w, n, rank + 1);
                    }
                }
            }

            public GetFileMetadatasResponseBody Deserialize(RocketPackReader r, int rank)
            {
                if (rank > 256) throw new FormatException();

                // Read property count
                int propertyCount = (int)r.GetUInt64();

                IList<FileMetadata> p_fileMetadatas = default;

                for (; propertyCount > 0; propertyCount--)
                {
                    int id = (int)r.GetUInt64();
                    switch (id)
                    {
                        case 0: // FileMetadatas
                            {
                                var length = (int)r.GetUInt64();
                                p_fileMetadatas = new FileMetadata[length];
                                for (int i = 0; i < p_fileMetadatas.Count; i++)
                                {
                                    p_fileMetadatas[i] = FileMetadata.Formatter.Deserialize(r, rank + 1);
                                }
                                break;
                            }
                    }
                }

                return new GetFileMetadatasResponseBody(p_fileMetadatas);
            }
        }
    }

}
