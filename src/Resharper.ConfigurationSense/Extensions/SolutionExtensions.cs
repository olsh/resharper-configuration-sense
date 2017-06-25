using System.Collections.Generic;

using JetBrains.Annotations;
using JetBrains.ProjectModel;

namespace Resharper.ConfigurationSense.Extensions
{
    public static class SolutionExtensions
    {
        [NotNull]
        public static IEnumerable<string> GetAdditionalConfigurationFiles(this ISolution solution)
        {
            return solution.GetSettingsStore()
                .GetAdditionalConfigurationFiles(solution.GetId());
        }

        [NotNull]
        public static string GetId(this ISolution solution)
        {
            if (solution.SolutionFile == null)
            {
                return solution.SolutionProject.GetPersistentID();
            }

            return solution.SolutionFile.GetPersistentID();
        }
    }
}
