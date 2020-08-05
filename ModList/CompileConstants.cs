

using System;

namespace IPA.ModList.BeatSaber
{
    internal static class CompileConstants
    {
        public const string AssemblyName = "IPA.ModList.BeatSaber";
        public const string SolutionDirectory = 
        #if DEBUG
            "$(SolutionDir)";
        #else
            "";
        #endif
        public const string CopyrightDate = "2020";
        public const string CopyrightString = "Copyright © " + Author + " " + CopyrightDate;

        public const string Author = Manifest.Author;
        public const string Version = Manifest.Version;

        public const int BuildYear = 2020;
        public const int BuildMonth = 8;
        public const int BuildDay = 4;

        public static DateTime BuildDate => new DateTime(BuildYear, BuildMonth, BuildDay);

        public static class Manifest
        {
                        public const System.String Name =  
                        "Mod List";
                                public const System.String Author =  
                        "DaNike";
                                public const System.String Version =  
                        "0.1.0";
                            }
    }
}

