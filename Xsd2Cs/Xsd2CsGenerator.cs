using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
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
            AdditionalText? settingsFile = settings.FirstOrDefault() ?? throw new ArgumentNullException("No 'Xsd2CsSettings.xml' found.");
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
            List<OutputFile> outputFiles = new();
            foreach (KeyValuePair<string, List<XmlSchemaType>> schemaFile in schemaSet.SchemaTypes)
            {
                OutputFile outputFile = new(schemaFile.Key, _excludedIncludes.Contains(schemaFile.Key));
                outputFiles.Add(outputFile);

                foreach (XmlSchemaType schemaType in schemaFile.Value)
                {
                    Xsd2Cs.Type? type = schemaSet.GetType(schemaType);
                    if (SchemaSet.IsSimple(schemaType))
                    {
                        if (SchemaSet.IsEnum(schemaType))
                        {
                            outputFile.OutputTypes.Add(new OutputEnum(_settings, schemaType, type));
                        }
                        else if (SchemaSet.IsBitFlags(schemaType))
                        {
                            outputFile.OutputTypes.Add(new OutputBitFlags(_settings, schemaType, type));
                        }
                        else if (SchemaSet.IsRef(schemaType))
                        {
                            outputFile.OutputTypes.Add(new OutputReference(schemaSet.SchemaTypes.Values, _settings, schemaType, type));
                        }
                        else if (SchemaSet.IsWeakRef(schemaType))
                        {
                            outputFile.OutputTypes.Add(new OutputWeakReference(_settings, schemaType, type));
                        }
                        // TODO: ref types
                        // TODO: TypedAssetId
                    }
                    else
                    {
                        // TODO: complex types based on simple types (like MultisoundSubsoundRef)
                        // TODO: polymorphic type
                        outputFile.OutputTypes.Add(new OutputStruct(schemaSet.SchemaTypes.Values, _settings, schemaType, type));
                    }
                }
            }
            OutputFile.Link(outputFiles);
            foreach (OutputFile outputFile in outputFiles)
            {
                if (outputFile.IsExcluded)
                {
                    continue;
                }
                context.AddSource(outputFile.Name.Replace('\\', '.'), outputFile.WriteDeclaration());
                context.AddSource(outputFile.Name.Replace('\\', '.') + ".Marshaler", outputFile.WriteMarshaler(_settings.Types!.TargetNamespace!));
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
