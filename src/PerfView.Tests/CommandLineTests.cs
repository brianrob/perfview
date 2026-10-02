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
    }
}
