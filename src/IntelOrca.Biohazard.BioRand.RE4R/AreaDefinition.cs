﻿using System;

namespace IntelOrca.Biohazard.BioRand.RE4R
{

    public class AreaDefinition
    {
        public string Path { get; set; } = "";
        public int Chapter { get; set; }
        public AreaRestriction[]? Restrictions { get; set; }
        public AreaExtra[]? Extra { get; set; }
    }

    public class AreaRestriction
    {
        public Guid[]? Guids { get; set; }
        public bool PreventDuplicate { get; set; }
        public string[]? Exclude { get; set; }
    }

    public class AreaExtra
    {
        public string? Condition { get; set; }
        public AreaExtraEnemy[]? Enemies { get; set; }
    }

    public class AreaExtraEnemy
    {
        public int Stage { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public int Z { get; set; }
    }
}
