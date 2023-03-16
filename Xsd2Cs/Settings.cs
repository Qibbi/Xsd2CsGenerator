using System.Xml.Serialization;

namespace Xsd2Cs;

public enum DeclareType
{
    none,
    declare
}

public sealed class Member
{
    [XmlAttribute] public string? Name { get; set; }
    [XmlAttribute] public string? MappedMemberName { get; set; }
    [XmlIgnore] public bool MappedMemberNameSpecified { get; set; }
    [XmlAttribute] public string? MappedTypeName { get; set; }
    [XmlIgnore] public bool MappedTypeNameSpecified { get; set; }
    [XmlAttribute(AttributeName = "declare")] public DeclareType Declare { get; set; } = DeclareType.declare;
    [XmlIgnore] public bool DeclareSpecified { get; set; }
    [XmlAttribute(AttributeName = "marshal")] public bool Marshal { get; set; } = true;
    [XmlIgnore] public bool MarshalSpecified { get; set; }
    [XmlAttribute] public int Size { get; set; }
    [XmlIgnore] public bool SizeSpecified { get; set; }
    [XmlAttribute(AttributeName = "align")] public int Align { get; set; }
    [XmlIgnore] public bool AlignSpecified { get; set; }
}

public sealed class Type
{
    [XmlAttribute] public string? Name { get; set; }
    [XmlAttribute] public string? MappedTypeName { get; set; }
    [XmlIgnore] public bool MappedTypeNameSpecified { get; set; }
    [XmlAttribute] public string? TargetNamespace { get; set; }
    [XmlIgnore] public bool TargetNamespaceSpecified { get; set; }
    [XmlAttribute(AttributeName = "declare")] public DeclareType Declare { get; set; } = DeclareType.declare;
    [XmlIgnore] public bool DeclareSpecified { get; set; }
    [XmlAttribute(AttributeName = "marshal")] public bool Marshal { get; set; } = true;
    [XmlIgnore] public bool MarshalSpecified { get; set; }
    [XmlAttribute] public int Size { get; set; }
    [XmlIgnore] public bool SizeSpecified { get; set; }
    [XmlAttribute(AttributeName = "align")] public int Align { get; set; }
    [XmlIgnore] public bool AlignSpecified { get; set; }
    [XmlAttribute] public string? MemberPrefix { get; set; }
    [XmlIgnore] public bool MemberPrefixSpecified { get; set; }
    [XmlAttribute(AttributeName = "useEnumPrefix")] public bool UseEnumPrefix { get; set; }
    [XmlIgnore] public bool UseEnumPrefixSpecified { get; set; }
    [XmlAttribute(AttributeName = "enumItemPrefix")] public string? EnumItemPrefix { get; set; }
    [XmlIgnore] public bool EnumItemPrefixSpecified { get; set; }
    [XmlAttribute(AttributeName = "enumStartVal")] public int EnumStartVal { get; set; }
    [XmlIgnore] public bool EnumStartValSpecified { get; set; }
    [XmlAttribute(AttributeName = "deserialize")] public bool Deserialize { get; set; }
    [XmlIgnore] public bool DeserializeSpecified { get; set; }
    [XmlElement] public Member[]? Member { get; set; }
}

public sealed class Types
{
    [XmlAttribute] public string? TargetNamespace { get; set; }
    [XmlAttribute] public string? ListTypeName { get; set; }
    [XmlAttribute] public string? SortedListTypeName { get; set; }
    [XmlAttribute] public string? SizedListTypeName { get; set; }
    [XmlAttribute] public string? BfTypeName { get; set; }
    [XmlAttribute] public string? OptionalTypeName { get; set; }
    [XmlElement] public Type[]? Type { get; set; }
}

[XmlRoot(nameof(Settings), Namespace = "urn:xmlns:ea.com:xml2cs", IsNullable = false)]
public sealed class Settings
{
    [XmlAttribute] public string? EntryXsd { get; set; }
    [XmlAttribute] public string? ExcludedIncludes { get; set; }
    [XmlElement] public Types? Types { get; set; }
}
