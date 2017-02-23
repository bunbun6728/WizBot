﻿using Discord;
using Discord.Commands;
using WizBot.Attributes;
using WizBot.Extensions;
using WizBot.Services;
using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;

namespace WizBot.Modules.Utility
{
    public partial class Utility
    {
        [Group]
        public class CrossServerTextChannel : WizBotSubmodule
        {
            static CrossServerTextChannel()
            {
                WizBot.Client.MessageReceived += async imsg =>
                {
                    try
                    {
                        if (imsg.Author.IsBot)
                            return;
                        var msg = imsg as IUserMessage;
                        if (msg == null)
                            return;
                        var channel = imsg.Channel as ITextChannel;
                        if (channel == null)
                            return;
                        if (msg.Author.Id == WizBot.Client.CurrentUser.Id) return;
                        foreach (var subscriber in Subscribers)
                        {
                            var set = subscriber.Value;
                            if (!set.Contains(channel))
                                continue;
                            foreach (var chan in set.Except(new[] { channel }))
                            {
                                try
                                {
                                    await chan.SendMessageAsync(GetMessage(channel, (IGuildUser)msg.Author,
                                        msg)).ConfigureAwait(false);
                                }
                                catch
                                {
                                    // ignored
                                }
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                };
            }

            private static string GetMessage(ITextChannel channel, IGuildUser user, IUserMessage message) =>
                $"**{channel.Guild.Name} | {channel.Name}** `{user.Username}`: " + message.Content.SanitizeMentions();

            public static readonly ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>> Subscribers =
                new ConcurrentDictionary<int, ConcurrentHashSet<ITextChannel>>();

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task Scsc()
            {
                var token = new WizBotRandom().Next();
                var set = new ConcurrentHashSet<ITextChannel>();
                if (Subscribers.TryAdd(token, set))
                {
                    set.Add((ITextChannel)Context.Channel);
                    await ((IGuildUser)Context.User).SendConfirmAsync(GetText("csc_token"), token.ToString())
                        .ConfigureAwait(false);
                }
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Jcsc(int token)
            {
                ConcurrentHashSet<ITextChannel> set;
                if (!Subscribers.TryGetValue(token, out set))
                    return;
                set.Add((ITextChannel)Context.Channel);
                await ReplyConfirmLocalized("csc_join").ConfigureAwait(false);
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [RequireUserPermission(GuildPermission.ManageGuild)]
            public async Task Lcsc()
            {
                foreach (var subscriber in Subscribers)
                {
                    subscriber.Value.TryRemove((ITextChannel)Context.Channel);
                }
                await ReplyConfirmLocalized("csc_leave").ConfigureAwait(false);
            }
        }
    }
}