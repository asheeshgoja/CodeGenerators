Code generate C++, Java , C#, C++/CLI classes from xsd.

Code generates cross platform serializable classes using XmlSerializer/DataContractJsonSerializer for .net and 
xstream/JettisonMappedXmlDriver for java

Supports nested xsds


Usage

Command line syntax : 	XXsd2Code <SourceDirectory> / <TargetDirectory> /<TargetLanguage>[C++,C++CLI,C#,Java]/<CPSS>[Optional]


Use the following commnand to generate code

C++  :  XXsd2Code.exe CustomerInfo.xsd/.\/C++

Java : XXsd2Code.exe CustomerInfo.xsd/.\/Java

C# : XXsd2Code.exe CustomerInfo.xsd/.\/C#

C++/CLI (managed .net c++) : XXsd2Code.exe CustomerInfo.xsd/.\/C++CLI
