using System;
using System.Text;
using System.Xml.Schema;

namespace Xsd2Cs;

internal sealed class OutputWeakReference : AOutputType
{
    public OutputWeakReference(Settings settings, XmlSchemaType schemaType, Type? type) : base(settings, schemaType, type)
    {
    }

    public override void WriteTypeDeclaration(int indent, StringBuilder sb)
    {
    }

    public override void WriteMarshal(int indent, StringBuilder sb)
    {
    }
}
