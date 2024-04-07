using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class RandomizerLogger
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly string _hr = new string('-', 80);

        public string Output => _sb.ToString();

        public void LogHr()
        {
            _sb.AppendLine(_hr);
        }

        public void LogArea(Area area)
        {
            _sb.AppendLine();
            LogHr();
            _sb.AppendLine(area.FileName);
            LogHr();
        }

        public void LogEnemy(Enemy enemy)
        {
            LogLine(enemy.Guid, enemy.Kind.Name, enemy.Health?.ToString() ?? "*");
        }

        private void LogLine(params object[] columns)
        {
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
