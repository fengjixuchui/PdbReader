﻿using System.IO.MemoryMappedFiles;

namespace PdbReader
{
    /// <summary>Provides a forward only reader of the blockmap content.</summary>
    internal class BlockMapReader
    {
        private readonly uint[] _blockMapBlocks;
        private readonly uint _blockSize;
        /// <summary>Index within <see cref="_blockMapBlocks"/> of the block map
        /// addresses being read.</summary>
        private uint _currentReaderBlockIndex;
        /// <summary>Index within <see cref="_pdb._mappedPdbView"/> of the first
        /// byte of <see cref="_blockMapBlocks[_currentReaderBlockIndex]"/></summary>
        private uint _currentReaderBlockStartOffset;
        private readonly Pdb _pdb;
        private uint _readerOffset;

        internal BlockMapReader(Pdb owner)
        {
            _pdb = owner ?? throw new ArgumentNullException(nameof(owner));
            MSFSuperBlock superBlock = _pdb.SuperBlock;
            _blockSize = superBlock.BlockSize;
            // Read list of blocks used for Stream Directory storage.
            uint blockMapOffset = owner.GetBlockOffset(superBlock.BlockMapAddr);
            if (_pdb.ShouldTraceStreamDirectory) {
                Console.WriteLine(
                    $"DBG : Block map addr {superBlock.BlockMapAddr}, offset {blockMapOffset}, block size {_blockSize}.");
            }
            uint blockMapEntryCount = Pdb.Ceil(superBlock.NumDirectoryBytes,
                superBlock.BlockSize);
            if (_pdb.ShouldTraceStreamDirectory) {
                Console.Write($"DBG : {blockMapEntryCount} entries : ");
            }
            // Make sure we fit in a single block (very likely).
            if ((sizeof(uint) * blockMapEntryCount) > _blockSize) {
                throw new PDBFormatException("Too many block map entries.");
            }
            _blockMapBlocks = new uint[blockMapEntryCount];
            uint offset = blockMapOffset;
            // Read block map blocks index.
            for (int index = 0; index < blockMapEntryCount; index++) {
                uint currentBlock = _pdb.ReadUInt32(ref offset);
                _blockMapBlocks[index] = currentBlock;
                if (_pdb.ShouldTraceStreamDirectory) {
                    if (0 < index) { Console.Write(", "); }
                    Console.Write(currentBlock);
                }
            }
            if (_pdb.ShouldTraceStreamDirectory) { Console.WriteLine(); }
            
            // Move to Stream Directory
            SetCurrentReaderBlock(0);
        }

        private void AdjustReaderBlock()
        {
            if (_readerOffset < _currentReaderBlockStartOffset) {
                throw new BugException();
            }
            uint delta = _readerOffset - _currentReaderBlockStartOffset;
            if (delta >= _blockSize) {
                SetCurrentReaderBlock(++_currentReaderBlockIndex);
            }
        }

        internal uint ReadUInt32()
        {
            uint result = _pdb.ReadUInt32(ref _readerOffset);
            AdjustReaderBlock();
            return result;
        }

        private void SetCurrentReaderBlock(uint blockMapIndex)
        {
            if (_blockMapBlocks.Length <= blockMapIndex) {
                throw new ArgumentOutOfRangeException(nameof(blockMapIndex));
            }
            uint moveToBlock = _blockMapBlocks[blockMapIndex];
            _currentReaderBlockIndex = blockMapIndex;
            _currentReaderBlockStartOffset = _pdb.GetBlockOffset(moveToBlock);
            _readerOffset = _currentReaderBlockStartOffset;
            if (_pdb.ShouldTraceStreamDirectory) {
                Console.WriteLine(
                    $"DBG : Moving to block map block {moveToBlock} at offset {_currentReaderBlockStartOffset}.");
            }
        }
    }
}
