using System;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    [AttributeUsage(AttributeTargets.Class)]
    public class ExportModAttribute : Attribute
    {
        public string? FileName { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Version { get; set; }
        public string? Authors { get; set; }
    }
}
