#nullable disable
using WizBot.Services.Database.Models;

namespace WizBot.Db.Models;

public class DiscordUser : DbEntity
{
    public ulong UserId { get; set; }
    public string Username { get; set; }
    public string Discriminator { get; set; }
    public string AvatarId { get; set; }

    public int? ClubId { get; set; }
    public ClubInfo Club { get; set; }
    public bool IsClubAdmin { get; set; }

    public long TotalXp { get; set; }
    public DateTime LastLevelUp { get; set; } = DateTime.UtcNow;
    public DateTime LastXpGain { get; set; } = DateTime.MinValue;
    public XpNotificationLocation NotifyOnLevelUp { get; set; }

    public long CurrencyAmount { get; set; }

    public override bool Equals(object obj)
        => obj is DiscordUser du ? du.UserId == UserId : false;

    public override int GetHashCode()
        => UserId.GetHashCode();

    public override string ToString()
        => Username + "#" + Discriminator;
}