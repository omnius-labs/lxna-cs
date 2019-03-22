using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Omnix.Base;
using Omnix.Io;
using Omnix.Serialization.RocketPack;

namespace Lxna.Core.Internal
{
   public static class RocketPackHelper
    {
        public static T StreamToMessage<T>(Stream inStream)
            where T : RocketPackMessageBase<T>
        {
            var hub = new Hub();

            try
            {
                const int bufferSize = 4096;

                while (inStream.Position < inStream.Length)
                {
                    var readLength = inStream.Read(hub.Writer.GetSpan(bufferSize));
                    if (readLength < 0) break;

                    hub.Writer.Advance(readLength);
                }

               return RocketPackMessageBase<T>.Import(hub.Reader.GetSequence(), BufferPool.Shared);
            }
            finally
            {
                hub.Reset();
            }
        }

        public static void MessageToStream<T>(T message, Stream outStream)
            where T : RocketPackMessageBase<T>
        {
            Stream stream = null;
            var hub = new Hub();

            try
            {
                message.Export(hub.Writer, BufferPool.Shared);
                hub.Writer.Complete();

                var sequence = hub.Reader.GetSequence();
                var position = sequence.Start;

                stream = new RecyclableMemoryStream(BufferPool.Shared);

                while (sequence.TryGet(ref position, out var memory))
                {
                    stream.Write(memory.Span);
                }

                stream.Seek(0, SeekOrigin.Begin);

                hub.Reader.Complete();
            }
            finally
            {
                hub.Reset();
            }
        }
    }
}
