using System;
using System.Text;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using MpqNameBreaker.Mpq;

namespace MpqNameBreaker
{
    [Cmdlet(VerbsCommon.Get,"MpqStringHash")]
    [OutputType(typeof(uint))]
    public class GetMpqStringHashCommand : PSCmdlet
    {
        [Parameter(
            Mandatory = true,
            Position = 0,
            ValueFromPipeline = true,
            ValueFromPipelineByPropertyName = true)]
        public string String { get; set; }

        [Parameter(
            Mandatory = true,
            Position = 1,
            ValueFromPipelineByPropertyName = true)]
        public HashType Type { get; set; }
  

        // Fields
        private HashCalculator _hashCalculator;

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
            WriteVerbose("Begin!");
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            WriteVerbose("Process");

            _hashCalculator = new HashCalculator();
            WriteObject( _hashCalculator.HashString( Encoding.ASCII.GetBytes(String), Type ) );
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
            WriteVerbose("End!");
        }
    }
}
