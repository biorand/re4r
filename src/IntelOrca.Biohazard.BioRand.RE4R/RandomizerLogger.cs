using System;
using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal sealed class RandomizerLoggerIO
    {
        public RandomizerLogger Input { get; } = new();
        public RandomizerLogger Process { get; } = new();
        public RandomizerLogger Output { get; } = new();
    }

    internal sealed class RandomizerLogger
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly string _hr = new string('-', 80);
        private int _indent;

        public string Output => _sb.ToString();

        public void Push()
        {
            _indent++;
        }

        public void Push(string header)
        {
            LogLine(header);
            Push();
        }

        public void Pop()
        {
            _indent--;
        }

        public void LogVersion()
        {
            var crf = ChainsawRandomizerFactory.Default;

            _sb.AppendLine(crf.CurrentVersionInfo);
            _sb.AppendLine("by IntelOrca & BioRand Team");
            _sb.AppendLine($"Generated at {DateTime.Now}");
        }

        public void LogHr()
        {
            _sb.AppendLine(_hr);
        }

        public void LogHeader(string header)
        {
            _sb.AppendLine();
            LogHr();
            _sb.AppendLine(header);
            LogHr();
        }

        public void LogLine(string line)
        {
            _sb.Append(' ', _indent * 2);
            _sb.AppendLine(line);
        }

        public void LogArea(Area area)
        {
            _sb.AppendLine();
            LogHr();
            _sb.AppendLine(area.FileName);
            LogHr();
        }

        public void LogLine(params object[] columns)
        {
            _sb.Append(' ', _indent * 2);
            if (columns.Length > 0)
            {
                foreach (var column in columns)
                {
                    _sb.Append(column);
                    _sb.Append(" ");
                }
                _sb.Remove(_sb.Length - 1, 1);
            }
            _sb.AppendLine();
        }
    }
}
