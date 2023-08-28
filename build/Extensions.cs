using System;

using JetBrains.Annotations;

using Nuke.Common.IO;
using Nuke.Common.ProjectModel;

namespace DefaultNamespace;

public static class Extensions
{
    public static AbsolutePath GetOutputDirectory([NotNull] this Project project, [NotNull] string configuration)
    {
        if (project == null)
        {
            throw new ArgumentNullException(nameof(project));
        }

        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        return project.Directory / "bin" / configuration;
    }
}
