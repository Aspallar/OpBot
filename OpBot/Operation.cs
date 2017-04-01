using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace OpBot
{
    [Serializable]
    internal class Operation
    {
        private string _operationName;
        private int _size;
        private string _mode;
        private List<AltRole> _altRoles;

        public ulong MessageId { get; set; }

        public DateTime Date { get; set; }
        public List<OperationMember> Members { get; set; }
        public List<string> Notes { get; set; }

        public Operation()
        {
            Notes = new List<string>();
            Members = new List<OperationMember>();
            _altRoles = new List<AltRole>();
        }

        public string OperationName
        {
            get
            {
                return _operationName;
            }
            set
            {
                lock (this)
                    _operationName = GetFullName(value);
            }
        }

        public int Size
        {
            get
            {
                return _size;
            }
            set
            {
                if (value != 8 && value != 16)
                    throw new OpBotInvalidValueException("Invalid size, must be 8 or 16");
                lock (this)
                    _size = value;
            }
        }

        public string Mode
        {
            get
            {
                return _mode;
            }
            set
            {
                if (value != "SM" && value != "VM" && value != "MM")
                    throw new OpBotInvalidValueException($"{value} is not a valid operation mode");
                lock (this)
                    _mode = value;
            }
        }

        public void SetSizeFromString(string sizeString)
        {
            int size;
            if (!int.TryParse(sizeString, out size))
                throw new OpBotInvalidValueException("Invalid size, must be 8 or 16");
            Size = size;
        }

        public static string GetFullName(string shortCode)
        {
            switch (shortCode.ToUpperInvariant())
            {
                case "EV": return "Eternity Vault";
                case "KP": return "Karagga's Palace";
                case "EC": return "Explosive Conflict";
                case "TFB": return "Terror From Beyond";
                case "SV": return "Scum and Villainy";
                case "DF": return "The Dread Fortress";
                case "DP": return "The Dread Palace";
                case "RAV": return "The Ravagers";
                case "TOS": return "Temple of Sacrifice";
                default:
                    throw new OpBotInvalidValueException($"Unknown operation name '{shortCode}'");
            }
        }

        public void Signup(ulong userId, string name, string role)
        {
            lock (this)
            {
                var member = Members.Where(m => m.UserId == userId).SingleOrDefault();
                if (member == null)
                {
                    member = new OperationMember();
                    Members.Add(member);
                }
                member.UserId = userId;
                member.UserName = name;
                member.PrimaryRole = role.ToUpperInvariant();
            }
        }

        public void Remove(ulong userId)
        {
            lock (this)
            {
                Members.RemoveAll(m => m.UserId == userId);
            }
        }

        public string GetOperationMessageText()
        {
            DateTime baseTime = Date.IsDaylightSavingTime() ? Date.AddHours(1) : Date;
            string text = $"**{OperationName}** {Size}-man {Mode}\n{Date.ToString("dddd")} {Date.ToLongDateString()} {Date.ToShortTimeString()} (UTC)\n";
            text += "  *" + baseTime.ToShortTimeString() + " Western Europe (UK)*\n";
            text += "  *" + baseTime.AddHours(1).ToShortTimeString() + " Central Europe (Belgium)*\n";
            text += "  *" + baseTime.AddHours(2).ToShortTimeString() + " Eastern Europe (Estonia)*\n";
            text += "```";
            text += "Tanks:\n";
            text += Roles("TANK");
            text += "Damage:\n";
            text += Roles("DPS");
            text += "Healers:\n";
            text += Roles("HEAL");
            text += "```";
            if (_altRoles.Count > 0)
            {
                text += $"\nAlternative/Reserve Roles {AltRoles()}";
            }
            if (Notes.Count > 0)
                text += "\n";
            foreach (string note in Notes)
            {
                text += note + "\n";
            }
            return text;
        }

        public void SetAltRoles(string username, ulong userid, string[] roles)
        {
            lock (this)
            {
                AltRole altRole = _altRoles.Where(x => x.UserId == userid).SingleOrDefault();
                if (altRole == null)
                {
                    altRole = new AltRole(username, userid);
                    _altRoles.Add(altRole);
                }
                altRole.Set(roles);
                if (!altRole.HasAnyRole)
                    _altRoles.Remove(altRole);
            }
        }

        private string Roles(string primaryRole)
        {
            int count = 0;
            string text = "";
            var roleMembers = Members.Where(m => m.PrimaryRole == primaryRole).ToList();
            foreach (var member in roleMembers)
            {
                ++count;
                text += $"    {count.ToString()}. {member.UserName}\n";
            }
            return text;
        }

        private string AltRoles()
        {
            int padding = _altRoles.Max(x => x.Name.Length) + 1;
            StringBuilder sb = new StringBuilder(1024);
            sb.Append("```");
            foreach (AltRole role in _altRoles)
            {
                sb.Append(role.Name.PadRight(padding));
                sb.Append(' ');
                sb.Append(role.ToString());
            }
            sb.AppendLine("```");
            return sb.ToString();
        }

    }
}
