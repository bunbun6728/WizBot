﻿using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Modules.Searches.Models;
using WizBot.Services;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NLog;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class JokeCommands : WizBotSubmodule
        {
            private static List<WoWJoke> wowJokes { get; } = new List<WoWJoke>();
            private static List<MagicItem> magicItems { get; } = new List<MagicItem>();
            private new static readonly Logger _log;

            static JokeCommands()
            {
                _log = LogManager.GetCurrentClassLogger();

                if (File.Exists("data/wowjokes.json"))
                {
                    wowJokes = JsonConvert.DeserializeObject<List<WoWJoke>>(File.ReadAllText("data/wowjokes.json"));
                }
                else
                    _log.Warn("data/wowjokes.json is missing. WOW Jokes are not loaded.");

                if (File.Exists("data/magicitems.json"))
                {
                    magicItems = JsonConvert.DeserializeObject<List<MagicItem>>(File.ReadAllText("data/magicitems.json"));
                }
                else
                    _log.Warn("data/magicitems.json is missing. Magic items are not loaded.");
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Yomama()
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.yomomma.info/").ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(JObject.Parse(response)["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task Randjoke()
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://tambal.azurewebsites.net/joke/random").ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(JObject.Parse(response)["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task ChuckNorris()
            {
                using (var http = new HttpClient())
                {
                    var response = await http.GetStringAsync("http://api.icndb.com/jokes/random/").ConfigureAwait(false);
                    await Context.Channel.SendConfirmAsync(JObject.Parse(response)["value"]["joke"].ToString() + " 😆").ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task WowJoke()
            {
                if (!wowJokes.Any())
                {
                    await ReplyErrorLocalized("jokes_not_loaded").ConfigureAwait(false);
                    return;
                }
                var joke = wowJokes[new WizBotRandom().Next(0, wowJokes.Count)];
                await Context.Channel.SendConfirmAsync(joke.Question, joke.Answer).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task MagicItem()
            {
                if (!wowJokes.Any())
                {
                    await ReplyErrorLocalized("magicitems_not_loaded").ConfigureAwait(false);
                    return;
                }
                var item = magicItems[new WizBotRandom().Next(0, magicItems.Count)];

                await Context.Channel.SendConfirmAsync("✨" + item.Name, item.Description).ConfigureAwait(false);
            }
        }
    }
}