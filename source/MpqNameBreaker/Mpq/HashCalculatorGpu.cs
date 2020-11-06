using ILGPU;
using ILGPU.Runtime;
using System;

namespace MpqNameBreaker.Mpq
{
    public class HashCalculatorGpu
    {
        public static void MyKernel(
            Index1 index,              // The global thread index (1D in this case)
            ArrayView<int> dataView,   // A view to a chunk of memory (1D in this case)
            int constant)              // A sample uniform constant
        {
            dataView[index] = index + constant;
        }
    }

}
