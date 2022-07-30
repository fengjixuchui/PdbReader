using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Procedure
    {
        internal LEAF_ENUM_e leaf; // LF_PROCEDURE
        internal uint /*CV_typ_t*/ rvtype; // type index of return value
        unsigned char calltype;       // calling convention (CV_call_t)
        CV_funcattr_t funcattr;       // attributes
        internal ushort parmcount; // number of parameters
        internal uint /*CV_typ_t*/ arglist;        // type index of argument list
    }
}
