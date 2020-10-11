
#nullable enable

namespace Omnius.Lxna.Components
{

    public sealed partial class TestObject : global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Tests.TestObject>
    {
        public static global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Components.Tests.TestObject> Formatter => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Tests.TestObject>.Formatter;
        public static global::Omnius.Lxna.Components.Tests.TestObject Empty => global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Tests.TestObject>.Empty;

        static TestObject()
        {
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Tests.TestObject>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.RocketPack.IRocketPackObject<global::Omnius.Lxna.Components.Tests.TestObject>.Empty = new global::Omnius.Lxna.Components.Tests.TestObject(0);
        }

        private readonly global::System.Lazy<int> ___hashCode;

        public TestObject(int value)
        {
            this.Value = value;

            ___hashCode = new global::System.Lazy<int>(() =>
            {
                var ___h = new global::System.HashCode();
                if (value != default) ___h.Add(value.GetHashCode());
                return ___h.ToHashCode();
            });
        }

        public int Value { get; }

        public static global::Omnius.Lxna.Components.Tests.TestObject Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.RocketPack.RocketPackObjectReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.RocketPack.RocketPackObjectWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(global::Omnius.Lxna.Components.Tests.TestObject? left, global::Omnius.Lxna.Components.Tests.TestObject? right)
        {
            return (right is null) ? (left is null) : right.Equals(left);
        }
        public static bool operator !=(global::Omnius.Lxna.Components.Tests.TestObject? left, global::Omnius.Lxna.Components.Tests.TestObject? right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is global::Omnius.Lxna.Components.Tests.TestObject)) return false;
            return this.Equals((global::Omnius.Lxna.Components.Tests.TestObject)other);
        }
        public bool Equals(global::Omnius.Lxna.Components.Tests.TestObject? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (this.Value != target.Value) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode.Value;

        private sealed class ___CustomFormatter : global::Omnius.Core.RocketPack.IRocketPackObjectFormatter<global::Omnius.Lxna.Components.Tests.TestObject>
        {
            public void Serialize(ref global::Omnius.Core.RocketPack.RocketPackObjectWriter w, in global::Omnius.Lxna.Components.Tests.TestObject value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                if (value.Value != 0)
                {
                    w.Write((uint)1);
                    w.Write(value.Value);
                }
                w.Write((uint)0);
            }

            public global::Omnius.Lxna.Components.Tests.TestObject Deserialize(ref global::Omnius.Core.RocketPack.RocketPackObjectReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                int p_value = 0;

                for (; ; )
                {
                    uint id = r.GetUInt32();
                    if (id == 0) break;
                    switch (id)
                    {
                        case 1:
                            {
                                p_value = r.GetInt32();
                                break;
                            }
                    }
                }

                return new global::Omnius.Lxna.Components.Tests.TestObject(p_value);
            }
        }
    }


}
