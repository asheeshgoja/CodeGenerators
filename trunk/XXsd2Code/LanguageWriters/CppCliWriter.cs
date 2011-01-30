﻿/*
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
using System.Diagnostics;
using System.Reflection;
using System.Xml.Schema;


namespace XXsd2Code.LanguageWriters
{
    public class CppCliWriter : LanguageWriterBase
    {
        public CppCliWriter(
            List<String> classNamesNoNestedTypes,
            List<String> classNames,
            List<String> includeFiles,
            List<String> includeFilesToSkip,
            List<String> externalNamespaces,
            Dictionary<int, string> xsdnNamespaces,
            string destinationFolder,
            string outerClassName,
            Dictionary<string, List<ClassElement>> externalClassesToGenerateMap,
            Dictionary<string, string> externalClassesnNamespaces,
            Dictionary<string, string> externalEnumsnNamespaces,
            Dictionary<string, List<EnumElement>> externalEnumsToGenerateMap,
            TargetLanguage targetLanguage
        )
        {
            _targetLanguage = targetLanguage;
            _classNamesNoNestedTypes = classNamesNoNestedTypes;
            _classNames = classNames;
            _includeFiles = includeFiles;
            _includeFilesToSkip = includeFilesToSkip;
            _externalNamespaces = externalNamespaces;
            _xsdnNamespaces = xsdnNamespaces;
            _destinationFolder = destinationFolder;
            _outerClassName = outerClassName;
            _externalClassesToGenerateMap = externalClassesToGenerateMap;
            _externalClassesnNamespaces = externalClassesnNamespaces;
            _externalEnumsnNamespaces = externalEnumsnNamespaces;
            _externalEnumsToGenerateMap = externalEnumsToGenerateMap;
        }

        public override void Write(StreamWriter sw, string namespaceName, Dictionary<string,
                                                                List<EnumElement>> enumsToGenerateMap,
                                                                Dictionary<string, List<ClassElement>> classesToGenerateMap)
        {

            IndentLevel = 0;

            sw.WriteLine("//Auto generated code");
            FileVersionInfo fv = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
            sw.WriteLine("//Code generated by XXsd2Code<http://xxsd2code.sourceforge.net/> {0} version {1}", fv.InternalName, fv.FileVersion, System.DateTime.Now.ToString());
            sw.WriteLine("//For any comments/suggestions contact code generator author at asheesh.goja@gmail.com");
            sw.WriteLine("//Auto generated code");
            sw.WriteLine("//Auto generated code");
            sw.WriteLine(" ");
            sw.WriteLine("#pragma once");
            sw.WriteLine("using System::String;");
            sw.WriteLine("using System::ICloneable;");
            sw.WriteLine("using System::Collections::Generic::List;");
            sw.WriteLine("using System::SerializableAttribute;");
            sw.WriteLine(" ");
            sw.WriteLine(" ");

            foreach (string f in _includeFiles)
            {
                if (_includeFilesToSkip.Contains(f))
                    continue;

                FileInfo fi = new FileInfo(f);
                //if(MixedMode)
                //    sw.WriteLine("#include \"{0}CLI{1}\"", fi.Name.Split('.')[0], fi.Extension);
                //else
                sw.WriteLine("#include \"{0}\"", fi.Name);
            }

            sw.WriteLine(" ");
            sw.WriteLine(" ");
            foreach (string s in _externalNamespaces)
            {
                //string nS = s.Replace("Contracts", "DataContract");
                sw.WriteLine("using namespace {0};", s);
            }

            sw.WriteLine(" ");
            sw.WriteLine(" ");

            //String[] namespaceParts = new String[] { namespaceName, "DataContract" };
            //namespaceName = namespaceName.Replace("DataContract", "Contracts");
            String[] namespaceParts = namespaceName.Split(new string[1] { "::" }, StringSplitOptions.None);

            GenerateNamespaceBeginBlock(namespaceParts, sw);
            GenerateEnums(sw, enumsToGenerateMap);
            GenerateClasses(sw, classesToGenerateMap, enumsToGenerateMap);
            GenerateNamespaceEndBlock(namespaceParts, sw);

            sw.WriteLine("");
            sw.WriteLine("");

            sw.Close();

        }

        protected override void WriteClass(StreamWriter sw, string className, List<ClassElement> classMetadata,
                             Dictionary<String, List<ClassElement>> classesToGenerateMap,
                             Dictionary<String, List<EnumElement>> enumsToGenerateMap)
        {

            if (IsExternalType(className))
            {
                classesToGenerateMap.Remove(className);
                return;
            }

            List<string> dep = GetClassDependencies(className, classesToGenerateMap);

            foreach (string s in dep)
            {
                if (s == className) continue;

                if (classesToGenerateMap.ContainsKey(s))
                {
                    WriteClass(sw, s, classesToGenerateMap[s], classesToGenerateMap, enumsToGenerateMap);
                    classesToGenerateMap.Remove(s);
                }
            }
            List<ClassElement> val = classMetadata;

            string contextClassName = "";
            contextClassName = String.Format("{0}", className);

            List<string> vars = new List<string>();

            sw.WriteLine(" ");

            //if (contextClassName.Contains("CaseData"))
            //{
            //    sw.WriteLine(GetTab() + "[XXsd2Code::Remoting::WorkflowDataContract]");
            //}

            sw.WriteLine(GetTab() + "[SerializableAttribute]");
            sw.WriteLine("{0}public ref class\t{1}: public ICloneable", GetTab(), contextClassName);
            sw.WriteLine("{0}{1}", GetTab(), "{");
            sw.WriteLine("{0}public:", GetTab());

            #region Declarations

            IndentLevel++;
            String collectionType = String.Empty;
            List<string> collectionTypeWritten = new List<string>();

            foreach (ClassElement var in val)
            {
                if (var.IsCollection)
                {
                    collectionType = CreateFormattedVectorDeclaration(var); //String.Format("{0}_VECTOR", XSDToCppType(var));
                    if (collectionTypeWritten.Contains(collectionType) == false)
                    {
                        sw.WriteLine("");

                        if (var.CustomType == null)
                            sw.WriteLine(GetTab() + "typedef List<" + XSDToCppType(var) + ">\t\t" + collectionType + ";");
                        else
                            sw.WriteLine(GetTab() + "typedef List<" + XSDToCppType(var) + "^>\t\t" + collectionType + ";");

                        collectionTypeWritten.Add(collectionType);
                    }


                    sw.WriteLine("{0}{1}^{0}{2};{3}", GetTab(), collectionType, var.Name, var.Comment);
                }
                else
                {
                    if ((var.CustomType == null))
                    {

                        String typeName = XSDToCppType(var);

                        if (String.Equals(typeName, "bool", StringComparison.Ordinal))
                        {
                            //.Net XML serializer writes bool as "true" and "false", but native serializer
                            //only handles 1 / 0. Generate a property to override the default XML serialization
                            //behavior such that serialized bool's are 1 / 0
                            sw.WriteLine();
                            sw.Write(GetTab());
                            sw.Write("[System::Xml::Serialization::XmlIgnore] ");
                            sw.WriteLine("{0} {1};{2}", typeName, var.Name, var.Comment);
                            sw.Write(GetTab());
                            //sw.WriteLine("///<summary>For use by XML Serializer only</summary>");
                            sw.Write(GetTab());
                            sw.Write("[System::Xml::Serialization::XmlElement(\"{0}\")] ", var.Name);
                            sw.WriteLine("property int xml_{0} ", var.Name);
                            sw.Write(GetTab());
                            sw.WriteLine("{{ int get() {{ return {0} ? 1 : 0; }} void set(int value) {{ {0} = (value != 0); }} }}", var.Name);
                            sw.WriteLine();
                        }
                        else
                        {
                            sw.WriteLine("{0}{1}{0}{2};{3}", GetTab(), typeName, var.Name, var.Comment);
                        }
                    }
                    else
                    {
                        string nSpace;
                        if (_externalClassesToGenerateMap.ContainsKey(var.CustomType))
                            nSpace = _externalClassesToGenerateMap[var.CustomType][0].Namespace;// + "::DataContract";
                        else if (_externalEnumsToGenerateMap.ContainsKey(var.CustomType))
                        {
                            nSpace = _externalEnumsnNamespaces[var.CustomType] + "::";
                        }
                        else
                            nSpace = "";// var.Namespace;// +"::DataContract";

                        if (var.IsEnum == true)
                            sw.WriteLine("{0}{4}{1}{0}{2};{3}", GetTab(), XSDToCppType(var), var.Name, var.Comment, nSpace);
                        else
                            sw.WriteLine("{0}{4}{1}^{0}{2};{3}", GetTab(), XSDToCppType(var), var.Name, var.Comment, nSpace);
                    }
                }
            }
            #endregion

            #region Default constructor
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "//default constructor");
            String defaultCtor = String.Format("{0}{1}()", GetTab(), contextClassName);
            sw.WriteLine(defaultCtor);
            sw.WriteLine(GetTab() + "{");
            IndentLevel++;
            foreach (ClassElement var in val)
            {

                string defVal = GetDefaultsString(var, enumsToGenerateMap);
                if (defVal != "")
                {
                    String defaultVal = String.Format("{0}{1} = {2} ;", GetTab(), var.Name, defVal);
                    sw.WriteLine(defaultVal);
                }
                if ((var.CustomType != null) && (var.IsEnum == false))
                {
                    if (var.IsCollection == false)
                    {
                        string nSpace;
                        if (_externalClassesToGenerateMap.ContainsKey(var.CustomType))
                            nSpace = _externalClassesToGenerateMap[var.CustomType][0].Namespace;//  + "::DataContract";
                        else if (_externalEnumsToGenerateMap.ContainsKey(var.CustomType))
                        {
                            nSpace = _externalEnumsnNamespaces[var.CustomType] + "::";
                        }
                        else
                            nSpace = "";// var.Namespace;// +"::DataContract";

                        sw.WriteLine("{0}{1} = gcnew {3}{2}() ;", GetTab(), var.Name, XSDToCppType(var), nSpace);
                    }
                    else
                    {
                        collectionType = CreateFormattedVectorDeclaration(var); //String.Format("{0}_VECTOR", XSDToCppType(var));
                        sw.WriteLine("{0}{1} = gcnew {2}() ;", GetTab(), var.Name, collectionType);
                    }
                }

                if (var.Type == XmlTypeCode.String)
                {
                    if (var.IsCollection == false)
                        sw.WriteLine("{0}{1} = String::Empty;", GetTab(), var.Name);
                    else
                    {
                        collectionType = CreateFormattedVectorDeclaration(var); //String.Format("{0}_VECTOR", XSDToCppType(var));
                        sw.WriteLine("{0}{1} = gcnew {2}() ;", GetTab(), var.Name, collectionType);
                    }
                }

            }
            IndentLevel--;
            sw.WriteLine(GetTab() + "}");
            #endregion

            #region Copy constuctor
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "//copy constuctor");

            String copyCtor = String.Format("{0}{1}( {1}%  rhs){2}*this = rhs;{3}", GetTab(), contextClassName, "{", "}");
            sw.WriteLine(copyCtor);
            #endregion

            #region IClonable Override
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "//IClonable Override");
            String clonableOverride = String.Format("{0}virtual\tObject^ Clone()", GetTab(), contextClassName);
            sw.WriteLine(clonableOverride);
            sw.WriteLine(GetTab() + "{");
            IndentLevel++;

            String body = String.Format("{0}{1}^\t instance = gcnew {1}() ;", GetTab(), contextClassName);
            sw.WriteLine(body);

            body = String.Format("{0}*instance = *this ;", GetTab());
            sw.WriteLine(body);

            sw.WriteLine(GetTab() + "return instance;");
            IndentLevel--;
            sw.WriteLine(GetTab() + "}");
            #endregion

            #region = Operator
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "//= operator");
            String equalToOperator = String.Format("{0}{1}% operator = ( {1}%  rhs)", GetTab(), contextClassName);
            sw.WriteLine(equalToOperator);
            sw.WriteLine(GetTab() + "{");
            IndentLevel++;
            foreach (ClassElement var in val)
            {
                //if (var.IsCollection && var.CustomType != null)
                if (var.IsCollection)
                {
                    String equalStatement = String.Format("{0}{2}->AddRange(rhs.{2}) ;", GetTab(), var.CustomType, var.Name);
                    sw.WriteLine(equalStatement);
                }
                else if (var.CustomType != null && var.IsEnum == false)
                {
                    String equalStatement = String.Format("{0}{1} = safe_cast<{2}^>(rhs.{1}->Clone()) ;", GetTab(), var.Name, var.CustomType);
                    sw.WriteLine(equalStatement);
                }
                else
                {
                    String equalStatement = String.Format("{0}{1} = rhs.{1} ;", GetTab(), var.Name);
                    sw.WriteLine(equalStatement);
                }
            }
            sw.WriteLine(GetTab() + "return *this;");
            IndentLevel--;
            sw.WriteLine(GetTab() + "}");
            #endregion

            #region DeepCopy
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "//DeepCopy");
            String copyFrom = String.Format("{0}void\tDeepCopy ( {1}^  from)", GetTab(), contextClassName);
            sw.WriteLine(copyFrom);
            sw.WriteLine(GetTab() + "{");
            IndentLevel++;
            foreach (ClassElement var in val)
            {
                //if (var.IsCollection && var.CustomType != null)
                if (var.IsCollection)
                {
                    String equalStatement = String.Format("{0}{2}->AddRange(from->{2}) ;", GetTab(), var.CustomType, var.Name);
                    sw.WriteLine(equalStatement);
                }
                else if (var.CustomType != null && var.IsEnum == false)
                {
                    String equalStatement = String.Format("{0}{1}->DeepCopy(from->{1}) ;", GetTab(), var.Name);
                    sw.WriteLine(equalStatement);
                }
                else
                {
                    String equalStatement = String.Format("{0}{1} = from->{1} ;", GetTab(), var.Name);
                    sw.WriteLine(equalStatement);
                }
            }
            IndentLevel--;
            sw.WriteLine(GetTab() + "}");
            #endregion

            IndentLevel--;
            sw.WriteLine("			");
            sw.WriteLine(GetTab() + "};");

            classesToGenerateMap.Remove(className);
        }

    }
}