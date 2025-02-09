﻿#nullable disable
using Newtonsoft.Json;

namespace WizBot.Modules.Games.Common.ChatterBot;

public class ChatterBotSession : IChatterBotSession
{
    private static WizBotRandom Rng { get; } = new();

    private string ApiEndpoint
        => "http://api.program-o.com/v2/chatbot/"
           + $"?bot_id={_botId}&"
           + "say={0}&"
           + $"convo_id=wizbot_{_chatterBotId}&"
           + "format=json";

    private readonly string _chatterBotId;
    private readonly IHttpClientFactory _httpFactory;
    private readonly int _botId = 6;

    public ChatterBotSession(IHttpClientFactory httpFactory)
    {
        _chatterBotId = Rng.Next(0, 1000000).ToString().ToBase64();
        _httpFactory = httpFactory;
    }

    public async Task<string> Think(string message)
    {
        using var http = _httpFactory.CreateClient();
        var res = await http.GetStringAsync(string.Format(ApiEndpoint, message));
        var cbr = JsonConvert.DeserializeObject<ChatterBotResponse>(res);
        return cbr.BotSay.Replace("<br/>", "\n", StringComparison.InvariantCulture);
    }
}