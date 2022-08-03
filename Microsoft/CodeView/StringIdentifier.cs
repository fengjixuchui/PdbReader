﻿using System.Runtime.InteropServices;
using System.Text;

namespace PdbReader.Microsoft.CodeView
{
    internal struct StringIdentifier
    {
        internal _StringIdentifier Identifier { get; private set; }
        internal string Name { get; private set; }

        internal static StringIdentifier Create(PdbStreamReader reader)
        {
            StringIdentifier result = new StringIdentifier() {
                Identifier = reader.Read<_StringIdentifier>()
            };
            StringBuilder builder = new StringBuilder();
            while (true) {
                byte  inputByte = reader.ReadByte();
                if (0 == inputByte) {
                    break;
                }
                builder.Append((char)inputByte);
            }
            result.Name = builder.ToString();
            return result;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        internal struct _StringIdentifier
        {
            internal LEAF_ENUM_e leaf; // LF_STRING_ID
            internal uint /*CV_ItemId*/ id; // ID to list of sub string IDs
        }
    }
}
