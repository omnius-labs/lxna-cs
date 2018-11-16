using System;
using System.Collections.Generic;
using System.Text;

namespace Lxna.Messages
{
    public enum ContentType
    {
        Directory,
        File,
    }

    public sealed class ContentMetadata
    {
        public string Path { get; set; }
        public ContentType Type { get; set; }
    }
}
