﻿
namespace PdbReader.Microsoft.CodeView
{
    /// <summary>From CVINFO.H
    /// No leaf index can have a value of 0x0000. The leaf indices are separated into ranges
    /// depending upon the use of the type record.
    /// The first range is for type records that are not referenced by symbols but instead are
    /// referenced by other type records.
    /// The second range is for the type records that are directly referenced in symbols
    /// All type records must have a starting leaf index in these first two ranges.
    /// The third range of leaf indices are used to build up complex lists such as the field
    /// list of a class type record. No type record can begin with one of the leaf indices.
    /// The fourth ranges of type indices are used to represent numeric data in a symbol or
    /// type record. These leaf indices are greater than 0x8000. At the point that type or
    /// symbol processor is expecting a numeric field, the next two bytes in the type record
    /// are examined.
    /// If the value is less than 0x8000, then the two bytes contain the numeric value.
    /// If the value is greater than 0x8000, then the data follows the leaf index in a format
    /// specified by the leaf index.
    /// The final range of leaf indices are used to force alignment of subfields within a
    /// complex type record.</summary>
    public enum LEAF_ENUM_e : ushort
    {
        // leaf indices starting records but referenced from symbol records
        Modifier16Bits = 0x0001,
        Pointer16Bits = 0x0002,
        Array16Bits = 0x0003,
        Class16Bits = 0x0004,
        Structure16Bits = 0x0005,
        Union16Bits = 0x0006,
        Enum16Bits = 0x0007,
        Procedure16Bits = 0x0008,
        MFunction16Bits = 0x0009,
        VirtualTableShape = 0x000a,
        Cobol016Bits = 0x000b,
        Cobol1 = 0x000c,
        BArray16Bits = 0x000d,
        Label = 0x000e,
        Null = 0x000f,
        NotTransacted = 0x0010,
        DimArray16Bits = 0x0011,
        VFPath16Bits = 0x0012,
        Precompilation16Bits = 0x0013,       // not referenced from symbol
        EndOfPrecompilation16Bits = 0x0014,       // not referenced from symbol
        OEM16Bits = 0x0015,       // oem definable type string
        TypeServerST = 0x0016,       // not referenced from symbol

        // Padding pseudo types
        Pad00 = 0xF0,
        Pad01 = 0xF1,
        Pad02 = 0xF2,
        Pad03 = 0xF3,
        Pad04 = 0xF4,
        Pad05 = 0xF5,
        Pad06 = 0xF6,
        Pad07 = 0xF7,
        Pad08 = 0xF8,
        Pad09 = 0xF9,
        Pad10 = 0xFA,
        Pad11 = 0xFB,
        Pad12 = 0xFC,
        Pad13 = 0xFD,
        Pad14 = 0xFE,
        Pad15 = 0xFF,

        // leaf indices starting records but referenced only from type records

        Skip16Bits = 0x0200,
        ArgumentList16Bits = 0x0201,
        DefaultArgument16Bits = 0x0202,
        List = 0x0203,
        FieldList16Bits = 0x0204,
        Derived16Bits = 0x0205,
        BitField16Bits = 0x0206,
        MethodList16Bits = 0x0207,
        DimensionCONU16Bits = 0x0208,
        DomensionCONLU16Bits = 0x0209,
        DimensionVARU16Bits = 0x020a,
        DimensionVARLU16Bits = 0x020b,
        ReferenceSymbol = 0x020c,

        BClass16Bits = 0x0400,
        VBClass16Bits = 0x0401,
        IVBClass16Bits = 0x0402,
        EnumrateST = 0x0403,
        FriendFunction16Bits = 0x0404,
        Index16Bits = 0x0405,
        Member16Bits = 0x0406,
        STMember16Bits = 0x0407,
        Method16Bits = 0x0408,
        NestedType16Bits = 0x0409,
        VFunctionTable16Bits = 0x040a,
        FriendClass16Bits = 0x040b,
        OEMMethod16Bits = 0x040c,
        VFunctionOFF16Bits = 0x040d,

        // 32-bit type index versions of leaves, all have the 0x1000 bit set
        Modifier = 0x1001,
        Pointer = 0x1002,
        ArrayST = 0x1003,
        ClassST = 0x1004,
        StructureST = 0x1005,
        UnionST = 0x1006,
        EnumST = 0x1007,
        Procedure = 0x1008,
        MFunction = 0x1009,
        Cobol0 = 0x100a,
        Barray = 0x100b,
        DimArrayST = 0x100c,
        VFTPath = 0x100d,
        PrecompST = 0x100e, // not referenced from symbol
        OEM = 0x100f,   // oem definable type string
        AliasST = 0x1010,   // alias (typedef) type
        OEM2 = 0x1011,  // oem definable type string

        Skip = 0x1200,
        ArgumentList = 0x1201,
        DefaultArgumentST = 0x1202,
        FieldList = 0x1203,
        Derived = 0x1204,
        BitField = 0x1205,
        MethodList = 0x1206,
        DimensionCONU = 0x1027,
        DimensionCONLU = 0x1208,
        DimensionVARU = 0x1209,
        DimensionVARLU = 0x120A,

        BClass = 0x1400,
        VBClass = 0x1401,
        IVNClass = 0x1402,
        FriendFunctionST = 0x1403,
        Index = 0x1404,
        MemberST = 0x1405,
        STMemberST = 0x1406,
        MethodST = 0x1407,
        NestTypeST = 0x1408,
        VFunctionTAB = 0x1409,
        FriendClass = 0x140a,
        OneMethodST = 0x140b,
        VFunctionOFF = 0x140c,
        NestedTypeExST = 0x140d,
        MemberModifyST = 0x140e,
        ManagedST = 0x140f,

        TypeServer = 0x1501,
        Enumerate = 0x1502,
        Array = 0x1503,
        Class = 0x1504,
        Structure = 0x1505,
        Union = 0x1506,
        Enum = 0x1507,
        DimArray = 0x1508,
        Precompilation = 0x1509, // not referenced from symbol
        Alias = 0x150a,       // alias (typedef) type
        DefaultArgument = 0x150b,
        FriendFunction = 0x150c,
        Member = 0x150d,
        STMember = 0x150e,
        Method = 0x150f,
        NestedType = 0x1510,
        OneMethod = 0x1511,
        NestedTypeEx = 0x1512,
        MemberModify = 0x1513,
        Managed = 0x1514,
        TypeServer2 = 0x1515,
        StridedArray = 0x1516,    // same as LF_ARRAY, but with stride between adjacent elements
        HLSL = 0x1517,
        ModifierEx = 0x1518,
        Interface = 0x1519,
        BInterface = 0x151a,
        Vector = 0x151b,
        Matrix = 0x151c,
        VirtualFunctionTable = 0x151d, // a virtual function table

        FunctionIdentifier = 0x1601, // global func ID
        MFunctionIdentifier = 0x1602, // member func ID
        BuildInformation = 0x1603, // build info: tool, version, command line, src/pdb file
        SubstringList = 0x1604, // similar to LF_ARGLIST, for list of sub strings
        StringIdentifier = 0x1605, // string ID
        UDTSourceLine = 0x1606, // source and line on where an UDT is defined only generated by compiler
        UDTModuleSourceLine = 0x1607, // module, source and line on where an UDT is defined only generated by linker
    }
}
