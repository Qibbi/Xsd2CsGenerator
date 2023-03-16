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
    public XmlSchemaType? XmlRefType { get; }
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
    }

    public override void Link(IReadOnlyList<OutputFile> files)
    {
        if (XmlRefType is null)
        {
            return;
        }

        RefType = GetOutputType(files, XmlRefType) ?? throw new InvalidOperationException($"The ref type '{XmlRefType.Name}' for AssetReference<{Name}> could not be found.");
    }

    public override void WriteInheritedMemberDeclaration(int indent, StringBuilder sb)
    {
        WriteIndent(indent, sb).AppendLine($"public AssetReference<{RefType!.Name}> Value {{ get => _value; set => _value = value; }}");
    }

    public override void WriteTypeDeclaration(int indent, StringBuilder sb)
    {
    }

    public override void WriteMarshal(int indent, StringBuilder sb)
    {
    }
}
