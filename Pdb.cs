
namespace PdbReader
{
    public class Pdb
    {
        private const string SymbolCacheRelativePath = @"AppData\Local\Temp\SymbolCache";
        private readonly FileInfo _pdbFile;

        public Pdb(FileInfo target)
        {
            _pdbFile = target ?? throw new ArgumentNullException(nameof(target));
            if (!_pdbFile.Exists) {
                throw new ArgumentException($"Input file doesn't exist : '{_pdbFile.FullName}'");
            }
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