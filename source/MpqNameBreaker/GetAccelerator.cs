using System.Linq;
using System.Management.Automation;
using ILGPU;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;

namespace MpqNameBreaker
{
    [Cmdlet(VerbsCommon.Get,"Accelerator")]
    [OutputType(typeof(uint))]
    public class GetAcceleratorCommand : PSCmdlet
    {
        // This method gets called once for each cmdlet in the pipeline when the pipeline starts executing
        protected override void BeginProcessing()
        {
        }

        // This method will be called for each input received from the pipeline to this cmdlet; if no input is received, this method is not called
        protected override void ProcessRecord()
        {
            var context = Context.Create(builder =>
            {
                builder.AllAccelerators()
                .Cuda()
                .OpenCL();
            });

            // For each available accelerator...
            context.Devices.ToList().ForEach(WriteObject);
        }

        // This method will be called once at the end of pipeline execution; if no input is received, this method is not called
        protected override void EndProcessing()
        {
        }
    }
}
