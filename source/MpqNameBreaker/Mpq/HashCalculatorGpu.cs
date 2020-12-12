using ILGPU;
using ILGPU.Runtime;
using System;

namespace MpqNameBreaker.Mpq
{
    
    public class HashCalculatorGpu
    {

                // Constants
        const uint CryptTableSize = 0x500;
        const uint CryptTableSeed = 0x00100001;
        const uint HashSeed1 = 0x7FED7FED;
        const uint HashSeed2 = 0xEEEEEEEE;

        // Properties
        public uint[] CryptTable {get; private set;}

        public Accelerator Accelerator {get; private set;}

        // Fields


        // Constructors
        public HashCalculatorGpu()
        { 
            InitializeCryptTable();
            InitializeGpuAccelarator();
        }

        // Methods
        public void InitializeCryptTable()
        {
            uint seed = CryptTableSeed;
            uint index1 = 0, index2 = 0;
            uint i;

            // Create the array with the proper size
            CryptTable = new uint[CryptTableSize];

            // Go through all the cells of the array
            for( index1 = 0; index1 < 0x100; index1++ )
            {
                for( index2 = index1, i = 0; i < 5; i++, index2 += 0x100 )
                {
                    uint temp1, temp2;

                    seed = (seed * 125 + 3) % 0x2AAAAB;
                    temp1  = (seed & 0xFFFF) << 0x10;

                    seed = (seed * 125 + 3) % 0x2AAAAB;
                    temp2  = (seed & 0xFFFF);

                    CryptTable[index2] = (temp1 | temp2);
                }
            }
        }

        public void InitializeGpuAccelarator()
        {
            var context = new Context();
            // For each available accelerator...
            foreach( var acceleratorId in Accelerator.Accelerators )
            {
                // Instanciate the Nvidia (CUDA) accelerator
                if( acceleratorId.AcceleratorType == AcceleratorType.Cuda )
                {
                    Accelerator = Accelerator.Create( context, acceleratorId );
                }
            }
        }

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

        public static void HashStringsBatchA(
            Index1 index,
            ArrayView<byte> charset,
            ArrayView2D<int> charsetIndexes,    // 2D array containing the char indexes of one batch string seed (one string per line, hashes will be computed starting from this string)
            // ArrayView2D<int> charsetStopIndexes // 2D array containing the char indexes where the batch will stop
            uint hashLookup,                    // The hash that we are looking for
            ArrayView<uint> cryptTable,         // 1D array crypt table used for hash computation
            int prefixLength,                   // String prefix length
            uint seed1,                         // Pre-computed seed 1 for the string prefix
            uint seed2,                         // Pre-computed seed 2 for the string prefix
            ArrayView<int> found                // 1D array with one element (value is set to 1 if the hash is found in the batch)
            // ? // Structure to hold the index in charsetIndexes when there's a hash match
        )
        {
            // Current char of the processed string
            uint ch;

            // Hash type A
            int type = 0x100;
/*
            for( int i = prefixLength; i < charsetIndexes.Height; i++ )
            {
                // Build 2D index for the strings 2D array
                Index2 idx = new Index2( index.X, i );

                // Retrieve the current char of the string
                ch = charset[ charsetIndexes[idx] ];

                // Break if we reached the end of the string (\0)
                if( ch == 0 )
                    break;
                
                seed1 = cryptTable[type + ch] ^ (seed1 + seed2);
                seed2 = ch + seed1 + seed2 + (seed2 << 5) + 3;
            }

            // Check if it matches the hash that we are looking for
            if( seed1 == hashLookup )
            {
                found[0] = 1;
            }


            // TODO: Check if the last name of the seed is reached
            // TODO: Add code to move to next name accordingly
*/

        }

    }

}
