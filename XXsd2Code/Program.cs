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
    class Program
    {
        static void Main(string[] args)
        {
            try
            {
                ProcessCommandLineInfo processCommandLineInfo = new ProcessCommandLine().Process(args);

                LanguageWriterBase.CrossPlatformSerializationSupport = processCommandLineInfo.CrossPlatformSerializationSupport;

                foreach (string sourceFile in processCommandLineInfo.SourceFiles)
                {
                    try
                    {
                        CodeGenDom codeGenerator = new CodeGenDom(processCommandLineInfo.OutputDirectory);
                        codeGenerator.GenerateCode(sourceFile, "", processCommandLineInfo.TargetLanguage);
                    }
                    catch (Exception x)
                    {
                        Console.WriteLine(String.Format("Code gen exception for file : {0}", sourceFile));
                        Console.WriteLine(x.ToString());
                    }
                }
            }
            catch (Exception x)
            {
                ProcessCommandLine.DisplayUsage();
                Console.WriteLine(x.ToString());
            }
        }
    }
}

