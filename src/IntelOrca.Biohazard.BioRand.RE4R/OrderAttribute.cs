using System;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    [AttributeUsage(AttributeTargets.Class)]
    internal class OrderAttribute(int order) : Attribute
    {
        public int Order => order;
    }
}
