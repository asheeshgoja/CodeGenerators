/*
This file is part of XXsd2Code <http://xxsd2code.sourceforge.net/>

XXsd2Code is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
any later version.

XXsd2Code is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with XXsd2Code.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
namespace XXsd2Code
{
    public class ProcessCommandLineInfo
    {
        public TargetLanguage TargetLanguage;
        public List<string> SourceFiles = new List<string>();
        public String OutputDirectory;
        public bool CrossPlatformSerializationSupport;
    }

    public class ProcessCommandLine
    {
        public static void DisplayUsage()
        {
            Console.WriteLine(@"Usage   :  XXsd2Code <SourceDirectory> / <TargetDirectory> /<TargetLanguage>[C++,C++CLI,C#,Java]/<CPSS>[Optional]");
            Console.WriteLine(@"Example :  .\bin\debug\XXsd2Code.exe .\Test_Xsdss/.\Test_Xsds\TestClients\TestClient_Java\TestClient_Java\src\XXsd2CodeSample/Java");
        }

        TargetLanguage ParseTargetLanguage(String languageSwitch)
        {
            languageSwitch = languageSwitch.Trim();

            if (languageSwitch == "C++")
                return TargetLanguage.CPP;
            if (languageSwitch == "C++CLI")
                return TargetLanguage.CPP_CLI;
            if (languageSwitch == "C#")
                return TargetLanguage.CSharp;
            if (languageSwitch == "Java")
                return TargetLanguage.JAVA;

            throw new ArgumentException(String.Format("Unknown target language: {0}", languageSwitch));
        }

        public ProcessCommandLineInfo Process(string[] args)
        {
            ProcessCommandLineInfo processCommandLineInfo = new ProcessCommandLineInfo();

            args = String.Join(" ", args).Split("/".ToCharArray());

            if (args.Length < 2 || args.Length > 4)
                throw new ArgumentException(String.Format("Arguments specified: {0}", args.Length));

            DirectoryInfo sourceDir = new DirectoryInfo(args[0]);
            if (!sourceDir.Exists)
            {
                FileInfo fi = new FileInfo(args[0]);
                sourceDir = new DirectoryInfo(fi.Directory.FullName);
            }

            if (!sourceDir.Exists)
                throw new ArgumentException(String.Format("Source directory not found: {0}", sourceDir.ToString()));

            TargetLanguage targetLanguage = TargetLanguage.CPP;
            DirectoryInfo targetDir = sourceDir;

            switch (args.Length)
            {
                case 2:
                    targetDir = sourceDir;
                    targetLanguage = ParseTargetLanguage(args[1]);
                    break;
                case 3:
                case 4:
                    targetDir = new DirectoryInfo(args[1]);
                    targetLanguage = ParseTargetLanguage(args[2]);
                    break;
            }

            if (!targetDir.Exists)
            {
                Directory.CreateDirectory(targetDir.FullName);
            }


            FileInfo[] sourceFiles = sourceDir.GetFiles("*.xsd");
            if (sourceFiles.Length == 0)
                throw new ArgumentException(String.Format("No .xsd files found in: {0}", sourceDir.ToString()));

            if (args.Length == 4)
            {
                processCommandLineInfo.CrossPlatformSerializationSupport = (args[3] == "CPSS" ? true : false);
            }

            processCommandLineInfo.TargetLanguage = targetLanguage;
            processCommandLineInfo.OutputDirectory = targetDir.FullName;

            foreach (FileInfo sourceFile in sourceFiles)
            {
                processCommandLineInfo.SourceFiles.Add(sourceFile.FullName);
            }

            return processCommandLineInfo;

        }
    }
}
