using System;
using Microsoft.Diagnostics.Tracing;

namespace VSD
{
    class Program
    {
        static void Main(string[] args)
        {
            string tracePath = args[0];
            int processID = Convert.ToInt32(args[1]);
            using (ETWTraceEventSource source = new ETWTraceEventSource(tracePath))
            {
                VirtualStubDispatchComputer computer = new VirtualStubDispatchComputer(source, processID);
                computer.Process();
                computer.WriteOutput(Console.Out);
            }
        }
    }
}
