using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Text;

namespace PdbReader
{
    public class Pdb
    {
        private const string SymbolCacheRelativePath = @"AppData\Local\Temp\SymbolCache";
        private DebugInformationStream _debugInfoStream;
        private MemoryMappedFile _mappedPdb;
        private MemoryMappedViewAccessor _mappedPdbView;
        private readonly FileInfo _pdbFile;
        private List<List<uint>> _streamDescriptors = new List<List<uint>>();
        private Dictionary<string, uint> _streamIndexByName;
        private uint[] _streamSizes;
        private bool _strictChecksEnabled;
        private readonly MSFSuperBlock _superBlock;
        private readonly TraceFlags _traceFlags;

        public Pdb(FileInfo target, TraceFlags traceFlags = 0, bool strictChecks = false)
        {
            _pdbFile = target ?? throw new ArgumentNullException(nameof(target));
            _traceFlags = traceFlags;
            if (!_pdbFile.Exists) {
                throw new ArgumentException($"Input file doesn't exist : '{_pdbFile.FullName}'");
            }
            _strictChecksEnabled = strictChecks;
            try {
                _mappedPdb = MemoryMappedFile.CreateFromFile(_pdbFile.FullName,
                    FileMode.Open, null, 0, MemoryMappedFileAccess.Read);
                _mappedPdbView = _mappedPdb.CreateViewAccessor(0, _pdbFile.Length,
                    MemoryMappedFileAccess.Read);
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
            // TODO : Partially completed
            // LoadInfoStream();
            _debugInfoStream = new DebugInformationStream(this);
        }

        public DebugInformationStream DebugInfoStream
            => _debugInfoStream ?? throw new BugException();

        internal bool FullDecodingDebugEnabled
            => (0 != (_traceFlags & TraceFlags.FullDecodingDebug));

        internal bool ShouldTraceNamedStreamMap
            => (0 != (_traceFlags & TraceFlags.NamedStreamMap));

        internal bool ShouldTraceModules
            => (0 != (_traceFlags & TraceFlags.ModulesInformation));

        internal bool ShouldTraceStreamDirectory
            => (0 != (_traceFlags & TraceFlags.StreamDirectoryBlocks));

        public bool StrictChecksEnabled
        {
            get { return _strictChecksEnabled; }
            set { _strictChecksEnabled = value; }
        }

        internal MSFSuperBlock SuperBlock => _superBlock;

        internal void AssertValidStreamNumber(uint candidate)
        {
            if (!IsValidStreamNumber(candidate)) {
                throw new PDBFormatException($"Invalid stream number #{candidate} encountered.");
            }
        }

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

        /// <summary></summary>
        /// <param name="buffer"></param>
        /// <param name="bufferOffset"></param>
        /// <param name="position"></param>
        /// <param name="fillSize"></param>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        /// <remarks>TODO Seek for a more optimized solution.</remarks>
        internal void FillBuffer(IntPtr buffer, int bufferOffset, uint position,
            uint fillSize)
        {
            if (int.MaxValue < fillSize) {
                throw new ArgumentOutOfRangeException(nameof(fillSize));
            }
            byte[] localBuffer = new byte[fillSize];
            _mappedPdbView.ReadArray<byte>(position, localBuffer, 0, (int)fillSize);
            Marshal.Copy(localBuffer, 0, IntPtr.Add(buffer, bufferOffset), (int)fillSize);
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

        /// <summary>Returns an array of block indexes for the stream having the given
        /// index.</summary>
        /// <param name="streamIndex"></param>
        /// <param name="streamSize">On return this parameter is updated with the stream
        /// size in bytes.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        internal uint[] GetStreamMap(uint streamIndex, out uint streamSize)
        {
            if (_streamDescriptors.Count <= streamIndex) {
                throw new ArgumentOutOfRangeException(nameof(streamIndex));
            }
            streamSize = _streamSizes[streamIndex];
            return _streamDescriptors[(int)streamIndex].ToArray();
        }

        internal uint GetStreamSize(uint streamIndex)
        {
            if (_streamSizes.Length <= streamIndex) {
                throw new ArgumentOutOfRangeException(nameof(streamIndex));
            }
            return _streamSizes[streamIndex];
        }

        private string GetString(byte[] buffer, uint bufferOffset)
        {
            if (null == buffer) {
                throw new ArgumentNullException(nameof(buffer));
            }
            if (int.MaxValue < bufferOffset) {
                throw new ArgumentOutOfRangeException(nameof(bufferOffset));
            }
            uint bufferLength = (uint)buffer.Length;
            uint maxStringLength = bufferLength - bufferOffset;
            int stringLength = 0;
            while(0 != buffer[bufferOffset + stringLength]) {
                if (++stringLength > maxStringLength) {
                    throw new PDBFormatException(
                        $"Unterminated string found at offset {bufferOffset} in string buffer.");
                }
            }
            return Encoding.ASCII.GetString(buffer, (int)bufferOffset, stringLength);
        }

        internal bool IsValidStreamNumber(uint candidate)
        {
            return (candidate < _streamDescriptors.Count);
        }

        /// <summary>Load the PDB info stream.</summary>
        private void LoadInfoStream()
        {
            // The PDB info stream is at fixed index 1.
            PdbStreamReader reader = new PdbStreamReader(this, 1);
            // Stream starts with an header ...
            PdbStreamHeader header = reader.Read<PdbStreamHeader>();
            if (StrictChecksEnabled) {
                if (!Enum.IsDefined(header.Version)) {
                    throw new PDBFormatException(
                        $"Invalid PDB stream header version {header.Version}");
                }
            }
            // ... followed by a length prefixed array of strings ...
            uint stringBufferLength = reader.ReadUInt32();
            byte[] stringBuffer = new byte[stringBufferLength];
            reader.Read(stringBuffer);
            // ... then by an <uint, uint> hash table where key is an index in
            // string buffer and value is a stream index.
            _streamIndexByName = new Dictionary<string, uint>();
            HashTableReader hashReader = new HashTableReader(this, 1, reader.Offset);
            Dictionary<uint, uint> hashValues = hashReader.ReadUInt32Table();

            // Build the dictionary.
            foreach (KeyValuePair<uint, uint> pair in hashValues) {
                // Extract name from stringBuffer
                string streamName = GetString(stringBuffer, pair.Key);
                _streamIndexByName.Add(streamName, pair.Value);
            }
        }

        private void LoadStreamDirectory()
        {
            BlockMapReader mapReader = new BlockMapReader(this);
            uint numStreams = mapReader.ReadUInt32();
            if (ShouldTraceStreamDirectory) {
                Console.WriteLine($"DBG : Expecting {numStreams} streams.");
            }
            _streamSizes = new uint[numStreams];
            for (int index = 0; index < numStreams; index++) {
                _streamSizes[index] = mapReader.ReadUInt32();
                if (ShouldTraceStreamDirectory) {
                    Console.WriteLine($"DBG : Stream #{index} is {_streamSizes[index]} bytes.");
                }
            }
            for (int index = 0; index < numStreams; index++) {
                List<uint> streamDescriptor = new List<uint>();
                _streamDescriptors.Add(streamDescriptor);
                uint streamBlocksCount = Ceil(_streamSizes[index], _superBlock.BlockSize);
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
                if (ShouldTraceStreamDirectory) { Console.WriteLine(); }
            }
        }

        internal void Read(uint position, byte[] into, uint offset, uint length)
        {
            if (int.MaxValue < offset) {
                throw new ArgumentOutOfRangeException(nameof(offset));
            }
            if (int.MaxValue < length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            _mappedPdbView.ReadArray(position, into, (int)offset, (int)length);
        }

        internal byte ReadByte(ref uint offset)
        {
            try { return _mappedPdbView.ReadByte(offset); }
            finally { offset += sizeof(byte); }
        }

        internal ushort ReadUInt16(ref uint offset)
        {
            try { return _mappedPdbView.ReadUInt16(offset); }
            finally { offset += sizeof(ushort); }
        }

        internal uint ReadUInt32(ref uint offset)
        {
            try { return _mappedPdbView.ReadUInt32(offset); }
            finally { offset += sizeof(uint); }
        }

        internal T Read<T>(long position)
            where T : struct
        {
            T result;
            _mappedPdbView.Read<T>(position, out result);
            return result;
        }

        internal static uint SafeCastToUint32(int value)
        {
            if (0 > value) { throw new BugException(); }
            return (uint)value;
        }

        [Flags()]
        public enum TraceFlags
        {
            None = 0,
            StreamDirectoryBlocks = 0x00000001,
            NamedStreamMap = 0x00000002,
            ModulesInformation = 0x00000004,
            FullDecodingDebug = 0x00000008
        }
    }
}