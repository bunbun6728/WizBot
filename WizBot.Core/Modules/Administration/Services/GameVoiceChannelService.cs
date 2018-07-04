using System;
using System.Linq;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Common.Collections;
using WizBot.Extensions;
using WizBot.Core.Services;
using NLog;

namespace WizBot.Modules.Administration.Services
{
    public class GameVoiceChannelService : INService
    {
        public ConcurrentHashSet<ulong> GameVoiceChannels { get; } = new ConcurrentHashSet<ulong>();

        private readonly Logger _log;
        private readonly DbService _db;
        private readonly DiscordSocketClient _client;

        public GameVoiceChannelService(DiscordSocketClient client, DbService db, WizBot bot)
        {
            _log = LogManager.GetCurrentClassLogger();
            _db = db;
            _client = client;

            GameVoiceChannels = new ConcurrentHashSet<ulong>(
                bot.AllGuildConfigs.Where(gc => gc.GameVoiceChannel != null)
                                         .Select(gc => gc.GameVoiceChannel.Value));

            _client.UserVoiceStateUpdated += Client_UserVoiceStateUpdated;

        }

        public ulong? ToggleGameVoiceChannel(ulong guildId, ulong vchId)
        {
            ulong? id;
            using (var uow = _db.UnitOfWork)
            {
                var gc = uow.GuildConfigs.ForId(guildId, set => set);

                if (gc.GameVoiceChannel == vchId)
                {
                    GameVoiceChannels.TryRemove(vchId);
                    id = gc.GameVoiceChannel = null;
                }
                else
                {
                    if (gc.GameVoiceChannel != null)
                        GameVoiceChannels.TryRemove(gc.GameVoiceChannel.Value);
                    GameVoiceChannels.Add(vchId);
                    id = gc.GameVoiceChannel = vchId;
                }

                uow.Complete();
            }
            return id;
        }

        private Task Client_UserVoiceStateUpdated(SocketUser usr, SocketVoiceState oldState, SocketVoiceState newState)
        {
            var _ = Task.Run(async () =>
            {
                try
                {
                    if (!(usr is SocketGuildUser gUser))
                        return;

                    var game = gUser.Activity?.Name?.TrimTo(50).ToLowerInvariant();

                    if (oldState.VoiceChannel == newState.VoiceChannel ||
                        newState.VoiceChannel == null)
                        return;

                    if (!GameVoiceChannels.Contains(newState.VoiceChannel.Id) ||
                        string.IsNullOrWhiteSpace(game))
                        return;

                    var vch = gUser.Guild.VoiceChannels
                        .FirstOrDefault(x => x.Name.ToLowerInvariant() == game);

                    if (vch == null)
                        return;

                    await Task.Delay(1000).ConfigureAwait(false);
                    await gUser.ModifyAsync(gu => gu.Channel = vch).ConfigureAwait(false);
                }
                catch (Exception ex)
                {
                    _log.Warn(ex);
                }
            });

            return Task.CompletedTask;
        }
    }
}