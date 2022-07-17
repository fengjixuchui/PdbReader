using System.IO.MemoryMappedFiles;

namespace PdbReader
{
    public class Pdb
    {
        private const string SymbolCacheRelativePath = @"AppData\Local\Temp\SymbolCache";
        private MemoryMappedFile _mappedPdb;
        private MemoryMappedViewAccessor _mappedPdbView;
        private readonly FileInfo _pdbFile;
        private List<List<uint>> _streamDescriptors = new List<List<uint>>();
        private readonly MSFSuperBlock _superBlock;
        private readonly TraceFlags _traceFlags;

        public Pdb(FileInfo target, TraceFlags traceFlags = 0)
        {
            _pdbFile = target ?? throw new ArgumentNullException(nameof(target));
            _traceFlags = traceFlags;
            if (!_pdbFile.Exists) {
                throw new ArgumentException($"Input file doesn't exist : '{_pdbFile.FullName}'");
            }
            try {
                _mappedPdb = MemoryMappedFile.CreateFromFile(_pdbFile.FullName);
                _mappedPdbView = _mappedPdb.CreateViewAccessor();
            }
            catch (Exception ex) {
                throw new PDBFormatException("Unable to map PDB file.", ex);
            }
            try { _mappedPdbView.Read(0, out _superBlock); }
            catch (Exception ex){
                throw new PDBFormatException("Unable to read PDB superblock.", ex);
            }
            _superBlock.AssertSignature();
            LoadStreamDirectory();
        }

        internal bool ShouldTraceStreamDirectory
            => (0 != (_traceFlags & TraceFlags.StreamDirectoryBlocks));

        internal MSFSuperBlock SuperBlock => _superBlock;

        internal static uint Ceil(uint value, uint dividedBy)
        {
            if (0 == dividedBy) {
                throw new ArgumentException(nameof(dividedBy));
            }
            if (0 == value) { return 1; }
            return 1 + ((value - 1) / dividedBy);
        }

        /// <summary>Ensure the symbol cache directory exists otherwise create
        /// it.</summary>
        /// <returns>A descriptor for the cache directory.</returns>
        /// <exception cref="BugException"></exception>
        private static DirectoryInfo EnsureSymbolCacheDirectory()
        {
            string? userProfileDirectory = Environment.GetEnvironmentVariable("USERPROFILE");
            if (null == userProfileDirectory) {
                throw new BugException();
            }
            DirectoryInfo result = new DirectoryInfo(
                Path.Combine(userProfileDirectory, SymbolCacheRelativePath));
            if (!result.Exists) {
                result.Create();
                result.Refresh();
            }
            return result;
        }

        internal uint GetBlockOffset(uint blockNumber)
        {
            if (blockNumber >= _superBlock.NumBlocks) {
                throw new ArgumentOutOfRangeException(nameof(blockNumber));
            }
            ulong result = blockNumber * _superBlock.BlockSize;
            if (result > uint.MaxValue) {
                throw new OverflowException();
            }
            return (uint)result;
        }

        private void LoadStreamDirectory()
        {
            BlockMapReader mapReader = new BlockMapReader(this);
            uint numStreams = mapReader.ReadUInt32();
            if (ShouldTraceStreamDirectory) {
                Console.WriteLine($"DBG : Expecting {numStreams} streams.");
            }
            uint[] streamSizes = new uint[numStreams];
            for (int index = 0; index < numStreams; index++) {
                streamSizes[index] = mapReader.ReadUInt32();
                if (ShouldTraceStreamDirectory) {
                    Console.WriteLine($"DBG : Stream #{index} is {streamSizes[index]} bytes.");
                }
            }
            for (int index = 0; index < numStreams; index++) {
                List<uint> streamDescriptor = new List<uint>();
                _streamDescriptors.Add(streamDescriptor);
                uint streamBlocksCount = Ceil(streamSizes[index], _superBlock.BlockSize);
                if (ShouldTraceStreamDirectory) {
                    Console.Write($"DBG : Stream #{index} ({streamBlocksCount} blocks) : ");
                }
                for (int blockIndex = 0; blockIndex < streamBlocksCount; blockIndex++) {
                    uint blockNumber = mapReader.ReadUInt32();
                    streamDescriptor.Add(blockNumber);
                    if (ShouldTraceStreamDirectory) {
                        if (0 != blockIndex) { Console.Write(", "); }
                        Console.Write(blockNumber);
                    }
                }
                Console.WriteLine();
            }
        }

        internal uint ReadUInt32(ref uint offset)
        {
            try { return _mappedPdbView.ReadUInt32(offset); }
            finally { offset += sizeof(uint); }
        }

        [Flags()]
        public enum TraceFlags
        {
            None = 0,
            StreamDirectoryBlocks = 0x00000001
        }
    }
}