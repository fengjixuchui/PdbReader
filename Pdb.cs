using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace PdbReader
{
    public class Pdb
    {
        private const string SymbolCacheRelativePath = @"AppData\Local\Temp\SymbolCache";
        private MemoryMappedFile _mappedPdb;
        private MemoryMappedViewAccessor _mappedPdbView;
        private readonly FileInfo _pdbFile;
        private readonly MSFSuperBlock _superBlock;

        public Pdb(FileInfo target)
        {
            _pdbFile = target ?? throw new ArgumentNullException(nameof(target));
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
        }

        /// <summary>Ensure the symbol cache directory exists otherwise create
        /// it.</summary>
        /// <returns>A descriptor for the cache directory.</returns>
        /// <exception cref="BugException"></exception>
        private static DirectoryInfo EnsureSymbolCacheDirectory()
        {
            string? userProfileDirectory = Environment.GetEnvironmentVariable("USERPROFILE");
            if (null == userProfileDirectory)
            {
                throw new BugException();
            }
            DirectoryInfo result = new DirectoryInfo(
                Path.Combine(userProfileDirectory, SymbolCacheRelativePath));
            if (!result.Exists)
            {
                result.Create();
                result.Refresh();
            }
            return result;
        }

    }
}