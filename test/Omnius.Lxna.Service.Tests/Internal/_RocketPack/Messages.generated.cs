
#nullable enable

namespace Omnius.Lxna.Service
{
    public sealed partial class TestObject : global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<TestObject>
    {
        public static global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<TestObject> Formatter => global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<TestObject>.Formatter;
        public static TestObject Empty => global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<TestObject>.Empty;

        static TestObject()
        {
            global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<TestObject>.Formatter = new ___CustomFormatter();
            global::Omnius.Core.Serialization.RocketPack.IRocketPackObject<TestObject>.Empty = new TestObject(0);
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

        public static TestObject Import(global::System.Buffers.ReadOnlySequence<byte> sequence, global::Omnius.Core.IBytesPool bytesPool)
        {
            var reader = new global::Omnius.Core.Serialization.RocketPack.RocketPackReader(sequence, bytesPool);
            return Formatter.Deserialize(ref reader, 0);
        }
        public void Export(global::System.Buffers.IBufferWriter<byte> bufferWriter, global::Omnius.Core.IBytesPool bytesPool)
        {
            var writer = new global::Omnius.Core.Serialization.RocketPack.RocketPackWriter(bufferWriter, bytesPool);
            Formatter.Serialize(ref writer, this, 0);
        }

        public static bool operator ==(TestObject? left, TestObject? right)
        {
            return (right is null) ? (left is null) : right.Equals(left);
        }
        public static bool operator !=(TestObject? left, TestObject? right)
        {
            return !(left == right);
        }
        public override bool Equals(object? other)
        {
            if (!(other is TestObject)) return false;
            return this.Equals((TestObject)other);
        }
        public bool Equals(TestObject? target)
        {
            if (target is null) return false;
            if (object.ReferenceEquals(this, target)) return true;
            if (this.Value != target.Value) return false;

            return true;
        }
        public override int GetHashCode() => ___hashCode.Value;

        private sealed class ___CustomFormatter : global::Omnius.Core.Serialization.RocketPack.IRocketPackFormatter<TestObject>
        {
            public void Serialize(ref global::Omnius.Core.Serialization.RocketPack.RocketPackWriter w, in TestObject value, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                {
                    uint propertyCount = 0;
                    if (value.Value != 0)
                    {
                        propertyCount++;
                    }
                    w.Write(propertyCount);
                }

                if (value.Value != 0)
                {
                    w.Write((uint)0);
                    w.Write(value.Value);
                }
            }

            public TestObject Deserialize(ref global::Omnius.Core.Serialization.RocketPack.RocketPackReader r, in int rank)
            {
                if (rank > 256) throw new global::System.FormatException();

                uint propertyCount = r.GetUInt32();

                int p_value = 0;

                for (; propertyCount > 0; propertyCount--)
                {
                    uint id = r.GetUInt32();
                    switch (id)
                    {
                        case 0:
                            {
                                p_value = r.GetInt32();
                                break;
                            }
                    }
                }

                return new TestObject(p_value);
            }
        }
    }

}
