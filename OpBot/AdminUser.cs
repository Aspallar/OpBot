using System.Collections.Generic;
using System.Collections.Specialized;

namespace OpBot
{
    internal class AdminUsers : IAdminUser
    {
        List<ulong> _adminUsers;

        public AdminUsers(StringCollection userIds)
        {
            _adminUsers = new List<ulong>();
            if (userIds != null)
            {
                foreach (string userIdString in userIds)
                {
                    _adminUsers.Add(ulong.Parse(userIdString));
                }
            }
        }

        public bool IsAdmin(ulong userId)
        {
            return _adminUsers.Contains(userId);
        }
    }
}
