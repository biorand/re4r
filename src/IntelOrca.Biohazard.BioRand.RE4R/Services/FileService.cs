using System.Collections.Generic;

namespace IntelOrca.Biohazard.BioRand.RE4R.Services
{
#pragma warning disable CS9113 // Parameter is unread.
    internal class FileService(ChainsawRandomizer chainsaw)
#pragma warning restore CS9113 // Parameter is unread.
    {
        private int _id = 2;

        public List<FilePlacement> FilePlacements { get; private set; } = [];

        internal int GetNextId()
        {
            return _id++;
        }
    }

    internal class FilePlacement
    {
        public int TemplateId { get; set; }
        public int Id { get; set; }
        public string Content { get; set; } = "";

        public int Stage { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Z { get; set; }
        public float Yaw { get; set; }
        public float Pitch { get; set; }
        public float Roll { get; set; }
    }
}
