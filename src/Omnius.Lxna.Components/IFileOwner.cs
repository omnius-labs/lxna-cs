using System;

namespace Omnius.Lxna.Components
{
    public interface IFileOwner : IDisposable
    {
        public string Path { get; }
    }
}
