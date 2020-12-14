using System;
using ILGPU;
using ILGPU.Runtime;

namespace MpqNameBreaker.Mpq
{
    
    public class HashCalculatorAccelerated
    {

                // Constants
        public const uint CryptTableSize = 0x500;
        public const uint CryptTableSeed = 0x00100001;
        public const uint HashSeed1 = 0x7FED7FED;
        public const uint HashSeed2 = 0xEEEEEEEE;

        // Properties
        public uint[] CryptTable {get; private set;}

        public Accelerator Accelerator {get; private set;}

        // Fields


        // Constructors
        public HashCalculatorAccelerated()
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

        public static void HashStringsBatch(
            Index1 index,
            ArrayView<byte> charset,                // 1D array holding the charset bytes
            ArrayView<uint> cryptTable,             // 1D array crypt table used for hash computation
            ArrayView2D<int> charsetIndexes,        // 2D array containing the char indexes of one batch string seed (one string per line, hashes will be computed starting from this string)
            ArrayView<byte> suffixBytes,            // 1D array holding the indexes of the suffix chars
            uint hashALookup,                       // The hash A that we are looking for
            uint hashBLookup,                       // The hash B that we are looking for
            uint seed1a,                            // Pre-computed hash A seed 1 for the string prefix
            uint seed2a,                            // Pre-computed hash A seed 2 for the string prefix
            uint seed1b,                            // Pre-computed hash B seed 1 for the string prefix
            uint seed2b,                            // Pre-computed hash B seed 2 for the string prefix
            bool firstBatch,
            int nameCount,                          // Name count limit (used as return condition)
            int batchCharCount,                     // Number of generated chars in the batch
            ArrayView<int> foundNameCharsetIndexes  // 1D array containing the found name (if found)
        )
        {
            // Brute force increment variables
            int generatedCharIndex = 0;

            // Hash variables
            uint ch;           // Current char of the processed string
            int typeA = 0x100; // Hash type A
            int typeB = 0x200; // Hash type B

            bool suffix = true;
            if( suffixBytes[0] == 0 )
                suffix = false;

            // Brute force increment preparation
            // Increase name count to !numChars-1 for first batch first name seed
            if( firstBatch && index == 0 )
            {
                nameCount = -1;
                for( int i = 1; i <= batchCharCount; i++ )
                {
                    int temp = 1;

                    for( int j = 0; j < i; j++ )
                        temp *= (int)charset.Length;
                    nameCount += temp;

                    if( i == batchCharCount )
                    {
                        temp = 1;
                        for( int j = 0; j < i; j++ )
                            temp *= (int)charset.Length;
                        nameCount += temp;
                    }
                }
            }
            
            // Find the position of the last generated char
            for( int i = 0; i < charsetIndexes.Height; i++ )
            {
                Index2 idx = new Index2( index.X, i );
                int charIndex = charsetIndexes[idx];
                if( charsetIndexes[idx] == -1 )
                {
                    generatedCharIndex = i - 1;
                    break;
                }
            }

            // For each name compute hash
            while( nameCount != 0 )
            {
                uint s1 = seed1a;
                uint s2 = seed2a;

                for( int i = 0; i < charsetIndexes.Height; i++ )
                {
                    // Retrieve the current char of the string
                    Index1 charsetIdx = charsetIndexes[new Index2( index.X, i )];

                    if( charsetIdx == -1 ) // break if end of the string is reached
                        break; 

                    ch = charset[ charsetIdx ];

                    // Hash calculation                    
                    s1 = cryptTable[typeA + ch] ^ (s1 + s2);
                    s2 = ch + s1 + s2 + (s2 << 5) + 3;
                }

                // Process suffix
                if( suffix )
                {
                    for( int i = 0; i < suffixBytes.Length; i++ )
                    {
                        // Retrieve current suffix char
                        ch = suffixBytes[i];

                        // Hash calculation                    
                        s1 = cryptTable[typeA + ch] ^ (s1 + s2);
                        s2 = ch + s1 + s2 + (s2 << 5) + 3;
                    }
                }

                // Check if it matches the hash that we are looking for
                if( s1 == hashALookup )
                {
                    s1 = seed1b;
                    s2 = seed2b;

                    for( int i = 0; i < charsetIndexes.Height; i++ )
                    {
                        // Retrieve the current char of the string
                        Index1 charsetIdx = charsetIndexes[new Index2( index.X, i )];

                        if( charsetIdx == -1 ) // break if end of the string is reached
                            break; 

                        ch = charset[ charsetIdx ];

                        // Hash calculation                    
                        s1 = cryptTable[typeB + ch] ^ (s1 + s2);
                        s2 = ch + s1 + s2 + (s2 << 5) + 3;
                    }

                    // Process suffix
                    if( suffix )
                    {
                        for( int i = 0; i < suffixBytes.Length; i++ )
                        {
                            // Retrieve current suffix char
                            ch = suffixBytes[i];

                            // Hash calculation                    
                            s1 = cryptTable[typeB + ch] ^ (s1 + s2);
                            s2 = ch + s1 + s2 + (s2 << 5) + 3;
                        }
                    }

                    if( s1 == hashBLookup )
                    {
                        // Populate foundNameCharsetIndexes and return
                        for( int i = 0; i < charsetIndexes.Height; i++ )
                            foundNameCharsetIndexes[i] = charsetIndexes[new Index2( index.X, i )];

                        return;
                    }

                }

                // Move to next name in the batch (brute force increment)

                // Debug
                /*
                var tes0 = charsetIndexes[new Index2(index.X,0)];
                var tes1 = charsetIndexes[new Index2(index.X,1)];
                var tes2 = charsetIndexes[new Index2(index.X,2)];
                var tes3 = charsetIndexes[new Index2(index.X,3)];
                var tes4 = charsetIndexes[new Index2(index.X,4)];
                var tes5 = charsetIndexes[new Index2(index.X,5)];
                */

                // If we are AT the last char of the charset
                if( charsetIndexes[new Index2( index.X, generatedCharIndex )] == charset.Length-1 )
                {
                    bool increaseNameSize = false;

                    // Go through all the charset indexes in reverse order
                    int stopValue = generatedCharIndex - batchCharCount + 1;
                    if( firstBatch )
                        stopValue = 0;

                    for( int i = generatedCharIndex; i >= stopValue; --i )
                    {
                        // Retrieve the current char of the string
                        Index2 idx = new Index2( index.X, i );

                        // If we are at the last char of the charset then go back to the first char
                        if( charsetIndexes[idx] == charset.Length-1 )
                        {
                            charsetIndexes[idx] = 0;
                            
                            if( i == 0 )
                                increaseNameSize = true;
                        }
                        // If we are not at the last char of the charset then move to next char
                        else
                        {
                            charsetIndexes[idx]++;
                            break;
                        }
                    }

                    if( increaseNameSize )
                    {
                        // Increase name size by one char
                        generatedCharIndex++;
                        charsetIndexes[new Index2( index.X, generatedCharIndex )] = 0;
                    }
                }
                // If the generated char is within the charset
                else
                {
                    // Move to next char
                    charsetIndexes[new Index2( index.X, generatedCharIndex )]++;
                }

                nameCount--;
            }

            /*
            // Debug
            var test0 = charsetIndexes[new Index2(index.X,0)];
            var test1 = charsetIndexes[new Index2(index.X,1)];
            var test2 = charsetIndexes[new Index2(index.X,2)];
            var test3 = charsetIndexes[new Index2(index.X,3)];
            var test4 = charsetIndexes[new Index2(index.X,4)];
            var test5 = charsetIndexes[new Index2(index.X,5)];
            */

        }

        public static void HashStringsBatchOptimized(
            Index1 index,
            ArrayView<byte> charset,                // 1D array holding the charset bytes
            ArrayView<uint> cryptTable,             // 1D array crypt table used for hash computation
            ArrayView2D<int> charsetIndexes,        // 2D array containing the char indexes of one batch string seed (one string per line, hashes will be computed starting from this string)
            ArrayView<byte> suffixBytes,            // 1D array holding the indexes of the suffix chars
            uint hashALookup,                       // The hash A that we are looking for
            uint hashBLookup,                       // The hash B that we are looking for
            uint seed1a,                            // Pre-computed hash A seed 1 for the string prefix
            uint seed2a,                            // Pre-computed hash A seed 2 for the string prefix
            uint seed1b,                            // Pre-computed hash B seed 1 for the string prefix
            uint seed2b,                            // Pre-computed hash B seed 2 for the string prefix
            bool firstBatch,
            int nameCount,                          // Name count limit (used as return condition)
            int batchCharCount,                     // Number of generated chars in the batch
            ArrayView<int> foundNameCharsetIndexes  // 1D array containing the found name (if found)
        )
        {
            // Brute force increment variables
            int generatedCharIndex = 0;

            // Hash variables
            uint ch;           // Current char of the processed string
            int typeA = 0x100; // Hash type A
            int typeB = 0x200; // Hash type B

            bool suffix = true;
            if( suffixBytes[0] == 0 )
                suffix = false;

            // Brute force increment preparation
            // Increase name count to !numChars-1 for first batch first name seed
            if( firstBatch && index == 0 )
            {
                nameCount = -1;
                for( int i = 1; i <= batchCharCount; i++ )
                {
                    int temp = 1;

                    for( int j = 0; j < i; j++ )
                        temp *= (int)charset.Length;
                    nameCount += temp;

                    if( i == batchCharCount )
                    {
                        temp = 1;
                        for( int j = 0; j < i; j++ )
                            temp *= (int)charset.Length;
                        nameCount += temp;
                    }
                }
            }
            
            // Find the position of the last generated char
            for( int i = 0; i < charsetIndexes.Height; i++ )
            {
                Index2 idx = new Index2( index.X, i );
                int charIndex = charsetIndexes[idx];
                if( charsetIndexes[idx] == -1 )
                {
                    generatedCharIndex = i - 1;
                    break;
                }
            }

            // For each name compute hash
            while( nameCount != 0 )
            {
                uint s1 = seed1a;
                uint s2 = seed2a;

                for( int i = 0; i < charsetIndexes.Height; i++ )
                {
                    // Retrieve the current char of the string
                    Index1 charsetIdx = charsetIndexes[new Index2( index.X, i )];

                    if( charsetIdx == -1 ) // break if end of the string is reached
                        break; 

                    ch = charset[ charsetIdx ];

                    // Hash calculation                    
                    s1 = cryptTable[typeA + ch] ^ (s1 + s2);
                    s2 = ch + s1 + s2 + (s2 << 5) + 3;
                }

                // Process suffix
                if( suffix )
                {
                    for( int i = 0; i < suffixBytes.Length; i++ )
                    {
                        // Retrieve current suffix char
                        ch = suffixBytes[i];

                        // Hash calculation                    
                        s1 = cryptTable[typeA + ch] ^ (s1 + s2);
                        s2 = ch + s1 + s2 + (s2 << 5) + 3;
                    }
                }

                // Check if it matches the hash that we are looking for
                if( s1 == hashALookup )
                {
                    s1 = seed1b;
                    s2 = seed2b;

                    for( int i = 0; i < charsetIndexes.Height; i++ )
                    {
                        // Retrieve the current char of the string
                        Index1 charsetIdx = charsetIndexes[new Index2( index.X, i )];

                        if( charsetIdx == -1 ) // break if end of the string is reached
                            break; 

                        ch = charset[ charsetIdx ];

                        // Hash calculation                    
                        s1 = cryptTable[typeB + ch] ^ (s1 + s2);
                        s2 = ch + s1 + s2 + (s2 << 5) + 3;
                    }

                    // Process suffix
                    if( suffix )
                    {
                        for( int i = 0; i < suffixBytes.Length; i++ )
                        {
                            // Retrieve current suffix char
                            ch = suffixBytes[i];

                            // Hash calculation                    
                            s1 = cryptTable[typeB + ch] ^ (s1 + s2);
                            s2 = ch + s1 + s2 + (s2 << 5) + 3;
                        }
                    }

                    if( s1 == hashBLookup )
                    {
                        // Populate foundNameCharsetIndexes and return
                        for( int i = 0; i < charsetIndexes.Height; i++ )
                            foundNameCharsetIndexes[i] = charsetIndexes[new Index2( index.X, i )];

                        return;
                    }

                }

                // Move to next name in the batch (brute force increment)
                // If we are AT the last char of the charset
                if( charsetIndexes[new Index2( index.X, generatedCharIndex )] == charset.Length-1 )
                {
                    bool increaseNameSize = false;

                    // Go through all the charset indexes in reverse order
                    int stopValue = generatedCharIndex - batchCharCount + 1;
                    if( firstBatch )
                        stopValue = 0;

                    for( int i = generatedCharIndex; i >= stopValue; --i )
                    {
                        // Retrieve the current char of the string
                        Index2 idx = new Index2( index.X, i );

                        // If we are at the last char of the charset then go back to the first char
                        if( charsetIndexes[idx] == charset.Length-1 )
                        {
                            charsetIndexes[idx] = 0;
                            
                            if( i == 0 )
                                increaseNameSize = true;
                        }
                        // If we are not at the last char of the charset then move to next char
                        else
                        {
                            charsetIndexes[idx]++;
                            break;
                        }
                    }

                    if( increaseNameSize )
                    {
                        // Increase name size by one char
                        generatedCharIndex++;
                        charsetIndexes[new Index2( index.X, generatedCharIndex )] = 0;
                    }
                }
                // If the generated char is within the charset
                else
                {
                    // Move to next char
                    charsetIndexes[new Index2( index.X, generatedCharIndex )]++;
                }

                nameCount--;
            }
        }
    }
}
