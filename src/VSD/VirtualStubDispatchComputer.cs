using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Diagnostics.Tracing;
using Microsoft.Diagnostics.Tracing.Parsers;
using Microsoft.Diagnostics.Tracing.Parsers.ClrPrivate;

namespace VSD
{
    internal sealed class VirtualStubDispatchComputer
    {
        private TraceEventDispatcher _source;
        private int _processID;
        private Dictionary<long, ResolveWorkerInfo> _resolveWorkerStats = new Dictionary<long, ResolveWorkerInfo>();
        private Dictionary<int, ResolveWorkerThreadState> _threadStateMap = new Dictionary<int, ResolveWorkerThreadState>();
        private SortedDictionary<long, long> _bucketIndexToSizeMap = new SortedDictionary<long, long>();

        internal VirtualStubDispatchComputer(TraceEventDispatcher source, int processID)
        {
            _source = source;
            _processID = processID;
        }

        internal void Process()
        {
            ClrPrivateTraceEventParser parser = new ClrPrivateTraceEventParser(_source);
            parser.VirtualStubDispatchResolveWorker += delegate (VSDResolveWorkerTraceData data)
            {
                if (_processID != data.ProcessID)
                {
                    return;
                }

                ResolveWorkerInfo info;
                if (!_resolveWorkerStats.TryGetValue(data.Token, out info))
                {
                    info = new ResolveWorkerInfo(data.TypeName, data.Token);
                    _resolveWorkerStats.Add(data.Token, info);
                }

                SetupThreadState(data, info);
            };
            parser.VirtualStubDispatchGenerateStubStart += delegate (VSDGenerateStubTraceData data)
            {
                if (_processID != data.ProcessID)
                {
                    return;
                }

                ResolveWorkerThreadState threadState = GetThreadState(data.ThreadID);
                if (threadState != null)
                {
                    threadState.Info.ResolveStubGenerationCount++;
                    System.Diagnostics.Debug.Assert(threadState.Info.BucketIndex == -1 || threadState.Info.BucketIndex == threadState.BucketIndex);
                    threadState.Info.BucketIndex = threadState.BucketIndex;

                    // At this point, we know that we are going to add the stub to the bucket.
                    _bucketIndexToSizeMap.TryGetValue(threadState.BucketIndex, out long size);
                    _bucketIndexToSizeMap[threadState.BucketIndex] = size + 1;
                }
            };
            parser.VirtualStubDispatchLookupResolveStubStart += delegate (VSDGenerateStubTraceData data)
            {
                if (_processID != data.ProcessID)
                {
                    return;
                }

                ResolveWorkerThreadState threadState = GetThreadState(data.ThreadID);
                if (threadState != null)
                {
                    threadState.CurrentlySearchingForResolveStub = true;
                }
            };
            parser.VirtualStubDispatchLookupResolveStubStop += delegate (VSDGenerateStubTraceData data)
            {
                if (_processID != data.ProcessID)
                {
                    return;
                }

                ResolveWorkerThreadState threadState = GetThreadState(data.ThreadID);
                if (threadState != null)
                {
                    System.Diagnostics.Debug.Assert(threadState.CurrentlySearchingForResolveStub);
                    threadState.CurrentlySearchingForResolveStub = false;
                    threadState.Info.BucketIndexPerStubSearch.Add(threadState.BucketIndex);
                }
            };
            parser.VirtualStubDispatchLookupResolveStubListRead += delegate (VSDListReadTraceData data)
            {
                if (_processID != data.ProcessID)
                {
                    return;
                }

                ResolveWorkerThreadState threadState = GetThreadState(data.ThreadID);
                if (threadState != null && threadState.CurrentlySearchingForResolveStub)
                {
                    threadState.Info.ReadsPerResolveStubSearch.Add(data.ReadCount);
                }
            };
            parser.VirtualStubDispatchLookupResolveStubBucket += delegate (VSDBucketTraceData data)
            {
                if (_processID != data.ProcessID)
                {
                    return;
                }

                ResolveWorkerThreadState threadState = GetThreadState(data.ThreadID);
                if (threadState != null)
                {
                    threadState.BucketIndex = data.Index;
                }
            };
            _source.Process();
        }

        internal void WriteOutput(TextWriter writer)
        {
            //foreach (KeyValuePair<long, long> pair in _bucketIndexToSizeMap)
            //{
            //    writer.WriteLine($"{pair.Key} --> {pair.Value}");
            //}
            writer.WriteLine($"Token, StubType, BucketIndex");
            foreach (ResolveWorkerInfo info in _resolveWorkerStats.Values)
            {
                if(info.BucketIndex != -1)
                    info.WriteOutput(writer);
            }
        }

        private ResolveWorkerThreadState SetupThreadState(VSDResolveWorkerTraceData data, ResolveWorkerInfo info)
        {
            ResolveWorkerThreadState threadState;
            if (!_threadStateMap.TryGetValue(data.ThreadID, out threadState))
            {
                threadState = new ResolveWorkerThreadState();
                _threadStateMap.Add(data.ThreadID, threadState);
            }

            threadState.Reset();
            threadState.Info = info;
            
            // Increment the number of times we enter ResolveWorker for this token.
            info.ResolveCount++;

            return threadState;
        }

        private ResolveWorkerThreadState GetThreadState(int threadID)
        {
            ResolveWorkerThreadState threadState;
            _threadStateMap.TryGetValue(threadID, out threadState);
            return threadState;
        }
    }

    internal sealed class ResolveWorkerInfo
    {
        public ResolveWorkerInfo(string typeName, long token)
        {
            TypeName = typeName;
            Token = token;
            BucketIndex = -1;
        }

        public string TypeName { get; private set; }
        public long Token { get; private set; }
        public long ResolveCount { get; set; }
        public long ResolveStubGenerationCount { get; set; }
        public List<long> BucketIndexPerStubSearch { get; set; } = new List<long>();
        public List<long> ReadsPerResolveStubSearch { get; set; } = new List<long>();
        public long BucketIndex { get; set; }

        public void WriteOutput(TextWriter writer)
        {
            writer.WriteLine($"0x{Token:X}, 0, {BucketIndex}");
            //writer.WriteLine($"0x{Token:X}, {ResolveCount}, {ResolveStubGenerationCount}, {TypeName}");
            //foreach (long bucketIndex in BucketIndexPerStubSearch)
            //{
            //    writer.Write($"{bucketIndex}, ");
            //}
            //writer.WriteLine();
            //foreach (long readCount in ReadsPerResolveStubSearch)
            //{
            //    writer.Write($"{readCount}, ");
            //}
            //writer.WriteLine();
        }
    }

    internal sealed class ResolveWorkerThreadState
    {
        public ResolveWorkerThreadState()
        {
            Info = null;
            BucketIndex = -1;
            CurrentlySearchingForResolveStub = false;
        }

        public ResolveWorkerInfo Info { get; set; }
        public long BucketIndex { get; set; }
        public bool CurrentlySearchingForResolveStub { get; set; }

        public void Reset()
        {
            Info = null;
            BucketIndex = -1;
        }
    }
}
