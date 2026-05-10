using System;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
    public class ExportModAttribute : Attribute
    {
        public required string FileName { get; init; }
        public required string Name { get; init; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Author { get; set; }
    }
}
