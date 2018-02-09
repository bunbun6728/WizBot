using Discord;
using Discord.Commands;
using WizBot.Extensions;
using WizBot.Core.Services;
using System.Threading.Tasks;
using Discord.WebSocket;
using WizBot.Common.Attributes;
using WizBot.Modules.Gambling.Common;
using WizBot.Modules.Gambling.Services;
using WizBot.Modules.Gambling.Common.CurrencyEvents;

namespace WizBot.Modules.Gambling
{
    public partial class Gambling
    {
        [Group]
        public class CurrencyEventsCommands : WizBotSubmodule<CurrencyEventsService>
        {
            public enum CurrencyEvent
            {
                Reaction,
                SneakyGameStatus
            }

            private readonly DiscordSocketClient _client;
            private readonly ICurrencyService _cs;

            public CurrencyEventsCommands(DiscordSocketClient client, ICurrencyService cs)
            {
                _client = client;
                _cs = cs;
            }

            [WizBotCommand, Usage, Description, Aliases]
            [RequireContext(ContextType.Guild)]
            [OwnerOnly]
            public async Task StartEvent(CurrencyEvent e, int arg = -1)
            {
                switch (e)
                {
                    case CurrencyEvent.Reaction:
                        await ReactionEvent(Context, arg).ConfigureAwait(false);
                        break;
                    case CurrencyEvent.SneakyGameStatus:
                        await SneakyGameStatusEvent(Context, arg).ConfigureAwait(false);
                        break;
                }
            }

            private async Task SneakyGameStatusEvent(ICommandContext context, int num)
            {
                if (num < 10 || num > 600)
                    num = 60;

                var ev = new SneakyEvent(_cs, _client, _bc, num);
                if (!await _service.StartSneakyEvent(ev, context.Message, context))
                    return;
                try
                {
                    var title = GetText("sneakygamestatus_title");
                    var desc = GetText("sneakygamestatus_desc", 
                        Format.Bold(100.ToString()) + _bc.BotConfig.CurrencySign,
                        Format.Bold(num.ToString()));
                    await context.Channel.SendConfirmAsync(title, desc)
                        .ConfigureAwait(false);
                }
                catch
                {
                    // ignored
                }
            }

            public async Task ReactionEvent(ICommandContext context, int amount)
            {
                if (amount <= 0)
                    amount = 100;

                var title = GetText("reaction_title");
                var desc = GetText("reaction_desc", _bc.BotConfig.CurrencySign, Format.Bold(amount.ToString()) + _bc.BotConfig.CurrencySign);
                var footer = GetText("reaction_footer", 24);
                var re = new ReactionEvent(_bc.BotConfig, _client, _cs, amount);
                var msg = await context.Channel.SendConfirmAsync(title,
                        desc, footer: footer)
                    .ConfigureAwait(false);
                await re.Start(msg, context);
            }
        }
    }
}