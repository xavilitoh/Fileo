using System;

namespace Fileo.Core.Interfaces
{
    public interface IKnownFolderDetector
    {
        string? GetKnownFolderPath(KnownFolder folder);
    }

    public enum KnownFolder { Downloads, Documents }
}
