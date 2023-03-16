using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Schema;

namespace Xsd2Cs;

internal abstract class AOutputType
{
    protected readonly XmlSchemaType _schemaType;

    public string Name => MappedTypeName ?? _schemaType.Name;
    public string XmlName => _schemaType.Name;
    public string TargetNamespace { get; }
    public string? MappedTypeName { get; }
    public bool Declare { get; }
    public bool Marshal { get; }
    public string TypeDefinition { get; private set; }
    public string TypeMarshal { get; private set; }

    public AOutputType(Settings settings, XmlSchemaType schemaType, Xsd2Cs.Type? type)
    {
        _schemaType = schemaType;
        string rootNamespace = settings.Types!.TargetNamespace!;
        TargetNamespace = rootNamespace;
        Declare = true;
        Marshal = true;
        TypeDefinition = string.Empty;
        TypeMarshal = string.Empty;

        if (schemaType.UnhandledAttributes is not null)
        {
            foreach (XmlAttribute attribute in schemaType.UnhandledAttributes)
            {
                switch (attribute.Name)
                {
                    case "xas:targetNamespace":
                        if (!string.IsNullOrEmpty(attribute.Value))
                        {
                            TargetNamespace = attribute.Value.Replace("::", ".");
                        }
                        break;
                }
            }
        }

        if (type is not null)
        {
            if (type.TargetNamespaceSpecified && !string.IsNullOrEmpty(type.TargetNamespace))
            {
                TargetNamespace = type.TargetNamespace!.Replace("::", ".");
            }
            MappedTypeName = type.MappedTypeName;
            Declare = type.Declare == DeclareType.declare;
            Marshal = type.Marshal;
        }
    }

    public virtual void Link(IReadOnlyList<OutputFile> files)
    {
    }

    public virtual void WriteInheritedMemberDeclaration(int indent, StringBuilder sb)
    {
    }

    public abstract void WriteTypeDeclaration(int indent, StringBuilder sb);

    public abstract void WriteMarshal(int indent, StringBuilder sb);

    public override string ToString()
    {
        return $"{TargetNamespace}::{MappedTypeName ?? Name}";
    }
}
