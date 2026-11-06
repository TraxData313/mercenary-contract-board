using MercenaryContractBoard.Core;

namespace MercenaryContractBoard.Core.Tests;

public class ContractRulesTests
{
    /// <summary>A kingdom that would hire: at war, not too strong, player tier 1 with some renown.</summary>
    internal static KingdomFacts Hiring(string name = "Battania", int award = 90) => new KingdomFacts
    {
        Name = name,
        RulerName = "Caladog",
        ClanTier = 1,
        MercenaryEligibleTier = 1,
        RulerRelation = 0,
        MinimumRelation = -10,
        Enemies = new[] { "Vlandia" },
        Strength = 1000,
        EnemyStrength = 800,
        PlayerRenown = 50,
        PlayerClanStrength = 100,
        Award = award,
    };

    private static Verdict Eval(KingdomFacts k, PlayerStanding standing = PlayerStanding.Independent, bool inArmy = false) =>
        ContractRules.Evaluate(standing, "Vlandia", inArmy, k);

    [Fact]
    public void AKingdomAtWarHires() => Assert.True(Eval(Hiring()).Offered);

    [Fact]
    public void AMercenaryMaySwitch() => Assert.True(Eval(Hiring(), PlayerStanding.Mercenary).Offered);

    [Fact]
    public void VassalsAndRulersGetNothing()
    {
        var v = Eval(Hiring(), PlayerStanding.Vassal);
        Assert.Equal(Refusal.Vassal, v.Refusal);
        Assert.Contains("vassal of Vlandia", v.Reason);
        Assert.Equal(Refusal.Ruler, Eval(Hiring(), PlayerStanding.Ruler).Refusal);
    }

    [Fact]
    public void AMercenaryInAnArmyCannotSwitch()
    {
        Assert.Equal(Refusal.InArmy, Eval(Hiring(), PlayerStanding.Mercenary, inArmy: true).Refusal);
        // Not under contract: vanilla has no army condition on joining.
        Assert.True(Eval(Hiring(), PlayerStanding.Independent, inArmy: true).Offered);
    }

    [Fact]
    public void OnlyKingdomsAtWarHire()
    {
        var k = Hiring();
        k.Enemies = Array.Empty<string>();
        k.EnemyStrength = 0;
        Assert.Equal(Refusal.NotAtWar, Eval(k).Refusal);
    }

    [Fact]
    public void EnemiesWithNoStrengthCountAsNoWar()
    {
        var k = Hiring();
        k.EnemyStrength = 0.5f;
        Assert.Equal(Refusal.NotAtWar, Eval(k).Refusal);
    }

    [Fact]
    public void AKingdomThreeTimesStrongerThanItsEnemiesDoesNotNeedYou()
    {
        var k = Hiring();
        k.Strength = 3001;
        k.EnemyStrength = 1000;
        Assert.Equal(Refusal.NotNeeded, Eval(k).Refusal);
        k.Strength = 3000; // exactly 3 (minus vanilla's 0.0001 guard) still hires
        Assert.True(Eval(k).Offered);
    }

    [Fact]
    public void AtWarWithYouComesFirst()
    {
        var k = Hiring();
        k.PlayerAtWarWith = true;
        k.ClanTier = 0;
        Assert.Equal(Refusal.AtWarWithYou, Eval(k).Refusal);
    }

    [Fact]
    public void ClanTierBelowTheModelsTierRefuses()
    {
        var k = Hiring();
        k.ClanTier = 0;
        var v = Eval(k);
        Assert.Equal(Refusal.ClanTier, v.Refusal);
        Assert.Contains("1", v.Reason);
    }

    [Fact]
    public void RelationBelowMinimumRefuses()
    {
        var k = Hiring();
        k.RulerRelation = -11;
        var v = Eval(k);
        Assert.Equal(Refusal.Relation, v.Refusal);
        Assert.Equal("You need -10 relation with Caladog (you have -11).", v.Reason);
        k.RulerRelation = -10;
        Assert.NotEqual(Refusal.Relation, Eval(k).Refusal);
    }

    [Fact]
    public void YourOwnWarsMustBeTheirsToo()
    {
        var k = Hiring();
        k.UnsharedPlayerWars = new[] { "Sturgia", "Aserai" };
        var v = Eval(k);
        Assert.Equal(Refusal.UnsharedWars, v.Refusal);
        Assert.Contains("Sturgia and Aserai", v.Reason);
    }

    [Fact]
    public void SettlementOwnersAreNotMercenaries()
    {
        var k = Hiring();
        k.PlayerOwnsSettlements = true;
        Assert.Equal(Refusal.OwnsSettlement, Eval(k).Refusal);
    }

    [Fact]
    public void ClickableChecksComeBeforeTheLordsAnswer()
    {
        // Vanilla: the player line's clickable condition (tier) is checked before the lord says "not at war".
        var k = Hiring();
        k.ClanTier = 0;
        k.Enemies = Array.Empty<string>();
        Assert.Equal(Refusal.ClanTier, Eval(k).Refusal);
    }

    [Fact]
    public void UnknownClanIsRefused()
    {
        var k = Hiring();
        k.PlayerRenown = 0;
        k.PlayerClanStrength = 500;
        k.RulerRelation = 0;
        Assert.Equal(Refusal.UnknownClan, Eval(k).Refusal);
    }

    [Theory]
    // renown + min(10, renown)/10 * 0.2 * strength + relation * 2
    [InlineData(25f, 0f, 0f, 25f)]
    [InlineData(5f, 100f, 0f, 15f)]
    [InlineData(50f, 100f, 3f, 76f)]
    [InlineData(0f, 1000f, 13f, 26f)]
    public void RenownScoreIsVanillas(float renown, float strength, float relation, float expected) =>
        Assert.Equal(expected, ContractRules.RenownScore(renown, strength, relation), 3);

    [Fact]
    public void RenownScoreMustExceed25()
    {
        var k = Hiring();
        k.PlayerRenown = 25;
        k.PlayerClanStrength = 0;
        k.RulerRelation = 0;
        Assert.Equal(Refusal.UnknownClan, Eval(k).Refusal); // 25 is not > 25
        k.PlayerRenown = 25.5f;
        Assert.True(Eval(k).Offered);
    }

    [Fact]
    public void EveryRefusalHasAShortLabel()
    {
        foreach (Refusal r in Enum.GetValues(typeof(Refusal)))
        {
            if (r == Refusal.None) Assert.Equal("", ContractRules.ShortLabel(r));
            else Assert.False(string.IsNullOrWhiteSpace(ContractRules.ShortLabel(r)));
        }
    }
}
