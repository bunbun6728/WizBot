﻿using Discord;
using Discord.Commands;
using WizBot.Extensions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using WizBot.Common.Attributes;
using WizBot.Modules.Gambling.Common;
using Image = ImageSharp.Image;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class DrawCommands : WizBotSubmodule
        {
            private static readonly ConcurrentDictionary<IGuild, Cards> _allDecks = new ConcurrentDictionary<IGuild, Cards>();
            private const string _cardsPath = "data/images/cards";

            
            private async Task<(Stream ImageStream, string ToSend)> InternalDraw(int num, ulong? guildId = null)
            {
                if (num < 1 || num > 10)
                    throw new ArgumentOutOfRangeException(nameof(num));

                Cards cards = guildId == null ? new Cards() : _allDecks.GetOrAdd(Context.Guild, (s) => new Cards());
                var images = new List<Image>();
                var cardObjects = new List<Cards.Card>();
                for (var i = 0; i < num; i++)
                {
                    if (cards.CardPool.Count == 0 && i != 0)
                    {
                        try
                        {
                            await ReplyErrorLocalized("no_more_cards").ConfigureAwait(false);
                        }
                        catch
                        {
                            // ignored
                        }
                        break;
                    }
                    var currentCard = cards.DrawACard();
                    cardObjects.Add(currentCard);
                    using (var stream = File.OpenRead(Path.Combine(_cardsPath, currentCard.ToString().ToLowerInvariant() + ".jpg").Replace(' ', '_')))
                        images.Add(new Image(stream));
                }
                MemoryStream bitmapStream = new MemoryStream();
                images.Merge().Save(bitmapStream);
                bitmapStream.Position = 0;

                var toSend = $"{Context.User.Mention}";
                if (cardObjects.Count == 5)
                    toSend += $" drew `{Cards.GetHandValue(cardObjects)}`";

                return (bitmapStream, toSend);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task Draw(int num = 1)
            {
                if (num < 1)
                    num = 1;
                if (num > 10)
                    num = 10;

                var data = await InternalDraw(num, Context.Guild.Id).ConfigureAwait(false);
                await Context.Channel.SendFileAsync(data.ImageStream, num + " cards.jpg", data.ToSend).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            public async Task DrawNew(int num = 1)
            {
                if (num < 1)
                    num = 1;
                if (num > 10)
                    num = 10;

                var data = await InternalDraw(num).ConfigureAwait(false);
                await Context.Channel.SendFileAsync(data.ImageStream, num + " cards.jpg", data.ToSend).ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            public async Task DeckShuffle()
            {
                //var channel = (ITextChannel)Context.Channel;

                _allDecks.AddOrUpdate(Context.Guild,
                        (g) => new Cards(),
                        (g, c) =>
                        {
                            c.Restart();
                            return c;
                        });

                await ReplyConfirmLocalized("deck_reshuffled").ConfigureAwait(false);
            }
        }
    }
}