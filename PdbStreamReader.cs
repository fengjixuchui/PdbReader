using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader
{
    internal class PdbStreamReader
    {
        internal delegate T ReadDelegate<T>();

        private readonly uint[] _blocks;
        private readonly uint _blockSize;
        /// <summary>Index within <see cref="_blocks"/> of current block.
        /// WARNING : Never set this field value. Use CurrentBlockIndex setter instead.
        /// </summary>
        private int _currentBlockIndex;
        /// <summary>For optimization purpose, always equal to _blocks[_currentBlockIndex]
        /// </summary>
        private uint _currentBlockNumber;
        /// <summary>Index within current block of first unread byte.</summary>
        private uint _currentBlockOffset;
        private readonly Pdb _pdb;
        private readonly uint _streamSize;

        internal PdbStreamReader(Pdb owner, uint streamIndex)
        {
            if (null == owner) { throw new ArgumentNullException(nameof(owner)); }
            _pdb = owner;
            _blocks = owner.GetStreamMap(streamIndex, out _streamSize);
            _blockSize = _pdb.SuperBlock.BlockSize;
            SetCurrentBlockIndex(0, true);
        }

        /// <summary>Returns the current offset within the stream this reader is bound to.
        /// </summary>
        internal uint Offset
        {
            get {
                ulong result = ((uint)_currentBlockIndex * _blockSize) + _currentBlockOffset;
                if (uint.MaxValue < result) {
                    throw new OverflowException();
                }
                return (uint)result;
            }
            set
            {
                // TODO : Check stream size against value.
                uint currentBlockIndex = (value / _blockSize);
                if (int.MaxValue < currentBlockIndex) {
                    throw new ArgumentOutOfRangeException(nameof(value));
                }
                SetCurrentBlockIndex(currentBlockIndex, false);
                _currentBlockOffset = value % _blockSize;
            }
        }

        /// <summary>Get number of bytes not yet read within current block.</summary>
        internal uint RemainingBlockBytes
        {
            get
            {
                if (_blockSize < _currentBlockOffset) {
                    throw new BugException();
                }
                return _blockSize - _currentBlockOffset;
            }
        }

        internal IStreamGlobalOffset GetGlobalOffset(bool ensureAtLeastOneAvailableByte = false)
        {
            return new GlobalOffset(this, _GetGlobalOffset(ensureAtLeastOneAvailableByte));
        }

        /// <summary>Get the current global offset for this stream in the underlying file.
        /// This version is much more memory efficient than the one returning an
        /// <see cref="IStreamGlobalOffset"/>. However, NO ARITHMETIC may be safely applied
        /// to the resulting value.</summary>
        /// <param name="ensureAtLeastOneAvailableByte"></param>
        /// <returns></returns>
        /// <exception cref="BugException"></exception>
        private uint _GetGlobalOffset(bool ensureAtLeastOneAvailableByte = false)
        {
            // Account for the flag parameter prior to computing global offset.
            if (ensureAtLeastOneAvailableByte && (0 >= RemainingBlockBytes)) {
                MoveToNextBlock();
            }
            ulong result = (_blockSize * _currentBlockNumber) + _currentBlockOffset;
            if (uint.MaxValue < result) {
                throw new BugException("Out of range global offset.");
            }
            return (uint)result;
        }

        internal void FillBuffer(IntPtr buffer, int bufferOffset, uint position,
            uint fillSize)
        {
            _pdb.FillBuffer(buffer, bufferOffset, position, fillSize);
            _currentBlockOffset += fillSize;
        }

        private void MoveToNextBlock()
        {
            SetCurrentBlockIndex((uint)(_currentBlockIndex + 1), true);
        }

        internal T Read<T>()
            where T : struct
        {
            uint remainingBlockBytes = RemainingBlockBytes;
            uint requiredBytes = (uint)Marshal.SizeOf(typeof(T));

            if (requiredBytes <= remainingBlockBytes) {
                // Fast read.
                T result = _pdb.Read<T>(_GetGlobalOffset());
                _currentBlockOffset += requiredBytes;
                // We may have consumed the whole block. In such a case we should set position
                // at beginning of the next one.
                if (0 == RemainingBlockBytes) {
                    MoveToNextBlock();
                }
                return result;
            }
            IntPtr buffer = IntPtr.Zero;
            try {
                if (int.MaxValue < requiredBytes) {
                    throw new NotSupportedException();
                }
                buffer = Marshal.AllocHGlobal((int)requiredBytes);
                int bufferOffset = 0;
                uint pendingReadSize = requiredBytes;
                while (0 < pendingReadSize) {
                    uint readSize = Math.Min(RemainingBlockBytes, pendingReadSize);
                    this.FillBuffer(buffer, bufferOffset, _GetGlobalOffset(), readSize);
                    pendingReadSize -= readSize;
                    bufferOffset += (int)readSize;
                    if (0 == RemainingBlockBytes) {
                        MoveToNextBlock();
                    }
                }
                return Marshal.PtrToStructure<T>(buffer);
            }
            finally {
                if (IntPtr.Zero != buffer) { Marshal.FreeHGlobal(buffer); }
            }
        }

        internal void Read(byte[] array)
        {
            if (null == array) {
                throw new ArgumentNullException(nameof(array));
            }
            uint requiredBytes = (uint)array.Length;
            uint arrayOffset = 0;
            while(0 < requiredBytes) {
                uint remainingBlockBytes = RemainingBlockBytes;
                uint readSize = Math.Min(requiredBytes, remainingBlockBytes);
                _pdb.Read(_GetGlobalOffset(), array, arrayOffset, readSize);
                _currentBlockOffset += readSize;
                requiredBytes -= readSize;
                arrayOffset += readSize;
                if (0 == requiredBytes) {
                    return;
                }
                MoveToNextBlock();
            }
        }

        internal void ReadArray<T>(T[] into, ReadDelegate<T> reader)
        {
            if (null == into) {
                throw new ArgumentNullException(nameof(into));
            }
            ReadArray(into, 0, into.Length, reader);
        }

        internal void ReadArray<T>(T[] into, int startOffset, int length,
            ReadDelegate<T> reader)
        {
            if (null == into) {
                throw new ArgumentNullException(nameof(into));
            }
            if (0 > length) {
                throw new ArgumentOutOfRangeException(nameof(length));
            }
            if (0 == length) {
                // Nothing to do.
                return;
            }
            if ((0 > startOffset) || (startOffset >= into.Length)) {
                throw new ArgumentOutOfRangeException(nameof(startOffset));
            }
            int endOffset = startOffset + length - 1;
            if ((0 > endOffset) || (endOffset >= into.Length)) {
                throw new ArgumentOutOfRangeException(nameof(endOffset));
            }
            for (int index = 0; index < length; index++) {
                into[index] = reader();
            }
        }

        internal byte ReadByte()
        {
            uint remainingBlockBytes = RemainingBlockBytes;
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(byte) > remainingBlockBytes) {
                // We must be at end of block.
                MoveToNextBlock();
                // Note : globalOffset is incremented by the reader.
            }
            byte result = _pdb.ReadByte(ref globalOffset);
            _currentBlockOffset += sizeof(byte);
            return result;
        }

        internal string ReadNTBString()
        {
            List<byte> bytes = new List<byte>();
            while (true) {
                byte inputByte = ReadByte();
                if (0 == inputByte) { break; }
                bytes.Add(inputByte);
            }
            return Encoding.UTF8.GetString(bytes.ToArray());
        }

        internal ushort PeekUInt16()
        {
            uint startOffset = Offset;
            try { return ReadUInt16(); }
            finally { this.Offset = startOffset; }
        }

        internal ushort ReadUInt16()
        {
            uint remainingBlockBytes = RemainingBlockBytes;
            ushort result;
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(ushort) <= remainingBlockBytes) {
                // Fast read.
                try { return _pdb.ReadUInt16(ref globalOffset); }
                finally { _currentBlockOffset += sizeof(ushort); }
            }
            // Must cross block boundary.
            int unreadBytes = sizeof(ushort);
            result = 0;
            while (0 < remainingBlockBytes) {
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                remainingBlockBytes--;
                unreadBytes--;
            }
            // End of block reached. 
            MoveToNextBlock();
            remainingBlockBytes = RemainingBlockBytes;
            while (0 < unreadBytes) {
                if (0 >= remainingBlockBytes) {
                    throw new BugException();
                }
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                // No need to decrement remainingBlockBytes because we are reading at
                // most three bytes which is guaranteed to be less than remaining block
                // bytes.
                unreadBytes--;
            }
            return result;
        }

        internal uint ReadUInt32()
        {
            uint remainingBlockBytes = RemainingBlockBytes;
            uint result;
            uint globalOffset = _GetGlobalOffset();
            if (sizeof(uint) <= remainingBlockBytes) {
                // Fast read.
                try { return _pdb.ReadUInt32(ref globalOffset); }
                finally { _currentBlockOffset += sizeof(uint); }
            }
            // Must cross block boundary.
            int unreadBytes = sizeof(uint);
            result = 0;
            while (0 < remainingBlockBytes) {
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                remainingBlockBytes--;
                unreadBytes--;
            }
            // End of block reached. 
            MoveToNextBlock();
            remainingBlockBytes = RemainingBlockBytes;
            while (0 < unreadBytes) {
                if (0 >= remainingBlockBytes) {
                    throw new BugException();
                }
                result <<= 8;
                // Note : globalOffset is incremented by the reader.
                result += _pdb.ReadByte(ref globalOffset);
                // No need to decrement remainingBlockBytes because we are reading at
                // most three bytes which is guaranteed to be less than remaining block
                // bytes.
                unreadBytes--;
            }
            return result;
        }

        private void SetCurrentBlockIndex(uint value, bool resetBlockOffset = false)
        {
            if (value >= _blocks.Length) {
                throw new ArgumentOutOfRangeException(nameof(value));
            }
            _currentBlockIndex = (int)value;
            _currentBlockNumber = _blocks[value];
            if (resetBlockOffset) {
                _currentBlockOffset = 0;
            }
        }

        internal void SetGlobalOffset(IStreamGlobalOffset value, bool doNotWarn = false)
        {
            if (!doNotWarn) {
                Console.WriteLine($"WARN : Setting reader global offset is not expected in normal course.");
            }
            uint newBlockOffset;
            int newBlockIndex = FindBlockIndex(value.Value, out newBlockOffset);
            if (0 > newBlockIndex) {
                throw new BugException();
            }
            SetCurrentBlockIndex((uint)newBlockIndex);
            _currentBlockOffset = newBlockOffset;
        }

        private int FindBlockIndex(uint globalOffset, out uint blockOffset)
        {
            for (int result = 0; result < _blocks.Length; result++) {
                uint blockNumber = _blocks[result];
                uint blockStartGlobalOffset = blockNumber * _blockSize;
                if (blockStartGlobalOffset > globalOffset) {
                    continue;
                }
                uint blockEndGlobalOffsetExcluded = blockStartGlobalOffset + _blockSize;
                if (blockEndGlobalOffsetExcluded <= globalOffset) {
                    continue;
                }
                SetCurrentBlockIndex((uint)result);
                blockOffset = globalOffset - blockStartGlobalOffset;
                return result;
            }
            throw new BugException($"Unable to restriev block matching global offset 0x{globalOffset:X8}.");
        }

        private class GlobalOffset : IStreamGlobalOffset
        {
            private PdbStreamReader _owner;
            private int _blockIndex;
            private uint _blockOffset;

            public uint Value
            {
                get
                {
                    ulong candidateResult =
                        ((ulong)_owner._blockSize * _owner._blocks[_blockIndex]) +
                        _blockOffset;
                    if (uint.MaxValue < candidateResult) {
                        throw new BugException();
                    }
                    return (uint)candidateResult;
                }
            }

            internal GlobalOffset(PdbStreamReader owner, uint value)
            {
                _owner = owner;
                _blockIndex = owner.FindBlockIndex(value, out _blockOffset);
            }

            public IStreamGlobalOffset Add(uint relativeOffset)
            {
                uint initialGlobalOffsetValue = Value;
                uint currentBlockOffset;
                int currentBlockIndex = _owner.FindBlockIndex(initialGlobalOffsetValue,
                    out currentBlockOffset);
                uint remainingDisplacement = relativeOffset;
                while (true) {
                    uint candidateOffset = remainingDisplacement + currentBlockOffset;
                    if (candidateOffset < _owner._blockSize) {
                        _blockIndex = currentBlockIndex;
                        _blockOffset = candidateOffset;
                        return this;
                    }
                    // Continue with next block.
                    uint availableBlockBytes = _owner._blockSize - currentBlockOffset;
                    remainingDisplacement -= availableBlockBytes;
                    if (++currentBlockIndex >= _owner._blocks.Length) {
                        throw new BugException(
                            $"Unable to add {relativeOffset} to global offset at {initialGlobalOffsetValue}.");
                    }
                    currentBlockOffset = 0;
                }
            }

            public int CompareTo(IStreamGlobalOffset? other)
            {
                if (null == other) {
                    throw new ArgumentNullException(nameof(other));
                }
                GlobalOffset? otherOffset = other as GlobalOffset;
                if (null == otherOffset) {
                    throw new NotSupportedException();
                }
                if (this._blockIndex > otherOffset._blockIndex) {
                    return 1;
                }
                if (this._blockIndex < otherOffset._blockIndex) {
                    return -1;
                }
                return this._blockOffset.CompareTo(otherOffset._blockOffset);
            }

            public IStreamGlobalOffset Subtract(uint relativeOffset)
            {
                uint initialGlobalOffsetValue = Value;
                uint currentBlockOffset;
                int currentBlockIndex = _owner.FindBlockIndex(initialGlobalOffsetValue,
                    out currentBlockOffset);
                uint remainingDisplacement = relativeOffset;
                while (true) {
                    if (currentBlockOffset > remainingDisplacement) {
                        _blockIndex = currentBlockIndex;
                        _blockOffset = currentBlockOffset - remainingDisplacement;
                        return this;
                    }
                    // Continue with previous block.
                    uint availableBlockBytes = currentBlockOffset + 1;
                    remainingDisplacement -= availableBlockBytes;
                    if (0 == currentBlockIndex--) {
                        throw new BugException(
                            $"Unable to subtract {relativeOffset} to global offset at {initialGlobalOffsetValue}.");
                    }
                    currentBlockOffset = _owner._blockSize - 1;
                }
            }
        }
    }
}
