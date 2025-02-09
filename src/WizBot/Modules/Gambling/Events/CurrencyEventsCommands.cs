﻿#nullable disable
using WizBot.Modules.Gambling.Common;
using WizBot.Modules.Gambling.Common.Events;
using WizBot.Modules.Gambling.Services;
using WizBot.Services.Database.Models;

namespace WizBot.Modules.Gambling;

public partial class Gambling
{
    [Group]
    public partial class CurrencyEventsCommands : GamblingSubmodule<CurrencyEventsService>
    {
        public CurrencyEventsCommands(GamblingConfigService gamblingConf)
            : base(gamblingConf)
        {
        }

        [Cmd]
        [RequireContext(ContextType.Guild)]
        [WizBotOptionsAttribute(typeof(EventOptions))]
        [OwnerOnly]
        public async partial Task EventStart(CurrencyEvent.Type ev, params string[] options)
        {
            var (opts, _) = OptionsParser.ParseFrom(new EventOptions(), options);
            if (!await _service.TryCreateEventAsync(ctx.Guild.Id, ctx.Channel.Id, ev, opts, GetEmbed))
                await ReplyErrorLocalizedAsync(strs.start_event_fail);
        }

        private IEmbedBuilder GetEmbed(CurrencyEvent.Type type, EventOptions opts, long currentPot)
            => type switch
            {
                CurrencyEvent.Type.Reaction => _eb.Create()
                                                  .WithOkColor()
                                                  .WithTitle(GetText(strs.event_title(type.ToString())))
                                                  .WithDescription(GetReactionDescription(opts.Amount, currentPot))
                                                  .WithFooter(GetText(strs.event_duration_footer(opts.Hours))),
                CurrencyEvent.Type.GameStatus => _eb.Create()
                                                    .WithOkColor()
                                                    .WithTitle(GetText(strs.event_title(type.ToString())))
                                                    .WithDescription(GetGameStatusDescription(opts.Amount, currentPot))
                                                    .WithFooter(GetText(strs.event_duration_footer(opts.Hours))),
                _ => throw new ArgumentOutOfRangeException(nameof(type))
            };

        private string GetReactionDescription(long amount, long potSize)
        {
            var potSizeStr = Format.Bold(potSize == 0 ? "∞" + CurrencySign : N(potSize));

            return GetText(strs.new_reaction_event(CurrencySign, Format.Bold(N(amount)), potSizeStr));
        }

        private string GetGameStatusDescription(long amount, long potSize)
        {
            var potSizeStr = Format.Bold(potSize == 0 ? "∞" + CurrencySign : potSize + CurrencySign);

            return GetText(strs.new_gamestatus_event(CurrencySign, Format.Bold(N(amount)), potSizeStr));
        }
    }
}