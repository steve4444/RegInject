using System;
using System.Collections.Generic;
using System.Text;

// mine
using System.IO;
using OffregLib;
using Mono.Options;
using System.Linq;

namespace RegInjectApp
{
    class RegInjectApp
    {

        #region major, minor versions ...
        // Win 7 => 6.1 see:
        // https://msdn.microsoft.com/en-us/library/ee210773
        static uint major = 6, minor = 1;
        #endregion

        static string[] managedRegTypes;
        static bool debug = false, fromscratch = true;


        static void Main(string[] args)
        {
            string dotregpath="", hivepath = "", newhivep = "", exploreKey = "", debfile = "";
            bool suffix = true, explore = false, vers = false;
            var help = false;

            #region Option text
            var p = new OptionSet {
                { "s|source=",
                    "Path of the hive file to inject. If not given, the hive is created from scratch.",
                    (string s) => { hivepath = s; fromscratch=false; } },
                { "i|inject=", "Path to the new hive injected. " +
                "If not given, it is built adding a '.new' suffix to the hive path in '-s'. if " +
                "neither '-s' is given, 'hive' is added to the regfile removing '.reg' extension.",
                    (string i) => { newhivep= i; suffix = false; } },
                { "e|explore=", "Explore the hive file with human readable output.",
                    (string e) => { hivepath = e; explore=true; } },
                { "k|key=",     "Optional subtree to explore.",       (string k) => { exploreKey=k; } },
                { "m|major=", "Major OS registry compat. Def. to 6.", (uint m)=>{ major=m; vers= true; } },
                { "n|minor=", "Minor OS registry compat. Def. to 1.", (uint n)=>{ minor=n; vers= true; } },
                { "d|debug=", "Verbose and Debug (reg) file.", (string d) => {debfile = d; debug =true;} },
                { "h|help",   "show this message and exit",                      h => help = h != null },
            };

            string[] usage = {
                "\nInject a `.reg' file into an offline hive or create a new one from scratch.",
                @"
Syntax:
RegInject [OPTIONS]  <.reg file path> 
RegInject -e <hive file path> [-k subkey]

Note: If the path of the new hive to be created exists, it will be overwritten. 
" };
            #endregion

            ArgParser apr = new ArgParser(args, usage, p);
            if (apr.parseExit()) return;

            // Incompatible options
            if (explore && (
                fromscratch == false || suffix == false || debug == true || vers == true))
            {
                Console.WriteLine("'-e' is only compatible with '-k'");
                return;
            }

            if (exploreKey != "" && !explore) {
                Console.WriteLine("'-k' requires '-e'");
                return;
            }


            // Manage explore file paths 
            List<string> fpaths;
            if (explore)
            {
                if (!apr.unNamedRange(0, 0)) return;
                fpaths = new List<string>(new string[] { hivepath });
                if (!apr.testFilePaths(fpaths)) return;
                if (debug)
                {
                    if (!apr.testWouldBeFiles(new string[] { debfile }, true)) return;
                    fpaths.Add(debfile);
                }

            }

            // Manage file paths for inject
            else
            {
                if (!apr.unNamedRange(1, 1)) return;
                dotregpath = apr.unNamed[0];
                if (suffix)
                {
                    newhivep = fromscratch ? Path.GetDirectoryName(dotregpath) + "\\"
                                                + Path.GetFileNameWithoutExtension(dotregpath) + "hive"
                                                : hivepath + ".new";
                }

                fpaths = new List<string>(new string[] { dotregpath });
                if (!fromscratch) fpaths.Add(hivepath);
                if (!apr.testFilePaths(fpaths)) return;

                List<string> wbpaths = new List<string>(new string[] { newhivep });
                if (debug) wbpaths.Add(debfile);
                if (!apr.testWouldBeFiles(wbpaths, true)) return;
                fpaths.AddRange(wbpaths);
            }

            if (!apr.testDifferentPaths(fpaths)) return;

            // Start the real thing
            if (explore)
                exploreHive(hivepath, exploreKey, debfile);
            else
                Inject(hivepath, dotregpath, newhivep, debfile);


        }

        static void Inject(string hivepath, string regfpath, string newhivep, string debfile)
        {

            string key = "", name, type, value;

            // Read registry file
            DotRegFile dotreg = new DotRegFile(regfpath);
            if (!dotreg.checkFormat()) return;
            if (debug)
            {
                File.WriteAllText(debfile, dotreg.simplified());
                Console.WriteLine("Debug file generated as '{0}'", debfile);
            }

            // Parse 
            managedRegTypes = dotreg.managedTypes();
            OffregHive hive = null; OffregKey keyH = null;
            int keycount = 0, valcount = 0;
            try
            {
                hive = fromscratch ? OffregHive.Create() : OffregHive.Open(hivepath);
                foreach (string line in dotreg.getLines())
                {
                    if (dotreg.isKey(line))
                    {
                        key = dotreg.getKey();
                        Console.WriteLine("{0}", key);
                        keyH = safeCreateKey(hive, keyH, key);
                        keycount++;
                        continue;
                    }

                    if (dotreg.isDataItem(line))
                    {
                        name = dotreg.getName();
                        type = dotreg.getType();
                        value = dotreg.getValue();
                        if (debug)
                            Console.WriteLine("key: {0}, name: {1}, type: {2}, value: {3}",
                                key, name, type, value);
                        setVal(keyH, name, type, value);
                        valcount++;

                    }
                }

                safeSaveHive(hive, newhivep, major, minor);
                safeCloseHandles(hive, keyH);
            }

            catch (Exception ex)
            {
                string err = ex.Message;
                if (ex.HResult == -2147467259 && ex.TargetSite.Name == "CreateSubKey")
                    err = string.Format("The system cannot inject the key {0}", key);
                Console.WriteLine("Exception thrown\n{0}", err);
                safeCloseHandles(hive, keyH);
                return;
            }

            Console.WriteLine("Injected {0} key(s) and {1} value(s).",
                                keycount, valcount);
        }
        
        /// <summary>
        /// Create or delete a key, but first close the current handle if open.
        /// </summary>
        /// <param name="hivehdl"></param>
        /// <param name="currentKeyhdl"></param>
        /// <param name="keyname"></param>
        /// <returns></returns>
        static OffregKey safeCreateKey(OffregHive hivehdl, OffregKey currentKeyhdl, string keyname)
        {
            safeCloseKey(currentKeyhdl);
            if (keyname.StartsWith("-"))
            {
                keyname = keyname.TrimStart('-');
                hivehdl.Root.DeleteSubKey(keyname);
                return currentKeyhdl;
            }
            return hivehdl.Root.CreateSubKey(keyname);
        }

        /// <summary>
        /// Close key handle only if open
        /// </summary>
        /// <param name="keyhdl"></param>
        static void safeCloseKey(OffregKey keyhdl)
        {
            if (keyhdl != null) keyhdl.Close();
        }

        /// <summary>
        /// Close hive and key only if open
        /// </summary>
        /// <param name="hivehdl"></param>
        /// <param name="keyhdl"></param>
        static void safeCloseHandles(OffregHive hivehdl, OffregKey keyhdl)
        {
            safeCloseKey(keyhdl);
            if (hivehdl != null) hivehdl.Close();
        }

        static void safeSaveHive(OffregHive hivehdl, string newhivep, uint major, uint minor)
        {
            if (File.Exists(newhivep)) File.Delete(newhivep);
            hivehdl.SaveHive(newhivep, major, minor);
        }
        
        /// <summary>
        /// Delete or create values
        /// </summary>
        /// <param name="KHdl"></param>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        static void setVal(OffregKey KHdl, string name, string type, string value)
        {
            byte[] bytes;
            int num;
            RegValueType typeEnum = RegValueType.REG_BINARY; //  null not allowed


            if (name == "@") name = "";

            if (value == "-")
            {
                KHdl.DeleteValue(name);
                return;
            }

            switch (type)
            {
                case "REG_NONE":
                    KHdl.SetValueNone(name);
                    return;

                case "REG_SZ":
                    KHdl.SetValue(name, value, RegValueType.REG_SZ);
                    return;

                case "REG_EXPAND_SZ":
                    typeEnum = RegValueType.REG_EXPAND_SZ;
                    break;

                case "REG_BINARY":
                    typeEnum = RegValueType.REG_BINARY;
                    break;

                case "REG_DWORD":
                    num = Convert.ToInt32(value, 16);
                    KHdl.SetValue(name, num, RegValueType.REG_DWORD);
                    return;

                case "REG_DWORD_BIG_ENDIAN":
                    typeEnum = RegValueType.REG_DWORD_BIG_ENDIAN;
                    break;

                case "REG_LINK":
                    typeEnum = RegValueType.REG_LINK;
                    break;

                case "REG_MULTI_SZ":
                    typeEnum = RegValueType.REG_MULTI_SZ;
                    break;

                case "REG_RESOURCE_LIST":
                    typeEnum = RegValueType.REG_RESOURCE_LIST;
                    break;

                case "REG_FULL_RESOURCE_DESCRIPTOR":
                    typeEnum = RegValueType.REG_FULL_RESOURCE_DESCRIPTOR;
                    break;

                case "REG_RESOURCE_REQUIREMENTS_LIST":
                    typeEnum = RegValueType.REG_RESOURCE_REQUIREMENTS_LIST;
                    break;

                case "REG_QWORD":
                    typeEnum = RegValueType.REG_QWORD;
                    break;

                case "REG_QWORD_LITTLE_ENDIAN":
                    typeEnum = RegValueType.REG_QWORD_LITTLE_ENDIAN;
                    break;
            }


            if (managedRegTypes.Contains(type))
            {
                bytes = value.Split(',').Select(s => Convert.ToByte(s, 16)).ToArray();
                KHdl.SetValue(name, bytes, typeEnum);
            }
            else
                throw new Exception(string.Format("The type {0} is not known.", type));

        }

        static void exploreHive(string hivepath, string key, string debfile)
        {
            key=key.TrimStart('\\');
            using (OffregHive hive = OffregHive.Open(hivepath))
            {
                OffregKey startKey;
                startKey = key == "" ? hive.Root : hive.Root.OpenSubKey(key);
                //-k "empty"
                enumSub(startKey);
                //Console.WriteLine("Done");
            }
        }

        private static void enumSub(OffregKey key)
        {
            Console.WriteLine("[" + key.FullName + "]");

            ValueContainer[] values = key.EnumerateValues();
            if (values.Length > 0)
            {
                foreach (ValueContainer value in values)
                {
                    RegValueType type = value.Type;
                    object data = value.Data;
                    Console.WriteLine("\"" + value.Name + "\"(" + type + ")=" + data);
                }

                Console.WriteLine("");
            }

            SubKeyContainer[] subKeys = key.EnumerateSubKeys();

            foreach (SubKeyContainer subKey in subKeys)
            {
                try
                {
                    using (OffregKey sub = key.OpenSubKey(subKey.Name))
                    {
                        enumSub(sub);
                    }
                }
                catch (Exception ex) // Win32Exception ex
                {
                    Console.WriteLine("<" + key.FullName + " -> " + subKey.Name + ": " + ex.Message + ">");
                }
            }
        }

    }  // End Class
}
