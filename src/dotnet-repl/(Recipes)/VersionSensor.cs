using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;

#nullable disable

namespace Recipes
{
#if !RecipesProject
    [DebuggerStepThrough]
#endif
    internal partial class VersionSensor
    {
        private static readonly Lazy<BuildInfo> buildInfo = new(() =>
        {
            var assembly = typeof(VersionSensor).GetTypeInfo().Assembly!;

            var info = new BuildInfo
            {
                AssemblyName = assembly.GetName().Name,
                AssemblyInformationalVersion = assembly
                                               .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
                                               .InformationalVersion,
                AssemblyVersion = assembly.GetName().Version.ToString(),
                BuildDate = new FileInfo(assembly.Location).CreationTimeUtc.ToString("o")
            };

            return info;
        });

        public static BuildInfo Version()
        {
            return buildInfo.Value;
        }

        public record BuildInfo
        {
            public string AssemblyVersion { get; init; }
            public string BuildDate { get; init; }
            public string AssemblyInformationalVersion { get; init; }
            public string AssemblyName { get; init; }
        }
    }
}