using System;
using System.Text;
using System.Diagnostics;
using System.Management.Automation;
using System.Management.Automation.Runspaces;
using MpqNameBreaker.Mpq;
using ILGPU;
using ILGPU.Runtime;

namespace MpqNameBreaker
{
    [Cmdlet(VerbsCommon.Get,"Accelerator")]
    [OutputType(typeof(uint))]
    public class GetAcceleratorCommand : PSCmdlet
    {
        // Fields
        private HashCalculator _hashCalculator;

        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            var context = new Context();

            // For each available accelerator...
            int id = 0;
            foreach( var acceleratorId in Accelerator.Accelerators )
            {
                var accelerator = Accelerator.Create( context, acceleratorId );

                // Output the accelerator information
                WriteObject( new {Id = id, Type = acceleratorId.AcceleratorType, Name = accelerator.Name} );
                
                id++;
            }
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
