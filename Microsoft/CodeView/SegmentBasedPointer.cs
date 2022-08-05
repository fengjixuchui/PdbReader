using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct SegmentBasedPointer : IPointer
    {
        internal PointerBody _body;
        // base segment if CV_PTR_BASE_SEG
        internal ushort bseg;

        public PointerBody Body => _body;

        internal static SegmentBasedPointer Create(PdbStreamReader reader, uint startOffset)
        {
            reader.Offset = startOffset;
            SegmentBasedPointer result = reader.Read<SegmentBasedPointer>();
            return result;
        }
    }
}
