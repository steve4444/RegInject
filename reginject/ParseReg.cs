using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RegInjectApp
{
    class DotRegFile
    {
        static string[] _lines;
        static string _fileContent,
            _currentKey, _currentName, _currentType, _currentValue;

        Dictionary<string, string> regTypes;
        List<KeyValuePair<string, string>> fastRegexps = new List<KeyValuePair<string, string>>();


        public DotRegFile(string regpath)
        {
            _fileContent = File.ReadAllText(regpath);
            fillRegmap();
        }

        public bool checkFormat()
        {
            return simplify();
        }

        /// <summary>
        /// Simplify content return false if expected items are not found.
        /// </summary>
        /// <returns>True if no error is detected during the semplification.</returns>
        bool simplify()
        {

            string pat;
            // Temporary set Unix line endings 
            RepWhileMatch("\r\n", "\n");
            RepWhileMatch("\r", "\n"); // just in case 


            // Trim line left
            pat = setRxp("^%B");
            RepWhileMatch(pat, "", multi: true);

            // Trim line right
            pat = setRxp("%B$");
            RepWhileMatch(pat, "", multi: true);

            //Remove double blank lines except last (requires trim)
            pat = setRxp("\n\n");
            RepWhileMatch(pat, "\n");
            _fileContent += "\n";

            // Remove Version Line 
            pat = setRxp("^%zWindows%BRegistry%BEditor%BVersion%B5\\.00\n");
            if (!RepWhileMatch(pat, ""))
            {
                Console.WriteLine("Unrecognised file format!\n" +
                    "Unable to find 'Windows Registry Editor Version 5.00' line.");
                return false;
            }

            // Remove long lines splits
            pat = setRxp("\\\\%N");
            RepWhileMatch(pat, "");

            // Line split 
            _lines = Regex.Split(_fileContent, "\n");

            // Set back Windows line endings
            _fileContent = Regex.Replace(_fileContent, "\n", "\r\n"); // Side eff: fix non-win endings

            return true;
        }

        public string[] getLines()
        {
            return _lines;
        }

        public string simplified()
        {
            return _fileContent;
        }

        public bool isKey(string line) {

            bool yes = line.StartsWith("[");
            if (yes)
            {
                line = line.Replace("[", "");
                line = line.Replace("]", "");
                _currentKey = line;

            }

            return yes;
        }

        public string getKey()
        {
            return _currentKey;
        }

        public bool isDataItem(string line)
        {
            string pat;
            Match match;

            bool yes = line.StartsWith("\"") || line.StartsWith("@"); 
            if (yes)
            {
                // get and remove name 
                if (line.StartsWith("@"))
                {
                    _currentName = "@";
                    line = Regex.Replace(line, "^@", "");
                }
                else
                {
                    pat = ".+?[^\\\\]\"";
                    match = Regex.Match(line, pat);
                    line = (new Regex(pat)).Replace(line, "", 1);
                    _currentName = Regex.Replace(match.Value, "^\"", "");
                    _currentName = Regex.Replace(_currentName, "\"$", "");
                }

                // test for remove value command 
                if (line.StartsWith("=-"))
                {
                    _currentType = null;
                    _currentValue = "-";
                    return true;
                }

                // get and remove type 
                if (line.StartsWith("=\"")) _currentType = "REG_SZ";
                else
                {
                    pat = ".+?:";
                    match = Regex.Match(line, pat);
                    line = (new Regex(pat)).Replace(line, "", 1);
                    _currentType = match.Value.Replace("=", "");
                    _currentType = _currentType.Replace(":", "");
                }

                // get friendly mapped value for type
                _currentType = setType(_currentType);

                // get value
                _currentValue = Regex.Replace(line, "^=", ""); // for reg_sz only
                _currentValue = Regex.Replace(_currentValue, "^\"", "");
                _currentValue = Regex.Replace(_currentValue, "\"$", "");

            }

            return yes;
        }

        public string getName()
        {
            return _currentName;
        }

        public string getType()
        {
            return _currentType;
        }

        public string getValue()
        {
            return _currentValue;
        }

        public string[] managedTypes() {
            return regTypes.Values.ToArray();    
        }

        private string setType(string token)
        {
            /*
            switch (token)
            {
                case "REG_SZ":
                    return token;
         //       case "dword":
         //           return "REG_DWORD";
         //       case "hex(ffff0005)":
          //          return "REG_DWORD_BIG_ENDIAN";                    
            }
            */

            return regTypes.Keys.Contains(token) ? regTypes[token] : token;

        }

        private void fillRegmap()
        {
            regTypes = new Dictionary<string, string>() {
                { "hex(0)", "REG_NONE"                       },
                { "REG_SZ", "REG_SZ"                         },  // no type, implicit for stings 
                { "hex(2)", "REG_EXPAND_SZ"                  },  
                { "hex", "REG_BINARY"                        },
                { "dword", "REG_DWORD"                       },   // not hex(4) 
                // { "hex(4)", "REG_DWORD_LITTLE_ENDIAN"     },   // not on Intel? 
                { "hex(ffff0005)", "REG_DWORD_BIG_ENDIAN"    },   // not hex(5) in win10
                { "hex(6)", "REG_LINK"                       },
                { "hex(7)", "REG_MULTI_SZ"                   },
                { "hex(8)", "REG_RESOURCE_LIST"              },
                { "hex(9)", "REG_FULL_RESOURCE_DESCRIPTOR"   },
                { "hex(a)", "REG_RESOURCE_REQUIREMENTS_LIST" },
                { "hex(b)", "REG_QWORD"                      },
                //  { "hex(b)", "REG_QWORD_LITTLE_ENDIAN"        },
                //  Littel endian is the default on standard PC
            };
        }
       
        #region  Reg Exp functions


        /// <summary>
        /// Continue replace untile there is a match in file content.
        /// </summary>
        /// <param name="pattern"></param>
        /// <param name="repstring"></param>
        /// <param name="multi"></param>
        /// <returns></returns>
        bool RepWhileMatch(string pattern, string repstring, bool multi = false)
        {
            /* Warning!
             * If repstring contains also pattern (e.g. you want to dobule the pattern) 
             * you got into infinite recursion)
            */

            bool success;
            RegexOptions mopt = RegexOptions.Multiline;

            if (multi)
            {
                success = Regex.Match(_fileContent, pattern, mopt).Success;
                while (Regex.Match(_fileContent, pattern, mopt).Success)
                    _fileContent = Regex.Replace(_fileContent, pattern, repstring, mopt);
            }
            else
            {
                success = Regex.Match(_fileContent, pattern).Success;
                while (Regex.Match(_fileContent, pattern).Success)
                    _fileContent = Regex.Replace(_fileContent, pattern, repstring);
            }

            return success;
        }

        string setRxp(string pattern) {

            // Add to fastRegexp object 
            addrxp("%b", "[\t ]");       // one space
            addrxp("%z", "[\t ]*");      // zero or more spaces
            addrxp("%B", "[\t ]+");      // one or more spaces
            addrxp("%N", "[\n\t ]+");    // one or more blanks


            foreach (KeyValuePair<string, string> rxp in fastRegexps)                
                pattern = Regex.Replace(pattern, rxp.Key, rxp.Value);
            return pattern;
        }

        void addrxp(string rxname, string rxvalue)
        {
            fastRegexps.Add(new KeyValuePair<string, string>(rxname, rxvalue));
        }

        //end  Reg Exp functions

        #endregion


    } // End Class
}
