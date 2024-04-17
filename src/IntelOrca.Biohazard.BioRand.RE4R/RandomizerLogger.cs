using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace IntelOrca.Biohazard.BioRand.RE4R
{
    internal class RandomizerLogger
    {
        private readonly StringBuilder _sb = new StringBuilder();
        private readonly string _hr = new string('-', 80);
        private readonly Stack<string> _stack = new Stack<string>();

        public string Output => _sb.ToString();

        public void Push(string header)
        {
            LogLine(header);
            _stack.Push(header);
        }

        public void Pop()
        {
            _stack.Pop();
        }

        public void LogVersion()
        {
            var crf = ChainsawRandomizerFactory.Default;

            _sb.AppendLine(crf.CurrentVersionInfo);
            _sb.AppendLine("by IntelOrca");
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
            _sb.Append(' ', _stack.Count * 2);
            _sb.AppendLine(line);
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
            var weapons = "";
            foreach (var w in new[] { enemy.Weapon, enemy.SecondaryWeapon })
            {
                if (w != 0)
                {
                    var ecf = EnemyClassFactory.Default;
                    var weaponDef = ecf.Weapons.FirstOrDefault(x => x.Id == w);
                    if (weaponDef != null)
                    {
                        if (weapons.Length != 0)
                            weapons += " | ";
                        weapons += weaponDef.Key;
                    }
                }
            }

            var itemDrop = ".";
            if (enemy.ItemDrop is Item drop)
            {
                itemDrop = "*";
                if (!drop.IsAutomatic)
                {
                    var itemRepo = ItemDefinitionRepository.Default;
                    var itemDef = itemRepo.Find(drop.Id);
                    if (itemDef != null)
                    {
                        itemDrop = itemDef.Name ?? itemDef.Id.ToString();
                        itemDrop += $" x{drop.Count}";
                    }
                }
            }

            var parasite = "";
            if ((enemy.ParasiteKind ?? 0) != 0)
            {
                if (enemy.ParasiteKind == 1)
                    parasite = "pA(";
                else if (enemy.ParasiteKind == 2)
                    parasite = "pB(";
                else if (enemy.ParasiteKind == 3)
                    parasite = "pC(";
                if (enemy.ForceParasiteAppearance)
                    parasite += "100%)";
                else
                    parasite += $"{enemy.ParasiteAppearanceProbability}%)";
            }

            LogLine(enemy.Guid, enemy.Kind.Key, weapons, enemy.Health?.ToString() ?? "*", parasite, itemDrop);
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
