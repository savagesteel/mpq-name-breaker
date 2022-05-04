using System.Text;
using System.Threading.Tasks;

namespace MpqNameBreaker.NameGenerator
{
    public class BruteForceBatches
    {
        // Constants
        public const int MaxGeneratedChars = 16;
        public const string DefaultCharset = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ_-";

        public string Charset { get; } = DefaultCharset;
        public byte[] CharsetBytes { get; }


        // Properties
        // The number of name seeds that will be generated
        public int BatchSize { get; }

        // Number of chars in one batch item.
        // e.g. if this number is 5 each batch item will contain Charset.Length ^ 5 names
        public int BatchItemCharCount { get; }

        public bool Initialized { get; private set; } = false;

        // Fields

        // The batch seeds are stored in a 2D array.
        // Each line contains the bytes of one seed name string.
        private readonly int[,] batchNameSeedCharsetIndexes;

        private bool firstBatch = true;
        private int batchNumber = 0;

        private int _generatedCharIndex = 0;
        private int[] _nameCharsetIndexes;

        private readonly object asyncBatchLock = new object();
        private readonly object batchLock = new object();
        private Task<Batch> nextBatchTask = null;

        public class Batch
        {
            public bool FirstBatch { get; } = true;
            public int BatchNumber { get; } = 0;
            public int[,] BatchNameSeedCharsetIndexes { get; }

            public Batch(bool firstBatch, int batchNum, int[,] charsetIndexes)
            {
                this.FirstBatch = firstBatch;
                this.BatchNumber = batchNum;
                this.BatchNameSeedCharsetIndexes = charsetIndexes.Clone() as int[,];
            }
        }


        // Constructors
        public BruteForceBatches()
        {
            CharsetBytes = Encoding.ASCII.GetBytes(Charset.ToUpper());
        }

        public BruteForceBatches(int size, int charCount, string additionalChars = "", string charset = DefaultCharset)
        {
            this.BatchSize = size;
            this.BatchItemCharCount = charCount;
            this.batchNameSeedCharsetIndexes = new int[size, MaxGeneratedChars];

            this.Charset = charset + additionalChars;
            this.CharsetBytes = Encoding.ASCII.GetBytes(Charset.ToUpper());
        }

        // Methods
        public void Initialize()
        {
            // Initialize an array to keep track of the indexes of each char in the charset
            _nameCharsetIndexes = new int[MaxGeneratedChars];
            for (int i = 0; i < MaxGeneratedChars; ++i)
                _nameCharsetIndexes[i] = -1;

            Initialized = true;
            nextBatchTask = Task.Run(NextBatchAsync);
        }

        public bool NextBatchNameSeed()
        {
            if (!Initialized)
                throw new System.InvalidOperationException("Batch not initialized");

            if (_generatedCharIndex == MaxGeneratedChars)
                return false;

            // If we are AT the last char of the charset
            if (_nameCharsetIndexes[_generatedCharIndex] == CharsetBytes.Length - 1)
            {
                bool increaseNameSize = false;

                // Go through all the charset indexes in reverse order
                for (int i = _generatedCharIndex; i >= 0; --i)
                {
                    // If we are at the last char of the charset then go back to the first char
                    if (_nameCharsetIndexes[i] == CharsetBytes.Length - 1)
                    {
                        _nameCharsetIndexes[i] = 0;

                        if (i == 0)
                            increaseNameSize = true;
                    }
                    // If we are not at the last char of the charset then move to next char
                    else
                    {
                        _nameCharsetIndexes[i]++;
                        break;
                    }
                }

                if (increaseNameSize)
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

        private Batch NextBatchAsync()
        {
            lock (asyncBatchLock)
            {
                // TODO: mutex here
                if (firstBatch & batchNumber > 0)
                    firstBatch = false;

                int count = 0;
                while (NextBatchNameSeed() && count < BatchSize)
                {
                    // Copy name charset indexes in the batch 2D array
                    for (int i = 0; i < MaxGeneratedChars; ++i)
                    {
                        batchNameSeedCharsetIndexes[count, i] = _nameCharsetIndexes[i];
                    }

                    if (firstBatch == false || count > 0)
                    {
                        // Copy additional seed bytes to the 2D array
                        for (int j = _generatedCharIndex + 1; j < _generatedCharIndex + 1 + BatchItemCharCount; j++)
                        {
                            batchNameSeedCharsetIndexes[count, j] = 0;
                        }
                    }


                    count++;
                }

                batchNumber++;
                return new Batch(firstBatch, batchNumber, batchNameSeedCharsetIndexes);
            }
        }

        public Batch NextBatch()
        {
            lock (batchLock)
            {
                if (!Initialized)
                    throw new System.InvalidOperationException("Batch not initialized");

                Batch result = nextBatchTask.Result;
                nextBatchTask = Task.Run(NextBatchAsync);
                return result;
            }
        }
    }
}
