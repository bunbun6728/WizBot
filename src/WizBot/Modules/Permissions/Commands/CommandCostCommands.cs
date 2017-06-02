﻿using Discord.Commands;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace WizBot.Modules.Permissions
{
    public partial class Permissions
    {
        [Group]
        public class CommandCostCommands : WizBotSubModule
        {
            private static readonly ConcurrentDictionary<string, int> _commandCosts = new ConcurrentDictionary<string, int>();
            public static IReadOnlyDictionary<string, int> CommandCosts => _commandCosts;

            static CommandCostCommands()
            {
                //_commandCosts = new ConcurrentDictionary<string, int>(WizBot.BotConfig.CommandCosts.ToDictionary(
                //    x => x.CommandName.Trim().ToUpperInvariant(),
                //    x => x.Cost));
            }

            //[WizBotCommand, Usage, Description, Aliases]
            //public async Task CmdCosts(int page = 1)
            //{
            //    var prices = _commandCosts.ToList();

            //    if (!prices.Any())
            //    {
            //        await Context.Channel.SendConfirmAsync(GetText("no_costs")).ConfigureAwait(false);
            //        return;
            //    }

            //    await Context.Channel.SendPaginatedConfirmAsync(page, (curPage) => {
            //        var embed = new EmbedBuilder().WithOkColor()
            //            .WithTitle(GetText("command_costs"));
            //        var current = prices.Skip((curPage - 1) * 9)
            //            .Take(9);
            //        foreach (var price in current)
            //        {
            //            embed.AddField(efb => efb.WithName(price.Key).WithValue(price.Value.ToString()).WithIsInline(true));
            //        }
            //        return embed;
            //    }, prices.Count / 9).ConfigureAwait(false);
            //}

            //[WizBotCommand, Usage, Description, Aliases]
            //public async Task CommandCost(int cost, CommandInfo cmd)
            //{
            //    if (cost < 0)
            //        return;

            //    var cmdName = cmd.Aliases.First().ToLowerInvariant();

            //    var cmdPrice = new CommandCost()
            //    {
            //        CommandName = cmdName,
            //        Cost = cost
            //    };

            //    using (var uow = _db.UnitOfWork)
            //    {
            //        var bc = uow.BotConfig.GetOrCreate();
                    
            //        if (cost != 0)
            //        {
            //            var elem = bc.CommandCosts.Where(cc => cc.CommandName == cmdPrice.CommandName).FirstOrDefault();
            //            if (elem == null)
            //                bc.CommandCosts.Add(cmdPrice);
            //            else
            //                elem.Cost = cost;

            //            _commandCosts.AddOrUpdate(cmdName, cost, (key, old) => cost);
            //        }
            //        else
            //        {
            //            bc.CommandCosts.RemoveAt(bc.CommandCosts.IndexOf(cmdPrice));
            //            int throwaway;
            //            _commandCosts.TryRemove(cmdName, out throwaway);
            //        }

            //        await uow.CompleteAsync().ConfigureAwait(false);
            //    }

            //    if (cost == 0)
            //        await Context.Channel.SendConfirmAsync($"Removed the cost from the {Format.Bold(cmd.Name)} command.").ConfigureAwait(false);
            //    else
            //        await Context.Channel.SendConfirmAsync($"{Format.Bold(cmd.Name)} now costs {cost}{WizBot.BotConfig.CurrencySign} to run.").ConfigureAwait(false);
            //}
        }
    }
}
