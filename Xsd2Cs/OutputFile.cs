using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Xsd2Cs;

internal sealed class OutputFile
{
    public bool IsExcluded { get; }
    public string Name { get; }

    public List<AOutputType> OutputTypes { get; } = new();

    public OutputFile(string name, bool isExcluded)
    {
        Name = name;
        IsExcluded = isExcluded;
    }

    public static StringBuilder WriteIndent(int indent, StringBuilder sb)
    {
        for (int idx = 0; idx < indent; ++idx)
        {
            sb.Append("    ");
        }
        return sb;
    }

    public static XmlSchemaType? GetSchemaType(IEnumerable<List<XmlSchemaType>> schemaTypes, string name)
    {
        foreach (List<XmlSchemaType> xmlSchemaTypes in schemaTypes)
        {
            foreach (XmlSchemaType xmlSchemaType in xmlSchemaTypes)
            {
                if (string.Compare(name, xmlSchemaType.QualifiedName.Name, StringComparison.Ordinal) == 0)
                {
                    return xmlSchemaType;
                }
            }
        }
        return null;
    }

    public static XmlSchemaType? GetSchemaType(IEnumerable<List<XmlSchemaType>> schemaTypes, XmlQualifiedName name)
    {
        foreach (List<XmlSchemaType> xmlSchemaTypes in schemaTypes)
        {
            foreach (XmlSchemaType xmlSchemaType in xmlSchemaTypes)
            {
                if (name.Equals(xmlSchemaType.QualifiedName))
                {
                    return xmlSchemaType;
                }
            }
        }
        return null;
    }

    public static AOutputType? GetOutputType(IReadOnlyList<OutputFile> outputFiles, XmlSchemaType schemaType)
    {
        foreach (OutputFile outputFile in outputFiles)
        {
            foreach (AOutputType outputType in outputFile.OutputTypes)
            {
                if (string.Compare(outputType.XmlName, schemaType.Name, StringComparison.Ordinal) == 0)
                {
                    return outputType;
                }
            }
        }
        return null;
    }

    public static void Link(IReadOnlyList<OutputFile> outputFiles)
    {
        foreach (OutputFile outputFile in outputFiles)
        {
            foreach (AOutputType outputType in outputFile.OutputTypes)
            {
                outputType.Link(outputFiles);
            }
        }
    }

    public string WriteDeclaration()
    {
        if (OutputTypes.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new();

        bool hasStruct = false;
        bool hasEnums = false;
        bool hasBitFlags = false;
        bool hasReference = false;
        List<string> namespaces = new();
        foreach (AOutputType type in OutputTypes)
        {
            if (!type.Declare)
            {
                continue;
            }
            if (type is OutputStruct)
            {
                hasStruct = true;
            }
            else if (type is OutputEnum)
            {
                hasEnums = true;
            }
            else if (type is OutputBitFlags)
            {
                hasBitFlags = true;
            }
            else if (type is OutputReference or OutputWeakReference)
            {
                hasReference = true;
            }
            if (!namespaces.Contains(type.TargetNamespace))
            {
                namespaces.Add(type.TargetNamespace);
            }
        }

        string currentNamespace = namespaces[0];
        bool hasMultipleNamespaces = namespaces.Count > 1;

        int indent = 0;

        if (hasEnums)
        {
            sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        }
        if (hasBitFlags || hasStruct)
        {
            sb.AppendLine("using System.Runtime.InteropServices;");
        }
        if (hasReference)
        {
            sb.AppendLine("using Relo;");
        }
        sb.AppendLine();

        if (hasMultipleNamespaces)
        {
            sb.AppendLine($"namespace {currentNamespace}");
            sb.AppendLine("{");
            indent++;
        }
        else
        {
            sb.AppendLine($"namespace {currentNamespace};");
            sb.AppendLine();
        }

        foreach (AOutputType type in OutputTypes)
        {
            if (!type.Declare)
            {
                continue;
            }
            if (hasMultipleNamespaces && string.Compare(currentNamespace, type.TargetNamespace) != 0)
            {
                currentNamespace = type.TargetNamespace;
                sb.AppendLine("}");
                indent--;
                sb.AppendLine();
                sb.AppendLine($"namespace {currentNamespace}");
                sb.AppendLine("{");
                indent++;
            }
            type.WriteTypeDeclaration(indent, sb);
        }

        if (hasMultipleNamespaces)
        {
            sb.AppendLine("}");
        }

        return sb.ToString();
    }

    public string WriteMarshaler()
    {
        if (OutputTypes.Count == 0)
        {
            return string.Empty;
        }

        StringBuilder sb = new();

        int indent = 0;

        sb.AppendLine("using System.ComponentModel.DataAnnotations;");
        sb.AppendLine("using System.Runtime.CompilerServices;");
        sb.AppendLine("using static global::Marshaler;");
        sb.AppendLine();

        sb.AppendLine("partial class Marshaler");
        sb.AppendLine("{");
        indent++;

        foreach (AOutputType outputType in OutputTypes)
        {
            if (!outputType.Marshal)
            {
                continue;
            }
            outputType.WriteMarshal(indent, sb);
        }

        indent--;
        sb.AppendLine("}");

        return sb.ToString();
    }

    public override string ToString()
    {
        return Name;
    }
}
