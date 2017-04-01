using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace OpBot
{
    [Serializable]
    internal class AltRole
    {
        public string Name { get; private set; }
        public ulong UserId { get; private set; }
        public bool Tank { get; private set; }
        public bool Dps { get; private set; }
        public bool Heal { get; private set; }

        public AltRole(string username, ulong userId)
        {
            Name = username;
            UserId = userId;
            ResetRoles();
        }

        public void Set(string[] roles)
        {
            bool isTank = false;
            bool isDps = false;
            bool isHeal = false;

            for (int k = 1; k < roles.Length; k++)
            {
                string role = roles[k].ToUpperInvariant();
                switch (role)
                {
                    case "TANK":
                        isTank = true;
                        break;
                    case "DPS":
                        isDps = true;
                        break;
                    case "HEAL":
                        isHeal = true;
                        break;
                    case "*":
                    case "ALL":
                        isTank = isDps = isHeal = true;
                        break;
                    default:
                        throw new OpBotInvalidValueException($"Invalid role {role}.");
                }
            }

            Tank = isTank;
            Dps = isDps;
            Heal = isHeal;
        }

        public bool HasAnyRole
        {
            get
            {
                return Tank | Dps | Heal;
            }
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(14);
            if (Tank)
                sb.Append("TANK ");
            if (Dps)
                sb.Append("DPS  ");
            if (Heal)
                sb.Append("HEAL");
            if (sb.Length > 0 && sb[sb.Length - 1] == ' ')
                sb.Length--;
            sb.Append('\n');
            return sb.ToString();
        }

        private void ResetRoles()
        {
            Tank = Dps = Heal = false;
        }

    }
}
