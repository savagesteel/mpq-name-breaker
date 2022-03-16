using System;
using System.Text;

namespace MpqNameBreaker.NameGenerator
{
    public class BruteForceBatches
    {
        // Constants
        public const int MaxGeneratedChars = 16;
        public const string DefaultCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-";

        public string Charset { get; private set; } = DefaultCharset;
        public byte[] CharsetBytes { get; private set; }


        // Properties

        // The number of name seeds that will be generated
        public int BatchSize { get; private set; }

        // Number of chars in one batch item.
        // e.g. if this number is 5 each batch item will contain Charset.Length ^ 5 names
        public int BatchItemCharCount { get; private set; }

        // The batch seeds are stored in a 2D array.
        // Each line contains the bytes of one seed name string.
        public int[,] BatchNameSeedCharsetIndexes { get; private set; }
        public string[] BatchNames { 
            get {
                string[] res = new string[BatchSize];
                byte[] str;

                for( int i = 0; i < BatchSize; ++i )
                {
                    str = new byte[MaxGeneratedChars];

                    res[i] = "";
                    for( int j = 0; j < MaxGeneratedChars; ++j )
                    {
                        int idx = BatchNameSeedCharsetIndexes[i,j];
                        //int nextIdx = BatchNameSeedCharsetIndexes[i+1,j];
                        
                        if( idx == -1 )
                            break;

                        res[i] += Convert.ToChar( CharsetBytes[ idx ] );
                    }
                }
                return res;
            } 
        }

        public bool Initialized { get; private set; } = false;

        public bool FirstBatch { get; private set; } = true;
        public int BatchNumber { get; private set; } = 0;

        // Fields
        private int _generatedCharIndex = 0;
        private int[] _nameCharsetIndexes;

        // Constructors
        public BruteForceBatches()
        {
            CharsetBytes = Encoding.ASCII.GetBytes( Charset.ToUpper() );
        }

        public BruteForceBatches( int size, int charCount, string additionalChars = "", string charset = DefaultCharset)
        {
            this.BatchSize = size;
            this.BatchItemCharCount = charCount;
            this.BatchNameSeedCharsetIndexes = new int[size, MaxGeneratedChars];

            this.Charset = charset + additionalChars;
            this.CharsetBytes = Encoding.ASCII.GetBytes( Charset.ToUpper() );
        }

        // Methods
        public void Initialize()
        {
            // Initialize an array to keep track of the indexes of each char in the charset
            _nameCharsetIndexes = new int[MaxGeneratedChars];
            for( int i = 0; i < MaxGeneratedChars; ++i )
                _nameCharsetIndexes[i] = -1;

            Initialized = true;
        }

        public bool NextBatchNameSeed()
        {
            if( !Initialized )
                throw new System.ArgumentException();

            if( _generatedCharIndex == MaxGeneratedChars )
                return false;

            // If we are AT the last char of the charset
            if( _nameCharsetIndexes[_generatedCharIndex] == CharsetBytes.Length-1 )
            {
                bool increaseNameSize = false;

                // Go through all the charset indexes in reverse order
                for( int i = _generatedCharIndex; i >= 0; --i )
                {
                    // If we are at the last char of the charset then go back to the first char
                    if( _nameCharsetIndexes[i] == CharsetBytes.Length-1 )
                    {
                        _nameCharsetIndexes[i] = 0;
                        
                        if( i == 0 )
                            increaseNameSize = true;
                    }
                    // If we are not at the last char of the charset then move to next char
                    else
                    {
                        _nameCharsetIndexes[i]++;
                        break;
                    }
                }

                if( increaseNameSize )
                {
                    // Increase name size by one char
                    _generatedCharIndex++;
                    _nameCharsetIndexes[_generatedCharIndex] = 0;
                }
            }
            // If the generated char is within the charset
            else
            {
                // Move to next char
                _nameCharsetIndexes[_generatedCharIndex]++;
            }

            return true;
        }

        public bool NextBatch()
        {
            if( FirstBatch & BatchNumber > 0 )
                FirstBatch = false;
            
            int count = 0;
            while( NextBatchNameSeed() && count < BatchSize )
            {
                // Copy name charset indexes in the batch 2D array
                for( int i = 0; i < MaxGeneratedChars; ++i )
                {
                    BatchNameSeedCharsetIndexes[count,i] = _nameCharsetIndexes[i];
                }

                if( FirstBatch == false || count > 0 )
                {
                    // Copy additional seed bytes to the 2D array
                    for( int j = _generatedCharIndex+1; j < _generatedCharIndex+1+BatchItemCharCount; j++ )
                    {
                        BatchNameSeedCharsetIndexes[count,j] = 0;
                    }
                }


                count++;
            }
  
            BatchNumber++;
            return true;
        }
    }

    public class BruteForceBatches3D
    {
        // Constants
        public const int MaxGeneratedChars = 16;
        //public const string Charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-.()\\";
        // ".\\()"

        public string Charset { get; private set; }
        public byte[] CharsetBytes { get; private set; }


        // Properties

        // The number of name seeds that will be generated
        public int BatchSize { get; private set; }

        // Number of chars in one batch item.
        // e.g. if this number is 5 each batch item will contain Charset.Length ^ 5 names
        public int BatchItemCharCount { get; private set; } 

        // The batch seeds are stored in a 2D array.
        // Each line contains the bytes of one seed name string.
        public int[,,] BatchNameSeedCharsetIndexes { get; private set; }
        public string[,] BatchNames { 
            get {
                string[,] res = new string[BatchSize,BatchSize];
                byte[] str;

                for( int i = 0; i < BatchSize; ++i )
                {
                    for( int j = 0; j < BatchSize; ++j )
                    {
                        str = new byte[MaxGeneratedChars];

                        res[i,j] = "";
                        for( int k = 0; k < MaxGeneratedChars; ++k )
                        {
                            int idx = BatchNameSeedCharsetIndexes[i,j,k];
                            
                            if( idx == -1 )
                                break;

                            res[i,j] += Convert.ToChar( CharsetBytes[ idx ] );
                        }
                    }
                }
                return res;
            } 
        }

        public bool Initialized { get; private set; }

        public bool FirstBatch { get; private set; }
        public int BatchNumber { get; private set; }

        // Fields
        private int _generatedCharIndex;
        private int[] _nameCharsetIndexes;


        
        // Constructors
        public BruteForceBatches3D()
        {
            Initialized = false;
            Charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-";
            CharsetBytes = Encoding.ASCII.GetBytes( Charset.ToUpper() );
            FirstBatch = true;
            BatchNumber = 0;
            _generatedCharIndex = 0;
        }

        public BruteForceBatches3D( int size, int charCount ) : this()
        {
            this.BatchSize = size;
            this.BatchItemCharCount = charCount;

            BatchNameSeedCharsetIndexes = new int[ size, size, MaxGeneratedChars ];
        }

        public BruteForceBatches3D( int size, int charCount, string additionalChars ) : this( size, charCount )
        {
            Charset += additionalChars;
            CharsetBytes = Encoding.ASCII.GetBytes( Charset.ToUpper() );
        }

        // Methods
        public void Initialize()
        {
            // Initialize an array to keep track of the indexes of each char in the charset
            _nameCharsetIndexes = new int[MaxGeneratedChars];
            for( int i = 0; i < MaxGeneratedChars; ++i )
                _nameCharsetIndexes[i] = -1;

            Initialized = true;
        }

        public bool NextBatchNameSeed()
        {
            if( !Initialized )
                throw new System.ArgumentException();

            if( _generatedCharIndex == MaxGeneratedChars )
                return false;

            // If we are AT the last char of the charset
            if( _nameCharsetIndexes[_generatedCharIndex] == CharsetBytes.Length-1 )
            {
                bool increaseNameSize = false;

                // Go through all the charset indexes in reverse order
                for( int i = _generatedCharIndex; i >= 0; --i )
                {
                    // If we are at the last char of the charset then go back to the first char
                    if( _nameCharsetIndexes[i] == CharsetBytes.Length-1 )
                    {
                        _nameCharsetIndexes[i] = 0;
                        
                        if( i == 0 )
                            increaseNameSize = true;
                    }
                    // If we are not at the last char of the charset then move to next char
                    else
                    {
                        _nameCharsetIndexes[i]++;
                        break;
                    }
                }

                if( increaseNameSize )
                {
                    // Increase name size by one char
                    _generatedCharIndex++;
                    _nameCharsetIndexes[_generatedCharIndex] = 0;
                }
            }
            // If the generated char is within the charset
            else
            {
                // Move to next char
                _nameCharsetIndexes[_generatedCharIndex]++;
            }

            return true;
        }

        public bool NextBatch()
        {
            if( FirstBatch & BatchNumber > 0 )
                FirstBatch = false;
            
            for( int i = 0; i < BatchSize; ++i )
            {
                for( int j = 0; j < BatchSize; ++j)
                {
                    NextBatchNameSeed();

                    // Copy name charset indexes in the batch 3D array
                    for( int k = 0; k < MaxGeneratedChars; ++k )
                    {
                        BatchNameSeedCharsetIndexes[i,j,k] = _nameCharsetIndexes[k];
                    }

                    if( FirstBatch == false || i > 0 || j > 0 )
                    {
                        // Copy additional seed bytes to the 3D array
                        for( int k = _generatedCharIndex+1; k < _generatedCharIndex+1+BatchItemCharCount; k++ )
                        {
                            BatchNameSeedCharsetIndexes[i,j,k] = 0;
                        }
                    }
                }
            }
  
            BatchNumber++;
            return true;
        }
    }


}
