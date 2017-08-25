using System;
using System.Collections.Generic;
using Mono.Options;
using System.IO;
using System.Linq;

namespace RegInjectApp
{
    class ArgParser
    {
        public List<string> unNamed;

        bool _parseExit =false;
        OptionSet _ops;
        string[] _usage;

        public ArgParser(string[] args, string[] usageString, OptionSet ops )
        {
            _ops = ops;
            _usage = usageString;

            // Parse options
            try
            {
                unNamed = ops.Parse(args);
            }
            catch (OptionException e)
            {
                Console.WriteLine("\n{0}", e.Message);
                ShowHelpShort();
                _parseExit = true;
                return;
            }

            // Get unused options 
            List<string> unused= unNamed.Where(i => i.StartsWith("-")).ToList();
            if (unused.Count > 0) {
                Console.WriteLine("\nUnused option {0}", unused[0]);
                ShowHelpShort();
                _parseExit = true;
                return;
            }

            // Print help if required
            bool showhlp = false;
            var ops_help = new OptionSet { { "h|help", "", h => showhlp = h != null } };
            ops_help.Parse(args);
            if (showhlp)
            {
                ShowHelp();
                _parseExit = true; ;
            }


        }

        public bool parseExit() { return _parseExit; }

        public void ShowHelpShort()
        {

            Console.WriteLine(_usage[1]);
            Console.WriteLine("Options:");
            _ops.WriteOptionDescriptions(Console.Out);
            _parseExit = true; ;

        }

        public void ShowHelp()
        {
            Console.WriteLine( _usage[0]);
            ShowHelpShort();
        }

        public bool unNamedRange(int min, int max)
        {
            if (unNamed.Count < min)
            {
                Console.WriteLine("\nNot enough arguments");
                ShowHelpShort();
                return false;                
            }

            if (unNamed.Count > max)
            {
                Console.WriteLine("\nToo many unamedOpts arguments ({0}).", unNamed.Count);
                ShowHelpShort();
                return false;
            }
            return true;
        }

        public bool testFilePaths(List<string> pathList)
        {
            return testFilePaths(pathList.ToArray());
        }

        public bool testFilePaths(string[] pathArray)
        {

            foreach (string file in pathArray)
            {                
                if (!File.Exists(file))
                {
                    Console.WriteLine("\nFile '{0}' not found.", file);
                    return false;
                }
            }

            return true;
        }

        public bool testWouldBeFiles(List<string> pathList, bool overwrite)
        {
            return testWouldBeFiles(pathList.ToArray());
        }

        /// <summary>
        ///  File should not be an existing directory or file. Its directory component should exist
        /// </summary>
        /// <param name="pathArray"></param> The array of would be file paths 
        /// <param name="overwrite"></param> Overwrite by default.
        /// <returns></returns>
        public bool testWouldBeFiles(string[] pathArray, bool overwrite=true)
        {
           

            foreach (string file in pathArray)
            {
                if (File.Exists(file) && !overwrite)
                {
                    Console.WriteLine("\nFile '{0}' exists. I can't overwrite it", file);
                    ShowHelpShort();
                    return false;
                }

                if (Directory.Exists(file) )
                {
                    Console.WriteLine("\nThe path '{0}' is a directory. It should be a file.", file);
                    ShowHelpShort();
                    return false;
                }

                string dirComp =  new FileInfo(file).Directory.FullName;  
                if (!Directory.Exists(dirComp))
                {
                    Console.WriteLine("\nWith respect to '{0}', \n directory {1} does not exist.", 
                        file, dirComp);
                    return false;
                }



            }

            return true;
        }


        public bool testDifferentPaths(List<string> pathList)
        {
            return testDifferentPaths(pathList.ToArray());
        }

        public bool testDifferentPaths(string[] pathArray)
        {

            List<string> pathL = pathArray.ToList();

            pathL = pathL
                .Select(x => Path.GetFullPath(x))
                .ToList();

            var dup = pathL.GroupBy(x => x)
                    .Where(group => group.Count() > 1)
                    .Select(group => group.Key);

            if (dup.Count() > 0)
            {
                foreach (var elt in dup)
                {
                    Console.WriteLine("\nYou have duplicate path arguments:\n{0}", elt);
                }                    
                return false;
            }

            return true;
        }
        

    // end class
    }
}
