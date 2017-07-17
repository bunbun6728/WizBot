﻿using System.Net.Http;
using System.Threading.Tasks;
using WizBot.Common;
using WizBot.Extensions;
using WizBot.Services;
using Newtonsoft.Json;

namespace WizBot.Modules.Games.Common
{
    public class ChatterBotSession
    {
        private static WizBotRandom rng { get; } = new WizBotRandom();
        public string ChatterbotId { get; }
        public string ChannelId { get; }
#if GLOBAL_WIZBOT
        private int _botId = 1;
#else
        private int _botId = 6;
#endif

        public ChatterBotSession(ulong channelId)
        {
            ChannelId = channelId.ToString().ToBase64();
            ChatterbotId = rng.Next(0, 1000000).ToString().ToBase64();
        }

#if GLOBAL_WIZBOT
        private string apiEndpoint => "http://wizbot.xyz/cb/chatbot/" +
                                      $"?bot_id={_botId}&" +
                                      "say={0}&" +
                                      $"convo_id=wizbot_{ChatterbotId}_{ChannelId}&" +
                                      "format=json";
#else
        private string apiEndpoint => "http://api.program-o.com/v2/chatbot/" +
                                      $"?bot_id={_botId}&" +
                                      "say={0}&" +
                                      $"convo_id=WizBot_{ChatterbotId}_{ChannelId}&" +
                                      "format=json";
#endif

        public async Task<string> Think(string message)
        {
            using (var http = new HttpClient())
            {
                var res = await http.GetStringAsync(string.Format(apiEndpoint, message)).ConfigureAwait(false);
                var cbr = JsonConvert.DeserializeObject<ChatterBotResponse>(res);
                return cbr.BotSay.Replace("<br/>", "\n");
            }
        }
    }
}