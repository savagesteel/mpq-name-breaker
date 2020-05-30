using System;
using System.Text;
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
            // Initialize hash calculator
            _hashCalculator = new HashCalculator();

            // Initialize brute force name generator
            _bruteForce = new BruteForce( Prefix, Suffix );
            _bruteForce.Initialize();

            int count = 0;
            while( _bruteForce.NextName() && count < 56354 )
            {
                WriteObject( _bruteForce.Name );
                count++;
            }
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
