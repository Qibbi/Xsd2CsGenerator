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
    [XmlAttribute] public string? MappedTypeName { get; set; }
    [XmlAttribute(AttributeName = "declare")] public DeclareType Declare { get; set; } = DeclareType.declare;
    [XmlAttribute(AttributeName = "marshal")] public bool Marshal { get; set; } = true;
    [XmlAttribute] public int Size { get; set; }
    [XmlAttribute(AttributeName = "align")] public int Align { get; set; }
}

public sealed class Type
{
    [XmlAttribute] public string? Name { get; set; }
    [XmlAttribute] public string? MappedTypeName { get; set; }
    [XmlAttribute] public string? TargetNamespace { get; set; }
    [XmlAttribute(AttributeName = "declare")] public DeclareType Declare { get; set; } = DeclareType.declare;
    [XmlAttribute(AttributeName = "marshal")] public bool Marshal { get; set; } = true;
    [XmlAttribute] public int Size { get; set; }
    [XmlAttribute(AttributeName = "align")] public int Align { get; set; }
    [XmlAttribute] public string? MemberPrefix { get; set; }
    [XmlAttribute(AttributeName = "useEnumPrefix")] public bool UseEnumPrefix { get; set; }
    [XmlAttribute(AttributeName = "enumItemPrefix")] public string? EnumItemPrefix { get; set; }
    [XmlAttribute(AttributeName = "enumStartVal")] public int EnumStartVal { get; set; }
    [XmlAttribute(AttributeName = "deserialize")] public bool Deserialize { get; set; }
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
