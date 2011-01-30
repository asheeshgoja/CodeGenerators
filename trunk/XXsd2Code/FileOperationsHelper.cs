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
    class FileOperations
    {

        public string CreateFileName(string outputFile, string xsdSchemaFileName, string destinationFolder, out string outputFileOriginal, TargetLanguage targetLanguage, string xsd_Id)
        {

            if (targetLanguage == TargetLanguage.CSharp)
                outputFile = destinationFolder + "\\" + xsd_Id + ".cs";
            else if (targetLanguage == TargetLanguage.JAVA)
                outputFile = destinationFolder + "\\" + xsd_Id + ".java";
            else
                outputFile = destinationFolder + "\\" + xsd_Id + ".h";

            outputFileOriginal = outputFile;
            outputFile = outputFile + ".tmp";

            return outputFile;

        }

        //http://support.microsoft.com/kb/320348 
        //This method accepts two strings the represent two files to 
        // compare. A return value of 0 indicates that the contents of the files
        // are the same. A return value of any other value indicates that the 
        // files are not the same.
        public bool FileCompare(string file1, string file2)
        {
            int file1byte;
            int file2byte;
            FileStream fs1;
            FileStream fs2;

            // Determine if the same file was referenced two times.
            if (file1 == file2)
            {
                // Return true to indicate that the files are the same.
                return true;
            }
            if ((File.Exists(file1) == false) || (File.Exists(file2) == false))
                return false;

            // Open the two files.
            fs1 = new FileStream(file1, FileMode.Open);
            fs2 = new FileStream(file2, FileMode.Open);

            // Check the file sizes. If they are not the same, the files 
            // are not the same.
            if (fs1.Length != fs2.Length)
            {
                // Close the file
                fs1.Close();
                fs2.Close();

                // Return false to indicate files are different
                return false;
            }

            // Read and compare a byte from each file until either a
            // non-matching set of bytes is found or until the end of
            // file1 is reached.
            do
            {
                // Read one byte from each file.
                file1byte = fs1.ReadByte();
                file2byte = fs2.ReadByte();
            }
            while ((file1byte == file2byte) && (file1byte != -1));

            // Close the files.
            fs1.Close();
            fs2.Close();

            // Return the success of the comparison. "file1byte" is 
            // equal to "file2byte" at this point only if the files are 
            // the same.
            return ((file1byte - file2byte) == 0);
        }


        public void CreateAndReplaceIfRequired(string outputFileOriginal, string outputFile, TargetLanguage targetLanguage)
        {
            if (File.Exists(outputFileOriginal))
            {

                FileAttributes fa = File.GetAttributes(outputFileOriginal);
                fa = FileAttributes.Archive;
                File.SetAttributes(outputFileOriginal, fa);

                if (false == FileCompare(outputFileOriginal, outputFile))
                {
                    StreamWriter swo = new StreamWriter(outputFileOriginal);
                    swo.Close();
                    File.Delete(outputFileOriginal);
                    File.Copy(outputFile, outputFileOriginal);
                }
            }
            else
            {
                File.Copy(outputFile, outputFileOriginal);
            }

            File.Delete(outputFile);
            outputFile = outputFileOriginal;

        }
    }
}
