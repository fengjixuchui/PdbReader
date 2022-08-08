using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class ArgumentList
    {
        internal _ArgumentList _header;
        internal List<uint> _arguments = new List<uint>();

        internal static ArgumentList Create(PdbStreamReader reader)
        {
            ArgumentList result = new ArgumentList();
            result._header = reader.Read<_ArgumentList>();
            for (int index = 0; index < result._header.count; index++) {
                result._arguments.Add(reader.ReadUInt32());
            }
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _ArgumentList
        {
            internal ushort leaf; // LF_ARGLIST, LF_SUBSTR_LIST
            internal uint count; // number of arguments
        }
    }
}