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
    [Cmdlet(VerbsLifecycle.Invoke,"MpqNameBreaking")]
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
        public string AdditionalChars { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public int AcceleratorId { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public int BatchSize { get; set; }

        [Parameter(
            Mandatory = false,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public int BatchCharCount { get; set; } = 3;


        // Fields
        private BruteForce _bruteForce;
        private BruteForceBatches _bruteForceBatches;
        private BruteForceBatches3D _bruteForceBatches3D;
        private HashCalculator _hashCalculator;
        private HashCalculatorAccelerated _hashCalculatorAccelerated;

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            uint prefixSeed1A, prefixSeed2A, prefixSeed1B, prefixSeed2B;

            // Initialize brute force name generator
            _bruteForce = new BruteForce( Prefix, Suffix );
            _bruteForce.Initialize();

            // Initialize classic CPU hash calculator and pre-calculate prefix seeds
            _hashCalculator = new HashCalculator();
            if( Prefix.Length > 0 )
            {
                (prefixSeed1A, prefixSeed2A) = _hashCalculator.HashStringOptimizedCalculateSeeds( _bruteForce.PrefixBytes, HashType.MpqHashNameA );
                (prefixSeed1B, prefixSeed2B) = _hashCalculator.HashStringOptimizedCalculateSeeds( _bruteForce.PrefixBytes, HashType.MpqHashNameB );
            }
            else
            {
                prefixSeed1A = HashCalculatorAccelerated.HashSeed1;
                prefixSeed2A = HashCalculatorAccelerated.HashSeed2;
                prefixSeed1B = HashCalculatorAccelerated.HashSeed1;
                prefixSeed2B = HashCalculatorAccelerated.HashSeed2;
            }

            // Initialize GPU hash calculator
            if( this.MyInvocation.BoundParameters.ContainsKey("AcceleratorId") )
                _hashCalculatorAccelerated = new HashCalculatorAccelerated( AcceleratorId );
            else
                _hashCalculatorAccelerated = new HashCalculatorAccelerated();

            // Define the batch size to MaxNumThreads of the accelerator if no custom value has been provided
            if( !this.MyInvocation.BoundParameters.ContainsKey("BatchSize") )
                BatchSize = _hashCalculatorAccelerated.Accelerator.MaxNumThreads;

            // Initialize brute force batches name generator
            if( this.MyInvocation.BoundParameters.ContainsKey("AdditionalChars") )
                _bruteForceBatches = new BruteForceBatches( BatchSize, BatchCharCount, AdditionalChars );
            else
                _bruteForceBatches = new BruteForceBatches( BatchSize, BatchCharCount );

            _bruteForceBatches.Initialize();


            // Load kernel (GPU function)
            // This function will calculate HashA for each name of a batch and report matches/collisions
            var kernel = _hashCalculatorAccelerated.Accelerator.LoadAutoGroupedStreamKernel< 
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
                    bool,
                    int,
                    int,
                    ArrayView<int>
                >( Mpq.HashCalculatorAccelerated.HashStringsBatch );

            // Prepare data for the kernel
            var charsetBuffer = _hashCalculatorAccelerated.Accelerator.Allocate<byte>( _bruteForceBatches.CharsetBytes.Length );
            charsetBuffer.CopyFrom( _bruteForceBatches.CharsetBytes, 0, 0, _bruteForceBatches.CharsetBytes.Length );

            var charsetIndexesBuffer = _hashCalculatorAccelerated.Accelerator.Allocate<int>( BatchSize, BruteForceBatches.MaxGeneratedChars );

            // Suffix processing
            bool suffix;
            int suffixLength;
            byte[] suffixBytes;
            if( Suffix.Length > 0 )
            {
                suffix = true;
                suffixLength = Suffix.Length;
                suffixBytes = Encoding.ASCII.GetBytes( Suffix.ToUpper() );
            }
            else
            {
                suffix = false;
                suffixLength = 1;
                suffixBytes = new byte[1];
                suffixBytes[0] = 0;
            }
            var suffixBytesBuffer = _hashCalculatorAccelerated.Accelerator.Allocate<byte>( suffixLength );
            if( suffix )
            {
                suffixBytesBuffer.CopyFrom( suffixBytes, 0, 0, suffixBytes.Length );
            }


            var cryptTableBuffer = _hashCalculatorAccelerated.Accelerator.Allocate<uint>(HashCalculatorAccelerated.CryptTableSize);
            cryptTableBuffer.CopyFrom( _hashCalculatorAccelerated.CryptTable, 0, 0, _hashCalculatorAccelerated.CryptTable.Length );

            int nameCount = (int)Math.Pow( _bruteForceBatches.Charset.Length, BatchCharCount );

            // fill result array with -1
            var foundNameCharsetIndexesBuffer = _hashCalculatorAccelerated.Accelerator.Allocate<int>(BruteForceBatches.MaxGeneratedChars);
            int[] foundNameCharsetIndexes = new int[BruteForceBatches.MaxGeneratedChars]; 
            for( int i = 0; i < BruteForceBatches.MaxGeneratedChars; ++i )
                foundNameCharsetIndexes[i] = -1;
            foundNameCharsetIndexesBuffer.CopyFrom( foundNameCharsetIndexes, 0, 0, foundNameCharsetIndexesBuffer.Extent );
            string foundName = "";

            // MAIN

            WriteVerbose( "Accelerator: " + _hashCalculatorAccelerated.Accelerator.Name 
                + " (threads: " + _hashCalculatorAccelerated.Accelerator.MaxNumThreads + ")" );
            WriteVerbose( "Batch size : " + BatchSize + "\n" );

            WriteVerbose( "Starting at: " + DateTime.Now.ToString("HH:mm:ss.fff") + "\n" ); 

            double billionCount = 0;
            double tempCount = 0;
            double oneBatchBillionCount = ( Math.Pow(_bruteForceBatches.Charset.Length, BatchCharCount) * BatchSize ) / 1_000_000_000;

            DateTime start = DateTime.Now;
            while( _bruteForceBatches.NextBatch() )
            {
                // Debug
                string[] names = _bruteForceBatches.BatchNames;
                //string[,] names = _bruteForceBatches3D.BatchNames;

                // Copy char indexes to buffer
                charsetIndexesBuffer.CopyFrom( 
                    _bruteForceBatches.BatchNameSeedCharsetIndexes, Index2.Zero, Index2.Zero, charsetIndexesBuffer.Extent );

                // DEBUG: Inject a known name data in charsetIndexesBuffer to test the kernel
                
                /*
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
                */

                
                // Call the kernel
                kernel( charsetIndexesBuffer.Width, charsetBuffer.View, cryptTableBuffer.View,
                    charsetIndexesBuffer.View, suffixBytesBuffer.View, HashA, HashB, prefixSeed1A, prefixSeed2A, prefixSeed1B, prefixSeed2B,
                    _bruteForceBatches.FirstBatch, nameCount-1, BatchCharCount, foundNameCharsetIndexesBuffer.View );

                // Wait for the kernel to complete
                _hashCalculatorAccelerated.Accelerator.Synchronize();


                //var test = charsetIndexesBuffer.GetAs2DArray();

                // If name was found
                foundNameCharsetIndexes = foundNameCharsetIndexesBuffer.GetAsArray();

                if( foundNameCharsetIndexes[0] != -1 )
                {
                    byte[] str = new byte[BruteForceBatches.MaxGeneratedChars];

                    for( int i = 0; i < BruteForceBatches.MaxGeneratedChars; ++i )
                    {
                        int idx = foundNameCharsetIndexes[i];
                        
                        if( idx == -1 )
                            break;

                        foundName += Convert.ToChar( _bruteForceBatches.CharsetBytes[ idx ] );
                    }

                    WriteVerbose( "Name found!" );
                    WriteObject( Prefix.ToUpper() + foundName + Suffix.ToUpper() );

                    return;
                }

                // Display statistics
                billionCount += oneBatchBillionCount;
                if( tempCount < billionCount )
                {
                    tempCount = billionCount + 1;
                    TimeSpan elapsed = DateTime.Now - start;

                    string lastName = "";
                    for( int i = 0; i < BruteForceBatches.MaxGeneratedChars; i++ )
                    {
                        int idx = _bruteForceBatches.BatchNameSeedCharsetIndexes[ BatchSize-1, i ];

                        if( idx == -1 )
                            break;

                        lastName += Convert.ToChar( _bruteForceBatches.CharsetBytes[ idx ] );
                    }

                    WriteVerbose( String.Format("Elapsed time: {0} - Name: {1} - Name count: {2:N0} billion", elapsed.ToString(), Prefix.ToUpper()+lastName+Suffix.ToUpper(), billionCount) );
                }
                
            }

        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
