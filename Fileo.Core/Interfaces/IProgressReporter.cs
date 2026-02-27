using System;

namespace Fileo.Core.Interfaces
{
    public interface IProgressReporter
    {
        void Report(string category, int current, int total);
    }
}
