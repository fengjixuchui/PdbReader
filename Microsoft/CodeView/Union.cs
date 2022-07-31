using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Union
    {
        internal _Union _data;

        private Union(_Union data)
        {
            _data = data;
        }

        internal static Union Create(PdbStreamReader reader)
        {
            _Union data = reader.Read<_Union>();
            return new Union(data);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Union
        {
            internal LEAF_ENUM_e leaf; // LF_UNION
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
            // unsigned char data[CV_ZEROLEN]; // variable length data describing length of structure and name
        }
    }
}
