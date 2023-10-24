using Microsoft.Diagnostics.Tracing.Parsers.Clr;
using System;
using System.Diagnostics;

namespace Microsoft.Diagnostics.Tracing.Parsers.GCDynamicData
{
    public sealed class CommittedUsageTraceData
    {
        public short Version { get; internal set; }
        public long TotalCommittedInUse { get; internal set; }
        public long TotalCommittedInGlobalDecommit { get; internal set; }
        public long TotalCommittedInFree { get; internal set; }
        public long TotalCommittedInGlobalFree { get; internal set; }
        public long TotalBookkeepingCommitted { get; internal set; }
    }
    public sealed class HeapCountTuningTraceData
    {
        public TraceEvent RawEvent { get; internal set; }
        public short Version { get; internal set; }
        public short NewHeapCount { get; internal set; }
        public long GCIndex { get; internal set; }
        public float MedianPercentOverhead { get; internal set; }
        public float SmoothedMedianPercentOverhead { get; internal set; }
        public float OverheadReductionPerStepUp { get; internal set; }
        public float OverheadIncreasePerStepDown { get; internal set; }
        public float SpaceCostIncreasePerStepUp { get; internal set; }
        public float SpaceCostDecreasePerStepDown { get; internal set; }
    }
    public sealed class HeapCountSampleTraceData
    {
        public short Version { get; internal set; }
        public long GCElapsedTime { get; internal set; }
        public long SOHMslWaitTime { get; internal set; }
        public long UOHMslWaitTime { get; internal set; }
        public long ElapsedBetweenGCs { get; internal set; }
    }

    internal sealed class GCDynamicDataDispatcher
    {
        private static GCDynamicDataDispatcher s_Dispatcher;
        public static GCDynamicDataDispatcher EnsureRegistration(ClrTraceEventParser parser)
        {
            if (s_Dispatcher == null)
            {
                s_Dispatcher = new GCDynamicDataDispatcher();
                parser.GCDynamic += s_Dispatcher.Dispatch;
            }

            return s_Dispatcher;
        }

        private const string HeapCountTuningEventName = "HeapCountTuning";
        private HeapCountTuningTraceData _heapCountTuningTemplate = new HeapCountTuningTraceData();
        internal event Action<HeapCountTuningTraceData> HeapCountTuning;

        internal void Dispatch(GCDynamicTraceData data)
        {
            if (HeapCountTuning != null &&       
                string.CompareOrdinal(data.Name, HeapCountTuningEventName) == 0)
            {
                _heapCountTuningTemplate.RawEvent = data;
                _heapCountTuningTemplate.Version = BitConverter.ToInt16(data.Data, 0);
                Debug.Assert(!(_heapCountTuningTemplate.Version == 1 && data.Data.Length != 36));
                Debug.Assert(!(_heapCountTuningTemplate.Version > 1 && data.Data.Length < 36));
                _heapCountTuningTemplate.NewHeapCount = BitConverter.ToInt16(data.Data, 2);
                _heapCountTuningTemplate.GCIndex = BitConverter.ToInt64(data.Data, 4);
                _heapCountTuningTemplate.MedianPercentOverhead = BitConverter.ToSingle(data.Data, 12);
                _heapCountTuningTemplate.SmoothedMedianPercentOverhead = BitConverter.ToSingle(data.Data, 16);
                _heapCountTuningTemplate.OverheadReductionPerStepUp = BitConverter.ToSingle(data.Data, 20);
                _heapCountTuningTemplate.OverheadIncreasePerStepDown = BitConverter.ToSingle(data.Data, 24);
                _heapCountTuningTemplate.SpaceCostIncreasePerStepUp = BitConverter.ToSingle(data.Data, 28);
                _heapCountTuningTemplate.SpaceCostDecreasePerStepDown = BitConverter.ToSingle(data.Data, 32);

                HeapCountTuning(_heapCountTuningTemplate);
                _heapCountTuningTemplate.RawEvent = null;
            }
        }
    }
}
