#nullable disable
using Microsoft.EntityFrameworkCore;
using WizBot.Services.Database.Models;

namespace WizBot.Db;

public static class QuoteExtensions
{
    public static IEnumerable<Quote> GetForGuild(this DbSet<Quote> quotes, ulong guildId)
        => quotes.AsQueryable().Where(x => x.GuildId == guildId);

    public static IReadOnlyCollection<Quote> GetGroup(
        this DbSet<Quote> quotes,
        ulong guildId,
        int page,
        OrderType order)
    {
        var q = quotes.AsQueryable().Where(x => x.GuildId == guildId);
        if (order == OrderType.Keyword)
            q = q.OrderBy(x => x.Keyword);
        else
            q = q.OrderBy(x => x.Id);

        return q.Skip(15 * page).Take(15).ToArray();
    }

    public static async Task<Quote> GetRandomQuoteByKeywordAsync(
        this DbSet<Quote> quotes,
        ulong guildId,
        string keyword)
    {
        var rng = new WizBotRandom();
        return (await quotes.AsQueryable().Where(q => q.GuildId == guildId && q.Keyword == keyword).ToListAsync())
               .OrderBy(_ => rng.Next())
               .FirstOrDefault();
    }

    public static async Task<Quote> SearchQuoteKeywordTextAsync(
        this DbSet<Quote> quotes,
        ulong guildId,
        string keyword,
        string text)
    {
        var rngk = new WizBotRandom();
        return (await quotes.AsQueryable()
                            .Where(q => q.GuildId == guildId
                                        && q.Keyword == keyword
                                        && EF.Functions.Like(q.Text.ToUpper(), $"%{text.ToUpper()}%")
                                // && q.Text.Contains(text, StringComparison.OrdinalIgnoreCase)
                            )
                            .ToListAsync()).OrderBy(_ => rngk.Next())
                                           .FirstOrDefault();
    }

    public static void RemoveAllByKeyword(this DbSet<Quote> quotes, ulong guildId, string keyword)
        => quotes.RemoveRange(quotes.AsQueryable().Where(x => x.GuildId == guildId && x.Keyword.ToUpper() == keyword));
}