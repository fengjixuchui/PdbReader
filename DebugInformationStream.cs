﻿using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

using PdbReader.Microsoft;

namespace PdbReader
{
    /// <summary></summary>
    /// <remarks>Structures and most comments are from :
    /// https://llvm.org/docs/PDB/DbiStream.html</remarks>
    public class DebugInformationStream
    {
        private const uint ThisStreamIndex = 3;
        private DBIStreamHeader _header;
        private readonly Pdb _owner;
        private readonly PdbStreamReader _reader;
        private ushort? _fpoDataStreamIndex;
        private ushort? _exceptionDataStreamIndex;
        private ushort? _fixupDataStreamIndex;
        private ushort? _omapToSourceMappingStreamIndex;
        private ushort? _omapFromSourceMappingStreamIndex;
        private ushort? _sectionHeaderDataStreamIndex;
        private ushort? _tokenToRIDMappingStreamIndex;
        private ushort? _xdataStreamIndex;
        private ushort? _pdataStreamIndex;
        private ushort? _newFPODataStreamIndex;
        private ushort? _originalSectionHeaderDataStreamIndex;

        internal DebugInformationStream(Pdb owner)
        {
            _owner = owner ?? throw new ArgumentNullException(nameof(owner));
            _reader = new PdbStreamReader(owner, 3);
            // Read header.
            _header = _reader.Read<DBIStreamHeader>();
            if (!_header.IsNewVersionFormat()) {
                throw new NotSupportedException("Legacy DBI stream format.");
            }
            _owner.AssertValidStreamNumber(_header.GlobalStreamIndex);
            _owner.AssertValidStreamNumber(_header.PublicStreamIndex);
            // Read module information substream.
            LoadOptionalStreamsIndex();
            uint streamSize = _owner.GetStreamSize(ThisStreamIndex);
            if (_reader.Offset != streamSize) {
                Console.WriteLine(
                    $"WARN : DBI stream size {streamSize}, {streamSize - _reader.Offset} bytes remaining.");
            }
        }

        private ushort? GetOptionalStreamIndex()
        {
            ushort input = _reader.ReadUInt16();
            return (ushort.MaxValue == input) ? null : input;
        }

        private void LoadOptionalStreamsIndex()
        {
            // Set stream position which should be near the end of the DBI stream.
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize +
                _header.SourceInfoSize + _header.TypeServerMapSize +
                _header.ECSubstreamSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            // Read optional streams index.
            _fpoDataStreamIndex = GetOptionalStreamIndex();
            _exceptionDataStreamIndex = GetOptionalStreamIndex();
            _fixupDataStreamIndex = GetOptionalStreamIndex();
            _omapToSourceMappingStreamIndex = GetOptionalStreamIndex();
            _omapFromSourceMappingStreamIndex = GetOptionalStreamIndex();
            _sectionHeaderDataStreamIndex = GetOptionalStreamIndex();
            _tokenToRIDMappingStreamIndex = GetOptionalStreamIndex();
            _xdataStreamIndex = GetOptionalStreamIndex();
            _pdataStreamIndex = GetOptionalStreamIndex();
            _newFPODataStreamIndex = GetOptionalStreamIndex();
            _originalSectionHeaderDataStreamIndex = GetOptionalStreamIndex();
        }

        public void LoadEditAndContinueMappings()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize +
                _header.SourceInfoSize + _header.TypeServerMapSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            throw new NotImplementedException();
        }

        public void LoadFileInformations()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);
            IStreamGlobalOffset globalOffset = _reader.GetGlobalOffset();

            // Read stream content.
            ushort modulesCount = _reader.ReadUInt16();
            // In theory this is supposed to contain the number of source files for which
            // this substream contains information. But that would present a problem in
            // that the width of this field being 16-bits would prevent one from having
            // more than 64K source files in a program. In early versions of the file format,
            // this seems to have been the case. In order to support more than this, this
            // field of the is simply ignored, and computed dynamically by summing up the
            // values of the ModFileCounts array (discussed below). In short, this value
            // should be ignored.
            ushort sourceFilesCount = _reader.ReadUInt16();
            // This array is present, but does not appear to be useful.
            ushort[] moduleIndices = new ushort[modulesCount];
            _reader.ReadArray(moduleIndices, _reader.ReadUInt16);
            // An array of NumModules integers, each one containing the number of source
            // files which contribute to the module at the specified index. While each
            // individual module is limited to 64K contributing source files, the union of
            // all modules’ source files may be greater than 64K. The real number of source
            // files is thus computed by summing this array. Note that summing this array
            // does not give the number of unique source files, only the total number of
            // source file contributions to modules.
            ushort[] moduleFilesCount = new ushort[modulesCount];
            _reader.ReadArray(moduleFilesCount, _reader.ReadUInt16);
            // An array of NumSourceFiles integers (where NumSourceFiles here refers to
            // the 32-bit value obtained from summing moduleFilesCount), where each
            // integer is an offset into NamesBuffer pointing to a null terminated string.
            uint realFileCount = 0;
            foreach(ushort value in moduleFilesCount) {
                realFileCount += value;
            }
            uint[] fileNameOffsets = new uint[realFileCount];
            _reader.ReadArray(fileNameOffsets, _reader.ReadUInt32);
            // An array of null terminated strings containing the actual source file names.
            Dictionary<uint, string> fileNameByIndex = new Dictionary<uint, string>();
            uint filenameBaseOffset = _reader.Offset;
            for(int index = 0; index < realFileCount; index++) {
                if (876 == index) {
                    bool doBreak = true;
                }
                uint thisFileOffset = _reader.Offset;
                string filename = _reader.ReadNTBString();
                uint fileRelativeOffset = thisFileOffset - filenameBaseOffset;
                fileNameByIndex.Add(fileRelativeOffset, filename);
Console.WriteLine($"Added #{fileNameByIndex.Count} '{filename}' @0x{fileRelativeOffset:X8}");
                if (126 == fileNameByIndex.Count) {
                    bool doBreak = true;
                }
            }

            // Tracing
            int offsetIndex = 0;
            for(uint moduleIndex = 0; moduleIndex < modulesCount; moduleIndex++) {
                if (_owner.ShouldTraceModules) {
                    Console.WriteLine($"Module #{moduleIndex}");
                }
                int thisModuleFilesCount = moduleFilesCount[moduleIndex];
                for (int moduleFileIndex = 0;
                    moduleFileIndex < thisModuleFilesCount;
                    moduleFileIndex++)
                {
                    uint thisFileOffset = fileNameOffsets[offsetIndex++];
                    string? currentFilename;
                    if (!fileNameByIndex.TryGetValue(thisFileOffset, out currentFilename)) {
                        Console.WriteLine($"\tUnmatched file index {thisFileOffset}");
                    }
                    else {
                        Console.WriteLine($"\t{currentFilename}");
                    }
                }
            }
            return;
        }

        public void LoadModuleInformations()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>();
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            // Read stream content.
            uint offset = 0;
            int totalSize = _header.ModInfoSize;
            int moduleIndex = 0;
            for(; offset < totalSize; moduleIndex++) {
                ModuleInfoRecord record = _reader.Read<ModuleInfoRecord>();
                List<byte> stringBytes = new List<byte>();
                // Some records have trailing NULL bytes before names. Skip them
                byte scannedByte;
                do { scannedByte = _reader.ReadByte(); }
                while (0 == scannedByte);
                while (0 != scannedByte) {
                    stringBytes.Add(scannedByte);
                    scannedByte = _reader.ReadByte();
                }
                string moduleName = Encoding.UTF8.GetString(stringBytes.ToArray());
                stringBytes.Clear();
                while (0 != (scannedByte = _reader.ReadByte())) {
                    stringBytes.Add(scannedByte);
                }
                string objectFileName = Encoding.UTF8.GetString(stringBytes.ToArray());
                offset = _reader.Offset;
                if (_owner.ShouldTraceModules) {
                    Console.WriteLine($"Module #{moduleIndex}: {moduleName}");
                    if (!string.IsNullOrEmpty(objectFileName)) {
                        Console.WriteLine($"\t{objectFileName}");
                    }
                }
            }
            return;
        }

        public void LoadSectionContributions()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            // Read stream content.
            uint streamVersion = _reader.ReadUInt32();
            switch (streamVersion) {
                case 0xF12EBA2D:
                    // Ver60
                    // TODO
                    break;
                case 0xF13151E4:
                    // V2
                    // TODO
                    break;
                default:
                    throw new PDBFormatException(
                        $"Unknown section contribution version {streamVersion}");
            }
            ushort segmentCount = _reader.ReadUInt16();
            ushort logicalSegmentCount = _reader.ReadUInt16();
            SectionMapEntry mapEntry = _reader.Read<SectionMapEntry>();
        }

        public SectionMapEntry[] LoadSectionMappings()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            ushort sectionDescriptorsCount = _reader.ReadUInt16();
            ushort sectionLogicalDescriptorsCount = _reader.ReadUInt16();
            SectionMapEntry[] result = new SectionMapEntry[sectionDescriptorsCount];
            for(int index = 0; index < sectionDescriptorsCount; index++) {
                result[index] = _reader.Read<SectionMapEntry>();
            }
            return result;
        }

        public void LoadTypeServerMappings()
        {
            // Set stream position
            int newOffset = Marshal.SizeOf<DBIStreamHeader>() + _header.ModInfoSize +
                _header.SectionContributionSize + _header.SectionMapSize +
                _header.SourceInfoSize;
            _reader.Offset = Pdb.SafeCastToUint32(newOffset);

            throw new NotImplementedException();
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct DBIStreamHeader
        {
            /// <summary>Always uint.MaxValue.</summary>
            internal uint Magic;
            /// <summary>this value always appears to be V70, and it is not clear what the other
            /// values are for.</summary>
            internal StreamVersion VersionHeader;
            /// <summary>The number of times the PDB has been written. Equal to the same field
            /// from the PDB Stream header.</summary>
            internal uint Age;
            /// <summary>The index of the Global Symbol Stream, which contains CodeView symbol
            /// records for all global symbols. Actual records are stored in the symbol record
            /// stream, and are referenced from this stream.</summary>
            internal ushort GlobalStreamIndex;
            /// <summary>A bitfield containing values representing the major and minor version
            /// number of the toolchain (e.g. 12.0 for MSVC 2013) used to build the program.
            /// For bit layout <see cref="GetMajorVersion()"/>,  <see cref="GetMinorVersion()"/>
            /// and <see cref="IsNewVersionFormat()"/></summary>
            internal ushort BuildNumber;
            /// <summary>The index of the Public Symbol Stream, which contains CodeView symbol
            /// records for all public symbols. Actual records are stored in the symbol record
            /// stream, and are referenced from this stream.</summary>
            internal ushort PublicStreamIndex;
            /// <summary>The version number of mspdbXXXX.dll used to produce this PDB.</summary>
            internal ushort PdbDllVersion;
            /// <summary>The stream containing all CodeView symbol records used by the program.
            /// This is used for deduplication, so that many different compilands can refer to
            /// the same symbols without having to include the full record content inside of
            /// each module stream.</summary>
            internal ushort SymRecordStream;
            /// <summary>Unknown</summary>
            internal ushort PdbDllRbld;
            /// <summary>The length of the Module Info Substream.</summary>
            internal int ModInfoSize;
            /// <summary>The length of the Section Contribution Substream.</summary>
            internal int SectionContributionSize;
            /// <summary>The length of the Section Map Substream.</summary>
            internal int SectionMapSize;
            /// <summary>The length of the File Info Substream.</summary>
            internal int SourceInfoSize;
            /// <summary>The length of the Type Server Map Substream.</summary>
            internal int TypeServerMapSize;
            /// <summary>The index of the MFC type server in the Type Server Map Substream.</summary>
            internal uint MFCTypeServerIndex;
            /// <summary>The length of the Optional Debug Header Stream.</summary>
            internal int OptionalDbgHeaderSize;
            /// <summary>The length of the EC Substream.</summary>
            internal int ECSubstreamSize;
            /// <summary>A bitfield containing various information about how the program was
            /// built. For bit layout <see cref="HasConflictingTypes()"/>
            /// </summary>
            internal ushort Flags;
            /// <summary>A value from the CV_CPU_TYPE_e enumeration. Common values are 0x8664
            /// (x86-64) and 0x14C (x86).</summary>
            internal CV_CPU_TYPE_e Machine;
            internal uint Padding;

            internal bool IsNewVersionFormat() => (0 != (BuildNumber & 0x8000));

            internal uint GetMajorVersion() => (uint)((BuildNumber & 0x7F00) >> 8);

            internal uint GetMinorVersion() => (uint)(BuildNumber & 0xFF);

            [Flags()]
            internal enum _Flags : ushort
            {
                IncrementallyLinked = 0x0001,
                PrivateSymbolsStripped = 0x0002,
                HasConflictingTypes = 0x0004,
            }

            /// <summary>Note that values are different from the ones in
            /// <see cref="PdbStreamVersion"/></summary>
            internal enum StreamVersion : uint
            {
                VC41 = 930803,
                V50 = 19960307,
                V60 = 19970606,
                V70 = 19990903,
                V110 = 20091201
            }
        }

        public struct SectionMapEntry
        {
            internal _Flags Flags;
            /// <summary>Logical overlay number</summary>
            internal ushort Ovl;
            /// <summary>Group index into descriptor array.</summary>
            internal ushort Group;
            internal ushort Frame;
            /// <summary>Byte index of segment / group name in string table, or 0xFFFF.</summary>
            internal ushort SectionName;
            /// <summary>Byte index of class in string table, or 0xFFFF.</summary>
            internal ushort ClassName;
            /// <summary>Byte offset of the logical segment within physical segment.
            /// If group is set in flags, this is the offset of the group.</summary>
            internal uint Offset;
            /// <summary>Byte count of the segment or group.</summary>
            internal uint SectionLength;

            [Flags()]
            internal enum _Flags : ushort
            {
                Read = 0x0001,
                Write = 0x0002,
                Execute = 0x0004,
                /// <summary>Descriptor describes a 32-bit linear address.</summary>
                AddressIs32Bit = 0x0008,
                /// <summary>Frame represents a selector.</summary>
                IsSelector = 0x0100,
                /// <summary>Frame represents an absolute address.</summary>
                IsAbsoluteAddress = 0x0200,
                /// <summary>If set, descriptor represents a group.</summary>
                IsGroup = 0x0400
            }
        }
    }
}
