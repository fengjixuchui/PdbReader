using System.Reflection;

using PdbReader;

namespace PdbDumper
{
    public static class Dumper
    {
        private static FileInfo _pdbFile;

        public static int Main(string[] args)
        {
            if (!ParseArgs(args)) {
                Usage();
                return 1;
            }
            Pdb pdv = new Pdb(_pdbFile);
            return 0;
        }

        private static bool ParseArgs(string[] args)
        {
            if (0 == args.Length) {
                return false;
            }
            _pdbFile = new FileInfo(args[0]);
            if (!_pdbFile.Exists) {
                Console.WriteLine($"Input file '{_pdbFile.FullName}' doesn't exist.");
                return false;
            }
            return true;
        }

        private static void Usage()
        {
            Assembly thisAssembly = Assembly.GetExecutingAssembly();

            Console.WriteLine($"{thisAssembly.GetName().Name} <pdb file name>");
        }
    }
}