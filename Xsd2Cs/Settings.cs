using System.Xml.Serialization;

namespace Xsd2Cs;

[XmlRoot(nameof(Settings), Namespace = "urn:xmlns:ea.com:xml2cs", IsNullable = false)]
public sealed class Settings
{
    [XmlAttribute] public string? EntryXsd { get; set; }
    [XmlAttribute] public string? ExcludedIncludes { get; set; }
}
