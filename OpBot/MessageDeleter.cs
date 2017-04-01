using DSharpPlus;
using System;
using System.Collections.Generic;
using System.Threading;

namespace OpBot
{
    internal class MessageDeleter : IDisposable
    {
        private SortedList<DateTime, DiscordMessage> _messageList;
        private Timer _timer;

        public MessageDeleter()
        {
            _messageList = new SortedList<DateTime, DiscordMessage>();
            _timer = new Timer(x => DeleteMessage(), null, Timeout.Infinite, Timeout.Infinite);
        }

        public void AddMessage(DiscordMessage message, int lifetime)
        {
            lock (this)
            {
                DateTime due = DateTime.Now.AddMilliseconds(lifetime);
                _messageList.Add(due, message);
                SetTimer();
            }
        }

        public void Dispose()
        {
            lock (this)
            {
                if (_timer != null)
                {
                    _timer.Dispose();
                    _timer = null;
                }
            }
        }
         
        private void DeleteMessage()
        {
            DiscordMessage message = null;
            lock (this)
            {
                if (_messageList.Count > 0)
                {
                    message = _messageList.Values[0];
                    _messageList.RemoveAt(0);
                    SetTimer();
                }
            }
            if (message != null)
            {
                try
                {
                    message.Delete().GetAwaiter().GetResult();
                }
                catch (NotFoundException)
                {
                    // it is not an error if message not found
                    // as it may have been manually deleted
                }
            }
        }

        private void SetTimer()
        {
            if (_timer == null)
            {
                // we have been disposed, do nothing
                return;
            }

            if (_messageList.Count == 0)
            {
                _timer.Change(Timeout.Infinite, Timeout.Infinite);  // disable timer
                return;
            }

            DateTime due = _messageList.Keys[0];
            TimeSpan delta = due - DateTime.Now;
            int delay = Convert.ToInt32(delta.TotalMilliseconds);
            if (delay < 0)
                delay = 0;
            _timer.Change(delay, Timeout.Infinite);
        }
    }
}
