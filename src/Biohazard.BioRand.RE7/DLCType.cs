using System.Text.RegularExpressions;

namespace Biohazard.BioRand.RE7;

public enum DlcType {
    BeginningHourDemo,
    NotAHero,
    EndOfZoe,
    Nightmare,
    Bedroom,
    EthanMustDie,
    TwentyOne,
    Daughters,
    Jacks55thBirthday,
    AttackCoinAndMadhouseUnlock,
    DefenseCoinAndMadhouseUnlock,
    UniversalCoinAndMadhouseUnlock,
    ReloadCoinAndMadhouseUnlock,
    InstinctCoinAndMadhouseUnlock,
}

public static class DlcTypeExtensions {
    public static bool IsBannedFootage1(this DlcType type) =>
        type is DlcType.Nightmare or
            DlcType.Bedroom or
            DlcType.EthanMustDie;

    public static bool IsBannedFootage2(this DlcType type) =>
        type is DlcType.TwentyOne or
            DlcType.Daughters;

    public static bool IsBannedFootage(this DlcType type) {
        return type.IsBannedFootage1() || type.IsBannedFootage2();
    }

    private sealed record DlcMapping(Regex Pattern, DlcType Type);

#pragma warning disable SYSLIB1045

    private static readonly DlcMapping[] DlcMappings =[
        new(new Regex("((chapter|c)0?)?7_1", RegexOptions.IgnoreCase | RegexOptions.Compiled), DlcType.Bedroom),
        new(new Regex("(((chapter|c)0?)?7_2)|cardgame|survival", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            DlcType.TwentyOne),
        new(new Regex("((chapter|c)0?)?7_3", RegexOptions.IgnoreCase | RegexOptions.Compiled), DlcType.Nightmare),
        new(new Regex("((chapter|c)0?)?7_4", RegexOptions.IgnoreCase | RegexOptions.Compiled), DlcType.Daughters),
        new(new Regex("(chapter|c|ch)0?8", RegexOptions.IgnoreCase | RegexOptions.Compiled), DlcType.NotAHero),
        new(new Regex("(chapter|c|ch)0?9", RegexOptions.IgnoreCase | RegexOptions.Compiled), DlcType.EndOfZoe),

        new(new Regex("birthday", RegexOptions.IgnoreCase | RegexOptions.Compiled), DlcType.Jacks55thBirthday),
        new(new Regex("/(e|i)md", RegexOptions.IgnoreCase | RegexOptions.Compiled), DlcType.EthanMustDie),

        new(new Regex("(dlcitem|coin)_01", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            DlcType.DefenseCoinAndMadhouseUnlock),
        new(new Regex("(dlcitem|coin)_02", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            DlcType.AttackCoinAndMadhouseUnlock),
        new(new Regex("(dlcitem|coin)_03", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            DlcType.InstinctCoinAndMadhouseUnlock),
        new(new Regex("(dlcitem|coin)_04", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            DlcType.ReloadCoinAndMadhouseUnlock),
        new(new Regex("(dlcitem|coin)_05", RegexOptions.IgnoreCase | RegexOptions.Compiled),
            DlcType.UniversalCoinAndMadhouseUnlock),
    ];

#pragma warning restore SYSLIB1045

    public static DlcType? FromPakFileName(string pakFileName) {
        foreach (var mapping in DlcMappings) {
            if (mapping.Pattern.IsMatch(pakFileName))
                return mapping.Type;
        }

        return null;
    }

    public static int ToChapter(DlcType dlcType)
        => dlcType switch{
            DlcType.Bedroom or DlcType.TwentyOne or DlcType.Nightmare
                or DlcType.Daughters or DlcType.Jacks55thBirthday or DlcType.EthanMustDie => 7,
            DlcType.NotAHero => 8,
            DlcType.EndOfZoe => 9,
            _ => -1
        };
}
