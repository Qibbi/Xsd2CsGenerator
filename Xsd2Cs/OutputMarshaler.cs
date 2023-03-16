using System.Text;
using static Xsd2Cs.OutputFile;

namespace Xsd2Cs;

internal static class OutputMarshaler
{
    public static string Write()
    {
        StringBuilder sb = new();

        int indent = 0;

        sb.AppendLine("using Xml;");
        sb.AppendLine();

        sb.AppendLine($"public static partial class Marshaler");
        sb.AppendLine("{");
        indent++;
        {
            WriteIndent(indent, sb).AppendLine("public const float PI = 3.14159265359f;");
            WriteIndent(indent, sb).AppendLine("public const float RADS_PER_DEGREE = PI / 180.0f;");
            WriteIndent(indent, sb).AppendLine("public const float LOGICFRAMES_PER_SECOND = 5;");
            WriteIndent(indent, sb).AppendLine("public const float MSEC_PER_SECOND = 1000;");
            WriteIndent(indent, sb).AppendLine("public const float LOGICFRAMES_PER_MSEC_REAL = LOGICFRAMES_PER_SECOND / MSEC_PER_SECOND;");
            WriteIndent(indent, sb).AppendLine("public const float LOGICFRAMES_PER_SECONDS_REAL = LOGICFRAMES_PER_SECOND;");
            WriteIndent(indent, sb).AppendLine("public const float SECONDS_PER_LOGICFRAME_REAL = 1.0f / LOGICFRAMES_PER_SECONDS_REAL;");

            sb.AppendLine();

            WriteIndent(indent, sb).AppendLine("public static readonly char[] WhiteSpaces = new[] { ' ', '\\t', '\\n', '\\v', '\\f', '\\r' };");

            sb.AppendLine();

            WriteIndent(indent, sb).AppendLine("public static void Marshal(Node? node, ref SageBinaryData.BaseAssetType objT, Relo.Tracker state)");
            WriteIndent(indent, sb).AppendLine("{");
            WriteIndent(indent, sb).AppendLine("}");
            WriteIndent(indent, sb).AppendLine("public static void Marshal(Node? node, ref SageBinaryData.BaseInheritableAsset objT, Relo.Tracker state)");
            WriteIndent(indent, sb).AppendLine("{");
            WriteIndent(indent, sb).AppendLine("}");

            sb.AppendLine();

            WriteIndent(indent, sb).AppendLine("public static void Marshal<T>(ReadOnlySpan<char> text, ref Relo.AssetReference<T> objT, Relo.Tracker state) where T : unmanaged");
            WriteIndent(indent, sb).AppendLine("{");
            indent++;
            {
                WriteIndent(indent, sb).AppendLine("int index = text.IndexOf('\\\\');");
                WriteIndent(indent, sb).AppendLine("if (index == -1)");
                WriteIndent(indent, sb).AppendLine("{");
                indent++;
                {
                    WriteIndent(indent, sb).AppendLine("return;");
                }
                indent--;
                sb.AppendLine("}");
                WriteIndent(indent, sb).AppendLine("int value = int.Parse(text[(index + 1)..]);");
                WriteIndent(indent, sb).AppendLine("state.AddReference(ref objT, value);");
            }
            indent--;
            sb.AppendLine("}");
            WriteIndent(indent, sb).AppendLine("public static void Marshal<T>(Value? value, ref Relo.AssetReference<T> objT, Relo.Tracker state) where T : unmanaged");
            WriteIndent(indent, sb).AppendLine("{");
            indent++;
            {
                WriteIndent(indent, sb).AppendLine("if (value is null)");
                WriteIndent(indent, sb).AppendLine("{");
                indent++;
                {
                    WriteIndent(indent, sb).AppendLine("return;");
                }
                indent--;
                sb.AppendLine("}");
                WriteIndent(indent, sb).AppendLine("Marshal(value.Text, ref objT, state);");
            }
            indent--;
            sb.AppendLine("}");
            WriteIndent(indent, sb).AppendLine("public static void Marshal<T>(Node? node, ref Relo.AssetReference<T> objT, Relo.Tracker state) where T : unmanaged");
            WriteIndent(indent, sb).AppendLine("{");
            indent++;
            {
                WriteIndent(indent, sb).AppendLine("if (node is null)");
                WriteIndent(indent, sb).AppendLine("{");
                indent++;
                {
                    WriteIndent(indent, sb).AppendLine("return;");
                }
                indent--;
                sb.AppendLine("}");
                WriteIndent(indent, sb).AppendLine("Marshal(node.Value, ref objT, state);");
            }
            indent--;
            sb.AppendLine("}");
        }
        indent--;
        sb.AppendLine("}");

        return sb.ToString();
    }
}
