using WizBot.Services.Database.Models;
using System.Linq;
using Microsoft.EntityFrameworkCore;

namespace WizBot.Services.Database.Repositories.Impl
{
    public class XpRepository : Repository<UserXpStats>, IXpRepository
    {
        public XpRepository(DbContext context) : base(context)
        {
        }

        public UserXpStats GetOrCreateUser(ulong guildId, ulong userId)
        {
            var usr = _set.FirstOrDefault(x => x.UserId == userId && x.GuildId == guildId);

            if (usr == null)
            {
                _context.Add(usr = new UserXpStats()
                {
                    Xp = 0,
                    UserId = userId,
                    NotifyOnLevelUp = XpNotificationType.None,
                    GuildId = guildId,
                });
            }

            return usr;
        }

        public UserXpStats[] GetUsersFor(ulong guildId, int page)
        {
            return _set.Where(x => x.GuildId == guildId)
                .OrderByDescending(x => x.Xp + x.AwardedXp)
                .Skip(page * 9)
                .Take(9)
                .ToArray();
        }

        public int GetUserGuildRanking(ulong userId, ulong guildId)
        {
            return _set
                .Where(x => x.GuildId == guildId)
                .Count(x => x.Xp > (_set
                    .Where(y => y.UserId == userId && y.GuildId == guildId)
                    .Select(y => y.Xp)
                    .DefaultIfEmpty()
                    .Sum())) + 1;
        }
    }
}