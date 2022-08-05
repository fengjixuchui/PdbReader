using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct TypeBasedPointer : IPointer
    {
        internal PointerBody _body;
        internal uint index; // type index if CV_PTR_BASE_TYPE (CV_ptrtype_e.TypeBased)
        // Actually an array of characters (bytes).
        //internal byte name;    // name of base type

        public PointerBody Body => _body;

        internal static TypeBasedPointer Create(PdbStreamReader reader, uint startOffset)
        {
            reader.Offset = startOffset;
            TypeBasedPointer result = reader.Read<TypeBasedPointer>();
            // Name remains to be read.
            throw new NotImplementedException();
        }
    }
}
