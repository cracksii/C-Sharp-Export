    using System;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Collections.Generic;

namespace Export
{
    class Program
    {
        static void Main(string[] args)
        {
            if(args.Length < 2)
            {
                Console.WriteLine("Arg 1: Project root");
                Console.WriteLine("Arg 2: Output directory");
                Console.WriteLine("Arg 3: Include/exclude (by default all \"dll\", \"exe\" and \"json\" files are included. Add a ! before the file name to mark it as include)\n");
                Console.WriteLine("Eg: export.exe \"c:\\path\\to\\my\\project\" \"c:\\path\\to\\my\\project\\build\" \"data.json,test.dll\" <- Excludes \"data.json\" and \"test.dll\"");
                Console.WriteLine("Eg: export.exe \"c:\\path\\to\\my\\project\" \"c:\\path\\to\\my\\project\\build\" \"!save.png\"          <- Includes \"save.png\"\n");

                Console.WriteLine("You can also cd into the project root and use relative paths (Quotation marks are only required if the path contains a space)");
                Console.WriteLine("Eg: cd \"c:\\path\\to\\my\\project\" && export.exe \".\" \".\\build\"");
                Environment.Exit(-1);
            }
            string projectRoot = "";
            try{
                projectRoot = Directory.EnumerateDirectories(Path.Combine(args[0], "bin", "Debug")).First(i => i.Contains("net"));
            }catch{
                Console.WriteLine("No project found");
            }
            var allFiles = Directory.EnumerateFiles(projectRoot);
            List<string> files = new List<string>();
            List<string> exclude = new List<string>();
            List<string> include = new List<string>();

            if(args.Length == 3)
            {
                if(args[2].Contains(","))
                    exclude = args[2].Split(",").Select(i => {
                        i = i.Trim();
                        i = i.StartsWith(".\\") ? i[2..] : i;
                        if(i.StartsWith("!"))
                        {
                            include.Add(Path.Combine(projectRoot, i[1..]));
                            return null;
                        }
                        return Path.Combine(projectRoot, i);
                    }).Where(i => i != null).ToList();
                else
                {
                    args[2] = args[2].StartsWith(".\\") ? args[2][2..] : args[2];
                    if(!args[2].StartsWith("!"))
                        exclude.Add(Path.Combine(projectRoot, args[2][1..]));
                    else
                        include.Add(Path.Combine(projectRoot, args[2]));
                }   
            }

            Console.WriteLine($"Include: {string.Join("\n         ", include)}\nExclude: {string.Join("\n         ", exclude)}\n");

            foreach(var f in allFiles)
            {
                // Console.WriteLine($"{f} {include.Contains(f)}");
                if((f.EndsWith(".dll") || f.EndsWith(".exe") || f.EndsWith(".json") || include.Contains(f)) && !exclude.Contains(f))
                {
                    if(include.Contains(f))
                        include.Remove(f);
                    files.Add(f);
                    Console.WriteLine(f);
                }
            }

            if(args[1].EndsWith("\\"))
                args[1] = args[1][..^1];

            foreach(var f in Directory.EnumerateFiles(args[1]))
                File.Delete(f);

            foreach(var f in files)
                File.Copy(f, Path.Combine(args[1], Path.GetFileName(f)));
            
            using (ZipArchive zip = ZipFile.Open(Path.Combine(args[1], "Build.zip"), ZipArchiveMode.Create))
            {
                foreach(var f in Directory.GetFiles(args[1]).Where(i => !i.Contains("Build.zip")))
                    zip.CreateEntryFromFile(f, Path.GetFileName(f));
            }
            Console.WriteLine(Path.Combine(args[1], "Build.zip\n"));

            if(include.Count > 0)
                Console.WriteLine($"Following files couldn't get included:\n   {string.Join("\n   ", include)}");
        }
    }
}
