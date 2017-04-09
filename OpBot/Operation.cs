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
        private List<string> _notes;
        private List<OperationMember> _members;
        private ulong _messageId;
        private DateTime _date;

        private static List<OperationDesc> _operationInfo = new List<OperationDesc>()
        {
            new OperationDesc() { ShortCode="EV", FullName = "Eternity Vault" },
            new OperationDesc() { ShortCode="KP", FullName = "Karagga's Palace" },
            new OperationDesc() { ShortCode="EC", FullName = "Explosive Conflict" },
            new OperationDesc() { ShortCode="TFB", FullName = "Terror from Beyond" },
            new OperationDesc() { ShortCode="SV", FullName = "Scum and Villainy" },
            new OperationDesc() { ShortCode="DF", FullName = "The Dread Fortress" },
            new OperationDesc() { ShortCode="DP", FullName = "The Dread Palace" },
            new OperationDesc() { ShortCode="RAV", FullName = "The Ravagers" },
            new OperationDesc() { ShortCode="TOS", FullName = "Temple of Sacrifice" },
            new OperationDesc() { ShortCode="TY", FullName = "Tyth" },
            new OperationDesc() { ShortCode="WB", FullName = "World Boss" },
            new OperationDesc() { ShortCode="TC", FullName = "Toborro's Courtyard" },
            new OperationDesc() { ShortCode="XEN", FullName = "Xenoanalyst II" },
            new OperationDesc() { ShortCode="CM", FullName = "Colossal Monolith" },
            new OperationDesc() { ShortCode="EYE", FullName = "The Eyeless" },
            new OperationDesc() { ShortCode="OTH", FullName = "Other" },
        };

        public Operation()
        {
            _members = new List<OperationMember>();
            _altRoles = new List<AltRole>();
            _notes = new List<string>();
        }

        public ulong MessageId
        {
            get
            {
                return _messageId;
            }
            set
            {
                lock (this) _messageId = value;
            }
        }

        public DateTime Date
        {
            get
            {
                return _date;
            }
            set
            {
                lock (this) _date = value;
            }
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
            OperationDesc op = _operationInfo.Where(x => x.ShortCode == shortCode).SingleOrDefault();
            if (op == null)
                throw new OpBotInvalidValueException($"Unknown operation '{shortCode}'");
            return op.FullName;
        }

        public static bool IsValidOperationCode(string code)
        {
            return _operationInfo.Any(x => x.ShortCode == code);
        }

        public static bool IsValidOperationMode(string mode)
        {
            return mode == "SM" || mode == "VM" || mode == "MM";
        }

        public static bool IsValidSize(string size)
        {
            return size == "8" || size == "16";
        }

        public void Signup(ulong userId, string name, string role)
        {
            lock (this)
            {
                var member = _members.Where(m => m.UserId == userId).SingleOrDefault();
                if (member == null)
                {
                    member = new OperationMember();
                    _members.Add(member);
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
                _members.RemoveAll(m => m.UserId == userId);
            }
        }

        public void AddNote(string note)
        {
            lock (this)
            {
                _notes.Add(note);
            }
        }

        public void DeleteNote(int noteIndex)
        {
            lock (this)
            {
                _notes.RemoveAt(noteIndex);
            }
        }

        public void ResetNotes()
        {
            lock (this)
            {
                _notes = new List<string>();
            }
        }

        public int NoteCount => _notes.Count;

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

        public string GetOperationMessageText()
        {
            StringBuilder sb = new StringBuilder(1024);
            DateTime baseTime = _date.IsDaylightSavingTime() ? _date.AddHours(1) : _date;

            sb.Append("**");
            sb.Append(OperationName);
            sb.Append("** ");
            sb.Append(Size);
            sb.Append("-man ");
            sb.AppendLine(Mode);

            sb.Append(_date.ToLongDateString());
            sb.Append(' ');
            sb.Append(_date.ToShortTimeString());
            sb.AppendLine(" (UTC)");

            sb.Append("  *");
            sb.Append(baseTime.ToShortTimeString());
            sb.AppendLine(" Western Europe (UK)*");

            sb.Append("  *");
            sb.Append(baseTime.AddHours(1).ToShortTimeString());
            sb.AppendLine(" Central Europe (Belgium)*");

            sb.Append("  *");
            sb.Append(baseTime.AddHours(2).ToShortTimeString());
            sb.AppendLine(" Eastern Europe (Estonia)*");

            sb.Append(DiscordText.CodeBlock);
            sb.AppendLine("Tanks:");
            sb.Append(RolesToString("TANK"));
            sb.AppendLine("Damage:");
            sb.Append(RolesToString("DPS"));
            sb.AppendLine("Healers:");
            sb.Append(RolesToString("HEAL"));
            sb.Append(DiscordText.CodeBlock);

            if (_altRoles.Count > 0)
            {
                sb.Append("\nAlternative/Reserve Roles ");
                sb.Append(AltRolesToString());
            }

            if (_notes.Count > 0)
            {
                sb.AppendLine();
                foreach (string note in _notes)
                    sb.AppendLine(note);
            }

            return sb.ToString();
        }

        private string RolesToString(string primaryRole)
        {
            StringBuilder sb = new StringBuilder(512);
            int count = 0;
            var roleMembers = _members.Where(m => m.PrimaryRole == primaryRole).ToList();
            foreach (var member in roleMembers)
            {
                sb.Append("    ");
                sb.Append(++count);
                sb.Append(". ");
                sb.AppendLine(member.UserName);
            }
            return sb.ToString();
        }

        private string AltRolesToString()
        {
            int padding = _altRoles.Max(x => x.Name.Length) + 1;
            StringBuilder sb = new StringBuilder(512);
            sb.Append(DiscordText.CodeBlock);
            foreach (AltRole role in _altRoles)
            {
                sb.Append(role.Name.PadRight(padding));
                sb.Append(' ');
                sb.Append(role.ToString());
            }
            sb.AppendLine(DiscordText.CodeBlock);
            return sb.ToString();
        }

    }
}
