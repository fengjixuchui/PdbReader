using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class CodeViewArray
    {
        internal _Array _data;

        private CodeViewArray(_Array data)
        {
            _data = data;
        }

        internal static CodeViewArray Create(PdbStreamReader reader)
        {
            _Array data = reader.Read<_Array>();
            return new CodeViewArray(data);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Array
        {
            internal LEAF_ENUM_e leaf; // LF_ARRAY
            internal uint /*CV_typ_t*/ elemtype; // type index of element type
            internal uint /*CV_typ_t*/ idxtype; // type index of indexing type
            // internal byte[] data; //variable length data specifying size in bytes and name
        }
    }
}
