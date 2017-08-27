
# Syntax of .Reg Files

Excerpt from KB  [310516](https://support.microsoft.com/en-us/help/310516/how-to-add--modify--or-delete-registry-subkeys-and-values-by-using-a)   
See also [MSDN rules](https://msdn.microsoft.com/en-us/library/gg469889.aspx).

A .reg file has the following syntax:

    RegistryEditorVersion
    Blank line
    [RegistryPath1]
     
    "DataItemName1"="DataType1:DataValue1"
    DataItemName2"="DataType2:DataValue2"
    Blank line
    [RegistryPath2]
     
    "DataItemName3"="DataType3:DataValue3"

where:

`RegistryEditorVersion` is either "Windows Registry Editor Version 5.00" for Windows 2000, Windows XP, and Windows Server 2003, or "REGEDIT4" for Windows 98 and Windows NT 4.0. The "REGEDIT4" header also works on Windows 2000-based, Windows XP-based, and Windows Server 2003-based computers.

_Blank line_ is a blank line. This identifies the start of a new registry path. Each key or subkey is a new registry path. If you have several keys in your .reg file, blank lines can help you to examine and to troubleshoot the contents.

`RegistryPathx` is the path of the subkey that holds the first value you are importing. Enclose the path in square brackets, and separate each level of the hierarchy by a backslash. For example:

    [HKEY_LOCAL_ MACHINE\SOFTWARE\Policies\Microsoft\Windows\System]

A .reg file can contain several registry paths. If the bottom of the hierarchy in the path statement does not exist in the registry, a new subkey is created. The contents of the registry files are sent to the registry in the order you enter them. Therefore, if you want to create a new subkey with another subkey below it, you must enter the lines in the correct order.

`DataItemNamex` is the name of the data item that you want to import. If a data item in your file does not exist in the registry, the .reg file adds it (with the value of the data item). If a data item does exist, the value in your .reg file overwrites the existing value. Quotation marks enclose the name of the data item. An equal sign (=) immediately follows the name of the data item.

`DataTypex` is the data type for the registry value and immediately follows the equal sign. For all the data types other than `REG_SZ` (a string value), a colon immediately follows the data type. If the data type is `REG_SZ` , do not include the data type value or colon. In this case, Regedit.exe assumes `REG_SZ` for the data type. The following table lists the typical registry data types:

    Data Type	DataType in .reg
    REG_BINARY	hexadecimal
    REG_DWORD	dword
    REG_EXPAND_SZ	hexadecimal(2)
    REG_MULTI_SZ	hexadecimal(7)
	
For more information about registry data types, click the following article number to view the article in the Microsoft Knowledge Base:
 
[256986](https://support.microsoft.com/en-us/help/256986) Description of the Microsoft Windows registry
 
DataValuex immediately follows the colon (or the equal sign with `REG_SZ`) and must be in the appropriate format (for example, string or hexadecimal). Use hexadecimal format for binary data items.

Note You can enter several data item lines for the same registry path.

Note the registry file should contain a blank line at the bottom of the file.

...

Last Review: Apr 18, 2017 - Revision: 2

Applies to
Windows 7 Enterprise, Windows 7 Professional, Windows 7 Home Basic, ...



Registry data types from Windows `winnt.h`. See also 
[SHRegWriteUSValue function](https://msdn.microsoft.com/en-us/library/windows/desktop/bb773556)


    // Predefined Value Types.
    //
     
    #define REG_NONE                    ( 0ul ) // No value type
    #define REG_SZ                      ( 1ul ) // Unicode nul terminated string
    #define REG_EXPAND_SZ               ( 2ul ) // Unicode nul terminated string
                                                // (with environment variable references)
    #define REG_BINARY                  ( 3ul ) // Free form binary
    #define REG_DWORD                   ( 4ul ) // 32-bit number
    #define REG_DWORD_LITTLE_ENDIAN     ( 4ul ) // 32-bit number (same as REG_DWORD)
    #define REG_DWORD_BIG_ENDIAN        ( 5ul ) // 32-bit number
    #define REG_LINK                    ( 6ul ) // Symbolic Link (unicode)
    #define REG_MULTI_SZ                ( 7ul ) // Multiple Unicode strings
    #define REG_RESOURCE_LIST           ( 8ul ) // Resource list in the resource map
    #define REG_FULL_RESOURCE_DESCRIPTOR ( 9ul ) // Resource list in the hardware description
    #define REG_RESOURCE_REQUIREMENTS_LIST ( 10ul )
    #define REG_QWORD                   ( 11ul ) // 64-bit number
    #define REG_QWORD_LITTLE_ENDIAN     ( 11ul ) // 64-bit number (same as REG_QWORD)


In reg files less common data types are presented with the type `hex(x)` where `x` is the hex version of the integer in parenthesis.
