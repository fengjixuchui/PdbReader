﻿using PdbReader.Microsoft.CodeView;

namespace PdbReader
{
    public abstract class IndexedStream
    {
        protected readonly Header _header;
        protected readonly Pdb _owner;
        internal readonly PdbStreamReader _reader;
        private readonly uint _streamIndex;

        protected IndexedStream(Pdb owner, uint streamIndex)
        {
            _streamIndex = streamIndex;
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _reader = new PdbStreamReader(owner, streamIndex);
            _header = _reader.Read<Header>();
        }

        internal uint RecordsCount => _header.TypeIndexEnd - _header.TypeIndexBegin;

        internal abstract string StreamName { get; }

        internal virtual void LoadRecord(uint recordIdentifier)
        {
            // This is a special case. When no more bytes remain on the block, the first
            // read below will modify the global offset BEFORE reading the first byte.
            // Hence capturing global offset now would provide an erroneous result.
            IStreamGlobalOffset recordStartGlobalOffset = _reader.GetGlobalOffset(true);
            // WARNING : This offset must be capture AFTER the previous call which may have
            // modified the offset value.
            uint recordStartOffset = _reader.Offset;
            // The record length is the total number of bytes for this record EXCLUDING
            // the 2 bytes of the recordLength field itself.
            ushort recordLength = _reader.ReadUInt16();
            uint recordTotalLength = (uint)(recordLength + sizeof(ushort));
            IStreamGlobalOffset recordEndGlobalOffsetExcluded =
                recordStartGlobalOffset.Add(recordTotalLength);
            uint recordEndOffsetExcluded = recordStartOffset + recordTotalLength;
            LEAF_ENUM_e recordKind = (LEAF_ENUM_e)_reader.PeekUInt16();
            bool allowExtraBytes = false;
            switch (recordKind) {
                case LEAF_ENUM_e.ArgumentList:
                    ArgumentList thisArgsList = ArgumentList.Create(_reader);
                    break;
                case LEAF_ENUM_e.Array:
                    CodeViewArray thisArray = CodeViewArray.Create(_reader);
                    break;
                case LEAF_ENUM_e.BitField:
                    BitField thisBitField = BitField.Create(_reader);
                    break;
                case LEAF_ENUM_e.BuildInformation:
                    BuildInformation thisBuildInformation = BuildInformation.Create(_reader);
                    break;
                case LEAF_ENUM_e.Enum:
                    Enumeration enumeration = Enumeration.Create(_reader, this);
                    break;
                case LEAF_ENUM_e.FieldList:
                    FieldList thisFieldsList = FieldList.Create(_reader);
                    break;
                case LEAF_ENUM_e.FunctionIdentifier:
                    FunctionIdentifier thisFunctionIdentifier =
                        FunctionIdentifier.Create(_reader);
                    allowExtraBytes = true;
                    break;
                case LEAF_ENUM_e.Label:
                    Label thisLabel = _reader.Read<Label>();
                    break;
                case LEAF_ENUM_e.Modifier:
                    Modifier thisModifier = _reader.Read<Modifier>();
                    break;
                case LEAF_ENUM_e.Pointer:
                    IPointer pointer = PointerBody.Create(_reader, this);
                    // Remaining bytes may be present that are name chars related.S
                    break;
                case LEAF_ENUM_e.Procedure:
                    Procedure procedure = _reader.Read<Procedure>();
                    break;
                case LEAF_ENUM_e.StringIdentifier:
                    StringIdentifier thisStringIdentifier = StringIdentifier.Create(_reader);
                    allowExtraBytes = true;
                    break;
                case LEAF_ENUM_e.Structure:
                    Class thisStructure = Class.Create(_reader);
                    break;
                case LEAF_ENUM_e.UDTSourceLine:
                    UDTSourceLine thisSourceLine = UDTSourceLine.Create(_reader);
                    break;
                case LEAF_ENUM_e.Union:
                    Union thisUnion = Union.Create(_reader);
                    break;
                case LEAF_ENUM_e.VirtualTableShape:
                    VirtualTableShape thisVTShape = VirtualTableShape.Create(_reader);
                    break;
                default:
                    Console.WriteLine(
                        $"WARN : Unknwon leaf record kind '{recordKind}' / 0x{((int)recordKind):X4}");
                    break;
            }
            IStreamGlobalOffset currentGlobalOffset = _reader.GetGlobalOffset();
            uint currentOffset = _reader.Offset;
            if (currentOffset < recordEndOffsetExcluded) {
                uint ignoredBytesCount = recordEndOffsetExcluded - currentOffset;
                bool doNotWarnOnReset = false;
                if (!allowExtraBytes || (sizeof(uint) <= ignoredBytesCount)) {
                    // Emit warning when extra bytes count is greater or equal to word size
                    // or if extra bytes are not explictly allowed.
                    // This should be an incomplete decoding indicator.
                    // NOTICE : This is an heuristic which is not supported by official source
                    // code evidences.
                    Console.WriteLine(
                        $"WARN : {recordKind} record #{recordIdentifier} starting at 0x{recordStartGlobalOffset.Value:X8}/{recordStartOffset}.\r\n" +
                        $"Should have ended at 0x{recordEndGlobalOffsetExcluded.Value}/{recordEndOffsetExcluded} : {ignoredBytesCount} bytes ignored.");
                }
                else {
                    doNotWarnOnReset = true;
                    Console.WriteLine($"DBG : {recordKind} #{recordIdentifier} record fully decoded.");
                }
                // Adjust reader position.
                _reader.SetGlobalOffset(recordEndGlobalOffsetExcluded, doNotWarnOnReset);
            }
            else if (currentOffset > recordEndOffsetExcluded) {
                uint excessBytesCount = currentOffset - recordEndOffsetExcluded;
                Console.WriteLine(
                    $"ERROR : {recordKind} record #{recordIdentifier} starting 0x{recordStartGlobalOffset.Value:X8}/{recordStartOffset}.\r\n" +
                    $"Should have ended at 0x{recordEndGlobalOffsetExcluded:X8}/{recordEndOffsetExcluded} : consumed {excessBytesCount} bytes in excess");
                // Adjust reader position.
                _reader.SetGlobalOffset(recordEndGlobalOffsetExcluded);
            }
            else if (currentOffset == recordEndOffsetExcluded) {
                Console.WriteLine($"DBG : {recordKind} record #{recordIdentifier} fully decoded.");
            }
            else { throw new BugException(); }
        }

        public virtual void LoadRecords()
        {
            Console.WriteLine($"Loading {StreamName} stream records.");
            uint recordsCount = RecordsCount;
            uint totalRecordBytes = _header.TypeRecordBytes;
            uint offset = 0;
            uint recordIndex = 0;
            while (offset < totalRecordBytes) {
                uint startOffset = _reader.Offset;
                LoadRecord(recordIndex);
                uint deltaOffset = _reader.Offset - startOffset;
                if (0 == deltaOffset) {
                    throw new BugException();
                }
                offset += deltaOffset;
                if (++recordIndex >= recordsCount) {
                    // We should have consumed the expected total number of bytes.
                    if (offset < totalRecordBytes) {
                        throw new BugException();
                    }
                }
            }
            Console.WriteLine($"{StreamName} records loading completed.");
            return;
        }

        protected internal struct Header
        {
            internal _Version Version;
            /// <summary>Sizeof(Header)</summary>
            internal uint HeaderSize;
            /// <summary>The numeric value of the type index representing the first type
            /// record in the TPI stream. This is usually the value 0x1000 as type indices
            /// lower than this are reserved (see Type Indices for a discussion of reserved
            /// type indices).</summary>
            internal uint TypeIndexBegin;
            /// <summary>One greater than the numeric value of the type index representing
            /// the last type record in the TPI stream. The total number of type records in
            /// the TPI/IPI stream can be computed as TypeIndexEnd - TypeIndexBegin.</summary>
            internal uint TypeIndexEnd;
            /// <summary>The number of bytes of type record data following the header.</summary>
            internal uint TypeRecordBytes;

            /// <summary>The index of a stream which contains a list of hashes for every type
            /// record. This value may be -1, indicating that hash information is not present.
            /// In practice a valid stream index is always observed, so any producer
            /// implementation should be prepared to emit this stream to ensure compatibility
            /// with tools which may expect it to be present.</summary>
            internal ushort HashStreamIndex;
            /// <summary>Presumably the index of a stream which contains a separate hash
            /// table, although this has not been observed in practice and it’s unclear what
            /// it might be used for.</summary>
            internal ushort HashAuxStreamIndex;
            /// <summary>The size of a hash value (usually 4 bytes).</summary>
            internal uint HashKeySize;
            /// <summary>The number of buckets used to generate the hash values in the
            /// aforementioned hash streams.</summary>
            internal uint NumHashBuckets;

            /// <summary>The offset and size within the TPI Hash Stream of the list of hash
            /// values. It should be assumed that there are either 0 hash values, or a number
            /// equal to the number of type records in the TPI stream
            /// (TypeIndexEnd - TypeEndBegin). Thus, if HashBufferLength is not equal to
            /// (TypeIndexEnd - TypeEndBegin) * HashKeySize we can consider the PDB malformed.
            /// </summary>
            internal int HashValueBufferOffset;
            internal uint HashValueBufferLength;

            /// <summary>The offset and size within the TPI Hash Stream of the Type Index
            /// Offsets Buffer. This is a list of pairs of uint32_t’s where the first value
            /// is a Type Index and the second value is the offset in the type record data of
            /// the type with this index. This can be used to do a binary search followed by
            /// a linear search to get O(log n) lookup by type index.</summary>
            internal int IndexOffsetBufferOffset;
            internal uint IndexOffsetBufferLength;

            /// <summary>The offset and size within the TPI hash stream of a serialized hash
            /// table whose keys are the hash values in the hash value buffer and whose values
            /// are type indices. This appears to be useful in incremental linking scenarios,
            /// so that if a type is modified an entry can be created mapping the old hash
            /// value to the new type index so that a PDB file consumer can always have the
            /// most up to date version of the type without forcing the incremental linker to
            /// garbage collect and update references that point to the old version to now
            /// point to the new version. The layout of this hash table is described in The
            /// PDB Serialized Hash Table</summary>
            internal int HashAdjBufferOffset;
            internal uint HashAdjBufferLength;

            internal enum _Version : uint
            {
                V40 = 19950410,
                V41 = 19951122,
                V50 = 19961031,
                V70 = 19990903,
                V80 = 20040203
            }
        }

        /// <summary>From :
        /// https://code.woboq.org/llvm/llvm/include/llvm/DebugInfo/CodeView/TypeIndex.h.html</summary>
        public enum BuiltinTypeKind
        {
            // uncharacterized type (no type)
            None = 0x0000,
            Void = 0x0003,

            // type not translated by cvpack
            NotTranslated = 0x0007,
            // OLE/COM HRESULT
            HResult = 0x0008,

            // 8 bit signed
            SignedCharacter = 0x0010,
            // 16 bit signed
            Int16Short = 0x0011,
            // 32 bit signed
            Int32Long = 0x0012,
            // 64 bit signed
            Int64Quad = 0x0013,
            // 128 bit signed int
            Int128Oct = 0x0014,

            // 8 bit unsigned
            UnsignedCharacter = 0x0020,
            // 16 bit unsigned
            UInt16Short = 0x0021,
            // 32 bit unsigned
            UInt32Long = 0x0022,
            // 64 bit unsigned
            UInt64Quad = 0x0023,
            // 128 bit unsigned int
            UInt128Oct = 0x0024,

            // 8 bit boolean
            Boolean8 = 0x0030,
            // 16 bit boolean
            Boolean16 = 0x0031,
            // 32 bit boolean
            Boolean32 = 0x0032,
            // 64 bit boolean
            Boolean64 = 0x0033,
            // 128 bit boolean
            Boolean128 = 0x0034,

            // 32 bit real
            Float32 = 0x0040,
            // 64 bit real
            Float64 = 0x0041,
            // 80 bit real
            Float80 = 0x0042,
            // 128 bit real
            Float128 = 0x0043,
            // 48 bit real
            Float48 = 0x0044,
            // 32 bit PP real
            Float32PartialPrecision = 0x0045,
            // 16 bit real
            Float16 = 0x0046,

            // 32 bit complex
            Complex32 = 0x0050,
            // 64 bit complex
            Complex64 = 0x0051,
            // 80 bit complex
            Complex80 = 0x0052,
            // 128 bit complex
            Complex128 = 0x0053,
            // 48 bit complex
            Complex48 = 0x0054,
            // 32 bit PP complex
            Complex32PartialPrecision = 0x0055,
            // 16 bit complex
            Complex16 = 0x0056,

            // 8 bit signed int
            SByte = 0x0068,
            // 8 bit unsigned int
            Byte = 0x0069,

            // really a char
            NarrowCharacter = 0x0070,
            // wide char
            WideCharacter = 0x0071,
            // 16 bit signed int
            Int16 = 0x0072,
            // 16 bit unsigned int
            UInt16 = 0x0073,
            // 32 bit signed int
            Int32 = 0x0074,
            // 32 bit unsigned int
            UInt32 = 0x0075,
            // 64 bit signed int
            Int64 = 0x0076,
            // 64 bit unsigned int
            UInt64 = 0x0077,
            // 128 bit signed int
            Int128 = 0x0078,
            // 128 bit unsigned int
            UInt128 = 0x0079,
            // char16_t
            Character16 = 0x007a,
            // char32_t
            Character32 = 0x007b,
        }
    }
}
