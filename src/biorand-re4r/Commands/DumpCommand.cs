using System.ComponentModel;
using System.Text.Json;
using RszTool;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class DumpCommand : AsyncCommand<DumpCommand.Settings>
    {
        public sealed class Settings : CommandSettings
        {
            [Description("Input file to dump")]
            [CommandArgument(0, "<input>")]
            public string? InputPath { get; init; }
        }

        public override ValidationResult Validate(CommandContext context, Settings settings)
        {
            if (settings.InputPath == null)
            {
                return ValidationResult.Error($"Input path not specified");
            }
            return base.Validate(context, settings);
        }


        public override Task<int> ExecuteAsync(CommandContext context, Settings settings)
        {
            var scnFile = ChainsawRandomizerFactory.Default.ReadScnFile(File.ReadAllBytes(settings.InputPath!));
            var result = new Dictionary<string, object?>();
            result["GameObjects"] = scnFile.GameObjectDatas!.Select(SerializeGameObject);
            var output = JsonSerializer.Serialize(result, new JsonSerializerOptions()
            {
                WriteIndented = true
            });
            Console.WriteLine(output);
            return Task.FromResult(0);
        }

        private static object? SerializeGameObject(IGameObjectData gameObject)
        {
            var result = new Dictionary<string, object?>();
            result["Components"] = gameObject.Components.Select(SerializeInstance);
            result["Children"] = gameObject.GetChildren().Select(SerializeGameObject).ToArray();
            return result;
        }

        private static object? SerializeInstance(RszInstance instance)
        {
            if (!instance.HasValues)
                return null;

            var result = new Dictionary<string, object?>();
            for (var i = 0; i < instance.Fields.Length; i++)
            {
                var fieldName = instance.Fields[i].name;
                var fieldValue = instance.Values[i];
                result[fieldName] = SerializeValue(fieldValue);
            }
            return result;
        }

        private static object? SerializeValue(object value)
        {
            if (value is RszInstance child)
            {
                return SerializeInstance(child);
            }
            else if (value is List<object> list)
            {
                return list.Select(SerializeValue).ToArray();
            }
            else
            {
                return value;
            }
        }
    }
}
