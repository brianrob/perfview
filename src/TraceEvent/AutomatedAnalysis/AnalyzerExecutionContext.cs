using Microsoft.Diagnostics.Tracing.Stacks;

namespace Microsoft.Diagnostics.Tracing.AutomatedAnalysis
{
    /// <summary>
    /// The top-level object used to store contextual information during Analyzer execution.
    /// </summary>
    public sealed class AnalyzerExecutionContext
    {
        private Configuration _configuration;

        internal AnalyzerExecutionContext(Configuration configuration, ITrace trace)
        {
            _configuration = configuration;
            Trace = trace;
        }

        /// <summary>
        /// The configuration for the currently executing Analyzer.
        /// NULL if no configuration is available.
        /// </summary>
        public AnalyzerConfiguration Configuration
        {
            get
            {
                AnalyzerExecutionScope current = AnalyzerExecutionScope.Current;
                if (current != null)
                {
                    if (_configuration.TryGetAnalyzerConfiguration(current.ExecutingAnalyzer, out AnalyzerConfiguration config))
                    {
                        return config;
                    }
                }

                return null;
            }
        }

        /// <summary>
        /// The trace to be analyzed.
        /// </summary>
        public ITrace Trace { get; }

        internal AnalyzerIssueCollection Issues { get; } = new AnalyzerIssueCollection();

        // TODO: Create a new type (e.g. AnalyzerIssueStackSource) that encapsulates an InternStackSource.
        // - Responsible for copying stacks and samples from the actual trace data that is relevant to the issue.
        // - Knows how to partition the stack data based on the issue that it belongs to (probably just add a pseudo node that contains the ID).
        // - Also partitions based off of the process UniqueID, which is required whenever someone adds an issue via AddIssue either here or in ProcessContext.
        // - Expose off of ProcessContext as well.
        // - Can hand out FilterStackSource instances that represent the stacks for a given issue instance (process/issue).
        // - The entire stack source can be handed out (as a FilterStackSource) if someone just wants to consume the entire thing.
        //      - One possible concern about this is that the internal representation (e.g. using the GUID) might not be what we want to pass out to callers.  Should we replace the GUID with string Title when passing out (or always use the Title)?
        // - The "add" method will need to know how to walk stacks from the input source (the actual data) so that it can intern the full call stack.
        // - Probably should start with a CallTreeNodeBase and convert back to the set of Samples.
        internal AnalyzerIssueStackSource IssueStacks { get; } = new AnalyzerIssueStackSource();

        /// <summary>
        /// Add an identified issue.
        /// </summary>
        /// <param name="process">The process associated with the issue.</param>
        /// <param name="issue">The issue.</param>
        public void AddIssue(Process process, AnalyzerIssue issue)
        {
            Issues[process].Add(issue);
        }

        public void AddIssueStack(Process process, AnalyzerIssue issue, StackSourceSample sample, StackSource source)
        {
            // Intern the stack from the sample/source into IssueStacks.
            StackSourceCallStackIndex index = sample.StackIndex;
            while (index != StackSourceCallStackIndex.Start)
            {
                StackSourceFrameIndex frameIndex = source.GetFrameIndex(index);
                string frameName = source.GetFrameName(frameIndex, true);
                index = source.GetCallerIndex(index);
            }
        }

    }
}
