using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Session;
using System;
using System.Collections.Generic;
using System.IO;
using Xunit;
using Xunit.Abstractions;

namespace TraceEventTests
{
    public class MergeTests : TestBase
    {
        public MergeTests(ITestOutputHelper output) : base(output)
        {
        }

        [WindowsFact]
        public void MergeOptionsPreserveEvents()
        {
            Directory.CreateDirectory(OutputDir);
            string input = Path.Combine("inputs", "Regression", "SelfDescribingSingleEvent.etl");
            var expected = ReadEvents(input);
            Assert.NotEmpty(expected);
            foreach (var option in new[] { TraceEventMergeOptions.None, TraceEventMergeOptions.Compress, TraceEventMergeOptions.ImageIDsOnly })
            {
                string output = Path.Combine(OutputDir, option + ".etl");
                TraceEventSession.Merge(new[] { input }, output, option);
                Assert.Equal(expected, ReadEvents(output));
            }
        }

        private static List<string> ReadEvents(string path)
        {
            var events = new List<string>();
            using (var source = new ETWTraceEventSource(path))
            {
                source.AllEvents += data =>
                {
                    if (!data.IsClassicProvider)
                    {
                        events.Add(data.ProviderGuid + ":" + data.ID + ":" + Convert.ToBase64String(data.EventData()));
                    }
                };
                source.Process();
            }
            return events;
        }
    }
}
