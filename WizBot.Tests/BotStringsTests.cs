using NUnit.Framework;
using System.Globalization;
using System.Linq;
using System.Reflection;
using AngleSharp.Common;
using Discord.Commands;
using WizBot.Common.Attributes;
using WizBot.Core.Services;
using WizBot.Modules;
using YamlDotNet.Serialization;

namespace WizBot.Tests
{
    public class CommandStringsTests
    {
        [Test]
        public void AllCommandNamesHaveStrings()
        {
            var stringsSource = new LocalFileStringsSource(
                "../../../../src/WizBot/config/strings/responses",
                "../../../../src/WizBot/config/strings/commands");
            var strings = new LocalBotStringsProvider(stringsSource);

            var culture = new CultureInfo("en-US");

            var isSuccess = true;
            foreach (var entry in CommandNameLoadHelper.LoadCommandNames("../../../../src/WizBot/config/aliases.yml"))
            {
                var commandName = entry.Value[0];

                var cmdStrings = strings.GetCommandStrings(culture.Name, commandName);
                if (cmdStrings is null)
                {
                    isSuccess = false;
                    TestContext.Out.WriteLine($"{commandName} doesn't exist in commands.en-US.yml");
                }
            }

            Assert.IsTrue(isSuccess);
        }

        private static string[] GetCommandMethodNames()
            => typeof(WizBot.WizBot).Assembly
                .GetExportedTypes()
                .Where(type => type.IsClass && !type.IsAbstract)
                .Where(type => typeof(WizBotTopLevelModule).IsAssignableFrom(type) // if its a top level module
                               || !(type.GetCustomAttribute<GroupAttribute>(true) is null)) // or a submodule
                .SelectMany(x => x.GetMethods().Where(mi => mi.CustomAttributes.Any(ca => ca.AttributeType == typeof(WizBotCommandAttribute))))
                .Select(x => x.Name.ToLowerInvariant())
                .ToArray();

        [Test]
        public void AllCommandMethodsHaveNames()
        {
            var allAliases = CommandNameLoadHelper.LoadCommandNames(
                "../../../../src/WizBot/config/aliases.yml");

            var methodNames = GetCommandMethodNames();

            var isSuccess = true;
            foreach (var methodName in methodNames)
            {
                if (!allAliases.TryGetValue(methodName, out var _))
                {
                    TestContext.Error.WriteLine($"{methodName} is missing an alias.");
                    isSuccess = false;
                }
            }

            Assert.IsTrue(isSuccess);
        }

        [Test]
        public void NoObsoleteAliases()
        {
            var allAliases = CommandNameLoadHelper.LoadCommandNames(
                "../../../../src/WizBot/config/aliases.yml");

            var methodNames = GetCommandMethodNames()
                .ToHashSet();

            var isSuccess = true;

            foreach (var item in allAliases)
            {
                var methodName = item.Key;

                if (!methodNames.Contains(methodName))
                {
                    TestContext.WriteLine($"'{methodName}' from aliases.yml doesn't have a matching command method.");
                    isSuccess = false;
                }
            }

            Assert.IsTrue(isSuccess);
        }

        [Test]
        public void NoObsoleteCommandStrings()
        {
            var stringsSource = new LocalFileStringsSource(
                "../../../../src/WizBot/config/strings/responses",
                "../../../../src/WizBot/config/strings/commands");

            var culture = new CultureInfo("en-US");

            var isSuccess = true;
            var allCommandNames = CommandNameLoadHelper.LoadCommandNames("../../../../src/WizBot/config/aliases.yml");
            var enUsCommandNames = allCommandNames
                .Select(x => x.Value[0]) // first alias is command name
                .ToHashSet();
            foreach (var entry in stringsSource.GetCommandStrings()[culture.Name])
            {
                // key is command name which should be specified in aliases[0] of any method name
                var cmdName = entry.Key;

                if (!enUsCommandNames.Contains(cmdName))
                {
                    TestContext.Out.WriteLine($"'{cmdName}' It's either obsolete or missing an alias entry.");
                    isSuccess = false;
                }
            }

            Assert.IsTrue(isSuccess);
        }
    }
}
