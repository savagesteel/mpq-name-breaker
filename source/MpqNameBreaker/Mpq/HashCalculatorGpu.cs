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

        public static void MyKernel2( Index1 index, ArrayView2D<byte> dataView )
        {
            for( int i = 0; i < dataView.Height; i++ )
            {
                dataView[new Index2(index.X, i)] = (byte)i;
            }
        }
    }

}
