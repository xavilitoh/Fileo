using System;
using System.Collections.Generic;

namespace Fileo.Core.Interfaces
{
    public interface ICategoryProcessor
    {
        int ProcessCategory(string srcDir, string destName, Func<string,bool> matcher, bool includeDirs = false, bool flatten = false, bool dryRun = false, IProgressReporter? progress = null);
        void NormalizeCategories(string srcDir, List<(string name, Func<string,bool> matcher, bool includeDirs, bool flatten)> categories, bool dryRun = false, IProgressReporter? progress = null);
    }
}
