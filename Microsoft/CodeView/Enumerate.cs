using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Enumerate : INamedItem
    {
        internal _Enumerate _header;
        // variable length value field followed
        // by length prefixed name of field
        internal ulong _enumerationValue;
        internal string _name;

        public string Name => _name;

        internal static Enumerate Create(PdbStreamReader reader)
        {
            Enumerate result = new Enumerate();
            result._header = reader.Read<_Enumerate>();
            result._enumerationValue = reader.ReadVariableLengthValue();
            result._name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Enumerate
        {
            internal LEAF_ENUM_e _leaf; // LF_ENUMERATE
            internal CV_fldattr_t attr; // attribute mask
        }
    }
}
