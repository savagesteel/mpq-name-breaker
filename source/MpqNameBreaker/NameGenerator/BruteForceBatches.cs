using System;
using System.Text;

namespace MpqNameBreaker.NameGenerator
{
    public class BruteForceBatches
    {
        // Constants
        const int MaxGeneratedChars = 16;
        const string Charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-";
        // ".\\"

        // Properties

        // The number of name seeds that will be generated
        public int BatchSize { get; private set; }

        // Number of chars in one batch item.
        // e.g. if this number is 5 each batch item will contain Charset.Length ^ 5 names
        public int BatchItemCharCount { get; private set; } 

        // The batch seeds are stored in a 2D array.
        // Each line contains the bytes of one seed name string.
        public byte[,] BatchSeedNameBytes { get; private set; }

        public bool Initialized { get; private set; }

        // Fields
        private byte[] _charsetBytes;
        
        // Constructors
        public BruteForceBatches()
        {
            Initialized = false;

            _charsetBytes = Encoding.ASCII.GetBytes( Charset.ToUpper() );
        }

        public BruteForceBatches( int size, int charCount ) : this()
        {
            this.BatchSize = size;
            this.BatchItemCharCount = charCount;

            BatchSeedNameBytes = new byte[ size, MaxGeneratedChars ];
        }


        // Methods
        public void Initialize()
        {
            Initialized = true;
        }


        public bool NextBatch()
        {



            return false;
        }


    }
}
