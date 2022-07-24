Microsoft provides the DIA SDK for reading PDB (Program DataBase) files. However
this COM based interfaces are only available on Windows platforms and may require
some licences.

This repository attemps to provide a .Net 6.0 library that could be used on both
Windows and Linux platforms for reading PDB files. This work is based on several
documentation sources mainly :

The Microsoft public PDB repository
https://github.com/Microsoft/microsoft-pdb

The PDB file format from LLVM org documentation :
https://llvm.org/docs/PDB/index.html

The repository contains two projects. The PdbReader is the library itself while
PdbDumper is a console application that is both a utility and the library tester.
Both programs are intended to be run on x64 platforms only.

Additional materials
https://pierrelib.pagesperso-orange.fr/exec_formats/MS_Symbol_Type_v1.0.pdf
