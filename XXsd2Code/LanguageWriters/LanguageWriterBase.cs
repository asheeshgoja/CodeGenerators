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
using System.Collections;
using System.Text;
using System.IO;
using System.Xml.Schema;

using System.CodeDom;
using System.CodeDom.Compiler;
using System.Reflection;
using System.Diagnostics;
using Microsoft.CSharp;

namespace XXsd2Code
{

    public abstract class LanguageWriterBase
    {

        public static bool XmlIgnoreObjectTypes = false;
        public static bool CrossPlatformSerializationSupport = false;


        protected List<String> _classNamesNoNestedTypes;
        protected List<String> _classNames;
        protected List<String> _includeFiles;
        protected List<String> _includeFilesToSkip;
        protected List<String> _externalNamespaces;
        protected Dictionary<int, string> _xsdnNamespaces;
        protected string _destinationFolder = string.Empty;
        protected string _outerClassName = string.Empty;
        protected Dictionary<string, List<ClassElement>> _externalClassesToGenerateMap;
        protected Dictionary<string, string> _externalClassesnNamespaces;
        protected Dictionary<string, string> _externalEnumsnNamespaces;
        protected Dictionary<string, List<EnumElement>> _externalEnumsToGenerateMap;

        protected TargetLanguage _targetLanguage;
        protected string _fileUnderProcessing = String.Empty;

        protected int IndentLevel = 0;
  

        public abstract void Write(StreamWriter sw,
                                        string namespaceName, Dictionary<string,
                                        List<EnumElement>> enumsToGenerateMap,
                                        Dictionary<string, List<ClassElement>> classesToGenerateMap);

        protected abstract void WriteClass(StreamWriter sw, string className, List<ClassElement> classMetadata,
                    Dictionary<String, List<ClassElement>> classesToGenerateMap,
                    Dictionary<String, List<EnumElement>> enumsToGenerateMap);


        protected bool IsExternalType(string name)
        {
            return _externalClassesnNamespaces.ContainsKey(name);
        }

        protected bool IsExternalEnume(string name)
        {
            return _externalEnumsnNamespaces.ContainsKey(name);
        }
       
        protected void GenerateNamespaceBeginBlock(string[] namespaces, StreamWriter sw)
        {
            foreach (string ns in namespaces)
            {
                sw.WriteLine("{0}namespace {1}", GetTab(), ns);
                sw.WriteLine("{0}{1}", GetTab(), "{");
                IndentLevel++;
            }
        }

        protected void GenerateOuterClassBeginBlock(StreamWriter sw)
        {
            sw.WriteLine("{0}public class {1}", GetTab(), _outerClassName);
            sw.WriteLine("{0}{1}", GetTab(), "{");
            IndentLevel++;       
        }

        protected string GetTab()
        {
            string tabString = "";
            for (int i = 0; i < IndentLevel; i++)
            {
                tabString += "\t";
            }
            return tabString;
        }

        protected void GenerateNamespaceEndBlock(string[] namespaces, StreamWriter sw)
        {
            int length = namespaces.Length;
            foreach (string ns in namespaces)
            {
                IndentLevel--;
                sw.WriteLine("{0}{1}", GetTab(), "}");
            }
        }

        protected void GenerateOuterClassEndBlock(StreamWriter sw)
        {
            IndentLevel--;
            sw.WriteLine("{0}{1}", GetTab(), "}");

        }

        protected bool EnumNeedToExendLong(List<EnumElement> enumDef)
        {
            foreach (EnumElement  e in  enumDef)
            {
                if (e.ValueExceedsIntRange)
                    return true;
            }

            return false;
        }

        public void GenerateEnums(StreamWriter sw, Dictionary<String, List<EnumElement>> enumsToGenerateMap)
        {
            try
            {
                foreach (KeyValuePair<string, List<EnumElement>> kvp in enumsToGenerateMap)
                {
                    if (_targetLanguage == TargetLanguage.JAVA)
                    {
                        sw.WriteLine("//Auto generated code");
                        FileVersionInfo fv = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
                        sw.WriteLine("//Code generated by XXsd2Code<http://xxsd2code.sourceforge.net/> {0} version {1}", fv.InternalName, fv.FileVersion, System.DateTime.Now.ToString());
                        sw.WriteLine("//For any comments/suggestions contact code generator author at asheesh.goja@gmail.com");
                        sw.WriteLine("//Auto generated code");
                        sw.WriteLine(" ");
                        sw.WriteLine("package\t" + kvp.Value[0].NameSpace + ";");
                        //sw.WriteLine("import javax.xml.bind.annotation.XmlEnumValue;");
                    }
                  
                    if(IsExternalEnume( kvp.Key))
                        continue ;

                    string enumName = "";
                    enumName = String.Format("{0}", kvp.Key);

                    sw.WriteLine(" ");
                    sw.WriteLine(GetTab() + "//enumeration\t" + enumName);

                    string baseClass = string.Empty;

                    if (EnumNeedToExendLong(kvp.Value))
                        baseClass = ": long";

                    switch (_targetLanguage)
                    {
                        case TargetLanguage.CPP:
                            sw.WriteLine(GetTab() + "enum\t" + enumName);
                            break;
                        case TargetLanguage.CPP_CLI:
                            sw.WriteLine(GetTab() + "public enum class\t" + enumName + baseClass);
                            break;
                        case TargetLanguage.CSharp:
                        case TargetLanguage.JAVA:
                            sw.WriteLine(GetTab() + "public enum\t" + enumName + baseClass);
                            break;
                        default:
                            break;
                    }

                    sw.WriteLine(GetTab() + "{");

                    String collectionType = String.Empty;
                    int c = 1;
                    IndentLevel++;
                    foreach (EnumElement var in kvp.Value)
                    {
                        String varName = var.Name;
                        switch (_targetLanguage)
                        {
                            case TargetLanguage.CSharp:
                                {
                                    String[] arr = varName.Split(new String[] { "." }, StringSplitOptions.None);
                                    varName = arr[1];
                                    Int64 varValue = var.Value.IndexOf("x", StringComparison.OrdinalIgnoreCase) > 0 ?
                                            Convert.ToInt64(var.Value.Trim(), 16) :
                                            Convert.ToInt64(var.Value.Trim(), 10);
                                    sw.Write(GetTab());
                                    sw.Write("[System.Xml.Serialization.XmlEnum(Name = \"{2}\")] {0} = {1}",
                                        varName, var.Value, varValue);
                                }
                                break;
                            case TargetLanguage.JAVA:
                                {
                                    String[] arr = varName.Split(new String[] { "." }, StringSplitOptions.None);
                                    varName = arr[1];
                                    Int64 varValue = var.Value.IndexOf("x", StringComparison.OrdinalIgnoreCase) > 0 ?
                                            Convert.ToInt64(var.Value.Trim(), 16) :
                                            Convert.ToInt64(var.Value.Trim(), 10);
                                    sw.Write(GetTab());
                                    //sw.Write("@XmlEnumValue(\"{1}\") {0}",varName, var.Value);
                                    sw.Write("{0}(\"{1}\")",varName, var.Value);
                                }
                                break;
                            case TargetLanguage.CPP_CLI:
                                {
                                    String[] arr = varName.Split(new String[] { "::" }, StringSplitOptions.None);
                                    varName = arr[1];
                                    Int64 varValue = var.Value.IndexOf("x", StringComparison.OrdinalIgnoreCase) > 0 ?
                                            Convert.ToInt64(var.Value.Trim(), 16) :
                                            Convert.ToInt64(var.Value.Trim(), 10);
                                    sw.Write(GetTab());
                                    sw.Write("[System::Xml::Serialization::XmlEnum(Name = \"{2}\")] {0} = {1}",varName, var.Value, varValue);

                                }
                                break;
                            case TargetLanguage.CPP:
                                {
                                    sw.Write(GetTab());
                                    sw.Write("{0} = {1}", varName, var.Value);
                                }
                                break;
                        }

                        sw.WriteLine((kvp.Value.Count > c++) ? "," : String.Empty);
                    }

                    IndentLevel--;
                    sw.WriteLine("			");

                    if (_targetLanguage == TargetLanguage.CSharp)
                    {
                        sw.WriteLine(GetTab() + "}");
                    }
                    else if (_targetLanguage == TargetLanguage.JAVA)
                    {
                        sw.Write("\t;");

                        //final String value
                        sw.WriteLine("");
                        sw.WriteLine("\tprivate final String value;");

                       //private ctor
                        sw.WriteLine("");
                        sw.WriteLine("\t//private constructor");
                        sw.WriteLine("\t{0}(String value) ", enumName);
                        sw.WriteLine("\t{	   this.value = value;}");

                        // generate fromValue
                        sw.WriteLine("");
                        sw.WriteLine("\t//fromValue");
                        sw.WriteLine("\tpublic static {0} fromValue(String value) ", enumName);
                        sw.WriteLine("	 {   ");
                        sw.WriteLine("	   if (value != null) ");
                        sw.WriteLine("	   {   ");
                        sw.WriteLine("	     for ({0} v : values()) ", enumName);
                        sw.WriteLine("	     {   ");
                        sw.WriteLine("	       if (v.value.equals(value)) ");
                        sw.WriteLine("	       {   ");
                        sw.WriteLine("	         return v;   ");
                        sw.WriteLine("	       }   ");
                        sw.WriteLine("	     }   ");
                        sw.WriteLine("	   }   ");
                        sw.WriteLine("	   return null;   ");
                        sw.WriteLine("	 } ");


                        // generate toString
                        sw.WriteLine("");
                        sw.WriteLine("\t//toString");
                        sw.WriteLine("\t@Override");
                        sw.WriteLine("\tpublic String toString() {   return value;}   ");

                        sw.WriteLine(GetTab() + "}");
                    }
                    else
                    {
                        sw.WriteLine(GetTab() + "};");
                    }

                }
            }
            catch (Exception x)
            {
                Console.WriteLine("File Name = '{0}' , Message = '{1}'", _fileUnderProcessing, x.ToString());
            }
        }

        protected List<String> GetClassDependencies(string name, Dictionary<String, List<ClassElement>> classesToGenerateMap)
        {
            List<String> rL = new List<string>();

            foreach (ClassElement e in classesToGenerateMap[name])
            {
                if (e.CustomType != null)
                {
                    if (e.IsEnum == false)
                    {
                        rL.Add(e.CustomType);
                    }
                }
            }
            return rL;

        }

        protected string CreateFormattedVectorDeclaration (ClassElement var)
        {
            if (var.CustomType == null) return String.Format("{0}_VECTOR", var.Type);
            string[] cParts = var.CustomType.Split("::".ToCharArray());
            string cName = cParts[cParts.Length - 1];
            string collectionTypeDecl = String.Format("{0}_VECTOR", cName);
            return collectionTypeDecl;
        }
  
        protected string GetDefaultsString(ClassElement e, Dictionary<String, List<EnumElement>> enumsToGenerateMap)
        {
            string retString = "";
            XmlTypeCode type = e.Type;
            if (e.IsEnum)
            {
                string nspace = string.Empty;
                retString = enumsToGenerateMap[e.CustomType][0].Name;
                nspace = enumsToGenerateMap[e.CustomType][0].NameSpace;
                
                if (_targetLanguage == TargetLanguage.CSharp || _targetLanguage == TargetLanguage.JAVA)
                    return nspace + "." + retString;                
                else
                    return nspace + "::" + retString;

            }
            switch (type)
            {
                case XmlTypeCode.AnyAtomicType:
                    break;
                case XmlTypeCode.AnyUri:
                    break;
                case XmlTypeCode.Attribute:
                    break;
                case XmlTypeCode.Base64Binary:
                    break;
                case XmlTypeCode.Boolean: retString = "false";
                    break;
                case XmlTypeCode.Byte: retString = "0";
                    break;
                case XmlTypeCode.Comment:
                    break;
                case XmlTypeCode.Date:
                    break;
                case XmlTypeCode.DateTime:
                    break;
                case XmlTypeCode.DayTimeDuration:
                    break;
                case XmlTypeCode.Decimal:
                    switch (_targetLanguage)
                    {
                        case TargetLanguage.CSharp:
                            retString = "default(decimal)"; break;
                        case TargetLanguage.CPP_CLI:
                            retString = "0"; break;
                        default:
                            retString = "0.0"; break;
                    } break;
                case XmlTypeCode.Document:
                    break;
                case XmlTypeCode.Double:
                    switch (_targetLanguage)
                    {
                        case TargetLanguage.CSharp:
                            retString = "default(double)"; break;
                        default:
                            retString = "0.0"; break;
                    } break;
                case XmlTypeCode.Duration:
                    break;
                case XmlTypeCode.Element:
                    break;
                case XmlTypeCode.Entity:
                    break;
                case XmlTypeCode.Float: retString = "0.0";
                    break;
                case XmlTypeCode.GDay:
                    break;
                case XmlTypeCode.GMonth:
                    break;
                case XmlTypeCode.GMonthDay:
                    break;
                case XmlTypeCode.GYear:
                    break;
                case XmlTypeCode.GYearMonth:
                    break;
                case XmlTypeCode.HexBinary: retString = "0";
                    break;
                case XmlTypeCode.Id:
                    break;
                case XmlTypeCode.Idref:
                    break;
                case XmlTypeCode.Int: retString = "0";
                    break;
                case XmlTypeCode.Integer: retString = "0";
                    break;
                case XmlTypeCode.Item:
                    break;
                case XmlTypeCode.Language:
                    break;
                case XmlTypeCode.Long: retString = "0";
                    break;
                case XmlTypeCode.NCName:
                    break;
                case XmlTypeCode.Name:
                    break;
                case XmlTypeCode.Namespace:
                    break;
                case XmlTypeCode.NegativeInteger: retString = "0";
                    break;
                case XmlTypeCode.NmToken:
                    break;
                case XmlTypeCode.Node:
                    break;
                case XmlTypeCode.NonNegativeInteger: retString = "0";
                    break;
                case XmlTypeCode.NonPositiveInteger: retString = "0";
                    break;
                case XmlTypeCode.None:
                    break;
                case XmlTypeCode.NormalizedString:
                    break;
                case XmlTypeCode.Notation:
                    break;
                case XmlTypeCode.PositiveInteger: retString = "0";
                    break;
                case XmlTypeCode.ProcessingInstruction:
                    break;
                case XmlTypeCode.QName:
                    break;
                case XmlTypeCode.Short: retString = "0";
                    switch (_targetLanguage)
                    {
                        case TargetLanguage.CSharp:
                            retString = "default(Int16)"; break;
                        default:
                            retString = "0"; break;
                    } break;
                case XmlTypeCode.String: retString = "";
                    break;
                case XmlTypeCode.Text: retString = "";
                    break;
                case XmlTypeCode.Time:
                    break;
                case XmlTypeCode.Token:
                    break;
                case XmlTypeCode.UnsignedByte: retString = "0";
                    break;
                case XmlTypeCode.UnsignedInt: retString = "0";
                    break;
                case XmlTypeCode.UnsignedLong: retString = "0";
                    break;
                case XmlTypeCode.UnsignedShort: retString = "0";
                    break;
                case XmlTypeCode.UntypedAtomic:
                    break;
                case XmlTypeCode.YearMonthDuration:
                    break;
                default:
                    break;
            }

            return retString;
        }

        protected void GenerateClasses(StreamWriter sw, Dictionary<String, List<ClassElement>> classesToGenerateMap, Dictionary<String, List<EnumElement>> enumsToGenerateMap)
        {
            try
            {
                _classNames.Reverse();

                while (classesToGenerateMap.Count > 0)
                {
                    foreach (string classEntry in _classNames)
                    {
                        if (classesToGenerateMap.ContainsKey(classEntry))
                        {
                            List<ClassElement> val = classesToGenerateMap[classEntry];
                            WriteClass(sw, classEntry, val, classesToGenerateMap, enumsToGenerateMap);
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("File Name = '{0}' , Message = '{1}'", _fileUnderProcessing, x.ToString());
            }
        }

        protected string XSDToCppType(ClassElement element)
        {
            string retVal = string.Empty;

            switch (element.Type)
            {
                case XmlTypeCode.Float:
                    retVal = "double"; break;
                case XmlTypeCode.Short:
                    switch (_targetLanguage)
                    {
                        case TargetLanguage.CSharp:
                            retVal = "Int16"; break;
                        default:
                            retVal = "int"; break;
                    } break;
                case XmlTypeCode.Byte:
                    retVal = "byte"; break;
                case XmlTypeCode.String:
                    switch (_targetLanguage)
                    {
                        case TargetLanguage.CPP:
                            retVal = "tstring"; break;
                        case TargetLanguage.CPP_CLI:
                            retVal = "String"; break;
                        case TargetLanguage.CSharp:
                            retVal = "string"; break;                    
                        default:
                            retVal = "tstring"; break;
                    }break;
                case XmlTypeCode.Long:
                    retVal = "long"; break;
                case XmlTypeCode.Double:
                    retVal = "double"; break;
                case XmlTypeCode.Decimal:
                    switch (_targetLanguage)
                    {
                        case TargetLanguage.CPP:
                            retVal = "double"; break;
                        case TargetLanguage.CPP_CLI:
                            retVal = "System::Decimal"; break;
                        case TargetLanguage.CSharp:
                            retVal = "System.Decimal"; break;                    
                        default:
                            retVal = "double"; break;
                    }break; 
                case XmlTypeCode.Int:
                case XmlTypeCode.Integer:
                case XmlTypeCode.PositiveInteger:
                case XmlTypeCode.NegativeInteger:
                    retVal = "int"; break;
                case XmlTypeCode.Boolean:
                    retVal = "bool"; break;                               
                case XmlTypeCode.Element:
                    retVal = element.CustomType; break;
                case XmlTypeCode.DateTime:
                case XmlTypeCode.Date:
                    retVal = _targetLanguage == TargetLanguage.CPP ? "void" : "System" + NamespaceToken + "DateTime"; break;
                default:
                    retVal = _targetLanguage == TargetLanguage.CPP ? "void" : "Object"; break;
            }


            return AppendCaretIfRequired(element, retVal);
        }

        protected String NamespaceToken
        {
            get
            {
                if (_targetLanguage == TargetLanguage.CPP_CLI)
                    return "::";
                else
                    return ".";
            }
        }

        protected String AppendCaretIfRequired(ClassElement element, String str)
        {
            if (_targetLanguage == TargetLanguage.CPP_CLI)
            {
                String caret = "";
                switch (element.Type)
                {
                    case XmlTypeCode.AnyAtomicType:
                        break;
                    case XmlTypeCode.AnyUri:
                        break;
                    case XmlTypeCode.Attribute:
                        break;
                    case XmlTypeCode.Base64Binary:
                        break;
                    case XmlTypeCode.Boolean:
                        break;
                    case XmlTypeCode.Byte:
                        break;
                    case XmlTypeCode.Comment:
                        break;
                    case XmlTypeCode.Date:
                        caret = "^";
                        break;
                    case XmlTypeCode.DateTime:
                        caret = "^";
                        break;
                    case XmlTypeCode.DayTimeDuration:
                        break;
                    case XmlTypeCode.Decimal:
                        break;
                    case XmlTypeCode.Document:
                        break;
                    case XmlTypeCode.Double:
                        break;
                    case XmlTypeCode.Duration:
                        break;
                    case XmlTypeCode.Element:
                        break;
                    case XmlTypeCode.Entity:
                        break;
                    case XmlTypeCode.Float:
                        break;
                    case XmlTypeCode.GDay:
                        break;
                    case XmlTypeCode.GMonth:
                        break;
                    case XmlTypeCode.GMonthDay:
                        break;
                    case XmlTypeCode.GYear:
                        break;
                    case XmlTypeCode.GYearMonth:
                        break;
                    case XmlTypeCode.HexBinary:
                        break;
                    case XmlTypeCode.Id:
                        break;
                    case XmlTypeCode.Idref:
                        caret = "^";
                        break;
                    case XmlTypeCode.Int:
                        break;
                    case XmlTypeCode.Integer:
                        break;
                    case XmlTypeCode.Item:
                        break;
                    case XmlTypeCode.Language:
                        break;
                    case XmlTypeCode.Long:
                        break;
                    case XmlTypeCode.NCName:
                        break;
                    case XmlTypeCode.Name:
                        break;
                    case XmlTypeCode.Namespace:
                        break;
                    case XmlTypeCode.NegativeInteger:
                        break;
                    case XmlTypeCode.NmToken:
                        break;
                    case XmlTypeCode.Node:
                        break;
                    case XmlTypeCode.NonNegativeInteger:
                        break;
                    case XmlTypeCode.NonPositiveInteger:
                        break;
                    case XmlTypeCode.None:
                        break;
                    case XmlTypeCode.NormalizedString:
                        break;
                    case XmlTypeCode.Notation:
                        break;
                    case XmlTypeCode.PositiveInteger:
                        break;
                    case XmlTypeCode.ProcessingInstruction:
                        break;
                    case XmlTypeCode.QName:
                        break;
                    case XmlTypeCode.Short:
                        break;
                    case XmlTypeCode.String:
                        caret = "^";
                        break;
                    case XmlTypeCode.Text:
                        break;
                    case XmlTypeCode.Time:
                        break;
                    case XmlTypeCode.Token:
                        break;
                    case XmlTypeCode.UnsignedByte:
                        break;
                    case XmlTypeCode.UnsignedInt:
                        break;
                    case XmlTypeCode.UnsignedLong:
                        break;
                    case XmlTypeCode.UnsignedShort:
                        break;
                    case XmlTypeCode.UntypedAtomic:
                        break;
                    case XmlTypeCode.YearMonthDuration:
                        break;
                    default:
                        break;
                }
                str += caret;
            }
            return str;
        }

        protected string XSDToJavaType(ClassElement element)
        {
            switch (element.Type)
            {
                case XmlTypeCode.Float:
                    return "double";
                case XmlTypeCode.Short:
                    return "int";
                case XmlTypeCode.String:
                    return "String";                                        
                case XmlTypeCode.Long:
                    return "long";
                case XmlTypeCode.Double:
                    return "double";
                case XmlTypeCode.Int:
                    return "int";
                case XmlTypeCode.Boolean:
                     return "boolean";    
                case XmlTypeCode.Element:
                    return element.CustomType;
                case XmlTypeCode.DateTime: 
                    return "Date";
                case XmlTypeCode.Date:
                    return "Date";
                default:
                    return "Object";
            }
        }



    }
}


