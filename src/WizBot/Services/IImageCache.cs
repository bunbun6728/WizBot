#nullable disable
namespace WizBot.Services;

public interface IImageCache
{
    ImageUrls ImageUrls { get; }

    IReadOnlyList<byte[]> Heads { get; }
    IReadOnlyList<byte[]> Tails { get; }

    IReadOnlyList<byte[]> Dice { get; }

    IReadOnlyList<byte[]> SlotEmojis { get; }
    IReadOnlyList<byte[]> Currency { get; }

    byte[] SlotBackground { get; }

    byte[] RategirlMatrix { get; }
    byte[] RategirlDot { get; }

    byte[] XpBackground { get; }

    byte[] Rip { get; }
    byte[] RipOverlay { get; }

    byte[] GetCard(string key);

    Task Reload();
}