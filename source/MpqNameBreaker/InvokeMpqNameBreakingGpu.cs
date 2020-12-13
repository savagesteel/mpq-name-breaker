using System;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using MpqNameBreaker.NameGenerator;
using MpqNameBreaker.Mpq;
using ILGPU;
using ILGPU.Runtime;
//using System.Collections.Immutable;

namespace MpqNameBreaker
{
    [Cmdlet(VerbsLifecycle.Invoke,"MpqNameBreakingGpu")]
    [OutputType(typeof(string))]
    public class InvokeMpqNameBreakingGpuCommand : PSCmdlet
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
            Mandatory = true,
            Position = 2,
            ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString()]
        public string Prefix { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 3,
            ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString()]
        public string Suffix { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public int GpuBatchSize { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public int GpuBatchCharCount { get; set; }


        // Fields
        private BruteForce _bruteForce;
        private BruteForceBatches _bruteForceBatches;
        private HashCalculator _hashCalculator;
        private HashCalculatorGpu _hashCalculatorGpu;

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            uint prefixSeed1A, prefixSeed2A, prefixSeed1B, prefixSeed2B, currentHashA, currentHashB;
            DateTime start = DateTime.Now;

/*
            WriteVerbose( DateTime.Now.ToString("HH:mm:ss.fff"));

            var context = new Context();
            // For each available accelerator...
            foreach( var acceleratorId in Accelerator.Accelerators )
            {
                // Instanciate the Nvidia (CUDA) accelerator
                if( acceleratorId.AcceleratorType == AcceleratorType.Cuda )
                {
                    var accelerator = Accelerator.Create( context, acceleratorId );
                    WriteObject( accelerator );


                    // Load kernel
                    var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1, ArrayView<int>, int>( Mpq.HashCalculatorGpu.MyKernel );

                    var buffer = accelerator.Allocate<int>(102400000);
                    
                    // Launch buffer.Length many threads and pass a view to buffer
                    // Note that the kernel launch does not involve any boxing
                    kernel(buffer.Length, buffer.View, 42);

                    // Wait for the kernel to finish...
                    accelerator.Synchronize();

                    // Resolve and verify data
                    var data = buffer.GetAsArray();
                    var test = data[1024];

                    // Load kernel
                    var kernel = accelerator.LoadAutoGroupedStreamKernel<Index1, ArrayView2D<byte>>( Mpq.HashCalculatorGpu.MyKernel2 );

                    var buffer = accelerator.Allocate<byte>(1024,64);
                    
                    // Launch buffer.Width many threads and pass a view to buffer
                    // Note that the kernel launch does not involve any boxing
                    kernel(buffer.Width, buffer.View);

                    // Wait for the kernel to finish...
                    accelerator.Synchronize();

                    // Resolve and verify data
                    var data = buffer.GetAs2DArray();
                }
            }
*/

            // Initialize brute force name generator
            _bruteForce = new BruteForce( Prefix, Suffix );
            _bruteForce.Initialize();

            // Initialize classic CPU hash calculator and pre-calculate prefix seeds
            _hashCalculator = new HashCalculator();
            (prefixSeed1A, prefixSeed2A) = _hashCalculator.HashStringOptimizedCalculateSeeds( _bruteForce.PrefixBytes, HashType.MpqHashNameA );
            (prefixSeed1B, prefixSeed2B) = _hashCalculator.HashStringOptimizedCalculateSeeds( _bruteForce.PrefixBytes, HashType.MpqHashNameB );

            // Initialize brute force batches name generator
            _bruteForceBatches = new BruteForceBatches( GpuBatchSize, GpuBatchCharCount ); // Batch size 1024 name seeds
            _bruteForceBatches.Initialize();

            // Initialize GPU hash calculator
            _hashCalculatorGpu = new HashCalculatorGpu();

            // Load kernel (GPU function)
            // This function will calculate HashA for each name of a batch and report matches/collisions
            var kernel = _hashCalculatorGpu.Accelerator.LoadAutoGroupedStreamKernel< 
                    Index1,
                    ArrayView<byte>,
                    ArrayView<uint>,
                    ArrayView2D<int>,
                    ArrayView<byte>,
                    uint,
                    uint,
                    uint,
                    uint,
                    uint,
                    uint,
                    int,
                    ArrayView<int>
                >( Mpq.HashCalculatorGpu.HashStringsBatchA );

            // Prepare data for the kernel
            var charsetBuffer = _hashCalculatorGpu.Accelerator.Allocate<byte>( _bruteForceBatches.CharsetBytes.Length );
            charsetBuffer.CopyFrom( _bruteForceBatches.CharsetBytes, 0, 0, _bruteForceBatches.CharsetBytes.Length );

            var charsetIndexesBuffer = _hashCalculatorGpu.Accelerator.Allocate<int>( GpuBatchSize, BruteForceBatches.MaxGeneratedChars );

            var suffixBytesBuffer = _hashCalculatorGpu.Accelerator.Allocate<byte>( Suffix.Length );
            if( Suffix.Length > 0 )
            {
                byte[] suffixBytes = Encoding.ASCII.GetBytes( Suffix.ToUpper() );
                suffixBytesBuffer.CopyFrom( suffixBytes, 0, 0, suffixBytes.Length );
            }            

            var cryptTableBuffer = _hashCalculatorGpu.Accelerator.Allocate<uint>(HashCalculatorGpu.CryptTableSize);
            cryptTableBuffer.CopyFrom( _hashCalculatorGpu.CryptTable, 0, 0, _hashCalculatorGpu.CryptTable.Length );

            int nameCount = (int)Math.Pow( BruteForceBatches.Charset.Length, GpuBatchCharCount );

            var foundNameCharsetIndexesBuffer = _hashCalculatorGpu.Accelerator.Allocate<int>(BruteForceBatches.MaxGeneratedChars);


            // Main loop

            WriteVerbose( DateTime.Now.ToString("HH:mm:ss.fff"));
            WriteObject( _hashCalculatorGpu.Accelerator );

            double billionCount = 0;
            double tempCount = 0;
            double oneBatchBillionCount = ( Math.Pow(BruteForceBatches.Charset.Length, GpuBatchCharCount) * GpuBatchSize ) / 1_000_000_000;

            while( _bruteForceBatches.NextBatch() )
            {
                // Debug
                string[] names = _bruteForceBatches.BatchNames;

                // Copy char indexes to buffer
                charsetIndexesBuffer.CopyFrom( 
                    _bruteForceBatches.BatchNameSeedCharsetIndexes, Index2.Zero, Index2.Zero, charsetIndexesBuffer.Extent );

                // DEBUG: Inject a known name data in charsetIndexesBuffer to test the kernel
                string testName = "AXE.CEL";
                byte[] testNameBytes = Encoding.ASCII.GetBytes( testName.ToUpper() );
                int[,] testNameIndexes = new int[1024,16];
                int cIndex = 0;
                for( int i = 0; i < 1024; i++ )
                {
                    for( int j = 0; j < 16; j++)
                    {
                        if( j < testName.Length )
                            cIndex = BruteForceBatches.Charset.IndexOf(testName[j]);
                        else
                            cIndex = -1;

                        testNameIndexes[i,j] = cIndex;
                    }
                }
                charsetIndexesBuffer.CopyFrom( 
                    testNameIndexes, Index2.Zero, Index2.Zero, charsetIndexesBuffer.Extent );
                var test = charsetIndexesBuffer.GetAs2DArray();

                var hash = _hashCalculator.HashString(Encoding.ASCII.GetBytes("ITEMS2\\AXE.CEL"),HashType.MpqHashNameA);
                var hasho = _hashCalculator.HashStringOptimized(Encoding.ASCII.GetBytes("ITEMS2\\AXE.CEL"),HashType.MpqHashNameA,7,prefixSeed1A,prefixSeed2A);

                // Call the kernel
                kernel( charsetIndexesBuffer.Width, charsetBuffer.View, cryptTableBuffer.View,
                    charsetIndexesBuffer.View, suffixBytesBuffer.View, HashA, HashB, prefixSeed1A, prefixSeed2A, prefixSeed1B, prefixSeed2B,
                    nameCount, foundNameCharsetIndexesBuffer.View );

                // Wait for the kernel to complete
                _hashCalculatorGpu.Accelerator.Synchronize();


                /*
                currentHashA = _hashCalculator.HashStringOptimized( _bruteForce.NameBytes, HashType.MpqHashNameA, _bruteForce.Prefix.Length, prefixSeed1A, prefixSeed2A );

                if( HashA == currentHashA )
                {
                    currentHashB = _hashCalculator.HashStringOptimized( _bruteForce.NameBytes, HashType.MpqHashNameB, _bruteForce.Prefix.Length, prefixSeed1B, prefixSeed2B );

                    // Detect collisions
                    if( HashB == currentHashB )
                    {
                        WriteObject( "Name found: " + _bruteForce.Name );
                        WriteVerbose( DateTime.Now.ToString("HH:mm:ss.fff"));
                        break;
                    }
                    else
                    {
                        WriteWarning( "Hash A collision found on name: " + _bruteForce.Name );
                    }
                }
                */

                
                billionCount += oneBatchBillionCount;
                if( tempCount < billionCount )
                {
                    tempCount = billionCount + 1;
                    TimeSpan elapsed = DateTime.Now - start;
                    WriteVerbose( String.Format("Time: {0} - Count : {1:N0} billion", elapsed.ToString(), billionCount) );
                }
                
            }

        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
