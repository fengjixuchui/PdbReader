using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Class
    {
        internal _Class _class;
        // data describing length of structure in bytes and name
        internal ulong _structureSize;
        internal string _name;
        internal string _decoratedName;

        private Class(_Class @class, ulong structureSize, string name, string decoratedName)
        {
            _class = @class;
            _structureSize = structureSize;
            _name = name;
            _decoratedName = decoratedName;
        }

        internal static Class Create(PdbStreamReader reader)
        {
            _Class header = reader.Read<_Class>();
            ulong structureSize = reader.ReadVariableLengthValue();
            string itemName = reader.ReadNTBString();
            string decoratedName = reader.ReadNTBString();
            return new Class(header, structureSize, itemName, decoratedName);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Class
        {
            internal LEAF_ENUM_e leaf; // LF_CLASS, LF_STRUCT, LF_INTERFACE
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field (prop_t)
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
            internal uint /*CV_typ_t*/ derived; // type index of derived from list if not zero
            internal uint /*CV_typ_t*/ vshape; // type index of vshape table for this class
        }
    }
}
