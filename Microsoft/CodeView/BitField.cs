using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class BitField
    {
        internal _BitField _data;

        private BitField(_BitField data)
        {
            _data = data;
        }

        internal static BitField Create(PdbStreamReader reader)
        {
            _BitField data = reader.Read<_BitField>();
            return new BitField(data);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _BitField
        {
            internal LEAF_ENUM_e leaf; // LF_BITFIELD
            internal uint /*CV_typ_t*/ type; // type of bitfield
            internal byte length;
            internal byte position;
        }
    }
}
