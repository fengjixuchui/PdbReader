
namespace PdbReader
{
    /// <summary>Also known as the IPI stream.</summary>
    public class IdIndexedStream : IndexedStream
    {
        private const uint ThisStreamIndex = 4;

        internal IdIndexedStream(Pdb owner)
            : base(owner, ThisStreamIndex)
        {
        }

    }
}
