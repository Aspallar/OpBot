using System;
using System.Net;
using System.Threading.Tasks;

namespace OpBot
{
    internal class SwtorAvailableQuery : IDisposable
    {
        private Uri _uri;
        private WebClient _client;

        public SwtorAvailableQuery(string url)
        {
            _uri = new Uri(url);
            _client = new WebClient();
        }

        public async Task<bool> ServersAvailable()
        {
            const string tweetContainer = "js-tweet-text-container";

            try
            {
                string page = await _client.DownloadStringTaskAsync(_uri);
                int startPos = page.IndexOf(tweetContainer) + tweetContainer.Length;
                int unavailablePos = page.IndexOf("unavailable", startPos);
                int availablePos = page.IndexOf("available", startPos);
                if (availablePos == -1 && unavailablePos == -1)
                    return true;
                else
                    return availablePos != -1 && availablePos <= unavailablePos;
            }
            catch (WebException)
            {
                return false;
            }
        }

        public void Dispose()
        {
            if (_client != null)
            {
                _client.Dispose();
                _client = null;
            }
        }
    }
}
