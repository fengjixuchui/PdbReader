﻿
namespace PdbReader.Microsoft.CodeView
{
    internal enum CV_ptrmode_e
    {
        NormalPointer = 0x00, // "normal" pointer
        OldReference = 0x01, // "old" reference
        LeftValueReference = 0x01, // l-value reference
        PointerToMember = 0x02, // pointer to data member
        PointerToMemberFunction = 0x03, // pointer to member function
        RightValueReference = 0x04, // r-value reference
        FirstUnusedPointerMode = 0x05  // first unused pointer mode
    }
}
