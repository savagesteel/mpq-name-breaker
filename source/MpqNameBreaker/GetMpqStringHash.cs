using System;
using System.Text;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using MpqNameBreaker.Mpq;

namespace MpqNameBreaker
{
    [Cmdlet(VerbsCommon.Get, "MpqStringHash")]
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
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            string strUpper;
            byte[] strBytes;
            uint hash;

            // Initialize hash calculator
            _hashCalculator = new HashCalculator();

            // Convert string to uppercase
            strUpper = String.ToUpper();
            // Get ASCII chars
            strBytes = Encoding.ASCII.GetBytes(strUpper);
            // Compute hash
            hash = _hashCalculator.HashString(strBytes, Type);

            // Output hash to console
            WriteObject(hash);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
