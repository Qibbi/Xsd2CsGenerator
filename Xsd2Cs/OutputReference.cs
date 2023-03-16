using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using static Xsd2Cs.OutputFile;

namespace Xsd2Cs;

internal sealed class OutputReference : AOutputType
{
    public XmlSchemaType XmlRefType { get; }
    public AOutputType? RefType { get; private set; }

    public OutputReference(IEnumerable<List<XmlSchemaType>> schemaTypes, Settings settings, XmlSchemaType schemaType, Type? type) : base(settings, schemaType, type)
    {
        if (schemaType.UnhandledAttributes is not null)
        {
            foreach (XmlAttribute attribute in schemaType.UnhandledAttributes)
            {
                if (string.Compare(attribute.Name, "xas:refType", StringComparison.Ordinal) == 0)
                {
                    XmlRefType = GetSchemaType(schemaTypes, attribute.Value) ?? throw new InvalidDataException($"The ref type '{attribute.Value}' for AssetReference<{Name}> is not declared.");
                }
            }
        }

        if (Declare && XmlRefType is null)
        {
            throw new InvalidDataException($"The ref type for '{Name}' is not declared.");
        }
    }

    public override void Link(IReadOnlyList<OutputFile> files)
    {
        if (!Declare && RefType is null)
        {
            return;
        }

        RefType = GetOutputType(files, XmlRefType) ?? throw new InvalidOperationException($"The ref type '{XmlRefType.Name}' for AssetReference<{Name}> could not be found.");
    }

    public override void WriteMemberDeclaration(int indent, StringBuilder sb)
    {
        WriteIndent(indent, sb);
        sb.AppendLine($"internal {RefType!.Name}* _reference;");

        sb.AppendLine();

        WriteIndent(indent, sb);
        sb.AppendLine($"public {RefType!.Name}* Reference {{ get => _reference; set => _reference = value; }}");
    }

    public override void WriteTypeDeclaration(int indent, StringBuilder sb)
    {
        WriteIndent(indent, sb);
        sb.AppendLine($"public unsafe interface I{Name}Interface");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;

        WriteIndent(indent, sb);
        sb.AppendLine($"{RefType!.Name}* Reference {{ get; set; }}");

        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");

        WriteIndent(indent, sb);
        sb.AppendLine("[StructLayout(LayoutKind.Sequential)]");
        WriteIndent(indent, sb);
        sb.AppendLine($"public unsafe struct {Name} : I{Name}Interface");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;

        WriteMemberDeclaration(indent, sb);

        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");
    }

    public override void WriteMarshal(int indent, StringBuilder sb)
    {
        string name = $"{TargetNamespace}.I{Name}Interface";

        WriteIndent(indent, sb);
        sb.AppendLine($"public static unsafe void Marshal(Xml.Node? node, ref {name} objT, Relo.Tracker tracker)");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;

        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");

        sb.AppendLine();
    }
}
