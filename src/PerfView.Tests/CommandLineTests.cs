using PerfView;
using PerfViewTests.Utilities;
using Xunit;
using Xunit.Abstractions;

namespace PerfViewTests
{
    public class CommandLineTests : PerfViewTestBase
    {
        public CommandLineTests(ITestOutputHelper output) : base(output)
        {
        }

        [Fact]
        public void CustomSessionNamesArePreserved()
        {
            var userSession = CommandProcessor.s_UserModeSessionName;
            var kernelSession = CommandProcessor.s_KernelessionName;
            try
            {
                var args = new CommandLineArgs();
                args.ParseArgs(new[] { "/SessionName:PerfViewCommandLineTest", "start" });
                Assert.Null(args.CommandLineFailure);
                Assert.Equal("PerfViewCommandLineTest", CommandProcessor.s_UserModeSessionName);
                Assert.Equal("PerfViewCommandLineTestKernel", CommandProcessor.s_KernelessionName);
            }
            finally
            {
                CommandProcessor.s_UserModeSessionName = userSession;
                CommandProcessor.s_KernelessionName = kernelSession;
            }
        }
    }
}
