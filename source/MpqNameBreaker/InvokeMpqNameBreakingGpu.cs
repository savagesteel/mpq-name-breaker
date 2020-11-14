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
            _bruteForceBatches = new BruteForceBatches( 1024, 5 ); // Batch size 1024 name seeds

            // Initialize GPU hash calculator
            _hashCalculatorGpu = new HashCalculatorGpu();



            WriteVerbose( DateTime.Now.ToString("HH:mm:ss.fff"));

            WriteObject( _hashCalculatorGpu.Accelerator );


            long count = 0;
            while( _bruteForceBatches.NextBatch() )
            {
                //int[,] batch;


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
                if( count % 1_000_000_000 == 0 )
                {
                    TimeSpan elapsed = DateTime.Now - start;
                    WriteVerbose( String.Format("Time: {0} - Name: {1} - Count : {2:N0} billion", elapsed.ToString(), _bruteForce.Name, count/1_000_000_000) );
                }
    
                count++;

            }


        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
