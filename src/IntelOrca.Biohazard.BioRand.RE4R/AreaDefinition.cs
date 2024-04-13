﻿using System;

namespace IntelOrca.Biohazard.BioRand.RE4R
{

    public class AreaDefinition
    {
        public string Path { get; set; } = "";
        public int Chapter { get; set; }
        public AreaRestriction[]? Restrictions { get; set; }
    }

    public class AreaRestriction
    {
        public Guid[]? Guids { get; set; }
        public string[]? Exclude { get; set; }
    }
}
