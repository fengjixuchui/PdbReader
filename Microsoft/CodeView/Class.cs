using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Class
    {
        internal LEAF_ENUM_e leaf; // LF_CLASS, LF_STRUCT, LF_INTERFACE
        internal ushort count; // count of number of elements in class
        internal CV_prop_t property; // property attribute field (prop_t)
        internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
        internal uint /*CV_typ_t*/ derived; // type index of derived from list if not zero
        internal uint /*CV_typ_t*/ vshape; // type index of vshape table for this class
        // data describing length of structure in bytes and name
        // unsigned char data[CV_ZEROLEN];

        internal static Class Create(PdbStreamReader reader)
        {
            Class result = reader.Read<Class>();
            // TODO : Handle data member.
            return result;
        }
    }
}
