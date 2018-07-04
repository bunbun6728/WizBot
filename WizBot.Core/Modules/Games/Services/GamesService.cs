using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Discord;
using Discord.WebSocket;
using WizBot.Common;
using WizBot.Common.Collections;
using WizBot.Extensions;
using WizBot.Modules.Games.Common;
using WizBot.Core.Services;
using WizBot.Core.Services.Impl;
using Newtonsoft.Json;
using NLog;
using WizBot.Modules.Games.Common.Acrophobia;
using WizBot.Modules.Games.Common.Hangman;
using WizBot.Modules.Games.Common.Trivia;
using WizBot.Modules.Games.Common.Nunchi;

namespace WizBot.Modules.Games.Services
{
    public class GamesService : INService, IUnloadableService
    {
        private readonly IBotConfigProvider _bc;

        public ConcurrentDictionary<ulong, GirlRating> GirlRatings { get; } = new ConcurrentDictionary<ulong, GirlRating>();

        public ImmutableArray<string> EightBallResponses { get; }

        private readonly Timer _t;
        private readonly CommandHandler _cmd;
        private readonly WizBotStrings _strings;
        private readonly IImageCache _images;
        private readonly Logger _log;
        private readonly WizBotRandom _rng;
        private readonly ICurrencyService _cs;
        public string TypingArticlesPath { get; } = "data/typing_articles3.json";
        private readonly CommandHandler _cmdHandler;

        public List<TypingArticle> TypingArticles { get; } = new List<TypingArticle>();

        //channelId, game
        public ConcurrentDictionary<ulong, AcrophobiaGame> AcrophobiaGames { get; } = new ConcurrentDictionary<ulong, AcrophobiaGame>();

        public ConcurrentDictionary<ulong, Hangman> HangmanGames { get; } = new ConcurrentDictionary<ulong, Hangman>();
        public TermPool TermPool { get; } = new TermPool();

        public ConcurrentDictionary<ulong, TriviaGame> RunningTrivias { get; } = new ConcurrentDictionary<ulong, TriviaGame>();
        public Dictionary<ulong, TicTacToe> TicTacToeGames { get; } = new Dictionary<ulong, TicTacToe>();
        public ConcurrentDictionary<ulong, TypingGame> RunningContests { get; } = new ConcurrentDictionary<ulong, TypingGame>();
        public ConcurrentDictionary<ulong, NunchiGame> NunchiGames { get; } = new ConcurrentDictionary<ulong, Common.Nunchi.NunchiGame>();

        public GamesService(CommandHandler cmd, IBotConfigProvider bc, WizBot bot,
            WizBotStrings strings, IDataCache data, CommandHandler cmdHandler,
            ICurrencyService cs)
        {
            _bc = bc;
            _cmd = cmd;
            _strings = strings;
            _images = data.LocalImages;
            _cmdHandler = cmdHandler;
            _log = LogManager.GetCurrentClassLogger();
            _rng = new WizBotRandom();
            _cs = cs;

            //8ball
            EightBallResponses = _bc.BotConfig.EightBallResponses.Select(ebr => ebr.Text).ToImmutableArray();

            //girl ratings
            _t = new Timer((_) =>
            {
                GirlRatings.Clear();

            }, null, TimeSpan.FromDays(1), TimeSpan.FromDays(1));

            //plantpick
            _cmd.OnMessageNoTrigger += PotentialFlowerGeneration;
            GenerationChannels = new ConcurrentHashSet<ulong>(bot
                .AllGuildConfigs
                .SelectMany(c => c.GenerateCurrencyChannelIds.Select(obj => obj.ChannelId)));

            try
            {
                TypingArticles = JsonConvert.DeserializeObject<List<TypingArticle>>(File.ReadAllText(TypingArticlesPath));
            }
            catch (Exception ex)
            {
                _log.Warn("Error while loading typing articles {0}", ex.ToString());
                TypingArticles = new List<TypingArticle>();
            }
        }

        public async Task Unload()
        {
            _t.Change(Timeout.Infinite, Timeout.Infinite);
            _cmd.OnMessageNoTrigger -= PotentialFlowerGeneration;

            AcrophobiaGames.ForEach(x => x.Value.Dispose());
            AcrophobiaGames.Clear();
            HangmanGames.ForEach(x => x.Value.Dispose());
            HangmanGames.Clear();
            await Task.WhenAll(RunningTrivias.Select(x => x.Value.StopGame())).ConfigureAwait(false);
            RunningTrivias.Clear();

            TicTacToeGames.Clear();

            await Task.WhenAll(RunningContests.Select(x => x.Value.Stop()))
                .ConfigureAwait(false);
            RunningContests.Clear();
            NunchiGames.ForEach(x => x.Value.Dispose());
            NunchiGames.Clear();
        }

        private void DisposeElems(IEnumerable<IDisposable> xs)
        {
            xs.ForEach(x => x.Dispose());
        }

        public void AddTypingArticle(IUser user, string text)
        {
            TypingArticles.Add(new TypingArticle
            {
                Source = user.ToString(),
                Extra = $"Text added on {DateTime.UtcNow} by {user}.",
                Text = text.SanitizeMentions(),
            });

            File.WriteAllText(TypingArticlesPath, JsonConvert.SerializeObject(TypingArticles));
        }

        public ConcurrentHashSet<ulong> GenerationChannels { get; }
        //channelid/message
        public ConcurrentDictionary<ulong, List<IUserMessage>> PlantedFlowers { get; } = new ConcurrentDictionary<ulong, List<IUserMessage>>();
        //channelId/last generation
        public ConcurrentDictionary<ulong, DateTime> LastGenerations { get; } = new ConcurrentDictionary<ulong, DateTime>();

        private ConcurrentDictionary<ulong, object> _locks { get; } = new ConcurrentDictionary<ulong, object>();
        public ConcurrentHashSet<ulong> HalloweenAwardedUsers { get; } = new ConcurrentHashSet<ulong>();

        public Uri GetRandomCurrencyImage()
        {
            var rng = new WizBotRandom();
            var cur = _images.ImageUrls.Currency;
            return cur[rng.Next(0, cur.Length)];
        }

        private string GetText(ITextChannel ch, string key, params object[] rep)
            => _strings.GetText(key, ch.GuildId, "Games".ToLowerInvariant(), rep);

        private Task PotentialFlowerGeneration(IUserMessage imsg)
        {
            var msg = imsg as SocketUserMessage;
            if (msg == null || msg.Author.IsBot)
                return Task.CompletedTask;

            var channel = imsg.Channel as ITextChannel;
            if (channel == null)
                return Task.CompletedTask;

            if (!GenerationChannels.Contains(channel.Id))
                return Task.CompletedTask;

            var _ = Task.Run(async () =>
            {
                try
                {
                    var lastGeneration = LastGenerations.GetOrAdd(channel.Id, DateTime.MinValue);
                    var rng = new WizBotRandom();

                    if (DateTime.UtcNow - TimeSpan.FromSeconds(_bc.BotConfig.CurrencyGenerationCooldown) < lastGeneration) //recently generated in this channel, don't generate again
                        return;

                    var num = rng.Next(1, 101) + _bc.BotConfig.CurrencyGenerationChance * 100;
                    if (num > 100 && LastGenerations.TryUpdate(channel.Id, DateTime.UtcNow, lastGeneration))
                    {
                        var dropAmount = _bc.BotConfig.CurrencyDropAmount;
                        var dropAmountMax = _bc.BotConfig.CurrencyDropAmountMax;

                        if (dropAmountMax != null && dropAmountMax > dropAmount)
                            dropAmount = new WizBotRandom().Next(dropAmount, dropAmountMax.Value + 1);

                        if (dropAmount > 0)
                        {
                            var msgs = new IUserMessage[dropAmount];
                            var prefix = _cmdHandler.GetPrefix(channel.Guild.Id);
                            var toSend = dropAmount == 1
                                ? GetText(channel, "curgen_sn", _bc.BotConfig.CurrencySign)
                                    + " " + GetText(channel, "pick_sn", prefix)
                                : GetText(channel, "curgen_pl", dropAmount, _bc.BotConfig.CurrencySign)
                                    + " " + GetText(channel, "pick_pl", prefix);
                            var file = GetRandomCurrencyImage();

                            var sent = await channel.EmbedAsync(new EmbedBuilder()
                                .WithOkColor()
                                .WithDescription(toSend)
                                .WithImageUrl(file.ToString())).ConfigureAwait(false);

                            msgs[0] = sent;

                            PlantedFlowers.AddOrUpdate(channel.Id, msgs.ToList(), (id, old) => { old.AddRange(msgs); return old; });
                        }
                    }
                }
                catch (Exception ex)
                {
                    LogManager.GetCurrentClassLogger().Warn(ex);
                }
            });
            return Task.CompletedTask;
        }

        public async Task<bool> GetTreat(ulong userId)
        {
            if (_rng.Next(0, 10) != 0)
            {
                await _cs.AddAsync(userId, "Halloween 2017 Treat", 10)
                    .ConfigureAwait(false);
                return true;
            }

            return false;
        }
    }
}