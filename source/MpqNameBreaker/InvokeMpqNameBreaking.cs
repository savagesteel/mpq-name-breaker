using System;
using System.Text;
using System.Management.Automation;
using MpqNameBreaker.NameGenerator;
using MpqNameBreaker.Mpq;
using ILGPU;
using ILGPU.Runtime;

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
        //private BruteForceBatches3D _bruteForceBatches3D;
        private HashCalculator _hashCalculator;
        private HashCalculatorAccelerated _hashCalculatorAccelerated;

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
        }

        private void PrintDeviceInfo(HashCalculatorAccelerated hashCalc)
        {
            WriteVerbose("Devices:");
            foreach (var device in hashCalc.GPUContext)
            {
                string selected = hashCalc.Accelerator.Device == device ? "-->" : "";
                WriteVerbose($"{selected}\t{device}");
            }
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
                BatchSize = _hashCalculatorAccelerated.Accelerator.MaxNumThreads;

            if (!this.MyInvocation.BoundParameters.ContainsKey("BatchCharCount"))
            {
                if (_hashCalculatorAccelerated.Accelerator.MaxNumThreads < 1024)
                    BatchCharCount = 3;
                else
                    BatchCharCount = 4;
            }

            // Initialize brute force batches name generator
            _bruteForceBatches = new BruteForceBatches(BatchSize, BatchCharCount, AdditionalChars, Charset);
            _bruteForceBatches.Initialize();

            // Load kernel (GPU function)
            // This function will calculate HashA for each name of a batch and report matches/collisions
            var kernel = _hashCalculatorAccelerated.Accelerator.LoadAutoGroupedStreamKernel<
                    Index1D,
                    ArrayView<byte>,
                    ArrayView<uint>,
                    ArrayView2D<int, Stride2D.DenseX>,
                    ArrayView<byte>,
                    uint,
                    uint,
                    uint,
                    uint,
                    uint,
                    uint,
                    bool,
                    int,
                    int,
                    ArrayView<int>
                >(Mpq.HashCalculatorAccelerated.HashStringsBatchOptimized);

            // Prepare data for the kernel
            var charsetBuffer = _hashCalculatorAccelerated.Accelerator.Allocate1D(_bruteForceBatches.CharsetBytes);

            var charsetIndexesBuffer = _hashCalculatorAccelerated.Accelerator.Allocate2DDenseX<int>(new Index2D(BatchSize, BruteForceBatches.MaxGeneratedChars));

            // Suffix processing
            int suffixLength;
            byte[] suffixBytes;
            if (Suffix.Length > 0)
            {
                suffixLength = Suffix.Length;
                suffixBytes = Encoding.ASCII.GetBytes(Suffix.ToUpper());
            }
            else
            {
                suffixLength = 1;
                suffixBytes = new byte[suffixLength];
                suffixBytes[0] = 0x00;
            }
            var suffixBytesBuffer = _hashCalculatorAccelerated.Accelerator.Allocate1D(suffixBytes);

            var cryptTableBuffer = _hashCalculatorAccelerated.Accelerator.Allocate1D(_hashCalculatorAccelerated.CryptTable);

            int nameCount = (int)Math.Pow(_bruteForceBatches.Charset.Length, BatchCharCount);

            // fill result array with -1
            int[] foundNameCharsetIndexes = new int[BruteForceBatches.MaxGeneratedChars];
            for (int i = 0; i < BruteForceBatches.MaxGeneratedChars; ++i)
                foundNameCharsetIndexes[i] = -1;

            var foundNameCharsetIndexesBuffer = _hashCalculatorAccelerated.Accelerator.Allocate1D(foundNameCharsetIndexes);
            string foundName = "";

            // MAIN

            WriteVerbose($"Accelerator: {_hashCalculatorAccelerated.Accelerator.Name}"
                + $" (threads: {_hashCalculatorAccelerated.Accelerator.MaxNumThreads})");
            WriteVerbose($"Batch size: {BatchSize}, {BatchCharCount}");
            WriteVerbose("Charset: " + _bruteForceBatches.Charset);

            DateTime start = DateTime.Now;
            WriteVerbose($"Start: {start:HH:mm:ss.fff}");

            double billionCount = 0;
            double tempCount = 0;
            double oneBatchBillionCount = (Math.Pow(_bruteForceBatches.Charset.Length, BatchCharCount) * BatchSize) / 1_000_000_000;

            while (_bruteForceBatches.NextBatch())
            {
                // Copy char indexes to buffer
                charsetIndexesBuffer.CopyFromCPU(_bruteForceBatches.BatchNameSeedCharsetIndexes);

                // Call the kernel
                kernel((int)charsetIndexesBuffer.Extent.X,
                       charsetBuffer.View,
                       cryptTableBuffer.View,
                       charsetIndexesBuffer.View,
                       suffixBytesBuffer.View,
                       HashA,
                       HashB,
                       prefixSeed1A,
                       prefixSeed2A,
                       prefixSeed1B,
                       prefixSeed2B,
                       _bruteForceBatches.FirstBatch,
                       nameCount,
                       BatchCharCount,
                       foundNameCharsetIndexesBuffer.View);

                // Wait for the kernel to complete
                _hashCalculatorAccelerated.Accelerator.Synchronize();

                // If name was found
                foundNameCharsetIndexes = foundNameCharsetIndexesBuffer.GetAsArray1D();

                if (foundNameCharsetIndexes[0] != -1)
                {
                    byte[] str = new byte[BruteForceBatches.MaxGeneratedChars];

                    for (int i = 0; i < BruteForceBatches.MaxGeneratedChars; ++i)
                    {
                        int idx = foundNameCharsetIndexes[i];

                        if (idx == -1)
                            break;

                        foundName += Convert.ToChar(_bruteForceBatches.CharsetBytes[idx]);
                    }

                    WriteVerbose($"End: {DateTime.Now:HH:mm:ss.fff}");
                    TimeSpan elapsed = DateTime.Now - start;
                    WriteVerbose($"Elapsed: {elapsed}");
                    WriteVerbose("Name found! ");
                    WriteObject(Prefix.ToUpper() + foundName + Suffix.ToUpper());

                    return;
                }

                // Display statistics
                billionCount += oneBatchBillionCount;
                if (tempCount < billionCount)
                {
                    tempCount = billionCount + 1;
                    TimeSpan elapsed = DateTime.Now - start;

                    string lastName = "";
                    for (int i = 0; i < BruteForceBatches.MaxGeneratedChars; i++)
                    {
                        int idx = _bruteForceBatches.BatchNameSeedCharsetIndexes[BatchSize - 1, i];

                        if (idx == -1)
                            break;

                        lastName += Convert.ToChar(_bruteForceBatches.CharsetBytes[idx]);
                    }

                    WriteVerbose($"Elapsed time: {elapsed} - Name: {Prefix.ToUpper() + lastName + Suffix.ToUpper()} - Name count: {billionCount:N0} billion");
                }
            }
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
