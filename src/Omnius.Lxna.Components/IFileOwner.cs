using System;
using Omnius.Lxna.Components.Models;

namespace Omnius.Lxna.Components
{
    public interface IFileOwner : IDisposable
    {
        public string Path { get; }
    }
}
