﻿using System;
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
            Position = 3,
            ValueFromPipelineByPropertyName = true)]
        [AllowEmptyString()]
        public string AdditionalChars { get; set; }

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
                prefixSeed1A = HashCalculatorGpu.HashSeed1;
                prefixSeed2A = HashCalculatorGpu.HashSeed2;
                prefixSeed1B = HashCalculatorGpu.HashSeed1;
                prefixSeed2B = HashCalculatorGpu.HashSeed2;
            }

            // Initialize brute force batches name generator
            if( AdditionalChars.Length > 0 )
                _bruteForceBatches = new BruteForceBatches( GpuBatchSize, GpuBatchCharCount, AdditionalChars );
            else
                _bruteForceBatches = new BruteForceBatches( GpuBatchSize, GpuBatchCharCount );

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
                    bool,
                    int,
                    int,
                    ArrayView<int>
                >( Mpq.HashCalculatorGpu.HashStringsBatchA );

            // Prepare data for the kernel
            var charsetBuffer = _hashCalculatorGpu.Accelerator.Allocate<byte>( _bruteForceBatches.CharsetBytes.Length );
            charsetBuffer.CopyFrom( _bruteForceBatches.CharsetBytes, 0, 0, _bruteForceBatches.CharsetBytes.Length );

            var charsetIndexesBuffer = _hashCalculatorGpu.Accelerator.Allocate<int>( GpuBatchSize, BruteForceBatches.MaxGeneratedChars );

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
            var suffixBytesBuffer = _hashCalculatorGpu.Accelerator.Allocate<byte>( suffixLength );
            if( suffix )
            {
                suffixBytesBuffer.CopyFrom( suffixBytes, 0, 0, suffixBytes.Length );
            }


            var cryptTableBuffer = _hashCalculatorGpu.Accelerator.Allocate<uint>(HashCalculatorGpu.CryptTableSize);
            cryptTableBuffer.CopyFrom( _hashCalculatorGpu.CryptTable, 0, 0, _hashCalculatorGpu.CryptTable.Length );

            int nameCount = (int)Math.Pow( _bruteForceBatches.Charset.Length, GpuBatchCharCount );

            // fill result array with -1
            var foundNameCharsetIndexesBuffer = _hashCalculatorGpu.Accelerator.Allocate<int>(BruteForceBatches.MaxGeneratedChars);
            int[] foundNameCharsetIndexes = new int[BruteForceBatches.MaxGeneratedChars]; 
            for( int i = 0; i < BruteForceBatches.MaxGeneratedChars; ++i )
                foundNameCharsetIndexes[i] = -1;
            foundNameCharsetIndexesBuffer.CopyFrom( foundNameCharsetIndexes, 0, 0, foundNameCharsetIndexesBuffer.Extent );
            string foundName = "";

            // MAIN

            WriteVerbose( DateTime.Now.ToString("HH:mm:ss.fff"));
            WriteObject( _hashCalculatorGpu.Accelerator );

            double billionCount = 0;
            double tempCount = 0;
            double oneBatchBillionCount = ( Math.Pow(_bruteForceBatches.Charset.Length, GpuBatchCharCount) * GpuBatchSize ) / 1_000_000_000;

            DateTime start = DateTime.Now;
            while( _bruteForceBatches.NextBatch() )
            {
                // Debug
                //string[] names = _bruteForceBatches.BatchNames;

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
                    _bruteForceBatches.FirstBatch, nameCount-1, GpuBatchCharCount, foundNameCharsetIndexesBuffer.View );

                // Wait for the kernel to complete
                _hashCalculatorGpu.Accelerator.Synchronize();


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

                    WriteObject( "Name found: " + Prefix.ToUpper() + foundName + Suffix.ToUpper() );
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
                        int idx = _bruteForceBatches.BatchNameSeedCharsetIndexes[ GpuBatchSize-1, i ];

                        if( idx == -1 )
                            break;

                        lastName += Convert.ToChar( _bruteForceBatches.CharsetBytes[ idx ] );
                    }


                    WriteVerbose( String.Format("Time: {0} - Name {1} - Count : {2:N0} billion", elapsed.ToString(), Prefix.ToUpper()+lastName+Suffix.ToUpper(), billionCount) );
                }
                
            }

        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
