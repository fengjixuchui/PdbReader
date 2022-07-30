using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    /// <summary>General format for pointer.</summary>
    /// <remarks>Structures are bytes aligned.</remarks>
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct Pointer : IPointer
    {
        internal PointerBody _body;

        public PointerBody Body => _body;

        internal static Pointer Create(IndexedStream stream, PdbStreamReader reader,
            PointerBody body)
        {
            Pointer result = new Pointer() {
                _body = body
            };
            // TODO : Copy of base symbol record remains to be read however it is unclear
            // what the content should be.
            return result;
        }
    }
}
