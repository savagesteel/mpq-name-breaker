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
        public string Prefix { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipelineByPropertyName = true)]
        public string Suffix { get; set; }

        // Constants
        public const uint Hero1HashA = 0xba2c211d;

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
            Hashtable hashLookup = new Hashtable()
            {
                //{ 0xBA2C211D, "levels\\l1data\\hero1.dun" },
                //{ 0xB29FC135, "unknownA" }
                { 0x22_57_5C_4A, "unknownB" }
            };

            uint hash;
            DateTime start = DateTime.Now;

            // Initialize hash calculator
            _hashCalculator = new HashCalculator();

            // Initialize brute force name generator
            _bruteForce = new BruteForce( Prefix, Suffix );
            _bruteForce.Initialize();


            WriteVerbose( DateTime.Now.ToString("HH:mm:ss.fff"));

            long count = 0;
            while( _bruteForce.NextName() && count < 4_347_792_138_496 )
            {
                hash = _hashCalculator.HashString( _bruteForce.NameBytes, HashType.MpqHashNameB );

                if( hashLookup.ContainsKey(hash) )
                {
                    WriteObject( "FOUND : " + _bruteForce.Name );
                    break;       
                }
    
                if( count % 100_000_000 == 0 )
                {
                    TimeSpan elapsed = DateTime.Now - start;
                    WriteVerbose( String.Format("Time: {0} - Name: {1} - Count (million): {2}", elapsed.ToString(), _bruteForce.Name, (double)count/1_000_000) );

                }
    
                count++;
            }

            WriteVerbose( _bruteForce.Name );
            WriteVerbose( DateTime.Now.ToString("HH:mm:ss.fff"));

        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
