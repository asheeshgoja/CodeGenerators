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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Xml;
using System.Xml.Schema;
using System.Xml.Serialization;
using System.Xml.XPath;

namespace XXsd2Code
{
    public enum TargetLanguage
    {
        CPP,
        CPP_CLI,
        CSharp,
        JAVA
    }

    public class ClassElement
    {
        public string Name;
        public XmlTypeCode Type;
        public String CustomType;
        public bool IsCollection = false;
        public bool IsEnum = false;
        public String ExternalNamespace = String.Empty;
        public String Comment = String.Empty;
        public String Namespace = String.Empty;

        public ClassElement(string name, XmlTypeCode type)
        {
            Name = name;
            Type = type;
        }

        public ClassElement(string name, String customType)
        {
            Name = name;
            CustomType = customType;
            Type = XmlTypeCode.Element;
        }
    }

    public class EnumElement
    {
        public string NameSpace;
        public string Name;
        public string Value;
        public bool ValueExceedsIntRange;
        public EnumElement(string n, string v)
        {
            Name = n;
            Value = v;
        }
    }

    public class CodeGenDom
    {
        List<String> _classNamesNoNestedTypes = new List<string>();
        List<String> _classNames = new List<string>();
        List<String> _includeFiles = new List<string>();
        List<String> _includeFilesToSkip = new List<string>();
        List<String> _externalNamespaces = new List<string>();
        List<XmlSchema> _externalSchemas = new List<XmlSchema>();
        Dictionary<string, List<ClassElement>> _externalClassesToGenerateMap = new Dictionary<string, List<ClassElement>>();
        Dictionary<string, string> _externalClassesnNamespaces = new Dictionary<string, string>();
        Dictionary<string, string> _externalEnumsnNamespaces = new Dictionary<string, string>();
        Dictionary<string, List<EnumElement>> _externalEnumsToGenerateMap = new Dictionary<string, List<EnumElement>>();
        Dictionary<int, string> _xsdnNamespaces = new Dictionary<int, string>();

        public static bool MixedMode = false;
        TargetLanguage _targetLanguage;
        String _fileUnderProcessing;
        string _destinationFolder;
        string _outerClassName;

        LanguageWriterBase _codeGenerator;
        FileOperations _fileOperationsHelper = new FileOperations();

        public CodeGenDom(string destinationFolder)
        {
            _destinationFolder = destinationFolder;
        }

        string GetNamespaceFromXsd(XmlSchema schema)
        {
            string nSpace = string.Empty;
            foreach (XmlQualifiedName qualifiedName in schema.Namespaces.ToArray())
            {
                if ("Namespace" == qualifiedName.Name)
                {
                    nSpace = qualifiedName.Namespace;

                    switch (_targetLanguage)
                    {
                        case TargetLanguage.CPP:
                        case TargetLanguage.CPP_CLI:
                            nSpace = nSpace.Replace(".", "::");
                            break;
                        case TargetLanguage.CSharp:
                        case TargetLanguage.JAVA: 
                            break;
                    }

                    if (_xsdnNamespaces.ContainsKey(schema.GetHashCode()) == false)
                        _xsdnNamespaces.Add(schema.GetHashCode(), nSpace);

                    break;
                }
            }
            return nSpace;
        }

        public string GenerateCode(string xsdSchemaFileName, string outputFile, TargetLanguage targetLanguage)
        {
            string outputFileOriginal = string.Empty;
            try
            {
                _targetLanguage = targetLanguage;

                _fileUnderProcessing = xsdSchemaFileName;


                Dictionary<string, List<EnumElement>> enumsToGenerateMap = new Dictionary<string, List<EnumElement>>();
                Dictionary<string, List<ClassElement>> classesToGenerateMap = new Dictionary<string, List<ClassElement>>();

                XmlSchema xsd;

                String orgDir = Environment.CurrentDirectory;
                FileInfo xsdFileInfo = new FileInfo(xsdSchemaFileName);
                Environment.CurrentDirectory = xsdFileInfo.Directory.ToString();

                using (StreamReader fs = new StreamReader(xsdFileInfo.Name))
                {
                    xsd = XmlSchema.Read(fs, null);
                    xsd.Compile(null);
                }

                Environment.CurrentDirectory = orgDir;

                if (xsd.Id == null)
                    xsd.Id = xsdFileInfo.Name.Split('.')[0];

                _outerClassName = xsd.Id;

                DetectAndExtractNestedNamespaces(xsd, xsdSchemaFileName, orgDir, targetLanguage);

                String namespaceName = GetNamespaceFromXsd(xsd);

                if (String.Empty == namespaceName)
                {
                    if (xsd.Id != null && xsd.Id != String.Empty)
                    {
                        namespaceName = xsd.Id;
                    }
                    else
                    {
                        namespaceName = new FileInfo(xsdSchemaFileName).Name.Split(new char[] { '.' })[0];
                    }
                }

                foreach (XmlSchema s in _externalSchemas)
                {
                    string nSpace = GetNamespaceFromXsd(s);

                    foreach (XmlSchemaObject element in s.Items)
                    {
                        IterateOverSchemaSequence(element, _externalClassesToGenerateMap, _externalEnumsToGenerateMap, nSpace);
                    }
                    foreach (KeyValuePair<String, List<ClassElement>> c in _externalClassesToGenerateMap)
                    {
                        if (_externalClassesnNamespaces.ContainsKey(c.Key) == false)
                        {
                            //if (targetLanguage == TargetLanguage.JAVA)
                            //    _externalClassesnNamespaces.Add(c.Key, String.Format("{0}.{1}", nSpace, s.Id));
                            // else
                            _externalClassesnNamespaces.Add(c.Key, nSpace);
                        }
                    }
                    foreach (KeyValuePair<String, List<EnumElement>> c in _externalEnumsToGenerateMap)
                    {
                        if (_externalEnumsnNamespaces.ContainsKey(c.Key) == false)
                        {
                            if (targetLanguage == TargetLanguage.JAVA)
                                _externalEnumsnNamespaces.Add(c.Key, String.Format("{0}.{1}", nSpace, s.Id));
                            else
                                _externalEnumsnNamespaces.Add(c.Key, nSpace);
                        }
                    }
                }

                foreach (XmlSchemaObject element in xsd.Items)
                {
                    IterateOverSchemaSequence(element, classesToGenerateMap, enumsToGenerateMap, namespaceName);
                }


                switch (_targetLanguage)
                {
                    case TargetLanguage.CPP:
                        _codeGenerator = new LanguageWriters.CppWriter(_classNamesNoNestedTypes, _classNames, _includeFiles, _includeFilesToSkip, _externalNamespaces, _xsdnNamespaces, _destinationFolder, _outerClassName, _externalClassesToGenerateMap, _externalClassesnNamespaces, _externalEnumsnNamespaces, _externalEnumsToGenerateMap, _targetLanguage);
                        break;
                    case TargetLanguage.CPP_CLI:
                        _codeGenerator = new LanguageWriters.CppCliWriter(_classNamesNoNestedTypes, _classNames, _includeFiles, _includeFilesToSkip, _externalNamespaces, _xsdnNamespaces, _destinationFolder, _outerClassName, _externalClassesToGenerateMap, _externalClassesnNamespaces, _externalEnumsnNamespaces, _externalEnumsToGenerateMap, _targetLanguage);
                        break;
                    case TargetLanguage.CSharp:
                        _codeGenerator = new LanguageWriters.CSharpWriter(_classNamesNoNestedTypes, _classNames, _includeFiles, _includeFilesToSkip, _externalNamespaces, _xsdnNamespaces, _destinationFolder, _outerClassName, _externalClassesToGenerateMap, _externalClassesnNamespaces, _externalEnumsnNamespaces, _externalEnumsToGenerateMap, _targetLanguage);
                        break;
                    case TargetLanguage.JAVA:
                        _codeGenerator = new LanguageWriters.JavaWriter(_classNamesNoNestedTypes, _classNames, _includeFiles, _includeFilesToSkip, _externalNamespaces, _xsdnNamespaces, _destinationFolder, _outerClassName, _externalClassesToGenerateMap, _externalClassesnNamespaces, _externalEnumsnNamespaces, _externalEnumsToGenerateMap, _targetLanguage);
                        break;
                    default:
                        break;
                }

                if (_targetLanguage == TargetLanguage.JAVA)
                {
                    String destinationFolder = _destinationFolder;

                    String nSpace = GetNamespaceFromXsd(xsd);
                    String[] folders = nSpace.Split('.');
                    foreach (string folder in folders)
                    {
                        destinationFolder += Path.DirectorySeparatorChar + folder;
                        Directory.CreateDirectory(destinationFolder);
                    }

                    foreach (KeyValuePair<String, List<EnumElement>> item in enumsToGenerateMap)
                    {
                        outputFile = _fileOperationsHelper.CreateFileName(outputFile, destinationFolder, out outputFileOriginal, _targetLanguage, item.Key);
                        StreamWriter sw = new StreamWriter(outputFile);
                        Dictionary<String, List<EnumElement>> tmpDicEnumToCreate = new Dictionary<string, List<EnumElement>>();
                        tmpDicEnumToCreate.Add(item.Key, item.Value);
                        _codeGenerator.GenerateEnums(sw, tmpDicEnumToCreate);
                        sw.Close();
                        _fileOperationsHelper.CreateAndReplaceIfRequired(outputFileOriginal, outputFile, _targetLanguage);
                    }

                    foreach (KeyValuePair<String, List<ClassElement>> item in classesToGenerateMap)
                    {
                        outputFile = _fileOperationsHelper.CreateFileName(outputFile, destinationFolder, out outputFileOriginal, _targetLanguage, item.Key);
                        StreamWriter sw = new StreamWriter(outputFile);
                        Dictionary<String, List<ClassElement>> tmpDicClassToCreate = new Dictionary<string, List<ClassElement>>();
                        tmpDicClassToCreate.Add(item.Key, item.Value);
                        _codeGenerator.Write(sw, namespaceName, enumsToGenerateMap, tmpDicClassToCreate);
                        _fileOperationsHelper.CreateAndReplaceIfRequired(outputFileOriginal, outputFile, _targetLanguage);
                        sw.Close();
                    }

                }
                else
                {
                    outputFile = _fileOperationsHelper.CreateFileName(outputFile, _destinationFolder, out outputFileOriginal, _targetLanguage, xsd.Id);
                    StreamWriter sw = new StreamWriter(outputFile);
                    _codeGenerator.Write(sw, namespaceName, enumsToGenerateMap, classesToGenerateMap);
                    _fileOperationsHelper.CreateAndReplaceIfRequired(outputFileOriginal, outputFile, _targetLanguage);
                    sw.Close();
                }

            }
            catch (Exception x)
            {
                Console.WriteLine("File Name = '{0}' , Message = '{1}'", _fileUnderProcessing, x.ToString());
            }
            finally
            {
            }

            return outputFileOriginal;
        }

        

        void DetectAndExtractNestedNamespaces(XmlSchema xsd, string xsdSchemaFileName, String orgDir, TargetLanguage targetLanguage)
        {
            try
            {
                foreach (XmlSchemaInclude inc in xsd.Includes)
                {
                    string ns = "";
                    XmlSchema xsdin = null;
                    FileInfo f = new FileInfo(xsdSchemaFileName);

                    string includeSchema = inc.SchemaLocation;
                    if (false == File.Exists(includeSchema))
                    {
                        includeSchema = f.Directory + "\\" + inc.SchemaLocation;
                    }

                    if (false == File.Exists(includeSchema))
                    {
                        includeSchema = orgDir + "\\" + new FileInfo(inc.SchemaLocation).Name;
                    }

                    using (StreamReader fs = new StreamReader(includeSchema))
                    {
                        FileInfo schemaLocation = new FileInfo(includeSchema);

                        CodeGenDom gen = new CodeGenDom(_destinationFolder);
                        FileInfo src = new FileInfo(xsdSchemaFileName);


                        string headerFileName = string.Empty;

                        if (_targetLanguage == TargetLanguage.CSharp)
                            headerFileName = gen.GenerateCode(includeSchema, _destinationFolder + "\\" + schemaLocation.Name.Replace(".xsd", ".cs"), targetLanguage);
                        else if (_targetLanguage == TargetLanguage.JAVA)
                            headerFileName = gen.GenerateCode(includeSchema, _destinationFolder + "\\" + schemaLocation.Name.Replace(".xsd", ".java"), targetLanguage);
                        else
                            headerFileName = gen.GenerateCode(includeSchema, _destinationFolder + "\\" + schemaLocation.Name.Replace(".xsd", ".h"), targetLanguage);

                        xsdin = XmlSchema.Read(fs, null);
                        DetectAndExtractNestedNamespaces(xsdin, xsdSchemaFileName, schemaLocation.Directory.FullName, targetLanguage);

                        if (xsdin.Id == null)
                            xsdin.Id = schemaLocation.Name.Split('.')[0];

                        _externalSchemas.Add(xsdin);

                        string currDir = Environment.CurrentDirectory;

                        try
                        {
                            Environment.CurrentDirectory = _destinationFolder;
                            string tempCurDir = new FileInfo(includeSchema).DirectoryName;
                            if (!Directory.Exists(tempCurDir))
                            {
                                tempCurDir = orgDir;
                            }

                            Environment.CurrentDirectory = tempCurDir;
                            xsdin.Compile(null);
                        }
                        catch (Exception x)
                        {
                            string s = x.Message;
                        }
                        finally
                        {
                            Environment.CurrentDirectory = currDir;
                        }

                        if (headerFileName != string.Empty)
                            _includeFiles.Add(headerFileName);

                        // if ((MixedMode == true) && inc.Id == "SuppressManagedCodeGeneration")
                        //    _includeFilesToSkip.Add(headerFileName);

                        if ((MixedMode == true) && (inc.Annotation != null))
                        {
                            if (inc.Annotation.Items.Count == 1)
                            {
                                XmlSchemaDocumentation ann = inc.Annotation.Items[0] as XmlSchemaDocumentation;
                                if (ann != null)
                                {
                                    if (ann.Markup[0].InnerText == "SuppressManagedCodeGeneration")
                                    {
                                        _includeFilesToSkip.Add(headerFileName);
                                    }
                                }
                            }
                        }

                        string nSpace = GetNamespaceFromXsd(xsdin);
                        if (nSpace != string.Empty)
                        {
                            if (_externalNamespaces.Contains(nSpace) == false)
                                _externalNamespaces.Add(nSpace);
                        }
                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("File Name = '{0}' , Message = '{1}'", _fileUnderProcessing, x.ToString());
            }
        }

        void GetComplexTypeAttributes(XmlSchemaComplexType complexType, Dictionary<String, List<ClassElement>> classesToGenerateMap, Dictionary<String, List<EnumElement>> enumsToGenerateMap, string namespaceName)
        {
            try
            {
                if (false == classesToGenerateMap.ContainsKey(complexType.Name))
                {
                    classesToGenerateMap.Add(complexType.Name, new List<ClassElement>());
                    _classNames.Add(complexType.Name);
                }

                if (complexType.AttributeUses.Count > 0)
                {
                    IDictionaryEnumerator enumerator =
                        complexType.AttributeUses.GetEnumerator();

                    while (enumerator.MoveNext())
                    {
                        XmlSchemaAttribute attribute = (XmlSchemaAttribute)enumerator.Value;

                        ClassElement pe = new ClassElement(attribute.Name, attribute.AttributeSchemaType.TypeCode);
                        pe.Namespace = GetNamespaceFromXsd(complexType.Parent as XmlSchema); ;



                        XmlSchemaSimpleTypeRestriction restriction = (XmlSchemaSimpleTypeRestriction)attribute.AttributeSchemaType.Content;

                        if ((attribute.Annotation != null) && (attribute.Annotation.Items.Count > 0))
                        {
                            XmlSchemaDocumentation doc = (XmlSchemaDocumentation)attribute.Annotation.Items[0];
                            if (null != doc)
                                pe.Comment = "//" + doc.Markup[0].Value;
                        }

                        ExtractEnumDefinition(attribute.AttributeSchemaType.Name, restriction, enumsToGenerateMap, ref pe, namespaceName);

                        classesToGenerateMap[complexType.Name].Add(pe);

                    }
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("File Name = '{0}' , Message = '{1}'", _fileUnderProcessing, x.ToString());
            }
        }

        void IterateOverSchemaSequence(XmlSchemaObject schemaObject, Dictionary<String, List<ClassElement>> classesToGenerateMap, Dictionary<String, List<EnumElement>> enumsToGenerateMap, string namespaceName)
        {

            try
            {
                XmlSchemaElement element = schemaObject as XmlSchemaElement;
                if (element != null) return;

                XmlSchemaComplexType complexType = schemaObject as XmlSchemaComplexType;
                if (complexType == null)
                    return;

                XmlSchemaSequence sequence = complexType.ContentTypeParticle as XmlSchemaSequence;
                if (null == sequence)
                {
                    GetComplexTypeAttributes(complexType, classesToGenerateMap, enumsToGenerateMap, namespaceName);
                    _classNamesNoNestedTypes.Add(complexType.Name);
                    return;
                }

                foreach (XmlSchemaElement childElement in sequence.Items)
                {
                    if (false == classesToGenerateMap.ContainsKey(complexType.Name))
                    {
                        _classNames.Add(complexType.Name);
                        classesToGenerateMap.Add(complexType.Name, new List<ClassElement>());
                        GetComplexTypeAttributes(complexType, classesToGenerateMap, enumsToGenerateMap, namespaceName);
                    }

                    ClassElement proxyElement = null;
                    if (null != childElement.ElementSchemaType.Name)//an element of custom complex types
                    {
                        if (false == classesToGenerateMap.ContainsKey(childElement.ElementSchemaType.Name))
                            IterateOverSchemaSequence(childElement, classesToGenerateMap, enumsToGenerateMap, namespaceName);

                        string customFQType = childElement.ElementSchemaType.Name;
                        if (_externalClassesnNamespaces.ContainsKey(customFQType))
                            AppendNameSpace(ref customFQType, _externalClassesnNamespaces[customFQType]);
                        proxyElement = new ClassElement(childElement.Name, customFQType);
                    }
                    else
                    {
                        proxyElement = new ClassElement(childElement.Name, childElement.ElementSchemaType.TypeCode);
                    }

                    if (null != childElement.MaxOccursString)//is collection
                        proxyElement.IsCollection = true;

                    if ((childElement.Annotation != null) && (childElement.Annotation.Items.Count > 0))
                    {
                        XmlSchemaDocumentation doc = (XmlSchemaDocumentation)childElement.Annotation.Items[0];
                        if (null != doc)
                            proxyElement.Comment = "//" + doc.Markup[0].Value;
                    }

                    proxyElement.Namespace = namespaceName;
                    classesToGenerateMap[complexType.Name].Add(proxyElement);
                }
            }
            catch (Exception x)
            {
                Console.WriteLine("File Name = '{0}' , Message = '{1}'", _fileUnderProcessing, x.ToString());
            }
        }

        void ExtractEnumDefinition(string typeName, XmlSchemaSimpleTypeRestriction restriction, Dictionary<String, List<EnumElement>> enumsToGenerateMap, ref ClassElement pe, string nameSpace)
        {
            if (null != restriction)
            {
                foreach (XmlSchemaEnumerationFacet fac in restriction.Facets)
                {
                    XmlSchema xsd = restriction.Parent.Parent as XmlSchema;

                    nameSpace = GetNamespaceFromXsd(xsd);
                    String enName = String.Empty;
                    EnumElement enm = null;
                    String enValue = String.Empty;
                    bool valueExceedsIntRange = false;

                    if (null != fac)
                    {
                        XmlSchemaDocumentation doc = (XmlSchemaDocumentation)fac.Annotation.Items[0];
                        enValue = fac.Value;

                        try
                        {
                            if (long.Parse(enValue.Replace("0x", ""), System.Globalization.NumberStyles.AllowHexSpecifier) > int.MaxValue)
                                valueExceedsIntRange = true;
                        }
                        catch (Exception) { }


                        if (null != doc)
                        {
                            enName = doc.Markup[0].Value;
                            switch (_targetLanguage)
                            {
                                case TargetLanguage.CSharp:
                                case TargetLanguage.JAVA:
                                    enName = typeName + "." + enName;
                                    break;
                                case TargetLanguage.CPP_CLI:
                                    enName = typeName + "::" + enName;
                                    break;
                            }
                        }

                        enm = new EnumElement(enName, enValue);

                        //AppendNameSpace(ref typeName, nameSpace);
                        if (false == enumsToGenerateMap.ContainsKey(typeName))
                        {
                            enumsToGenerateMap.Add(typeName, new List<EnumElement>());
                        }

                        pe.IsEnum = true;
                        pe.CustomType = typeName;
                        pe.Type = XmlTypeCode.Element;


                        enm.ValueExceedsIntRange = valueExceedsIntRange;

                        bool enumMemberExists = false;
                        foreach (EnumElement var in enumsToGenerateMap[typeName])
                        {
                            if (var.Name == enName)
                            {
                                enumMemberExists = true;
                                break;
                            }
                        }

                        //if (_targetLanguage == TargetLanguage.JAVA)
                       //     enm.NameSpace = nameSpace + "." + xsd.Id;
                       // else
                        enm.NameSpace = nameSpace;

                        if (false == enumMemberExists)
                            enumsToGenerateMap[typeName].Add(enm);
                    }
                }
            }
        }

        void AppendNameSpace(ref string name, string nameSpace)
        {
            if ((name.Contains(nameSpace)) ||
                 (name.Contains(".")) ||
                 (name.Contains("::")))
                return;

            if (_targetLanguage == TargetLanguage.CSharp || _targetLanguage == TargetLanguage.JAVA)
                name = nameSpace + "." + name;
            else
                name = nameSpace + "::" + name;
        }
    }
}
