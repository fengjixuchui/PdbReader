using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Union : INamedItem
    {
        internal _Union _data;
        // variable length data describing length of structure and name
        internal ulong _unionLength;
        internal string _name;
        internal string _decoratedName;

        public string Name => _name;

        internal static Union Create(PdbStreamReader reader)
        {
            Union result = new Union();
            result._data = reader.Read<_Union>();
            result._unionLength = reader.ReadVariableLengthValue();
            result._name = reader.ReadNTBString();
            result._decoratedName = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Union
        {
            internal LEAF_ENUM_e leaf; // LF_UNION
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
        }
    }
}
