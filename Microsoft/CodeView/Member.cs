using System.Runtime.InteropServices;

namespace PdbReader.Microsoft.CodeView
{
    internal class Member : INamedItem
    {
        private const ushort MethodAccessMask = 0x0003;
        private const ushort MethodPropertiesMask = 0x0007;
        private const ushort MethodPropertiesShift = 0x0002;
        internal ulong _fieldOffset;
        internal _Member _member;
        // variable length offset of field followed
        // by length prefixed name of field
        internal string _name;

        internal CV_access_e MethodAccess
            => (CV_access_e)((ushort)_member.attr & MethodAccessMask);

        internal CV_methodprop_e MethodProperties
            => (CV_methodprop_e)(((ushort)_member.attr & MethodPropertiesMask) >> MethodPropertiesShift);
        
            public string Name => _name;

        internal static Member Create(PdbStreamReader reader)
        {
            Member result = new Member();
            result._member = reader.Read<_Member>();
            /// Read field offset which is a variable length value.
            /// Algorithm is unclear and heuristically inferred.
            result._fieldOffset = reader.ReadVariableLengthValue();
            result._name = reader.ReadNTBString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Member
        {
            internal LEAF_ENUM_e _leaf; // LF_MEMBER
            internal CV_fldattr_t attr; // attribute mask
            internal uint /*CV_typ_t*/ index; // index of type record for field
        }
    }
}
