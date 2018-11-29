using AngleSharp.Dom.Html;
using AngleSharp.Parser.Html;
using DSharpPlus.Entities;
using log4net;
using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace OpBot
{
    internal class ServerStatusPoll
    {
        private static ILog log = LogManager.GetLogger(typeof(ServerStatusPoll));

        private readonly Uri serverStatusUri = new Uri("https://www.swtor.com/server-status");
        //private readonly Uri serverStatusUri = new Uri("http://localhost:8080/status.html");
        private DiscordChannel _channel;
        private CancellationToken _cancel;
        private HtmlParser _parser;
        private ServerStatus _currentStatus;

        public ServerStatusPoll(CancellationToken cancel)
        {
            _cancel = cancel;
            _parser = new HtmlParser();
        }

        public Task Start(DiscordChannel channel)
        {
            _channel = channel;
            return Task.Run(Poll, _cancel);
        }

        public async Task Poll()
        {
            const int pollPeriod = 60000;

            try
            {
                using (var client = new HttpClient())
                {
                    while (true)
                    {
                        _cancel.ThrowIfCancellationRequested();
                        await PollServer(client);
                        await Task.Delay(pollPeriod, _cancel);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                log.Debug("Server status poll cancelled");
            }
            catch (Exception ex)
            {
                log.Error("Server status poll failed: " + ex.Message);
            }
        }

        private async Task PollServer(HttpClient client)
        {
            log.Debug("Polling Server Status");
            IHtmlDocument document = await GetDocument(client);
            var newStatus = new ServerStatus(document);
            if (_currentStatus == null)
            {
                _currentStatus = newStatus;
            }
            else if (!newStatus.IsSameAs(_currentStatus))
            {
                _currentStatus = newStatus;
                string message = "Server status change detected\n" + _currentStatus.ToString();
                await _channel.SendMessageAsync(message);
            }
        }

        private async Task<IHtmlDocument> GetDocument(HttpClient client)
        {
            try
            {
                var response = await client.GetAsync(serverStatusUri, _cancel);
                var content = await response.Content.ReadAsStringAsync();
                return _parser.Parse(content);
            }
            catch (HttpRequestException ex)
            {
                log.Debug("Server status unavailable: " + ex.Message);
                return null;
            }
        }

    }
}
