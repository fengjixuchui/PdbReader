using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class CodeViewArray : INamedItem
    {
        internal _Array _data;
        //variable length data specifying size in bytes and name
        internal ulong _arrayLength;
        internal string _name;

        public string Name => _name;

        private CodeViewArray(_Array data)
        {
            _data = data;
        }

        internal static CodeViewArray Create(PdbStreamReader reader)
        {
            CodeViewArray result = new CodeViewArray(reader.Read<_Array>());
            result._arrayLength = reader.ReadVariableLengthValue();
            result._name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Array
        {
            internal LEAF_ENUM_e leaf; // LF_ARRAY
            internal uint /*CV_typ_t*/ elemtype; // type index of element type
            internal uint /*CV_typ_t*/ idxtype; // type index of indexing type
        }
    }
}
