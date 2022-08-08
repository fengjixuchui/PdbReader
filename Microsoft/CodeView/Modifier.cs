﻿using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Modifier
    {
        internal ushort leaf; // LF_MODIFIER
        internal uint typeIndex; // modified type
        internal CV_modifier_t modifiers; // modifier attribute modifier_t
        internal ushort _unknown;
    }
}
