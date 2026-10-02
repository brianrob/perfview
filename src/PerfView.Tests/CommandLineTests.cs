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

        [Theory]
        [InlineData("EnableKernelStacks")]
        [InlineData("DisableKernelStacks")]
        public void KernelPagingCommandsAreRemoved(string command)
        {
            Assert.DoesNotContain(command, CommandLineArgs.GetHelpString(120));
            Assert.Null(typeof(CommandProcessor).GetMethod(command));
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
