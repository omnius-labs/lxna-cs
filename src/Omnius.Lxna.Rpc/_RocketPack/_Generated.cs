using Omnius.Core.Cryptography;
using Omnius.Core.Network;
using Omnius.Lxna.Components.Models;

#nullable enable

namespace Omnius.Lxna.Rpc
{

    public readonly partial struct GetFilesParam : global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesParam>
    {
        public static global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Rpc.GetFilesParam> Formatter => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesParam>.Formatter;
        public static global::Omnius.Lxna.Rpc.GetFilesParam Empty => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesParam>.Empty;

        static GetFilesParam()
        {
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesParam>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesParam>.Empty = new global::Omnius.Lxna.Rpc.GetFilesParam(string.Empty, false);
        }

        private readonly int ___hashCode;

        public static readonly int MaxPatternLength = 2147483647;

        public GetFilesParam(string pattern, bool ignoreCase)
        {
            if (pattern is null) throw new global::System.ArgumentNullException("pattern");
            if (pattern.Length > 2147483647) throw new global::System.ArgumentOutOfRangeException("pattern");
            this.Pattern = pattern;
            this.IgnoreCase = ignoreCase;

            {
                var ___h = new global::System.HashCode();
                if (pattern != default) ___h.Add(pattern.GetHashCode());
                if (ignoreCase != default) ___h.Add(ignoreCase.GetHashCode());
                ___hashCode = ___h.ToHashCode();
            }
        }

        public string Pattern { get; }
        public bool IgnoreCase { get; }

        public static global::Omnius.Lxna.Rpc.GetFilesParam Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.RocketPack.RocketPackObjectReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.RocketPack.RocketPackObjectWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(global::Omnius.Lxna.Rpc.GetFilesParam left, global::Omnius.Lxna.Rpc.GetFilesParam right)
        {
            return right.Equals(left);
        }
        public static bool operator !=(global::Omnius.Lxna.Rpc.GetFilesParam left, global::Omnius.Lxna.Rpc.GetFilesParam right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is global::Omnius.Lxna.Rpc.GetFilesParam)) return false;
            return this.Equals((global::Omnius.Lxna.Rpc.GetFilesParam)other);
        }
        public bool Equals(global::Omnius.Lxna.Rpc.GetFilesParam target)
        {
            if (this.Pattern != target.Pattern) return false;
            if (this.IgnoreCase != target.IgnoreCase) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode;

        private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Rpc.GetFilesParam>
        {
            public void Serialize(ref global::Omnius.Core.RocketPack.RocketPackObjectWriter w, in global::Omnius.Lxna.Rpc.GetFilesParam value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                w.Write(value.Pattern);
                w.Write(value.IgnoreCase);
            }

            public global::Omnius.Lxna.Rpc.GetFilesParam Deserialize(ref global::Omnius.Core.RocketPack.RocketPackObjectReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                string p_pattern = string.Empty;
                bool p_ignoreCase = false;

                {
                    p_pattern = r.GetString(2147483647);
                }
                {
                    p_ignoreCase = r.GetBoolean();
                }
                return new global::Omnius.Lxna.Rpc.GetFilesParam(p_pattern, p_ignoreCase);
            }
        }
    }
    public readonly partial struct GetFilesResult : global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesResult>
    {
        public static global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Rpc.GetFilesResult> Formatter => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesResult>.Formatter;
        public static global::Omnius.Lxna.Rpc.GetFilesResult Empty => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesResult>.Empty;

        static GetFilesResult()
        {
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesResult>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetFilesResult>.Empty = new global::Omnius.Lxna.Rpc.GetFilesResult(global::System.Array.Empty<string>());
        }

        private readonly int ___hashCode;

        public static readonly int MaxPathsCount = 2147483647;

        public GetFilesResult(string[] paths)
        {
            if (paths is null) throw new global::System.ArgumentNullException("paths");
            if (paths.Length > 2147483647) throw new global::System.ArgumentOutOfRangeException("paths");
            foreach (var n in paths)
            {
                if (n is null) throw new global::System.ArgumentNullException("n");
                if (n.Length > 2147483647) throw new global::System.ArgumentOutOfRangeException("n");
            }

            this.Paths = new global::Omnius.Core.Collections.ReadOnlyListSlim<string>(paths);

            {
                var ___h = new global::System.HashCode();
                foreach (var n in paths)
                {
                    if (n != default) ___h.Add(n.GetHashCode());
                }
                ___hashCode = ___h.ToHashCode();
            }
        }

        public global::Omnius.Core.Collections.ReadOnlyListSlim<string> Paths { get; }

        public static global::Omnius.Lxna.Rpc.GetFilesResult Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.RocketPack.RocketPackObjectReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.RocketPack.RocketPackObjectWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(global::Omnius.Lxna.Rpc.GetFilesResult left, global::Omnius.Lxna.Rpc.GetFilesResult right)
        {
            return right.Equals(left);
        }
        public static bool operator !=(global::Omnius.Lxna.Rpc.GetFilesResult left, global::Omnius.Lxna.Rpc.GetFilesResult right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is global::Omnius.Lxna.Rpc.GetFilesResult)) return false;
            return this.Equals((global::Omnius.Lxna.Rpc.GetFilesResult)other);
        }
        public bool Equals(global::Omnius.Lxna.Rpc.GetFilesResult target)
        {
            if (!global::Omnius.Core.Helpers.CollectionHelper.Equals(this.Paths, target.Paths)) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode;

        private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Rpc.GetFilesResult>
        {
            public void Serialize(ref global::Omnius.Core.RocketPack.RocketPackObjectWriter w, in global::Omnius.Lxna.Rpc.GetFilesResult value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                w.Write((uint)value.Paths.Count);
                foreach (var n in value.Paths)
                {
                    w.Write(n);
                }
            }

            public global::Omnius.Lxna.Rpc.GetFilesResult Deserialize(ref global::Omnius.Core.RocketPack.RocketPackObjectReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                string[] p_paths = global::System.Array.Empty<string>();

                {
                    var length = r.GetUInt32();
                    p_paths = new string[length];
                    for (int i = 0; i < p_paths.Length; i++)
                    {
                        p_paths[i] = r.GetString(2147483647);
                    }
                }
                return new global::Omnius.Lxna.Rpc.GetFilesResult(p_paths);
            }
        }
    }
    public readonly partial struct GetDirectoriesParam : global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesParam>
    {
        public static global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Rpc.GetDirectoriesParam> Formatter => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesParam>.Formatter;
        public static global::Omnius.Lxna.Rpc.GetDirectoriesParam Empty => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesParam>.Empty;

        static GetDirectoriesParam()
        {
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesParam>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesParam>.Empty = new global::Omnius.Lxna.Rpc.GetDirectoriesParam(string.Empty, false);
        }

        private readonly int ___hashCode;

        public static readonly int MaxPatternLength = 2147483647;

        public GetDirectoriesParam(string pattern, bool ignoreCase)
        {
            if (pattern is null) throw new global::System.ArgumentNullException("pattern");
            if (pattern.Length > 2147483647) throw new global::System.ArgumentOutOfRangeException("pattern");
            this.Pattern = pattern;
            this.IgnoreCase = ignoreCase;

            {
                var ___h = new global::System.HashCode();
                if (pattern != default) ___h.Add(pattern.GetHashCode());
                if (ignoreCase != default) ___h.Add(ignoreCase.GetHashCode());
                ___hashCode = ___h.ToHashCode();
            }
        }

        public string Pattern { get; }
        public bool IgnoreCase { get; }

        public static global::Omnius.Lxna.Rpc.GetDirectoriesParam Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.RocketPack.RocketPackObjectReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.RocketPack.RocketPackObjectWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(global::Omnius.Lxna.Rpc.GetDirectoriesParam left, global::Omnius.Lxna.Rpc.GetDirectoriesParam right)
        {
            return right.Equals(left);
        }
        public static bool operator !=(global::Omnius.Lxna.Rpc.GetDirectoriesParam left, global::Omnius.Lxna.Rpc.GetDirectoriesParam right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is global::Omnius.Lxna.Rpc.GetDirectoriesParam)) return false;
            return this.Equals((global::Omnius.Lxna.Rpc.GetDirectoriesParam)other);
        }
        public bool Equals(global::Omnius.Lxna.Rpc.GetDirectoriesParam target)
        {
            if (this.Pattern != target.Pattern) return false;
            if (this.IgnoreCase != target.IgnoreCase) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode;

        private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Rpc.GetDirectoriesParam>
        {
            public void Serialize(ref global::Omnius.Core.RocketPack.RocketPackObjectWriter w, in global::Omnius.Lxna.Rpc.GetDirectoriesParam value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                w.Write(value.Pattern);
                w.Write(value.IgnoreCase);
            }

            public global::Omnius.Lxna.Rpc.GetDirectoriesParam Deserialize(ref global::Omnius.Core.RocketPack.RocketPackObjectReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                string p_pattern = string.Empty;
                bool p_ignoreCase = false;

                {
                    p_pattern = r.GetString(2147483647);
                }
                {
                    p_ignoreCase = r.GetBoolean();
                }
                return new global::Omnius.Lxna.Rpc.GetDirectoriesParam(p_pattern, p_ignoreCase);
            }
        }
    }
    public readonly partial struct GetDirectoriesResult : global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesResult>
    {
        public static global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Rpc.GetDirectoriesResult> Formatter => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesResult>.Formatter;
        public static global::Omnius.Lxna.Rpc.GetDirectoriesResult Empty => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesResult>.Empty;

        static GetDirectoriesResult()
        {
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesResult>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Rpc.GetDirectoriesResult>.Empty = new global::Omnius.Lxna.Rpc.GetDirectoriesResult(global::System.Array.Empty<string>());
        }

        private readonly int ___hashCode;

        public static readonly int MaxPathsCount = 2147483647;

        public GetDirectoriesResult(string[] paths)
        {
            if (paths is null) throw new global::System.ArgumentNullException("paths");
            if (paths.Length > 2147483647) throw new global::System.ArgumentOutOfRangeException("paths");
            foreach (var n in paths)
            {
                if (n is null) throw new global::System.ArgumentNullException("n");
                if (n.Length > 2147483647) throw new global::System.ArgumentOutOfRangeException("n");
            }

            this.Paths = new global::Omnius.Core.Collections.ReadOnlyListSlim<string>(paths);

            {
                var ___h = new global::System.HashCode();
                foreach (var n in paths)
                {
                    if (n != default) ___h.Add(n.GetHashCode());
                }
                ___hashCode = ___h.ToHashCode();
            }
        }

        public global::Omnius.Core.Collections.ReadOnlyListSlim<string> Paths { get; }

        public static global::Omnius.Lxna.Rpc.GetDirectoriesResult Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.RocketPack.RocketPackObjectReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.RocketPack.RocketPackObjectWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(global::Omnius.Lxna.Rpc.GetDirectoriesResult left, global::Omnius.Lxna.Rpc.GetDirectoriesResult right)
        {
            return right.Equals(left);
        }
        public static bool operator !=(global::Omnius.Lxna.Rpc.GetDirectoriesResult left, global::Omnius.Lxna.Rpc.GetDirectoriesResult right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is global::Omnius.Lxna.Rpc.GetDirectoriesResult)) return false;
            return this.Equals((global::Omnius.Lxna.Rpc.GetDirectoriesResult)other);
        }
        public bool Equals(global::Omnius.Lxna.Rpc.GetDirectoriesResult target)
        {
            if (!global::Omnius.Core.Helpers.CollectionHelper.Equals(this.Paths, target.Paths)) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode;

        private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Rpc.GetDirectoriesResult>
        {
            public void Serialize(ref global::Omnius.Core.RocketPack.RocketPackObjectWriter w, in global::Omnius.Lxna.Rpc.GetDirectoriesResult value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                w.Write((uint)value.Paths.Count);
                foreach (var n in value.Paths)
                {
                    w.Write(n);
                }
            }

            public global::Omnius.Lxna.Rpc.GetDirectoriesResult Deserialize(ref global::Omnius.Core.RocketPack.RocketPackObjectReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                string[] p_paths = global::System.Array.Empty<string>();

                {
                    var length = r.GetUInt32();
                    p_paths = new string[length];
                    for (int i = 0; i < p_paths.Length; i++)
                    {
                        p_paths[i] = r.GetString(2147483647);
                    }
                }
                return new global::Omnius.Lxna.Rpc.GetDirectoriesResult(p_paths);
            }
        }
    }

    public interface ILxnaService
    {
        global::System.Threading.Tasks.ValueTask<global::Omnius.Lxna.Rpc.GetFilesResult> GetFilesAsync(global::Omnius.Lxna.Rpc.GetFilesParam param, global::System.Threading.CancellationToken cancellationToken);
        global::System.Threading.Tasks.ValueTask<global::Omnius.Lxna.Rpc.GetDirectoriesResult> GetDirectoriesAsync(global::Omnius.Lxna.Rpc.GetDirectoriesParam param, global::System.Threading.CancellationToken cancellationToken);
    }
    public class LxnaServiceSender : global::Omnius.Core.AsyncDisposableBase, global::Omnius.Lxna.Rpc.ILxnaService
    {
        private readonly global::Omnius.Lxna.Rpc.ILxnaService _impl;
        private readonly global::Omnius.Core.Network.Connections.IConnection _connection;
        private readonly global::Omnius.Core.IBytesPool _bytesPool;
        private readonly global::Omnius.Core.RocketPack.Remoting.RocketPackRpc _rpc;
        public LxnaServiceSender(global::Omnius.Lxna.Rpc.ILxnaService impl, global::Omnius.Core.Network.Connections.IConnection connection, global::Omnius.Core.IBytesPool bytesPool)
        {
            _impl = impl;
            _connection = connection;
            _bytesPool = bytesPool;
            _rpc = new global::Omnius.Core.RocketPack.Remoting.RocketPackRpc(_connection, _bytesPool);
        }
        protected override async global::System.Threading.Tasks.ValueTask OnDisposeAsync()
        {
            await _rpc.DisposeAsync();
        }
        public async global::System.Threading.Tasks.ValueTask<global::Omnius.Lxna.Rpc.GetFilesResult> GetFilesAsync(global::Omnius.Lxna.Rpc.GetFilesParam param, global::System.Threading.CancellationToken cancellationToken)
        {
            using var stream = await _rpc.ConnectAsync(0, cancellationToken);
            return await stream.CallFunctionAsync<global::Omnius.Lxna.Rpc.GetFilesParam, global::Omnius.Lxna.Rpc.GetFilesResult>(param, cancellationToken);
        }
        public async global::System.Threading.Tasks.ValueTask<global::Omnius.Lxna.Rpc.GetDirectoriesResult> GetDirectoriesAsync(global::Omnius.Lxna.Rpc.GetDirectoriesParam param, global::System.Threading.CancellationToken cancellationToken)
        {
            using var stream = await _rpc.ConnectAsync(1, cancellationToken);
            return await stream.CallFunctionAsync<global::Omnius.Lxna.Rpc.GetDirectoriesParam, global::Omnius.Lxna.Rpc.GetDirectoriesResult>(param, cancellationToken);
        }
    }
    public class LxnaServiceReceiver : global::Omnius.Core.AsyncDisposableBase
    {
        private readonly global::Omnius.Lxna.Rpc.ILxnaService _impl;
        private readonly global::Omnius.Core.Network.Connections.IConnection _connection;
        private readonly global::Omnius.Core.IBytesPool _bytesPool;
        private readonly global::Omnius.Core.RocketPack.Remoting.RocketPackRpc _rpc;
        public LxnaServiceReceiver(global::Omnius.Lxna.Rpc.ILxnaService impl, global::Omnius.Core.Network.Connections.IConnection connection, global::Omnius.Core.IBytesPool bytesPool)
        {
            _impl = impl;
            _connection = connection;
            _bytesPool = bytesPool;
            _rpc = new global::Omnius.Core.RocketPack.Remoting.RocketPackRpc(_connection, _bytesPool);
        }
        protected override async global::System.Threading.Tasks.ValueTask OnDisposeAsync()
        {
            await _rpc.DisposeAsync();
        }
        public async global::System.Threading.Tasks.Task EventLoop(global::System.Threading.CancellationToken cancellationToken = default)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                cancellationToken.ThrowIfCancellationRequested();
                using var stream = await _rpc.AcceptAsync(cancellationToken);
                switch (stream.CallId)
                {
                    case 0:
                        {
                            await stream.ListenFunctionAsync<global::Omnius.Lxna.Rpc.GetFilesParam, global::Omnius.Lxna.Rpc.GetFilesResult>(_impl.GetFilesAsync, cancellationToken);
                        }
                        break;
                    case 1:
                        {
                            await stream.ListenFunctionAsync<global::Omnius.Lxna.Rpc.GetDirectoriesParam, global::Omnius.Lxna.Rpc.GetDirectoriesResult>(_impl.GetDirectoriesAsync, cancellationToken);
                        }
                        break;
                }
            }
        }
    }

}
