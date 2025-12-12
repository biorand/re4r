using System;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    public interface IProgressReporter
    {
        public void RunTask(string text, Action cb);
    }
}
