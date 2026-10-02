using Microsoft.Diagnostics.Utilities;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Xunit;

namespace TraceEventTests
{
    public class OperatingSystemVersionTests
    {
        [Theory]
        [InlineData(5, false)]
        [InlineData(6, false)]
        [InlineData(10, true)]
        [InlineData(11, true)]
        public void WindowsMinimumUsesMajorVersion(uint major, bool supported)
        {
            Assert.Equal(supported, OperatingSystemVersion.IsSupportedPlatform(PlatformID.Win32NT, () => major));
        }

        [Theory]
        [InlineData(PlatformID.Unix)]
        [InlineData(PlatformID.MacOSX)]
        public void OtherPlatformsNeverQueryWindows(PlatformID platform)
        {
            Assert.True(OperatingSystemVersion.IsSupportedPlatform(platform,
                () => throw new InvalidOperationException("Windows API must not be called.")));
        }

        [Fact]
        public void NativeVersionQueryFailureIsNotIgnored()
        {
            Assert.Throws<Win32Exception>(() => OperatingSystemVersion.CheckVersionQueryStatus(unchecked((int)0xC0000001)));
            OperatingSystemVersion.CheckVersionQueryStatus(0);
        }

        [Fact]
        public void CurrentHostPassedModuleInitialization()
        {
            Assert.True(OperatingSystemVersion.IsSupported);
        }

        [Fact]
        public void EtwFilteringIsOnlyAvailableOnWindows()
        {
            Assert.Equal(RuntimeInformation.IsOSPlatform(OSPlatform.Windows), TraceEventProviderOptions.FilteringSupported);
        }

        [Fact]
        public void ModuleConstructorCallsPlatformInitializer()
        {
            using (var stream = File.OpenRead(typeof(OperatingSystemVersion).Assembly.Location))
            using (var pe = new PEReader(stream))
            {
                var metadata = pe.GetMetadataReader();
                var module = metadata.TypeDefinitions.Single(handle =>
                    metadata.GetString(metadata.GetTypeDefinition(handle).Name) == "<Module>");
                var constructor = metadata.GetTypeDefinition(module).GetMethods().Single(handle =>
                    metadata.GetString(metadata.GetMethodDefinition(handle).Name) == ".cctor");
                var initializer = metadata.MethodDefinitions.Single(handle =>
                {
                    var method = metadata.GetMethodDefinition(handle);
                    return metadata.GetString(method.Name) == "Initialize" &&
                        metadata.GetString(metadata.GetTypeDefinition(method.GetDeclaringType()).Name) == "PlatformInitializer";
                });
                var il = pe.GetMethodBody(metadata.GetMethodDefinition(constructor).RelativeVirtualAddress).GetILBytes();
                Assert.Contains(Enumerable.Range(0, il.Length - 4), offset =>
                    il[offset] == 0x28 && BitConverter.ToInt32(il, offset + 1) == MetadataTokens.GetToken(initializer));
            }
        }
    }
}
