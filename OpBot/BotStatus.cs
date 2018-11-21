using System;
using DSharpPlus;
using System.Threading.Tasks;
using DSharpPlus.Entities;
using System.Collections.Generic;
using System.Threading;

namespace OpBot
{
    internal class BotStatus
    {
        private DiscordClient _client;
        private List<DiscordGame> _games;
        private DiscordGame _starWars;
        private Timer _timer;
        private Random _rand;
        private bool _isRandomStatus;

        public BotStatus(DiscordClient client)
        {
            _client = client;
            _starWars = new DiscordGame("Star Wars: The Old Republic");
            _games = new List<DiscordGame>()
            {
                new DiscordGame("Dejarik"),
                new DiscordGame("Letting the wookie win"),
                new DiscordGame("Chatta-ragul"),
                new DiscordGame("with a Joom-ball"),
                new DiscordGame("Theed Quoits"),
                new DiscordGame("the growdi harmonique"),
                new DiscordGame("the violin"),
                new DiscordGame("with Nen's lightsaber"),
                new DiscordGame("Star Wars Galaxies"),
                new DiscordGame("World of Warcraft"),
                new DiscordGame("with HK-51"),
            };
            _rand = new Random();
            _isRandomStatus = true;
        }

        internal void Start()
        {
            _timer = new Timer(async x => await ChangeStatus(), null, 0, 1800000);
        }

        private async Task ChangeStatus()
        {
            DiscordGame game = _isRandomStatus ? _starWars : _games[_rand.Next(_games.Count)];
            _isRandomStatus = !_isRandomStatus;
            await _client.UpdateStatusAsync(game);
        }
    }
}