using System;
using System.Text;
using System.Collections;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using MpqNameBreaker.NameGenerator;
using MpqNameBreaker.Mpq;

namespace MpqNameBreaker
{
    [Cmdlet(VerbsLifecycle.Invoke,"MpqNameBreaking")]
    [OutputType(typeof(string))]
    public class InvokeMpqNameBreakerCommand : PSCmdlet
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
        private HashCalculator _hashCalculator;

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            uint prefixSeed1A, prefixSeed2A, prefixSeed1B, prefixSeed2B, currentHashA, currentHashB;
            DateTime start = DateTime.Now;

            // Initialize brute force name generator
            _bruteForce = new BruteForce( Prefix, Suffix );
            _bruteForce.Initialize();

            // Initialize hash calculator
            _hashCalculator = new HashCalculator();
            // Prepare prefix seeds to speed up calculation
            (prefixSeed1A, prefixSeed2A) = _hashCalculator.HashStringOptimizedCalculateSeeds( _bruteForce.PrefixBytes, HashType.MpqHashNameA );
            (prefixSeed1B, prefixSeed2B) = _hashCalculator.HashStringOptimizedCalculateSeeds( _bruteForce.PrefixBytes, HashType.MpqHashNameB );


            WriteVerbose( DateTime.Now.ToString("HH:mm:ss.fff"));

            long count = 0;
            while( _bruteForce.NextName() && count < 4_347_792_138_496 )
            {
                //currentHash = _hashCalculator.HashString( _bruteForce.NameBytes, Type );
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
    
                if( count % 10_000_000_000 == 0 )
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
