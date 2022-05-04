using ILGPU;
using ILGPU.Runtime;
using ILGPU.Runtime.Cuda;
using ILGPU.Runtime.OpenCL;
using System.Linq;

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
        public uint[] CryptTable { get; private set; }

        public Context GPUContext { get; private set; }

        // Constructors
        public HashCalculatorAccelerated()
        {
            InitializeCryptTable();
            InitializeGpuContext();
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
            for (index1 = 0; index1 < 0x100; index1++)
            {
                for (index2 = index1, i = 0; i < 5; i++, index2 += 0x100)
                {
                    uint temp1, temp2;

                    seed = ((seed * 125) + 3) % 0x2AAAAB;
                    temp1 = (seed & 0xFFFF) << 0x10;

                    seed = ((seed * 125) + 3) % 0x2AAAAB;
                    temp2 = (seed & 0xFFFF);

                    CryptTable[index2] = (temp1 | temp2);
                }
            }
        }

        public void InitializeGpuContext()
        {
            GPUContext = Context.Create(builder =>
            {
                // Notes: OptimizationLevel.O2 is actually really slow, not sure how to leverage it better if at all.
                builder.Optimize(OptimizationLevel.O1)
                .AllAccelerators()
                .OpenCL()
                .Cuda();
            });
        }

        public Device GetBestDevice()
        {
            return GPUContext.Devices.OrderByDescending(device => device.MaxNumThreads).First();
        }

        public static void HashStringsBatchOptimized(
            Index1D index,
            ArrayView<byte> charset,                // 1D array holding the charset bytes
            ArrayView<uint> cryptTable,             // 1D array crypt table used for hash computation
            ArrayView2D<int, Stride2D.DenseX> charsetIndexes,        // 2D array containing the char indexes of one batch string seed (one string per line, hashes will be computed starting from this string)
            ArrayView<byte> suffixBytes,            // 1D array holding the indexes of the suffix chars
            uint hashALookup,                       // The hash A that we are looking for
            uint hashBLookup,                       // The hash B that we are looking for
            uint prefixSeed1a,                      // Pre-computed hash A seed 1 for the string prefix
            uint prefixSeed2a,                      // Pre-computed hash A seed 2 for the string prefix
            uint prefixSeed1b,                      // Pre-computed hash B seed 1 for the string prefix
            uint prefixSeed2b,                      // Pre-computed hash B seed 2 for the string prefix
            bool firstBatch,
            int nameCount,                          // Name count limit (used as return condition)
            int batchCharCount, // MAX = 8          // Number of generated chars in the batch
            ArrayView<int> foundNameCharsetIndexes  // 1D array containing the found name (if found)
        )
        {
            // Brute force increment variables
            int generatedCharIndex = 0;

            // Hash variables
            uint ch;           // Current char of the processed string
            uint s1, s2;       // Hash seeds
            int typeA = 0x100; // Hash type A
            int typeB = 0x200; // Hash type B

            bool suffix = true;
            if (suffixBytes[0] == 0)
                suffix = false;

            // Hash precalculated seeds (after prefix)
            uint[] precalcSeeds1 = new uint[8];
            uint[] precalcSeeds2 = new uint[8];
            precalcSeeds1[0] = prefixSeed1a;
            precalcSeeds2[0] = prefixSeed2a;
            int precalcSeedIndex = 0;

            // Brute force increment preparation
            // Increase name count to !numChars-1 for first batch first name seed
            if (firstBatch && index == 0)
            {
                nameCount = -1;
                for (int i = 1; i <= batchCharCount; ++i)
                {
                    int temp = 1;

                    for (int j = 0; j < i; j++)
                        temp *= (int)charset.Length;
                    nameCount += temp;

                    if (i == batchCharCount)
                    {
                        temp = 1;
                        for (int j = 0; j < i; j++)
                            temp *= (int)charset.Length;
                        nameCount += temp;
                    }
                }
            }

            // Find the position of the last generated char
            for (int i = 0; i < charsetIndexes.Extent.Y; ++i)
            {
                Index2D idx = new Index2D(index.X, i);
                if (charsetIndexes[idx] == -1)
                {
                    generatedCharIndex = i - 1;
                    break;
                }
            }

            // For each name compute hash
            while (nameCount != 0)
            {
                // Subsequent names
                s1 = precalcSeeds1[precalcSeedIndex];
                s2 = precalcSeeds2[precalcSeedIndex];

                // Hash calculation
                for (int i = precalcSeedIndex; i < charsetIndexes.Extent.Y; ++i)
                {
                    // Retrieve the current char of the string
                    Index1D charsetIdx = charsetIndexes[new Index2D(index.X, i)];

                    if (charsetIdx == -1) // break if end of the string is reached
                        break;

                    ch = charset[charsetIdx];

                    // Hash calculation
                    s1 = cryptTable[typeA + ch] ^ (s1 + s2);
                    s2 = ch + s1 + s2 + (s2 << 5) + 3;

                    // Store precalc seeds except if we are at the last character of the string
                    // (then it's not needed because this char changes constantly)
                    if (i < generatedCharIndex)
                    {
                        precalcSeedIndex++;
                        precalcSeeds1[precalcSeedIndex] = s1;
                        precalcSeeds2[precalcSeedIndex] = s2;
                    }
                }

                // Process suffix
                if (suffix)
                {
                    for (int i = 0; i < suffixBytes.Length; ++i)
                    {
                        // Retrieve current suffix char
                        ch = suffixBytes[i];

                        // Hash calculation                    
                        s1 = cryptTable[typeA + ch] ^ (s1 + s2);
                        s2 = ch + s1 + s2 + (s2 << 5) + 3;
                    }
                }

                // Check if it matches the hash that we are looking for
                // No precalculation because this is only executed on matches and collisions
                if (s1 == hashALookup)
                {
                    s1 = prefixSeed1b;
                    s2 = prefixSeed2b;

                    for (int i = 0; i < charsetIndexes.Extent.Y; ++i)
                    {
                        // Retrieve the current char of the string
                        Index1D charsetIdx = charsetIndexes[new Index2D(index.X, i)];

                        if (charsetIdx == -1) // break if end of the string is reached
                            break;

                        ch = charset[charsetIdx];

                        // Hash calculation                    
                        s1 = cryptTable[typeB + ch] ^ (s1 + s2);
                        s2 = ch + s1 + s2 + (s2 << 5) + 3;
                    }

                    // Process suffix
                    if (suffix)
                    {
                        for (int i = 0; i < suffixBytes.Length; ++i)
                        {
                            // Retrieve current suffix char
                            ch = suffixBytes[i];

                            // Hash calculation                    
                            s1 = cryptTable[typeB + ch] ^ (s1 + s2);
                            s2 = ch + s1 + s2 + (s2 << 5) + 3;
                        }
                    }

                    if (s1 == hashBLookup)
                    {
                        // Populate foundNameCharsetIndexes and return
                        for (int i = 0; i < charsetIndexes.Extent.Y; ++i)
                            foundNameCharsetIndexes[i] = charsetIndexes[new Index2D(index.X, i)];

                        return;
                    }
                }

                // Move to next name in the batch (brute force increment)
                // If we are AT the last char of the charset
                if (charsetIndexes[new Index2D(index.X, generatedCharIndex)] == charset.Length - 1)
                {
                    bool increaseNameSize = false;

                    // Go through all the charset indexes in reverse order
                    int stopValue = generatedCharIndex - batchCharCount + 1;
                    if (firstBatch)
                        stopValue = 0;

                    for (int i = generatedCharIndex; i >= stopValue; --i)
                    {
                        // Retrieve the current char of the string
                        Index2D idx = new Index2D(index.X, i);

                        // If we are at the last char of the charset then go back to the first char
                        if (charsetIndexes[idx] == charset.Length - 1)
                        {
                            charsetIndexes[idx] = 0;

                            if (i == 0)
                                increaseNameSize = true;

                            // Go back in the precalc seeds (to recalculate since the char changed)
                            if (precalcSeedIndex > 0)
                                precalcSeedIndex--;
                        }
                        // If we are not at the last char of the charset then move to next char
                        else
                        {
                            charsetIndexes[idx]++;
                            break;
                        }
                    }

                    if (increaseNameSize)
                    {
                        // Increase name size by one char
                        generatedCharIndex++;
                        charsetIndexes[new Index2D(index.X, generatedCharIndex)] = 0;
                    }
                }
                // If the generated char is within the charset
                else
                {
                    // Move to next char
                    charsetIndexes[new Index2D(index.X, generatedCharIndex)]++;
                }

                nameCount--;
            }
        }
    }
}
