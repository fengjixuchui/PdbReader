using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class BuildInformation
    {
        internal _InformationBase Base;
        internal ushort[] /*CV_ItemId*/ Arguments;

        private BuildInformation(_InformationBase @base)
        {
            Base = @base;
            Arguments = new ushort[Base.count];
        }

        internal static BuildInformation Create(PdbStreamReader reader)
        {
            BuildInformation result = new BuildInformation(reader.Read<_InformationBase>());
            reader.ReadArray<ushort>(result.Arguments, reader.ReadUInt16);
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _InformationBase
        {
            internal LEAF_ENUM_e leaf; // LF_BUILDINFO
            internal ushort count; // number of arguments
        }
    }
}
