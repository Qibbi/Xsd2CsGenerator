using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace Xsd2Cs
{
    [Generator(LanguageNames.CSharp)]
    public class Xsd2CsGenerator : IIncrementalGenerator
    {
        private Settings? _settings;
        private string[]? _excludedIncludes;

        private void Setup(SourceProductionContext context, ImmutableArray<AdditionalText> settings)
        {
            AdditionalText? settingsFile = settings.FirstOrDefault();
            if (settingsFile is null)
            {
                throw new ArgumentNullException("No 'Xsd2CsSettings.xml' found.");
            }

            XmlSerializer serializer = new(typeof(Settings));
            string settingsText = settingsFile.GetText(context.CancellationToken)?.ToString()!;
            StringReader reader = new(settingsText);
            _settings = serializer.Deserialize(reader) as Settings;
            _excludedIncludes = _settings!.ExcludedIncludes?.Split(' ') ?? Array.Empty<string>();
        }

        private void Run(SourceProductionContext context, ImmutableArray<AdditionalText> files)
        {
            if (_settings is null)
            {
                throw new InvalidOperationException("No settings loaded.");
            }
            SchemaSet schemaSet = new(_settings, context, files);
            foreach (KeyValuePair<string, List<XmlSchemaType>> schemaFile in schemaSet.SchemaTypes)
            {
                if (_excludedIncludes.Contains(schemaFile.Key))
                {
                    continue;
                }
                foreach (XmlSchemaType schemaType in schemaFile.Value)
                {
                    if (SchemaSet.IsSimple(schemaType))
                    {
                        if (SchemaSet.IsEnum(schemaType))
                        {
                            // TODO: add enum
                        }
                        else if (SchemaSet.IsBitFlags(schemaType))
                        {
                            // TODO: add bitflags
                        }
                        // TODO: ref types
                        // TODO: TypedAssetId
                    }
                    else
                    {
                        // TODO: complex types based on simple types (like MultisoundSubsoundRef)
                        // TODO: polymorphic type
                        // TODO: add struct
                    }
                }
                context.AddSource(schemaFile.Key.Replace('\\', '.'), $"// {schemaFile.Key}");
            }
        }

        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
#if DEBUG
            if (!Debugger.IsAttached)
            {
                Debugger.Launch();
            }
#endif
            IncrementalValueProvider<ImmutableArray<AdditionalText>> settings = context.AdditionalTextsProvider.Where(x => x.Path.EndsWith("Xsd2CsSettings.xml", StringComparison.OrdinalIgnoreCase)).Collect();
            context.RegisterSourceOutput(settings, Setup);

            IncrementalValueProvider<ImmutableArray<AdditionalText>> files = context.AdditionalTextsProvider.Where(x => x.Path.EndsWith(".xsd", StringComparison.OrdinalIgnoreCase)).Collect();
            context.RegisterSourceOutput(files, Run);
        }
    }
}
