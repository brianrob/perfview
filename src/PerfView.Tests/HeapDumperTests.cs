using PerfView;
using System;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using Xunit;

namespace PerfViewTests
{
    public class HeapDumperTests
    {
        [Fact]
        public void DetectsX64Target()
        {
            using (var process = Process.GetCurrentProcess())
            {
                Assert.Equal(ProcessorArchitecture.Amd64, GetTargetArchitecture(process));
            }
        }

        [Fact]
        public void DetectsX86TargetFromX64Host()
        {
            var startInfo = new ProcessStartInfo(
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.SystemX86), "cmd.exe"),
                "/d /c set /p PERFVIEW_TEST=")
            {
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardInput = true
            };
            using (var process = Process.Start(startInfo))
            {
                try
                {
                    Assert.Equal(ProcessorArchitecture.X86, GetTargetArchitecture(process));
                }
                finally
                {
                    process.StandardInput.Close();
                    if (!process.WaitForExit(5000))
                    {
                        process.Kill();
                        process.WaitForExit();
                    }
                }
            }
        }

        private static ProcessorArchitecture GetTargetArchitecture(Process process)
        {
            var method = typeof(HeapDumper).GetMethod("GetArchForProcess", BindingFlags.NonPublic | BindingFlags.Static);
            return (ProcessorArchitecture)method.Invoke(null, new object[] { process.Id });
        }
    }
}
