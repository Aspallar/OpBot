using DSharpPlus;

namespace OpBot
{
    internal class ErrorEmbed : DiscordEmbed
    {
        public ErrorEmbed(string description)
        {
            Color = 0xad1313;
            Title = "Error";
            Url = Constants.InstrucionUrl;
            Thumbnail = new DiscordEmbedThumbnail()
            {
                Url = "https://raw.githubusercontent.com/wiki/Aspallar/OpBot/images/2-128.png",
            };
            Description = description;
        }
    }
}
