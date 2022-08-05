﻿using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct UDTSourceLine
    {
        internal LEAF_ENUM_e leaf; // LF_UDT_SRC_LINE
        internal uint /*CV_typ_t*/ type; // UDT's type index
        internal uint /*CV_ItemId*/ src; // index to LF_STRING_ID record where source file name is saved
        internal uint line; // line number

        internal static UDTSourceLine Create(PdbStreamReader reader)
        {
            UDTSourceLine result = reader.Read<UDTSourceLine>();
            return result;
        }
    }
}
