﻿using Discord.Commands;
using WizBot.Extensions;
using System.Linq;
using Discord;
using WizBot.Core.Services;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using WizBot.Common.Attributes;
using WizBot.Modules.Help.Services;
using WizBot.Modules.Permissions.Services;
using WizBot.Common;
using WizBot.Common.Replacements;

namespace WizBot.Modules.Help
{
    public class Help : WizBotTopLevelModule<HelpService>
    {
        public const string PatreonUrl = "https://patreon.com/WizNet";
        public const string PaypalUrl = "https://paypal.me/Wizkiller96Network";
        private readonly IBotCredentials _creds;
        private readonly IBotConfigProvider _config;
        private readonly CommandService _cmds;
        private readonly GlobalPermissionService _perms;

        public EmbedBuilder GetHelpStringEmbed()
        {
            var r = new ReplacementBuilder()
                .WithDefault(Context)
                .WithOverride("{0}", () => _creds.ClientId.ToString())
                .WithOverride("{1}", () => Prefix)
                .Build();


            if (!CREmbed.TryParse(_config.BotConfig.HelpString, out var embed))
                return new EmbedBuilder().WithOkColor()
                    .WithDescription(String.Format(_config.BotConfig.HelpString, _creds.ClientId, Prefix));

            r.Replace(embed);

            return embed.ToEmbed();
        }

        public Help(IBotCredentials creds, GlobalPermissionService perms, IBotConfigProvider config, CommandService cmds)
        {
            _creds = creds;
            _config = config;
            _cmds = cmds;
            _perms = perms;
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Modules()
        {
            var embed = new EmbedBuilder().WithOkColor()
                .WithAuthor(eab => eab.WithIconUrl("http://i.imgur.com/fObUYFS.jpg"))
                .WithFooter(efb => efb.WithText("ℹ️" + GetText("modules_footer", Prefix)))
                .WithTitle(GetText("list_of_modules"))
                .WithDescription(string.Join("\n",
                                     _cmds.Modules.GroupBy(m => m.GetTopLevelModule())
                                         .Where(m => !_perms.BlockedModules.Contains(m.Key.Name.ToLowerInvariant()))
                                         .Select(m => "• " + m.Key.Name)
                                         .OrderBy(s => s)));
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Commands([Remainder] string module = null)
        {
            var channel = Context.Channel;

            module = module?.Trim().ToUpperInvariant();
            if (string.IsNullOrWhiteSpace(module))
                return;
            var cmds = _cmds.Commands.Where(c => c.Module.GetTopLevelModule().Name.ToUpperInvariant().StartsWith(module))
                                                .Where(c => !_perms.BlockedCommands.Contains(c.Aliases.First().ToLowerInvariant()))
                                                  .OrderBy(c => c.Aliases.First())
                                                  .Distinct(new CommandTextEqualityComparer())
                                                  .GroupBy(c => c.Module.Name.Replace("Commands", ""));
            cmds = cmds.OrderBy(x => x.Key == x.First().Module.Name ? int.MaxValue : x.Count());
            if (!cmds.Any())
            {
                await ReplyErrorLocalized("module_not_found").ConfigureAwait(false);
                return;
            }
            var i = 0;
            var groups = cmds.GroupBy(x => i++ / 48).ToArray();
            var embed = new EmbedBuilder().WithOkColor();
            foreach (var g in groups)
            {
                var last = g.Count();
                for (i = 0; i < last; i++)
                {
                    var transformed = g.ElementAt(i).Select(x =>
                    {
                        return $"{Prefix + x.Aliases.First(),-15} {"[" + x.Aliases.Skip(1).FirstOrDefault() + "]",-8}";
                        var str = $"{Prefix + x.Aliases.First(),-18}";
                        var al = x.Aliases.Skip(1).FirstOrDefault();
                        if (al != null)
                            str += $" {"(" + Prefix + al + ")",-9}";
                        return str;
                    });

                    if (i == last - 1 && (i + 1) % 2 != 0)
                    {
                        var grp = 0;
                        var count = transformed.Count();
                        transformed = transformed
                            .GroupBy(x => grp++ % count / 2)
                            .Select(x =>
                        {
                                if (x.Count() == 1)
                                    return $"{x.First()}";
                                else
                                    return String.Concat(x);
                            });
                    }
                    embed.AddField(g.ElementAt(i).Key, "```css\n" + string.Join("\n", transformed) + "\n```", true);
                }
            }
            embed.WithFooter(GetText("commands_instr", Prefix));
            await Context.Channel.EmbedAsync(embed).ConfigureAwait(false);
        }
        
        [WizBotCommand, Usage, Description, Aliases]
        [Priority(0)]
        public async Task H([Remainder] string fail)
        {
            await ReplyErrorLocalized("command_not_found").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [Priority(1)]
        public async Task H([Remainder] CommandInfo com = null)
        {
            var channel = Context.Channel;

            if (com == null)
            {
                IMessageChannel ch = channel is ITextChannel
                    ? await ((IGuildUser)Context.User).GetOrCreateDMChannelAsync()
                    : channel;
                await ch.EmbedAsync(GetHelpStringEmbed()).ConfigureAwait(false);
                return;
            }

            var embed = _service.GetCommandHelp(com, Context.Guild);
            await channel.EmbedAsync(embed).ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        [RequireContext(ContextType.Guild)]
        [OwnerOnly]
        public async Task Hgit()
        {
            var helpstr = new StringBuilder();
            helpstr.AppendLine(GetText("cmdlist_donate", PatreonUrl, PaypalUrl) + "\n");
            helpstr.AppendLine("##" + GetText("table_of_contents"));
            helpstr.AppendLine(string.Join("\n", _cmds.Modules.Where(m => m.GetTopLevelModule().Name.ToLowerInvariant() != "help")
                .Select(m => m.GetTopLevelModule().Name)
                .Distinct()
                .OrderBy(m => m)
                .Prepend("Help")
                .Select(m => string.Format("- [{0}](#{1})", m, m.ToLowerInvariant()))));
            helpstr.AppendLine();
            string lastModule = null;
            foreach (var com in _cmds.Commands.OrderBy(com => com.Module.GetTopLevelModule().Name).GroupBy(c => c.Aliases.First()).Select(g => g.First()))
            {
                var module = com.Module.GetTopLevelModule();
                if (module.Name != lastModule)
                {
                    if (lastModule != null)
                    {
                        helpstr.AppendLine();
                        helpstr.AppendLine($"###### [{GetText("back_to_toc")}](#{GetText("table_of_contents").ToLowerInvariant().Replace(' ', '-')})");
                    }
                    helpstr.AppendLine();
                    helpstr.AppendLine("### " + module.Name + "  ");
                    helpstr.AppendLine($"{GetText("cmd_and_alias")} | {GetText("desc")} | {GetText("usage")}");
                    helpstr.AppendLine("----------------|--------------|-------");
                    lastModule = module.Name;
                }
                helpstr.AppendLine($"{string.Join(" ", com.Aliases.Select(a => "`" + Prefix + a + "`"))} |" +
                                   $" {string.Format(com.Summary, Prefix)} {_service.GetCommandRequirements(com, Context.Guild)} |" +
                                   $" {string.Format(com.Remarks, Prefix)}");
            }
            File.WriteAllText("../../docs/Commands List.md", helpstr.ToString());
            await ReplyConfirmLocalized("commandlist_regen").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Guide()
        {
            await ConfirmLocalized("guide",
                "http://wizbot.readthedocs.io/en/latest/Commands%20List/",
                "http://wizbot.readthedocs.io/en/latest/").ConfigureAwait(false);
        }

        [WizBotCommand, Usage, Description, Aliases]
        public async Task Donate()
        {
            await ReplyConfirmLocalized("donate", PatreonUrl, PaypalUrl).ConfigureAwait(false);
        }
    }

    public class CommandTextEqualityComparer : IEqualityComparer<CommandInfo>
    {
        public bool Equals(CommandInfo x, CommandInfo y) => x.Aliases.First() == y.Aliases.First();

        public int GetHashCode(CommandInfo obj) => obj.Aliases.First().GetHashCode();

    }
}