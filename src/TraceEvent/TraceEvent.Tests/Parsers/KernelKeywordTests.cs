using Microsoft.Diagnostics.Tracing.Parsers;
using Xunit;

namespace TraceEventTests
{
    public class KernelKeywordTests
    {
        [Fact]
        public void VAMapDoesNotRequireTheDedicatedKernelSession()
        {
            Assert.Equal(KernelTraceEventParser.Keywords.None,
                KernelTraceEventParser.NonOSKeywords & KernelTraceEventParser.Keywords.VAMap);
        }

        [Fact]
        public void SpecializedKeywordsStillRequireTheDedicatedKernelSession()
        {
            var specialized = KernelTraceEventParser.Keywords.PMCProfile |
                KernelTraceEventParser.Keywords.ReferenceSet |
                KernelTraceEventParser.Keywords.ThreadPriority |
                KernelTraceEventParser.Keywords.IOQueue |
                KernelTraceEventParser.Keywords.Handle;
            Assert.Equal(specialized, KernelTraceEventParser.NonOSKeywords & specialized);
        }
    }
}
