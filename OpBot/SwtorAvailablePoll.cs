using System;
using DSharpPlus;
using System.Threading.Tasks;
using System.Threading;
using DSharpPlus.Entities;

namespace OpBot
{
    internal class SwtorAvailablePoll : IDisposable
    {
        private Task _task;
        private DiscordChannel _channel;
        private CancellationTokenSource _cancelSource;
        private CancellationToken _cancel;

        internal void Start(DiscordChannel channel)
        {
            _cancelSource = new CancellationTokenSource();
            _cancel = _cancelSource.Token;
            _channel = channel;
            _task = Task.Run(Poll, _cancel);
        }

        internal Task Stop()
        {
            _cancelSource.Cancel();
            return _task;
        }

        private async Task Poll()
        {
#if DEBUG
            const int expireTime = 5; // minute
#else
            const int expireTime = 1440; // 24 hhrs in minutes
#endif
            DateTime startTime = DateTime.Now;
            bool available = false;
            bool expired = false;

            using (SwtorAvailableQuery query = new SwtorAvailableQuery("https://twitter.com/SWTOR"))
            {
                while (true)
                {
                    if (_cancel.IsCancellationRequested)
                        break; // while (true)

                    if ((DateTime.Now - startTime).TotalMinutes >= expireTime)
                    {
                        expired = true;
                        break; // while (true)
                    }

                    available = await query.ServersAvailable();

                    if (available)
                        break; // while (true)

                    await Task.Delay(60000, _cancel);
                }
            }

            if (!_cancel.IsCancellationRequested && (available || expired))
                _serversAvailable?.InvokeAsync(new ServersAvailableEventArgs(_channel, expired));
        }

        public void Dispose()
        {
            if (_cancelSource != null)
            {
                _cancelSource.Dispose();
                _cancelSource = null;
            }
        }

        public event AsyncEventHandler<ServersAvailableEventArgs> ServersAvailable
        {
            add { this._serversAvailable.Register(value); }
            remove { this._serversAvailable.Unregister(value); }
        }
        private AsyncEvent<ServersAvailableEventArgs> _serversAvailable = new AsyncEvent<ServersAvailableEventArgs>();
    }
}