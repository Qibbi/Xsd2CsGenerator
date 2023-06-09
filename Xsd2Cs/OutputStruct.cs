﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using static Xsd2Cs.OutputFile;

namespace Xsd2Cs;

internal sealed class OutputStruct : AOutputType
{
    public XmlSchemaType? XmlBaseType { get; }
    public AOutputType? BaseType { get; private set; }
    public List<AOutputType> Types { get; } = new();
    public List<(int, AOutputMember)> Members { get; } = new();

    public OutputStruct(IEnumerable<List<XmlSchemaType>> schemaTypes, Settings settings, XmlSchemaType schemaType, Xsd2Cs.Type? type) : base(settings, schemaType, type)
    {
        if (schemaType is XmlSchemaComplexType complex)
        {
            if (complex.DerivedBy == XmlSchemaDerivationMethod.Extension)
            {
                if (complex.ContentModel is XmlSchemaComplexContent complexContent && complexContent.Content is XmlSchemaComplexContentExtension complexExtension)
                {
                    XmlBaseType = GetSchemaType(schemaTypes, complexExtension.BaseTypeName);
                }
                else if (complex.ContentModel is XmlSchemaSimpleContent simpleContent && simpleContent.Content is XmlSchemaSimpleContentExtension simpleExtension)
                {
                    XmlBaseType = GetSchemaType(schemaTypes, simpleExtension.BaseTypeName);
                }
            }
        }
    }

    private static int SortMembers((int order, AOutputMember member) x, (int order, AOutputMember member) y)
    {
        if (x.member.Size < 4 || y.member.Size < 4)
        {
            if (x.member.Size > y.member.Size)
            {
                return 1;
            }
            if (x.member.Size < y.member.Size)
            {
                return -1;
            }
        }
        if (x.order > y.order)
        {
            return 1;
        }
        return -1; // members never have the same original order index
    }

    public override void Link(IReadOnlyList<OutputFile> files)
    {
        if (XmlBaseType is not null)
        {
            BaseType = GetOutputType(files, XmlBaseType) ?? throw new InvalidOperationException($"The base type '{XmlBaseType.Name}' for struct '{Name}' could not be found.");
        }
        Members.Sort(SortMembers);
    }

    public override void WriteInheritedMemberDeclaration(int indent, StringBuilder sb)
    {
        // TODO:
    }

    public override void WriteTypeDeclaration(int indent, StringBuilder sb)
    {
        WriteIndent(indent, sb).Append($"public interface I{Name}Interface");
        if (BaseType is null or OutputReference or OutputWeakReference)
        {
            sb.AppendLine();
        }
        else
        {
            sb.AppendLine($" : I{BaseType.Name}Interface");
        }
        WriteIndent(indent, sb).AppendLine("{");
        indent++;

        indent--;
        WriteIndent(indent, sb).AppendLine("}");

        WriteIndent(indent, sb).AppendLine("[StructLayout(LayoutKind.Sequential)]");
        WriteIndent(indent, sb).AppendLine($"public unsafe struct {Name} : I{Name}Interface");
        WriteIndent(indent, sb).AppendLine("{");
        indent++;

        if (BaseType is not null)
        {
            WriteIndent(indent, sb).AppendLine($"// {BaseType.Name} interface");
            if (BaseType is OutputReference reference)
            {
                WriteIndent(indent, sb).AppendLine($"internal AssetReference<{reference.RefType!.Name}> _value;");
            }
            else if (BaseType is OutputWeakReference weakReference)
            {
                WriteIndent(indent, sb).AppendLine($"internal TypedAssetId<{weakReference.RefType!.Name}> _value;");
            }
            else
            {
                WriteIndent(indent, sb).AppendLine($"internal {BaseType!.TargetNamespace}.{BaseType!.Name} _base;");
            }
            sb.AppendLine();
            BaseType.WriteInheritedMemberDeclaration(indent, sb);
            WriteIndent(indent, sb).AppendLine($"// End {BaseType.Name} interface");
            sb.AppendLine();
        }

        // TODO:

        indent--;
        WriteIndent(indent, sb).AppendLine("}");
    }

    public override void WriteMarshal(int indent, StringBuilder sb)
    {
        string name = $"{TargetNamespace}.{Name}";
        
        WriteIndent(indent, sb).AppendLine($"public static unsafe void Marshal(Xml.Node? node, ref {name} objT, Relo.Tracker tracker)");
        WriteIndent(indent, sb).AppendLine("{");
        indent++;
        
        if (BaseType is not null)
        {
            if (BaseType is OutputReference or OutputWeakReference)
            {
                sb.AppendLine();
                WriteIndent(indent, sb).AppendLine($"Marshal(node, ref objT._value, tracker);");
            }
            else
            {
                sb.AppendLine();
                WriteIndent(indent, sb).AppendLine($"Marshal(node, ref objT._base, tracker);");
            }
        }
        
        indent--;
        WriteIndent(indent, sb).AppendLine("}");
        
        sb.AppendLine();
    }
}
