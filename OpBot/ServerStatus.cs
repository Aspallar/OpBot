using System;
using AngleSharp.Dom.Html;
using System.Collections.Generic;
using System.Text;

namespace OpBot
{
    internal class ServerStatus
    {
        private Dictionary<string, string> _serverStatus = new Dictionary<string, string>();

        private bool _swtorAvailable;
        private bool _hasStatus;

        public ServerStatus(IHtmlDocument document)
        {
            if (document != null)
            {
                _swtorAvailable = true;
                var statusRows = document.QuerySelectorAll(".serverBody.row");
                if (statusRows.Length > 0)
                {
                    _hasStatus = true;
                    foreach (var row in statusRows)
                    {
                        string status = row.GetAttribute("data-status");
                        string name = row.QuerySelector("div.name").InnerHtml;
                        _serverStatus[name] = status;
                    }
                }
                else
                {
                    _hasStatus = false;
                }
            }
            else
            {
                _swtorAvailable = false;
            }
        }

        public override string ToString()
        {
            if (!_swtorAvailable)
                return "Server status website unreachable.";

            if (!_hasStatus)
                return "Server status is not available.";

            var text = new StringBuilder();
            foreach (var server in _serverStatus)
            {
                text.Append(server.Value == "UP" ? ":green_apple: " : ":apple: ");
                text.Append(server.Key);
                text.Append(' ');
                text.AppendLine(server.Value);

            }
            return text.ToString();
        }

        internal bool IsSameAs(ServerStatus other)
        {
            if (other._swtorAvailable != _swtorAvailable
                || other._hasStatus != _hasStatus
                || other._serverStatus.Count != _serverStatus.Count)
            {
                    return false;
            }

            foreach (var others in other._serverStatus)
            {
                if (!_serverStatus.ContainsKey(others.Key) || _serverStatus[others.Key] != others.Value)
                    return false;
            }

            return true;
        }
    }
}