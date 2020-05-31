using System;
using System.Text;

namespace MpqNameBreaker.NameGenerator
{
    public class BruteForce
    {
        // Constants
        const int MaxGeneratedChars = 12;
        const string Charset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-";
        // ".\\"

        // Properties
        public string Prefix {get; private set;}
        public byte[] PrefixBytes {get; private set;}

        public string Suffix {get; private set;}
        public byte[] SuffixBytes {get; private set;}

        public string Name {
            get {
                return Encoding.ASCII.GetString( NameBytes );
            }
        }
        public byte[] NameBytes { get; private set; }
        public bool Initialized { get; private set; }

        // Fields
        private byte[] _charsetBytes;
        private int _prefixBytesCount;
        private int _suffixBytesCount;
        private int _generatedCharIndex;
        //private int _currentCharsetIndex;
        private int[] _allCharsetIndexes;

        // Constructors
        public BruteForce()
        {
            Initialized = false;

            _charsetBytes = Encoding.ASCII.GetBytes( Charset.ToUpper() );
            _prefixBytesCount = 0;
            _suffixBytesCount = 0;

            _generatedCharIndex = 0;
        }

        public BruteForce( string prefix, string suffix ) : this()
        {
            Prefix = prefix;
            Suffix = suffix;

            if( prefix != "" )
            {
                PrefixBytes = Encoding.ASCII.GetBytes( prefix.ToUpper() );
                _prefixBytesCount = PrefixBytes.Length;
            }
            if( suffix != "" )
            {
                SuffixBytes = Encoding.ASCII.GetBytes( suffix.ToUpper() );
                _suffixBytesCount = SuffixBytes.Length;
            }
        }

        // Methods
        public void Initialize()
        {
            // Initialize an array to keep track of the indexes of each char in the charset
            _allCharsetIndexes = new int[MaxGeneratedChars];
            for( int i = 0; i < MaxGeneratedChars; i++ )
                _allCharsetIndexes[i] = 0;

            // We start with one generated character plus prefix and suffix length
            NameBytes = new byte[ _prefixBytesCount + 1 + _suffixBytesCount ];

            if( _prefixBytesCount > 0 )
                Array.Copy( PrefixBytes, 0, NameBytes, 0, _prefixBytesCount );
            if( _suffixBytesCount > 0 )
                Array.Copy( SuffixBytes, 0, NameBytes, _prefixBytesCount+1, _suffixBytesCount );

            Initialized = true;
        }

        public void RefreshNameChar( int generatedCharIndex )
        {
            NameBytes[_prefixBytesCount+generatedCharIndex] = _charsetBytes[ _allCharsetIndexes[generatedCharIndex] ];
        }

        public void RefreshNameAllChars()
        {
            NameBytes = new byte[ _prefixBytesCount + _generatedCharIndex + 1 + _suffixBytesCount ];
            
            // Prefix
            if( _prefixBytesCount > 0 )
                Array.Copy( PrefixBytes, 0, NameBytes, 0, _prefixBytesCount );
            
            // Generated chars
            for( int i = 0; i <= _generatedCharIndex; i++ )
                NameBytes[_prefixBytesCount+i] = _charsetBytes[ _allCharsetIndexes[i] ];

            // Suffix
            if( _suffixBytesCount > 0 )
                Array.Copy( SuffixBytes, 0, NameBytes, _prefixBytesCount+_generatedCharIndex+1, _suffixBytesCount );
        }
        
        // _generatedCharIndex is the number of generated chars - 1
        // rename to _nameCharIndex?
        public bool NextName()
        {
            if( !Initialized )
                throw new System.ArgumentException();

            if( _generatedCharIndex == MaxGeneratedChars )
                return false;

            // If we are AFTER the last char of the charset
            if( _allCharsetIndexes[_generatedCharIndex] == _charsetBytes.Length )
            {
                bool increaseNameSize = false;

                // Go through all the charset indexes in reverse order
                for( int i = _generatedCharIndex; i >= 0; i-- )
                {
                    // If we are at the last char of the charset then go back to the first char
                    if( _allCharsetIndexes[i] >= _charsetBytes.Length-1 )
                    {
                        _allCharsetIndexes[i] = 0;
                        RefreshNameChar(i);
                        
                        if( i == 0 )
                            increaseNameSize = true;
                    }
                    // If we are not at the last char of the charset then move to next char
                    else
                    {
                        _allCharsetIndexes[i]++;
                        RefreshNameChar(i);
                        break;
                    }
                }

                //_allCharsetIndexes[_generatedCharIndex] = 0;

                if( increaseNameSize )
                {
                    // Increase name size by one char
                    _generatedCharIndex++;
                    RefreshNameAllChars();
                }
                else
                {
                    RefreshNameChar( _generatedCharIndex );
                }
            }
            // If we are in the charset
            else
            {
                RefreshNameChar( _generatedCharIndex );

            }

            // Move to next char
            _allCharsetIndexes[_generatedCharIndex]++;

            return true;
        }
    }
}
