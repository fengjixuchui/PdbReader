using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader.Microsoft.CodeView
{
    internal class Enumeration
    {
        internal _Enumeration _data;
        internal string _name;

        private Enumeration(_Enumeration data, string name)
        {
            _data = data;
            _name = name ?? throw new ArgumentNullException(nameof(name));
        }

        internal static Enumeration Create(PdbStreamReader reader, IndexedStream stream)
        {
            _Enumeration data = reader.Read<_Enumeration>();
            List<byte> nameBytes = new List<byte>();
            while (true) {
                byte inputByte = reader.ReadByte();
                if (0 == inputByte) {
                    break;
                }
                nameBytes.Add(inputByte);
            }
            string enumerationName = Encoding.ASCII.GetString(nameBytes.ToArray());
            // TODO : Looks like there is another name after this one at least from time to
            // time.
            return new Enumeration(data, enumerationName);
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _Enumeration
        {
            internal LEAF_ENUM_e leaf; // LF_ENUM
            internal ushort count; // count of number of elements in class
            internal CV_prop_t property; // property attribute field
            internal uint /*CV_typ_t*/ utype; // underlying type of the enum
            internal uint /*CV_typ_t*/ field; // type index of LF_FIELD descriptor list
            // TODO 
            // unsigned char Name[1]; // length prefixed name of enum
        }
    }
}
