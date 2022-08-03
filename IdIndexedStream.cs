
namespace PdbReader
{
    /// <summary>Also known as the IPI stream.</summary>
    public class IdIndexedStream : IndexedStream
    {
        private const uint ThisStreamIndex = 4;

        public IdIndexedStream(Pdb owner)
            : base(owner, ThisStreamIndex)
        {
        }

        internal override string StreamName => "IPI";
    }
}
