using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using static Xsd2Cs.OutputFile;

namespace Xsd2Cs;

internal sealed class OutputEnum : AOutputType
{
    public struct EnumValue
    {
        public string Name;
        public int Value;
    }

    public bool UseEnumPrefix { get; }
    public string EnumItemPrefix { get; } = string.Empty;
    public List<EnumValue> EnumValues { get; } = new();

    public OutputEnum(Settings settings, XmlSchemaType schemaType, Xsd2Cs.Type? type) : base(settings, schemaType, type)
    {
        if (schemaType is not XmlSchemaSimpleType)
        {
            throw new InvalidDataException($"'{Name}' is not a valid enum.");
        }

        int enumVal = 0;

        if (schemaType.UnhandledAttributes is not null)
        {
            foreach (XmlAttribute attribute in schemaType.UnhandledAttributes)
            {
                switch (attribute.Name)
                {
                    case "xas:targetNamespace":
                        // Handled by abstract type.
                        break;
                    case "xas:enumItemPrefix":
                        EnumItemPrefix = attribute.Value;
                        break;
                    case "xas:useEnumPrefix":
                        UseEnumPrefix = bool.Parse(attribute.Value);
                        break;
                    case "xas:enumStartVal":
                        enumVal = int.Parse(attribute.Value);
                        break;
                    default:
                        throw new InvalidDataException($"The attribute '{attribute.Name}' is unknown for enums.");
                }
            }
        }

        if (type is not null)
        {
            if (type.UseEnumPrefixSpecified)
            {
                UseEnumPrefix = type.UseEnumPrefix;
            }
            if (type.EnumItemPrefixSpecified && type.EnumItemPrefix is not null)
            {
                EnumItemPrefix = type.EnumItemPrefix;
            }
            if (type.EnumStartValSpecified)
            {
                enumVal = type.EnumStartVal;
            }
        }

        foreach (XmlSchemaEnumerationFacet facet in ((schemaType as XmlSchemaSimpleType)!.Content as XmlSchemaSimpleTypeRestriction)!.Facets.Cast<XmlSchemaEnumerationFacet>())
        {
            if (facet.Value == "ALL")
            {
                continue;
            }
            if (facet.UnhandledAttributes is not null)
            {
                foreach (XmlAttribute attribute in facet.UnhandledAttributes)
                {
                    enumVal = attribute.Name switch
                    {
                        "xas:forceValue" => int.Parse(attribute.Value),
                        _ => throw new InvalidDataException($"The attribute '{attribute.Name}' is unknown for enum values.")
                    };
                }
            }
            EnumValue enumValue = new() { Name = facet.Value, Value = enumVal++ };
            EnumValues.Add(enumValue);
        }
    }

    public override void WriteTypeDeclaration(int indent, StringBuilder sb)
    {
        WriteIndent(indent, sb);
        sb.AppendLine($"public enum {Name}");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;

        foreach (EnumValue enumValue in EnumValues)
        {
            WriteIndent(indent, sb);
            sb.AppendLine($"[Display(Name = \"{enumValue.Name}\")] {EnumItemPrefix + enumValue.Name} = {enumValue.Value},");
        }

        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");
    }

    public override void WriteMarshal(int indent, StringBuilder sb)
    {
        string name = $"{TargetNamespace}.{Name}";

        WriteIndent(indent, sb);
        sb.AppendLine($"public static unsafe void Marshal(string? text, ref {name} objT, Relo.Tracker tracker)");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;

        WriteIndent(indent, sb);
        sb.AppendLine("if (text is null)");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;
        WriteIndent(indent, sb);
        sb.AppendLine("return;");
        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");

        WriteIndent(indent, sb);
        sb.AppendLine("Marshal(text.AsSpan(), ref objT, tracker);");

        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");

        WriteIndent(indent, sb);
        sb.AppendLine($"public static unsafe void Marshal(ReadOnlySpan<char> text, ref {name} objT, Relo.Tracker tracker)");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;

        WriteIndent(indent, sb);
        sb.AppendLine($"Type type = typeof({name});");
        WriteIndent(indent, sb);
        sb.AppendLine("foreach (System.Reflection.FieldInfo field in type.GetFields())");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;
        WriteIndent(indent, sb);
        sb.AppendLine("if (Attribute.GetCustomAttribute(field, typeof(DisplayAttribute)) is DisplayAttribute displayAttribute && text.SequenceCompareTo(displayAttribute.Name) == 0)");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;
        WriteIndent(indent, sb);
        sb.AppendLine($"objT = ({name})field.GetValue(null)!;");
        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");
        WriteIndent(indent, sb);
        sb.AppendLine("if (text.SequenceCompareTo(field.Name) == 0)");
        WriteIndent(indent, sb);
        sb.AppendLine("{");
        indent++;
        WriteIndent(indent, sb);
        sb.AppendLine($"objT = ({name})field.GetValue(null)!;");
        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");

        WriteIndent(indent, sb);
        sb.AppendLine($"tracker.InplaceEndianToPlatform(ref Unsafe.As<{name}, uint>(ref objT));");

        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");

        indent--;
        WriteIndent(indent, sb);
        sb.AppendLine("}");

        sb.AppendLine();
    }
}
