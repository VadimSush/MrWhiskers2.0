using Discord;
using Discord.WebSocket;
using System;
using System.Net.Http.Json;
using System.Threading.Tasks;

class Program
{
    private DiscordSocketClient _client;
    static void Main(string[] args) => new Program().RunBotAsync().GetAwaiter().GetResult();
    private readonly HttpClient _httpClient = new HttpClient();
    public async Task RunBotAsync()
    {
        var config = new DiscordSocketConfig
        {
            GatewayIntents = GatewayIntents.Guilds | GatewayIntents.GuildMessages | GatewayIntents.MessageContent | GatewayIntents.GuildMessageReactions
        };
        _client = new DiscordSocketClient(config);
        _client.Log += Log;
        _client.MessageReceived += MessageReceivedAsync;
        _client.ButtonExecuted += HandleButtonAsync;
        string token = Environment.GetEnvironmentVariable("DISCORD_BOT_TOKEN");
        await _client.LoginAsync(TokenType.Bot, token);
        await _client.StartAsync();
        await Task.Delay(-1);
    }

    private Task Log(LogMessage log)
    {
        Console.WriteLine(log.ToString());
        return Task.CompletedTask;
    }

    private async Task MessageReceivedAsync(SocketMessage message)
    {
        // Ignore messages from the bot itself
        if (message.Author.IsBot) return;

        // Only process messages from text channels (not DMs)
        if (message.Channel is SocketTextChannel textChannel)
        {
            // Respond to specific phrases
            if (message.Content.ToLower().Contains("hello bot"))
            {
                //await textChannel.SendMessageAsync($"G'day, {message.Author.Username}! How's it going?");
                var embed = new EmbedBuilder()
                {
                    Title = "Woof! HR is here to assist you!",
                    //Description = "Hello, frend. What can HR do for you?",
                    Color = Color.Green,
                };
                embed.AddField("Fun Fact", "Did you know I can do many other things too?");
                embed.WithFooter("I'm here to assist you!");
                embed.ThumbnailUrl = _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl();
                embed.WithImageUrl("https://i.imgur.com/RLe6xwo.png");
                await message.Channel.SendMessageAsync(embed: embed.Build());
            }
        }
        if (message.Content.ToLower().Contains("random gif"))
        {
            string gifUrl = await GetRandomGifUrlAsync("funny dog"); // You can change the search term as needed
            if (gifUrl != null)
            {
                await message.Channel.SendMessageAsync(gifUrl);
            }
            else
            {
                await message.Channel.SendMessageAsync("Sorry, I couldn't fetch a GIF at the moment.");
            }
        }
        if (message.Content.ToLower().Contains("hr decision"))
        {
            // Create a randomizer
            var random = new Random();
            bool isYes = random.Next(0, 2) == 0;
            // Use Unicode emojis for "Yes" and "No"
            string answer = isYes ? "\u2705 Yes" : "\u274C No";  // Unicode for ✅ and ❌
            string userName = message.Author.Username;
            // Create an embed with the random answer
            var embed = new EmbedBuilder()
            {
                Title = $"HR has made their decision, {userName}",
                Description = $"The answer is: **{answer}**", // Add the emoji in the response
                Color = isYes ? Color.Green : Color.Red, // Green for "Yes", Red for "No"
                ThumbnailUrl= _client.CurrentUser.GetAvatarUrl() ?? _client.CurrentUser.GetDefaultAvatarUrl(),
                ImageUrl = ("https://i.imgur.com/RLe6xwo.png"),
            };
            // Send the embed message
            await message.Channel.SendMessageAsync(embed: embed.Build());
        }
        if (message.Content.ToLower().Contains("ask bot"))
        {
            // Create a randomizer
            var random = new Random();
            bool isYes = random.Next(0, 2) == 0;
            // Get the username of the person who called the command
            string username = message.Author.Username;
            // Create an embed with a message
            var embed = new EmbedBuilder()
            {
                Title = "Choose Your Answer",
                Description = $"Hey **{username}**, choose one of the options below:",
                Color = Color.Blue
            };
            // Create the buttons
            var builder = new ComponentBuilder()
                .WithButton("Yes", "button_yes", ButtonStyle.Success)
                .WithButton("No", "button_no", ButtonStyle.Danger);
            // Send the embed with buttons
            await message.Channel.SendMessageAsync(embed: embed.Build(), components: builder.Build());
        }
    }
    private async Task HandleButtonAsync(SocketMessageComponent component)
    {
        // Check which button was clicked
        string responseMessage;
        if (component.Data.CustomId == "button_yes")
        {
            responseMessage = "You clicked Yes! ✅";
        }
        else if (component.Data.CustomId == "button_no")
        {
            responseMessage = "You clicked No! ❌";
        }
        else
        {
            return; // In case there's another button, ignore
        }

        // Disable the buttons by creating a new ComponentBuilder
        var builder = new ComponentBuilder()
            .WithButton("Yes", "button_yes", ButtonStyle.Success, disabled: true)
            .WithButton("No", "button_no", ButtonStyle.Danger, disabled: true);

        // Update the original message to disable the buttons
        await component.UpdateAsync(msg =>
        {
            msg.Content = responseMessage; // Optionally update the message content
            msg.Components = builder.Build(); // Disable the buttons
        });
    }

    private async Task<string> GetRandomGifUrlAsync(string searchTerm)
    {
        try
        {
            string apiKey = "eXfOk5CaqSJkx36UWo2M99B74WXKE9lZ"; // Replace with your Giphy API Key
            string giphyEndpoint = $"https://api.giphy.com/v1/gifs/random?api_key={apiKey}&tag={searchTerm}&rating=PG-13";
            var response = await _httpClient.GetFromJsonAsync<GiphyResponse>(giphyEndpoint);
            return response?.Data?.Images?.Original?.Url;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching GIF: {ex.Message}");
            return null;
        }
    }

    // Helper class to deserialize Giphy API response
    public class GiphyResponse
    {
        public GiphyData Data { get; set; }
    }

    public class GiphyData
    {
        public GiphyImage Images { get; set; }
    }

    public class GiphyImage
    {
        public GiphyOriginalImage Original { get; set; }
    }

    public class GiphyOriginalImage
    {
        public string Url { get; set; }
    }

}