using Microsoft.Diagnostics.Utilities;
using System;
using System.Runtime.CompilerServices;

namespace System.Runtime.CompilerServices
{
    // File-local so friend assemblies can use their runtime's attribute without a type conflict.
    [AttributeUsage(AttributeTargets.Method, Inherited = false)]
    file sealed class ModuleInitializerAttribute : Attribute
    {
    }
}

namespace Microsoft.Diagnostics.Tracing
{
    internal static class PlatformInitializer
    {
        [ModuleInitializer]
        internal static void Initialize()
        {
            OperatingSystemVersion.EnsureSupported();
        }
    }
}
