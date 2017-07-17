﻿using AngleSharp;
using Discord.Commands;
using WizBot.Extensions;
using WizBot.Modules.Searches.Services;
using WizBot.Services;
using Newtonsoft.Json.Linq;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using WizBot.Common;
using WizBot.Common.Attributes;

namespace WizBot.Modules.Searches
{
    public partial class Searches
    {
        [Group]
        public class JokeCommands : WizBotSubmodule<SearchesService>
        {

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
                    http.AddFakeHeaders();

                    var config = Configuration.Default.WithDefaultLoader();
                    var document = await BrowsingContext.New(config).OpenAsync("http://www.goodbadjokes.com/random");

                    var html = document.QuerySelector(".post > .joke-content");

                    var part1 = html.QuerySelector("dt").TextContent;
                    var part2 = html.QuerySelector("dd").TextContent;

                    await Context.Channel.SendConfirmAsync("", part1 + "\n\n" + part2, footer: document.BaseUri).ConfigureAwait(false);
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
                if (!_service.WowJokes.Any())
                {
                    await ReplyErrorLocalized("jokes_not_loaded").ConfigureAwait(false);
                    return;
                }
                var joke = _service.WowJokes[new WizBotRandom().Next(0, _service.WowJokes.Count)];
                await Context.Channel.SendConfirmAsync(joke.Question, joke.Answer).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task MagicItem()
            {
                if (!_service.WowJokes.Any())
                {
                    await ReplyErrorLocalized("magicitems_not_loaded").ConfigureAwait(false);
                    return;
                }
                var item = _service.MagicItems[new WizBotRandom().Next(0, _service.MagicItems.Count)];

                await Context.Channel.SendConfirmAsync("✨" + item.Name, item.Description).ConfigureAwait(false);
            }
        }
    }
}