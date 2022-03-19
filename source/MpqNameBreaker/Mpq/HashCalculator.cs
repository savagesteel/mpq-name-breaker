namespace MpqNameBreaker.Mpq
{
    public enum HashType
    {
        MpqHashNameA = 0x100,
        MpqHashNameB = 0x200
    }

    public class HashCalculator
    {
        // Constants
        const uint CryptTableSize = 0x500;
        const uint CryptTableSeed = 0x00100001;
        const uint HashSeed1 = 0x7FED7FED;
        const uint HashSeed2 = 0xEEEEEEEE;

        // Properties
        public uint[] CryptTable { get; private set; }

        // Fields
        // Constructors
        public HashCalculator()
        {
            InitializeCryptTable();
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

        public uint HashString(byte[] str, HashType hashType)
        {
            uint seed1 = HashSeed1;
            uint seed2 = HashSeed2;
            uint ch;

            int type = (int)hashType;

            for (int i = 0; i < str.Length; i++)
            {
                ch = str[i];
                seed1 = CryptTable[type + ch] ^ (seed1 + seed2);
                seed2 = ch + seed1 + seed2 + (seed2 << 5) + 3;
            }

            return seed1;
        }

        public (uint, uint) HashStringOptimizedCalculateSeeds(byte[] str, HashType hashType)
        {
            uint seed1 = HashSeed1;
            uint seed2 = HashSeed2;
            uint ch;

            int type = (int)hashType;

            for (int i = 0; i < str.Length; i++)
            {
                ch = str[i];
                seed1 = CryptTable[type + ch] ^ (seed1 + seed2);
                seed2 = ch + seed1 + seed2 + (seed2 << 5) + 3;
            }

            return (seed1, seed2);
        }

        // Call HashStringOptimizedCalculateSeeds before this method
        public uint HashStringOptimized(byte[] str, HashType hashType, int prefixLength, uint seed1, uint seed2)
        {
            uint ch;

            int type = (int)hashType;

            for (int i = prefixLength; i < str.Length; i++)
            {
                ch = str[i];
                seed1 = CryptTable[type + ch] ^ (seed1 + seed2);
                seed2 = ch + seed1 + seed2 + (seed2 << 5) + 3;
            }

            return seed1;
        }
    }
}
