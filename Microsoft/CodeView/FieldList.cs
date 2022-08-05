using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    internal struct FieldList
    {
        internal LEAF_ENUM_e leaf; // LF_FIELDLIST
        // char data[CV_ZEROLEN]; // field list sub lists

        internal static FieldList Create(PdbStreamReader reader)
        {
            FieldList result = reader.Read<FieldList>();
            // TODO : Handle data member.
            return result;
        }
    }
}
