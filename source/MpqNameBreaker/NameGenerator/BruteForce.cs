using System;
using System.Text;

namespace MpqNameBreaker.NameGenerator
{
    public class BruteForce
    {
        // Constants
        const int MaxGeneratedChars = 12;
        const string Charset = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789_-";
        // ".\\"

        // Properties
        public string Prefix {get; private set;}
        public string Suffix {get; private set;}
        public string Name {
            get {
                return Encoding.ASCII.GetString( NameBytes );
            }
        }
        public byte[] NameBytes { get; private set; }
        public bool Initialized { get; private set; }

        // Fields
        private byte[] _charsetBytes;
        private byte[] _prefixBytes;
        private int _prefixBytesCount;
        private byte[] _suffixBytes;
        private int _suffixBytesCount;
        private int _generatedCharIndex;
        private int _currentCharsetIndex;
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
                _prefixBytes = Encoding.ASCII.GetBytes( prefix.ToUpper() );
                _prefixBytesCount = _prefixBytes.Length;
            }
            if( suffix != "" )
            {
                _suffixBytes = Encoding.ASCII.GetBytes( suffix.ToUpper() );
                _suffixBytesCount = _suffixBytes.Length;
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

            Array.Copy( _prefixBytes, 0, NameBytes, 0, _prefixBytesCount );
            Array.Copy( _suffixBytes, 0, NameBytes, _prefixBytesCount+1, _suffixBytesCount );

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
            Array.Copy( _prefixBytes, 0, NameBytes, 0, _prefixBytesCount );
            
            // Generated chars
            for( int i = 0; i <= _generatedCharIndex; i++ )
                NameBytes[_prefixBytesCount+i] = _charsetBytes[ _allCharsetIndexes[i] ];

            // Suffix
            Array.Copy( _suffixBytes, 0, NameBytes, _prefixBytesCount+_generatedCharIndex+1, _suffixBytesCount );
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
