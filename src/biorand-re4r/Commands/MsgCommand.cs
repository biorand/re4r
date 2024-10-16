﻿using System.ComponentModel;
using MsgTool;
using Spectre.Console;
using Spectre.Console.Cli;

namespace IntelOrca.Biohazard.BioRand.RE4R.Commands
{
    internal sealed class MsgCommand : AsyncCommand<MsgCommand.Settings>
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
            var msgFile = new Msg(File.ReadAllBytes(settings.InputPath!));
            foreach (var entry in msgFile.Entries)
            {
                var str = (msgFile.GetString(entry.Guid, LanguageId.English) ?? "")
                    .Replace("\r\n", "\n")
                    .Replace("\n", " ");
                Console.WriteLine($"{entry.Guid} {str}");
            }
            return Task.FromResult(0);
        }
    }
}
