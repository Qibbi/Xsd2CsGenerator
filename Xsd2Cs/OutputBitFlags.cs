using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml;
using System.Xml.Schema;
using static Xsd2Cs.OutputFile;

namespace Xsd2Cs;

internal sealed class OutputBitFlags : AOutputType
{
    public XmlSchemaType XmlEnumType { get; }
    public OutputEnum? EnumType { get; private set; }

    public OutputBitFlags(Settings settings, XmlSchemaType schemaType, Xsd2Cs.Type? type) : base(settings, schemaType, type)
    {
        if (schemaType is not XmlSchemaSimpleType simpleType)
        {
            throw new InvalidDataException($"'{Name}' is not a valid enum.");
        }

        XmlEnumType = (simpleType.Content as XmlSchemaSimpleTypeList)!.BaseItemType!;
        if (schemaType.UnhandledAttributes is not null)
        {
            foreach (XmlAttribute attribute in schemaType.UnhandledAttributes)
            {
                switch (attribute.Name)
                {
                    case "xas:targetNamespace":
                        // Handled by abstract type.
                        break;
                    default:
                        throw new InvalidDataException($"The attribute '{attribute.Name}' is unknown for bit flags.");
                }
            }
        }
    }

    public override void Link(IReadOnlyList<OutputFile> files)
    {
        EnumType = GetOutputType(files, XmlEnumType) as OutputEnum ?? throw new InvalidOperationException($"The enum type '{XmlEnumType.Name}' for bit flags '{Name}' could not be found.");
    }

    public override void WriteTypeDeclaration(int indent, StringBuilder sb)
    {
        WriteIndent(indent, sb).AppendLine("[StructLayout(LayoutKind.Sequential)]");
        WriteIndent(indent, sb).AppendLine($"public struct {Name}");
        WriteIndent(indent, sb).AppendLine("{");
        indent++;

        WriteIndent(indent, sb).AppendLine($"public const int Count = {EnumType!.EnumValues.Count};");
        WriteIndent(indent, sb).AppendLine("public const int BitsInSpan = 32;");
        WriteIndent(indent, sb).AppendLine("public const int NumSpans = (Count + (BitsInSpan - 1)) / BitsInSpan;");

        sb.AppendLine();

        WriteIndent(indent, sb).AppendLine("public unsafe fixed uint Value[NumSpans];");

        indent--;
        WriteIndent(indent, sb).AppendLine("}");
    }

    public override void WriteMarshal(int indent, StringBuilder sb)
    {
        string name = $"{TargetNamespace}.{Name}";

        WriteIndent(indent, sb).AppendLine($"public static unsafe void Marshal(string? text, ref {name} objT, Relo.Tracker tracker)");
        WriteIndent(indent, sb).AppendLine("{");
        indent++;
        {
            WriteIndent(indent, sb).AppendLine("if (text is null)");
            WriteIndent(indent, sb).AppendLine("{");
            indent++;
            {
                WriteIndent(indent, sb).AppendLine("return;");
            }
            indent--;
            WriteIndent(indent, sb).AppendLine("}");

            WriteIndent(indent, sb).AppendLine();

            WriteIndent(indent, sb).AppendLine("string[] tokens = text.Split(WhiteSpaces, StringSplitOptions.RemoveEmptyEntries);");
            WriteIndent(indent, sb).AppendLine("if (tokens.Length == 0)");
            WriteIndent(indent, sb).AppendLine("{");
            indent++;
            {
                WriteIndent(indent, sb).AppendLine("return;");
            }
            indent--;
            WriteIndent(indent, sb).AppendLine("}");

            sb.AppendLine();

            WriteIndent(indent, sb).AppendLine("for (int idy = 0; idy < tokens.Length; ++idy)");
            WriteIndent(indent, sb).AppendLine("{");
            indent++;
            {
                WriteIndent(indent, sb).AppendLine("ReadOnlySpan<char> token = tokens[idy];");
                WriteIndent(indent, sb).AppendLine("bool includeToken = true;");
                WriteIndent(indent, sb).AppendLine("if (token[0] == '+')");
                WriteIndent(indent, sb).AppendLine("{");
                indent++;
                {
                    WriteIndent(indent, sb).AppendLine("includeToken = true;");
                    WriteIndent(indent, sb).AppendLine("token = token[1..];");
                    WriteIndent(indent, sb);
                }
                indent--;
                WriteIndent(indent, sb).AppendLine("}");
                WriteIndent(indent, sb).AppendLine("else if (token[0] == '-')");
                WriteIndent(indent, sb).AppendLine("{");
                indent++;
                {
                    WriteIndent(indent, sb).AppendLine("includeToken = false;");
                    WriteIndent(indent, sb).AppendLine("token = token[1..];");
                }
                indent--;
                WriteIndent(indent, sb).AppendLine("}");

                sb.AppendLine();

                WriteIndent(indent, sb).AppendLine("if (token.SequenceCompareTo(\"ALL\") == 0)");
                WriteIndent(indent, sb).AppendLine("{");
                indent++;
                {
                    WriteIndent(indent, sb).AppendLine($"for (int idx = 0; idx < {name}.NumSpans; ++idx)");
                    WriteIndent(indent, sb).AppendLine("{");
                    indent++;
                    {
                        WriteIndent(indent, sb).AppendLine("objT.Value[idx] = uint.MaxValue;");
                    }
                    indent--;
                    WriteIndent(indent, sb).AppendLine("}");
                    WriteIndent(indent, sb).AppendLine("continue;");
                }
                indent--;
                WriteIndent(indent, sb).AppendLine("}");

                sb.AppendLine();

                WriteIndent(indent, sb).AppendLine($"{EnumType!.TargetNamespace}.{EnumType!.Name} value = ({EnumType!.TargetNamespace}.{EnumType!.Name})(-1);");
                WriteIndent(indent, sb).AppendLine("Marshal(token, ref value, tracker);");
                WriteIndent(indent, sb).AppendLine($"tracker.InplaceEndianToPlatform(ref Unsafe.As<{EnumType!.TargetNamespace}.{EnumType!.Name}, uint>(ref value));");

                sb.AppendLine();

                WriteIndent(indent, sb).AppendLine($"if (value != ({EnumType!.TargetNamespace}.{EnumType!.Name})(-1))");
                WriteIndent(indent, sb).AppendLine("{");
                indent++;
                {
                    WriteIndent(indent, sb).AppendLine("uint uintValue = (uint)value;");
                    WriteIndent(indent, sb).AppendLine($"if (uintValue < {name}.Count)");
                    WriteIndent(indent, sb).AppendLine("{");
                    indent++;
                    {
                        WriteIndent(indent, sb).AppendLine("if (includeToken)");
                        WriteIndent(indent, sb).AppendLine("{");
                        indent++;
                        {
                            WriteIndent(indent, sb).AppendLine($"objT.Value[uintValue / {name}.BitsInSpan] |= (uint)(1 << (int)(uintValue % {name}.BitsInSpan));");
                        }
                        indent--;
                        WriteIndent(indent, sb).AppendLine("}");
                        WriteIndent(indent, sb).AppendLine("else");
                        WriteIndent(indent, sb).AppendLine("{");
                        indent++;
                        {
                            WriteIndent(indent, sb).AppendLine($"objT.Value[uintValue / {name}.BitsInSpan] ^= (uint)(1 << (int)(uintValue % {name}.BitsInSpan));");
                        }
                        indent--;
                        WriteIndent(indent, sb).AppendLine("}");
                    }
                    indent--;
                    WriteIndent(indent, sb).AppendLine("}");
                }
                indent--;
                WriteIndent(indent, sb).AppendLine("}");
            }
            indent--;
            WriteIndent(indent, sb).AppendLine("}");

            WriteIndent(indent, sb).AppendLine($"for (int idx = 0; idx < {name}.NumSpans; ++idx)");
            WriteIndent(indent, sb).AppendLine("{");
            indent++;
            {
                WriteIndent(indent, sb).AppendLine("tracker.InplaceEndianToPlatform(ref objT.Value[idx]);");
            }
            indent--;
            WriteIndent(indent, sb).AppendLine("}");

        }
        indent--;
        WriteIndent(indent, sb).AppendLine("}");

        sb.AppendLine();
    }
}
