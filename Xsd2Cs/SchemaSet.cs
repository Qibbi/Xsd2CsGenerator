using Microsoft.CodeAnalysis;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Xml;
using System.Xml.Schema;

namespace Xsd2Cs;

internal sealed class SchemaSet
{
    private const string _xmlNamespace = "uri:ea.com:eala:asset";
    private const string _assetDeclarationElement = "AssetDeclaration";

    private readonly SortedDictionary<string, AdditionalText?> _schemaPaths = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, string> _schemaTypeToSourceMap = new();
    private readonly XmlSchemaSet _schemas = new();
    private readonly string _sourceDirectory;
    private string _currentSchema = "<unknown>";

    public Dictionary<string, XmlSchemaType> AssetTypes { get; } = new();
    public Dictionary<string, List<XmlSchemaType>> SchemaTypes { get; } = new();

    public SchemaSet(Settings settings, SourceProductionContext context, ImmutableArray<AdditionalText> files)
    {
        try
        {
            string schemaFile = files.Where(x => x.Path.EndsWith(settings.EntryXsd)).Select(x => x.Path).FirstOrDefault();
            _sourceDirectory = Path.GetDirectoryName(schemaFile);
            ReadSchema(context, files, _sourceDirectory, schemaFile);
        }
        catch (Exception ex)
        {
            throw new InvalidDataException($"Error encountered in schema '{_currentSchema}'.", ex);
        }
        _schemas.Compile();
        GetSchemaTypes();
        GetAssetTypes();
    }

    public static bool IsSimple(XmlSchemaType? type)
    {
        return type is XmlSchemaSimpleType;
    }

    public static bool IsRef(XmlSchemaType? type)
    {
        if (type is XmlSchemaSimpleType && type.UnhandledAttributes is not null)
        {
            foreach (XmlAttribute attribute in type.UnhandledAttributes)
            {
                if (attribute.Name == "xas:isRef" && bool.Parse(attribute.Value))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool IsWeakRef(XmlSchemaType? type)
    {
        if (type is XmlSchemaSimpleType && type.UnhandledAttributes is not null)
        {
            foreach (XmlAttribute attribute in type.UnhandledAttributes)
            {
                if (attribute.Name == "xas:isWeakRef" && bool.Parse(attribute.Value))
                {
                    return true;
                }
            }
        }
        return false;
    }

    public static bool IsEnum(XmlSchemaType? type)
    {
        if (type is XmlSchemaSimpleType simpleType &&
            simpleType.DerivedBy == XmlSchemaDerivationMethod.Restriction &&
            simpleType.Content is XmlSchemaSimpleTypeRestriction restriction &&
            restriction.BaseTypeName.Name.Equals("string", StringComparison.OrdinalIgnoreCase) &&
            restriction.Facets.Count > 0 &&
            restriction.Facets[0] is XmlSchemaEnumerationFacet)
        {
            return true;
        }
        return false;
    }

    public static bool IsBitFlags(XmlSchemaType? type)
    {
        if (type is XmlSchemaSimpleType simpleType &&
            simpleType.DerivedBy == XmlSchemaDerivationMethod.List &&
            simpleType.Content is XmlSchemaSimpleTypeList list &&
            IsEnum(list.BaseItemType))
        {
            return true;
        }
        return false;
    }

    public static bool IsList(XmlSchemaType? type)
    {
        if (type is XmlSchemaSimpleType && type.DerivedBy == XmlSchemaDerivationMethod.List)
        {
            return true;
        }
        return false;
    }

    private void ReadSchema(SourceProductionContext context, ImmutableArray<AdditionalText> files, string baseDirectory, string schemaFile)
    {
        schemaFile = Path.IsPathRooted(schemaFile) ? Path.GetFullPath(schemaFile) : Path.GetFullPath(Path.Combine(baseDirectory, schemaFile));
        _currentSchema = schemaFile;
        if (_schemaPaths.TryGetValue(schemaFile, out AdditionalText? file))
        {
            return;
        }
        file = files.Where(x => x.Path.Equals(schemaFile, StringComparison.OrdinalIgnoreCase)).FirstOrDefault();
        _schemaPaths.Add(schemaFile, file);
        if (file is null)
        {
            return;
        }
        string? schema = file.GetText(context.CancellationToken)?.ToString();
        if (schema is null)
        {
            return;
        }
        using StringReader reader = new(schema);
        XmlSchema xmlSchema = XmlSchema.Read(reader, null);
        xmlSchema.SourceUri = PathInternal.GetRelativePath(_sourceDirectory, schemaFile);
        foreach (XmlSchemaInclude include in xmlSchema.Includes.Cast<XmlSchemaInclude>())
        {
            ReadSchema(context, files, Path.GetDirectoryName(schemaFile), include.SchemaLocation);
        }
        xmlSchema.Includes.Clear();
        foreach (XmlSchemaObject obj in xmlSchema.Items)
        {
            if (obj is XmlSchemaType type)
            {
                _schemaTypeToSourceMap.Add(type.Name, xmlSchema.SourceUri);
            }
        }
        _schemas.Add(xmlSchema);
    }

    private void GetSchemaTypes()
    {
        foreach (DictionaryEntry entry in _schemas.GlobalTypes)
        {
            if (!_schemaTypeToSourceMap.TryGetValue((entry.Key as XmlQualifiedName)!.Name, out string? sourceFile))
            {
                continue;
            }
            sourceFile = Path.Combine(Path.GetDirectoryName(sourceFile) ?? string.Empty, Path.GetFileNameWithoutExtension(sourceFile));
            if (!SchemaTypes.TryGetValue(sourceFile, out List<XmlSchemaType>? types))
            {
                types = new List<XmlSchemaType>();
                SchemaTypes.Add(sourceFile, types);
            }
            types.Add((entry.Value as XmlSchemaType)!);
        }
    }

    private void GetAssetTypes()
    {
        if (_schemas.GlobalElements[new XmlQualifiedName(_assetDeclarationElement, _xmlNamespace)] is not XmlSchemaElement element)
        {
            throw new InvalidDataException($"No '{_assetDeclarationElement}' global element found.");
        }
        if (element.ElementSchemaType is not XmlSchemaComplexType complex || complex.ContentTypeParticle is not XmlSchemaSequence sequence || sequence.Items[sequence.Items.Count - 1] is not XmlSchemaChoice choice)
        {
            throw new InvalidDataException($"Invalid '{_assetDeclarationElement}' global element.");
        }
        foreach (XmlSchemaObject obj in choice.Items)
        {
            if (obj is XmlSchemaElement objT)
            {
                AssetTypes.Add(objT.Name, objT.ElementSchemaType);
            }
        }
    }
}
