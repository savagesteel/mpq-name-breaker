using System;
using System.Management.Automation;
using MpqNameBreaker.NameGenerator;
using MpqNameBreaker.Mpq;
using System.Collections.Generic;
using System.Threading;

namespace MpqNameBreaker
{
    [Cmdlet(VerbsLifecycle.Invoke, "MpqNameBreaking")]
    [OutputType(typeof(string))]
    public class InvokeMpqNameBreakingCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true)]
        public uint HashA { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public uint HashB { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 2,
            ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString()]
        public string Prefix { get; set; } = "";

        [Parameter(
            Mandatory = false,
            Position = 3,
            ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString()]
        public string Suffix { get; set; } = "";

        [Parameter(
            Mandatory = false,
            Position = 3,
            ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString()]
        public string AdditionalChars { get; set; } = "";

        [Parameter(
            Mandatory = false,
            Position = 3,
            ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString()]
        public string Charset { get; set; } = BruteForceBatches.DefaultCharset;

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public int BatchSize { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public int BatchCharCount { get; set; }


        // Fields
        private BruteForce _bruteForce;
        private BruteForceBatches _bruteForceBatches;
        private HashCalculator _hashCalculator;
        private HashCalculatorAccelerated _hashCalculatorAccelerated;
        private volatile bool nameFound = false;
        private string resultName;

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
        }

        private void PrintDeviceInfo(HashCalculatorAccelerated hashCalc)
        {
            WriteVerbose("Devices:");
            foreach (var device in hashCalc.GPUContext)
            {
                WriteVerbose($"\t{device}");
            }
        }

        private object logLock = new object();
        private List<string> verboseLogBuffer = new List<string>();
        private void WriteLogAsync(string text)
        {
            lock(logLock)
            {
                verboseLogBuffer.Add(text);
            }
        }

        private object nameFoundLock = new object();
        private void NameFoundAsync(string name)
        {
            lock (nameFoundLock)
            {
                resultName = name;
            }
            nameFound = true;
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            uint prefixSeed1A, prefixSeed2A, prefixSeed1B, prefixSeed2B;

            // Initialize brute force name generator
            _bruteForce = new BruteForce(Prefix, Suffix);
            _bruteForce.Initialize();

            // Initialize classic CPU hash calculator and pre-calculate prefix seeds
            _hashCalculator = new HashCalculator();
            if (Prefix.Length > 0)
            {
                (prefixSeed1A, prefixSeed2A) = _hashCalculator.HashStringOptimizedCalculateSeeds(_bruteForce.PrefixBytes, HashType.MpqHashNameA);
                (prefixSeed1B, prefixSeed2B) = _hashCalculator.HashStringOptimizedCalculateSeeds(_bruteForce.PrefixBytes, HashType.MpqHashNameB);
            }
            else
            {
                prefixSeed1A = HashCalculatorAccelerated.HashSeed1;
                prefixSeed2A = HashCalculatorAccelerated.HashSeed2;
                prefixSeed1B = HashCalculatorAccelerated.HashSeed1;
                prefixSeed2B = HashCalculatorAccelerated.HashSeed2;
            }

            // Initialize GPU hash calculator
            _hashCalculatorAccelerated = new HashCalculatorAccelerated();
            PrintDeviceInfo(_hashCalculatorAccelerated);

            // Define the batch size to MaxNumThreads of the accelerator if no custom value has been provided
            if (!this.MyInvocation.BoundParameters.ContainsKey("BatchSize"))
                BatchSize = _hashCalculatorAccelerated.GetBestDevice().MaxNumThreads;

            if (!this.MyInvocation.BoundParameters.ContainsKey("BatchCharCount"))
            {
                if (_hashCalculatorAccelerated.GetBestDevice().MaxNumThreads < 1024)
                    BatchCharCount = 3;
                else
                    BatchCharCount = 4;
            }

            // Initialize brute force batches name generator
            _bruteForceBatches = new BruteForceBatches(BatchSize, BatchCharCount, AdditionalChars, Charset);
            _bruteForceBatches.Initialize();

            DateTime start = DateTime.Now;
            WriteVerbose($"Start: {start:HH:mm:ss.fff}");

            var batches = new List<BatchJob>();

            foreach (var device in _hashCalculatorAccelerated.GPUContext)
            {
                // TODO: Temporary - only use the best device until we can utilize multiple devices with different batch sizes...
                if (device != _hashCalculatorAccelerated.GetBestDevice()) continue;

                var job = new BatchJob(_hashCalculatorAccelerated.GPUContext, device, _bruteForceBatches, _hashCalculatorAccelerated) {
                    Prefix = Prefix,
                    Suffix = Suffix,
                    HashA = HashA,
                    HashB = HashB,
                    PrefixSeed1A = prefixSeed1A,
                    PrefixSeed1B = prefixSeed1B,
                    PrefixSeed2A = prefixSeed2A,
                    PrefixSeed2B = prefixSeed2B
                };
                job.SetLoggerCallback(WriteLogAsync);
                job.SetNameFoundCallback(NameFoundAsync);
                batches.Add(job);
                job.Run();
            }

            while (!nameFound)
            {
                lock(logLock)
                {
                    foreach(string text in verboseLogBuffer)
                    {
                        WriteVerbose(text);
                    }
                    verboseLogBuffer.Clear();
                }
                Thread.Sleep(100);
            }

            foreach(BatchJob job in batches)
            {
                job.Stop();
            }

            WriteVerbose($"End: {DateTime.Now:HH:mm:ss.fff}");
            TimeSpan elapsed = DateTime.Now - start;
            WriteVerbose($"Elapsed: {elapsed}");
            WriteObject(resultName);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
