using PdfSharp.Fonts;

namespace CitizenService.Infrastructure.Documents;

internal sealed class OfficialDocumentFontResolver : IFontResolver
{
    private const string SansRegular = "official-sans-regular";
    private const string SansBold = "official-sans-bold";
    private const string SerifRegular = "official-serif-regular";
    private const string SerifBold = "official-serif-bold";

    private static readonly IReadOnlyDictionary<string, string[]> FontCandidates =
        new Dictionary<string, string[]>
        {
            [SansRegular] =
            [
                "/usr/share/fonts/truetype/noto/NotoSans-Regular.ttf",
                "/System/Library/Fonts/Supplemental/Arial.ttf"
            ],
            [SansBold] =
            [
                "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf",
                "/usr/share/fonts/truetype/noto/NotoSans-Bold.ttf",
                "/System/Library/Fonts/Supplemental/Arial Bold.ttf"
            ],
            [SerifRegular] =
            [
                "/usr/share/fonts/truetype/noto/NotoSerif-Regular.ttf",
                "/System/Library/Fonts/Supplemental/Times New Roman.ttf"
            ],
            [SerifBold] =
            [
                "/usr/share/fonts/truetype/noto/NotoSerif-Bold.ttf",
                "/System/Library/Fonts/Supplemental/Times New Roman Bold.ttf"
            ]
        };

    public FontResolverInfo ResolveTypeface(string familyName, bool bold, bool italic)
    {
        var isSerif = familyName.Contains("Serif", StringComparison.OrdinalIgnoreCase);
        var faceName = (isSerif, bold) switch
        {
            (true, true) => SerifBold,
            (true, false) => SerifRegular,
            (false, true) => SansBold,
            _ => SansRegular
        };

        return new FontResolverInfo(faceName, mustSimulateBold: false, mustSimulateItalic: italic);
    }

    public byte[] GetFont(string faceName)
    {
        if (!FontCandidates.TryGetValue(faceName, out var candidates))
            throw new InvalidOperationException($"Unknown official document font face '{faceName}'.");

        var path = candidates.FirstOrDefault(File.Exists)
            ?? throw new InvalidOperationException(
                $"No font file is available for '{faceName}'. Install the Noto core font package.");
        return File.ReadAllBytes(path);
    }
}