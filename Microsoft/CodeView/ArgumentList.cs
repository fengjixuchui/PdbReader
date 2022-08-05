using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct ArgumentList
    {
        internal ushort leaf; // LF_ARGLIST, LF_SUBSTR_LIST
        internal uint count; // number of arguments
        // internal uint /*CV_typ_t */[] arg;      // number of arguments

        internal static ArgumentList Create(PdbStreamReader reader)
        {
            ArgumentList result = reader.Read<ArgumentList>();
            // TODO : Handle arg member.
            return result;
        }
    }
}