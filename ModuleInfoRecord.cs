
namespace PdbReader
{
    internal struct ModuleInfoRecord
    {
        internal uint unused;
        internal SectionContributionEntry SectionContr;
        internal _Flags Flags;
        /// <summary>Type Server Index for this module. This is assumed to be related to
        /// /Zi, but as LLVM treats /Zi as /Z7, this field will always be invalid for LLVM
        /// generated PDBs.</summary>
        internal byte TSM;
        /// <summary>The index of the stream that contains symbol information for this
        /// module. This includes CodeView symbol information as well as source and line
        /// information. If this field is -1, then no additional debug info will be present
        /// for this module (for example, this is what happens when you strip private symbols
        /// from a PDB).</summary>
        internal ushort ModuleSymStream;
        /// <summary>The number of bytes of data from the stream identified by
        /// ModuleSymStream that represent CodeView symbol records.</summary>
        internal uint SymByteSize;
        /// <summary>The number of bytes of data from the stream identified by ModuleSymStream
        /// that represent C11-style CodeView line information.</summary>
        internal uint C11ByteSize;
        /// <summary>The number of bytes of data from the stream identified by ModuleSymStream
        /// that represent C13-style CodeView line information. At most one of C11ByteSize and
        /// C13ByteSize will be non-zero. Modern PDBs always use C13 instead of C11.</summary>
        internal uint C13ByteSize;
        /// <summary>The number of source files that contributed to this module during
        /// compilation.</summary>
        internal ushort SourceFileCount;
        internal ushort Padding;
        internal uint Unused2;
        /// <summary>he offset in the names buffer of the primary translation unit used to
        /// build this module. All PDB files observed to date always have this value equal
        /// to 0.</summary>
        internal uint SourceFileNameIndex;
        /// <summary>The offset in the names buffer of the PDB file containing this module’s
        /// symbol information. This has only been observed to be non-zero for the special
        /// * Linker * module.</summary>
        internal uint PdbFilePathNameIndex;
        // The following two fields are variable length sized
        /// <summary>The module name. This is usually either a full path to an object file
        /// (either directly passed to link.exe or from an archive) or a string of the form
        /// Import:<dll name>.</summary>
        // char[] ModuleName;
        /// <summary>The object file name. In the case of an module that is linked directly
        /// passed to link.exe, this is the same as ModuleName. In the case of a module that
        /// comes from an archive, this is usually the full path to the archive.</summary>
        // char[] ObjFileName;

        [Flags()]
        internal enum _Flags : byte
        {
            // ``true`` if this ModInfo has been written since reading the PDB.  This is
            // likely used to support incremental linking, so that the linker can decide
            // if it needs to commit changes to disk.
            Dirty = 0x0001,
            // ``true`` if EC information is present for this module. EC is presumed to
            // stand for "Edit & Continue", which LLVM does not support.  So this flag
            // will always be be false.
            EC = 0x0002,
        }

        internal struct SectionContributionEntry
        {
            internal ushort Section;
            internal ushort Padding1;
            internal int Offset;
            internal int Size;
            internal uint Characteristics;
            internal ushort ModuleIndex;
            internal ushort Padding2;
            internal uint DataCrc;
            internal uint RelocCrc;
        }
    }
}
