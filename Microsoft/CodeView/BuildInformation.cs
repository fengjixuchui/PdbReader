using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class BuildInformation
    {
        internal _InformationBase Base;
        internal uint[] /*CV_ItemId*/ Arguments;

        private BuildInformation(_InformationBase @base)
        {
            Base = @base;
            Arguments = new uint[Base.count];
        }

        internal static BuildInformation Create(PdbStreamReader reader)
        {
            BuildInformation result = new BuildInformation(reader.Read<_InformationBase>());
            reader.ReadArray<uint>(result.Arguments, reader.ReadUInt32);
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
