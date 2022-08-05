using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Modifier
    {
        internal ushort leaf;
        internal uint typeIndex;
        internal CV_modifier_t modifiers;
    }
}
