using DSharpPlus;
using System;
using System.Threading.Tasks;
using AngleSharp.Parser.Html;
using System.Net;
using System.Collections.Generic;
using System.IO;
using AngleSharp.Dom.Html;
using System.Threading;
using AngleSharp.Dom;

namespace OpBot
{
    internal class DevTracker : IDisposable
    {
        private const string saveFileName = "devtracker.csv";
        private const string swtorUrlBase = "http://www.swtor.com/community/";
        private const string devTrackerUrl = swtorUrlBase + "devtracker.php";

        private Task _task;
        private CancellationTokenSource _cancelSource;
        private CancellationToken _cancel;
        private HashSet<string> _processedPosts;
        private DiscordChannel _channel;
        private readonly char[] _unwantedTitleEndings = new char[] { ' ', '|' };

        public void Start(DiscordChannel channel)
        {
            _channel = channel;
            _processedPosts = Load();
            _cancelSource = new CancellationTokenSource();
            _cancel = _cancelSource.Token;
            _task = Task.Run(Poll, _cancel);
        }

        internal Task Stop()
        {
            _cancelSource.Cancel();
            return _task;
        }

        public async Task Poll()
        {
#if DEBUG
            const int pollPeriod = 90000;
#else
            const int pollPeriod = 600000;
#endif
            while (true)
            {
                if (_cancel.IsCancellationRequested)
                    break; // while true
                await ProcessPosts();
                await Task.Delay(pollPeriod, _cancel);
            }
        }

        private async Task ProcessPosts()
        {
            IHtmlDocument document = await GetDocument();

            if (document == null)
                return;

            bool isAtLeastOneNewPost = false;
            HashSet<string> postKeys = new HashSet<string>();
            var posts = document.QuerySelectorAll("table.threadPost");

            for (int k = posts.Length - 1; k >= 0; k--)
            {
                var dates = posts[k].QuerySelectorAll("span.threadDate");

                if (dates.Length != 2)
                    continue; // for k

                string relativeUrl = GetPostUrl(dates[0]);
                string key = GetPostKey(relativeUrl);

                postKeys.Add(key);
                if (_processedPosts.Contains(key))
                    continue; // for k

                isAtLeastOneNewPost = true;

                DiscordEmbed embed = new DiscordEmbed()
                {
                    Title = dates[0].TextContent.TrimStart().TrimEnd(_unwantedTitleEndings),
                    Url = swtorUrlBase + relativeUrl,
                    Description = dates[1].TextContent.TrimEnd(),
                };

                await _channel.SendMessage("", embed: embed);

                if (k != 0)
                    await Task.Delay(2000);
            }
            if (isAtLeastOneNewPost)
            {
                _processedPosts = postKeys;
                await Save(postKeys);
            }
        }

        private static string GetPostKey(string relativeUrl)
        {
            return relativeUrl.Substring(relativeUrl.IndexOf('?') + 1);
        }

        private static string GetPostUrl(IElement urlElement)
        {
            var anchor = urlElement.QuerySelector("a");
            string relativeUrl = anchor.GetAttribute("href");
            return relativeUrl;
        }

        private async Task<IHtmlDocument> GetDocument()
        {
            IHtmlDocument document = null;
            using (WebClient client = new WebClient())
            {
                try
                {
                    string page = await client.DownloadStringTaskAsync(devTrackerUrl);
                    HtmlParser parser = new HtmlParser();
                    document = parser.Parse(page);
                }
                catch (WebException ex)
                {
                    Console.WriteLine($"Warning: Unable to access dev tracker.\n{ex.ToString()}");
                }
            }
            return document;
        }

        private static async Task Save(HashSet<string> keys)
        {
            using (StreamWriter writer = new StreamWriter(saveFileName))
            {
                foreach (string key in keys)
                    await writer.WriteLineAsync(key);
            }
        }

        private static HashSet<string> Load()
        {
            HashSet<string> keys = new HashSet<string>();
            try
            {
                using (StreamReader reader = new StreamReader(saveFileName))
                {
                    string key;
                    while ((key = reader.ReadLine()) != null)
                        keys.Add(key);
                }
            }
            catch (FileNotFoundException)
            {
            }
            return keys;
        }

        public void Dispose()
        {
            if (_cancelSource != null)
            {
                _cancelSource.Dispose();
                _cancelSource = null;
            }
        }
    }
}
