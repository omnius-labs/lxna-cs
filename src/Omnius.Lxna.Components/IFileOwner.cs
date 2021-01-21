using System;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public interface IFileOwner : IAsyncDisposable
    {
        public string Path { get; }
    }
}
