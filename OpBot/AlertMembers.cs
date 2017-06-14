using System;

namespace OpBot
{
    internal class AlertMembers
    {
        public enum AlertStates
        {
            On, Off
        };

        internal AlertStates Toggle(ulong iD)
        {
            return AlertStates.On;
        }
    }
}