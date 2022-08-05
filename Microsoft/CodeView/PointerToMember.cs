using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct PointerToMember : IPointer
    {
        internal PointerBody _body;
        // index of containing class for pointer to member
        internal uint pmclass;
        // enumeration specifying pm format (CV_pmtype_e)
        internal CV_pmtype_e pmenum;

        public PointerBody Body => _body;

        internal static PointerToMember Create(PdbStreamReader reader, uint startOffset)
        {
            reader.Offset = startOffset;
            PointerToMember result = reader.Read<PointerToMember>();
            return result;
        }
    }
}
