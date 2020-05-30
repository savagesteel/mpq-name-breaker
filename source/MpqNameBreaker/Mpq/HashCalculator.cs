namespace MpqNameBreaker.Mpq
{
    public enum HashType
    {
        MpqHashNameA,
        MpqHashNameB
    }

    public class HashCalculator
    {
        // Constants
        const uint CryptTableSize = 0x500;
        const uint CryptTableSeed = 0x00100001;
        const uint HashSeedA = 0x7FED7FED;
        const uint HashSeedB = 0xEEEEEEEE;

        // Properties
        public uint[] CryptTable {get; private set;}

        // Fields


        // Constructors
        public HashCalculator()
        {
            InitializeCryptTable();
        }

        // Methods
        public void InitializeCryptTable()
        {
            
        }

        public uint HashString( byte[] str, HashType type )
        {
            return 0;
        }

    }
}
