# RegInject

Inject a `.reg` file into an offline hive or create a new one from scratch.      



## Preliminaries

Windows has two main [registry](https://en.wikipedia.org/wiki/Windows_Registry) file formats: the _registration file_ (`.reg`) and the _hive file_ (`.dat` or no extension). Only the latter file types are _loaded_ and directly used by Windows APIs. 

Registration files can be used by admins to inject new items in one of the loaded hives. 
However, there are situations where you want to edit hive file offline (without loading them). For example, you want to make different versions of a hive file, customised for different users; or you want to edit original hive files obtained from installation media, before building a system. 

Unfortunately there is no Microsoft utility to modify offline hive files, so you have to load the hive,  apply  modifications and unload it. This  procedure is not easily scriptable and  has security implications. 
Hence RegInject, where "inject"  is meant also as deleting, not only adding, items to a hive. 


## Usage: TLDR


If you are already knowledgeable about Windows registry, the RegInject help says the most of what you need:


    
    .\RegInject -h       

                                                                              
    Inject a `.reg' file into an offline hive or create a new one from scratch.   
                                                                                  
    Syntax:                                                                       
    RegInject [OPTIONS]  <.reg file path>                                         
    RegInject -e <hive file path> [-k subkey]                                     
                                                                                  
    Note: If the injected hive file exists, it will be overwritten.               
                                                                                  
    Options:                                                                      
      -s, --source=VALUE         Path to file to read the hive to inject. If not  
                                   given, the hive is created from scratch.       
      -i, --inject=VALUE         Path to file to write the injected hive. If not  
                                   given, it is built adding a '.new' suffix to th
                                   hive path in '-s'. If '-s' is not given, 'hive'
                                   is added to the regfile removing '.reg'.       
      -e, --explore=VALUE        Explore the hive file with human readable output.
      -k, --key=VALUE            Optional subtree to explore.                     
      -m, --major=VALUE          Major OS registry compat. Def. to 6.             
      -n, --minor=VALUE          Minor OS registry compat. Def. to 1.             
      -d, --debug=VALUE          Path to debug '.reg' file and verbose output.    
      -h, --help                 Show this message and exit.                      
  
  
Default version 6.1 refers to Windows 7,  see [Windows API](https://msdn.microsoft.com/en-us/library/ee210773)

Read on for more details. 

### Examples


    # Create the hive file adminhive with the entries from admin.reg
    .\RegInject admin.reg -i adminhive

    # Like above, but the hive is automatically named adminhive
    .\RegInject admin.reg

    # Inject user.reg in adminhive and write the resulting hive in adminhive.new
    .\RegInject user.reg -s adminhive

    # Like above, but the injected file is named deployhive
    .\RegInject admin.reg -s adminhive -i deployhive

    # See the content of deployhive. Binary strings are shown as human strings
    .\RegInject -e deployhive

    # For large hives redirect the output (>) to a file or specify a subkey tree
    .\RegInject -e deployhive -k subkeytree


## Setup 

`RegInject.exe` requires the free distributed Microsoft DLL `offreg.dll`, which is included in the project release. You can also obtain yourself the DLL from the [Offline Registry Library](https://msdn.microsoft.com/en-us/library/ee210757) page. 

The version shipped in the release package is the x64 version. The source code contain also the x86 version of the DLL in the `ridist` directory, anyway I was unable to find an x86 system to test it and it could be possibly removed. 


## Additional usage notes

### Relative Hive Paths

Bear in mind that key paths to be injected should be relative to the offline hive.   
In `regedit.exe`, assume  you exported the hive file (not the registration file `.reg`):

    HKEY_CURRENT_USER\Software
	
	
Say that you want to inject the value `1.2.3` named `version` in the subkey

    HKEY_CURRENT_USER\Software\MyApp

The `.reg` file to be injected should be: 


    [MyApp]
    "version"="1.2.3" 
	
When you later to attach the injected hive, you decide under which root to load it. 

### Make some tests

After injecting,  use the hive-explore option (`-e`) to test results. You might even compare the results with  `regedit.exe`.  In `regedit.exe`,  select `HKEY_USERS` and load your injected hive from the File menu. You are asked for a name for the loaded hive. You may unload the hive when you are finished. 
Comparing every time with Regedit would be against the purpose of RegInject, but a "second opinion" before going in production does not hurt. 

### Administrator rights

A RegInject design principle is to allow you to develop injection scripts on ordinary files, where you do not need any special permission. To enforce this principle, RegInject applies modifications on a copy of the source file, which means that you need a subsequent explicit shell command to replace the original file with the injected version. You are free to experiment with RegInject, but should apply special care when you proceed with their replacement. A very safe path would be developing your RegInject script on a copy of the target hive, test the injected hive file(s) in a virtual machine and possibly put the script in production. 

### Obtaining and replacing hive files

The registry is a database sparse into several hive files, where each hive contains a key and its subkeys. 
Depending on the Windows functions controlled or the users affected, hives are found here: 

    C:\Windows\System32\config
    %UserProfile%\NTUSER.DAT
    %LocalAppData%\Microsoft\Windows\Usrclass.dat

For the actual list consult the  registry key:

    HKEY_LOCAL_MACHINE\SYSTEM\CurrentControlSet\Control\hivelist

Loaded hives are protected from access, which means that you cannot access them (with ordinary means)  when they are in use. However, to have access to user specific hives suffices to act from another profile with admin rights. The original (untouched) version of system wide hives are found in `.wim` files on Windows installation media.   
Finally, it is possible to save hives in use with the Windows utility `reg.exe` (requires admin rights):

     REG SAVE KeySubtree hivepath

The same can be done with a GUI based `regedit.exe`, via its export key feature (in the "File" or key context menu).  In this case, uou have to choose as file type in the dialog Windows "Registry Hive files" (and not the default "Registration Files"). 

The replacement of user hives, requires the admin access from a different profile. If you are building or fixing a system and need to replace system hives, the preinstallation or recovery environment will usually give the permissions to touch the system hives. 


### Creating registration files

When  you create `.reg` files try not to divert from the [syntax rules](https://msdn.microsoft.com/en-us/library/gg469889.aspx). For a primer on `.reg` format see `reg-format.html` included with the project.  
Specifically, when adding values, do not add extra spaces around the `=` or around commas:

    "DWORD value"=dword:00000001
    "String value"="Hello world"
    "QWORD value"=hex(b):01,00,00,00,00,00,00,00

RegInject supports the delete syntax, using a dash before the key or after the value's equal sign, that is:

    [-KEY1]

deletes `KEY1` and

    "ValueName1"=-

deletes `ValueName1`. 


IF/ENDIF blocks are not supported. 


## Compatibility

Currently RegInject works with 'Windows Registry Editor Version 5.00' format.   
The Win95/NT `.reg` file format (REGEDIT4) is not supported. 

## Credits 

This project forks an `offreg.dll` wrapper by [Michael Bisbjerg](https://github.com/LordMike/OffregLib) (Lord Mike) 



_This ReadMe desperately needs a better template._
