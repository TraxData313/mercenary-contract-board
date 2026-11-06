using MercenaryContractBoard.Core;

namespace MercenaryContractBoard.Core.Tests;

public class BoardTextTests
{
    private static BoardEntry Offer(string name, int award) =>
        new BoardEntry(ContractRulesTests.Hiring(name, award), Verdict.Yes);

    private static BoardEntry Refused(string name) =>
        new BoardEntry(ContractRulesTests.Hiring(name), new Verdict(Refusal.NotAtWar, "Not at war: they have no use for mercenaries."));

    private static CurrentContract Contract() => new CurrentContract
    {
        Name = "Vlandia",
        RulerName = "Derthert",
        Award = 70,
        AwardToday = 80,
        DaysToReview = 12,
        OwedInfluence = 14.2f,
        Enemies = new[] { "Battania", "Sturgia" },
    };

    [Fact]
    public void NamesJoinLikeASentence()
    {
        Assert.Equal("nobody", BoardText.JoinNames(Array.Empty<string>()));
        Assert.Equal("A", BoardText.JoinNames(new[] { "A" }));
        Assert.Equal("A and B", BoardText.JoinNames(new[] { "A", "B" }));
        Assert.Equal("A, B and C", BoardText.JoinNames(new[] { "A", "B", "C" }));
    }

    [Fact]
    public void NumbersAreDigitsWithThousands() => Assert.Equal("12,345", BoardText.Number(12345));

    [Fact]
    public void OffersComeFirstBestPayFirstThenRefusalsByName()
    {
        var entries = new[] { Refused("Sturgia"), Offer("Aserai", 60), Refused("Khuzait"), Offer("Battania", 90) };
        var list = BoardText.Arrange(entries, new BoardSettings());
        Assert.Equal(new[] { "Battania", "Aserai", "Khuzait", "Sturgia" }, list.Select(e => e.Kingdom.Name));
    }

    [Fact]
    public void SortByNameAndHideRefusals()
    {
        var entries = new[] { Refused("Sturgia"), Offer("Battania", 90), Offer("Aserai", 60) };
        var list = BoardText.Arrange(entries, new BoardSettings { SortByPay = false, ShowUnavailable = false });
        Assert.Equal(new[] { "Aserai", "Battania" }, list.Select(e => e.Kingdom.Name));
    }

    [Fact]
    public void EntryTitles()
    {
        Assert.Equal("Battania (Caladog): 90 per influence", BoardText.EntryTitle(Offer("Battania", 90)));
        Assert.Equal("Sturgia (Caladog): not at war", BoardText.EntryTitle(Refused("Sturgia")));
    }

    [Fact]
    public void OfferHintHasPaySigningAndWars()
    {
        Assert.Equal(
            "Battania, ruled by Caladog\nPay: 90 denars per influence point\nSigning: 5 influence = 450 denars\nAt war with: Vlandia",
            BoardText.EntryHint(Offer("Battania", 90)));
    }

    [Fact]
    public void RefusalHintHasTheReason()
    {
        Assert.Equal(
            "Sturgia, ruled by Caladog\nNot offered: Not at war: they have no use for mercenaries.\nAt war with: Vlandia",
            BoardText.EntryHint(Refused("Sturgia")));
    }

    [Fact]
    public void CurrentContractBlock()
    {
        Assert.Equal(
            "YOUR CONTRACT: Vlandia, ruled by Derthert\n"
            + "Pay: 70 denars per influence point\n"
            + "Pay review in 12 days (the rate today would be 80)\n"
            + "Owed to you: 14 influence, about 994 denars, paid out 1/5 a day\n"
            + "At war with: Battania and Sturgia",
            BoardText.CurrentContractBlock(Contract()));
    }

    [Fact]
    public void CurrentContractBlockWhenReviewIsDueAndNothingOwed()
    {
        var c = Contract();
        c.DaysToReview = 0;
        c.AwardToday = 70;
        c.OwedInfluence = 0;
        var text = BoardText.CurrentContractBlock(c);
        Assert.Contains("Pay review at the next day's turn\n", text);
        Assert.Contains("Owed to you: nothing yet", text);
    }

    [Fact]
    public void DescriptionForAMercenary()
    {
        var text = BoardText.Description(PlayerStanding.Mercenary, "Vlandia", Contract(), 2);
        Assert.StartsWith("YOUR CONTRACT: Vlandia", text);
        Assert.Contains("\n\nOther contracts on offer: 2. Signing one ends this contract first. Pay is in denars", text);
    }

    [Fact]
    public void DescriptionForAnIndependentClan()
    {
        Assert.Equal("You are not under contract.\n\nNo kingdom would hire you right now.",
            BoardText.Description(PlayerStanding.Independent, "", null, 0));
        Assert.StartsWith("You are not under contract.\n\nContracts on offer: 3. Pay is in denars",
            BoardText.Description(PlayerStanding.Independent, "", null, 3));
    }

    [Fact]
    public void DescriptionForAVassal() =>
        Assert.StartsWith("You are a vassal of Vlandia.", BoardText.Description(PlayerStanding.Vassal, "Vlandia", null, 0));

    [Fact]
    public void SwitchConfirmationNamesTheContractYouLeave()
    {
        var text = BoardText.ConfirmSwitch(Contract(), ContractRulesTests.Hiring("Battania", 90));
        Assert.StartsWith("Leave your contract with Vlandia and sign with Battania?", text);
        Assert.Contains("The 14 influence still owed to you (about 994 denars) is lost", text);
        Assert.Contains("Battania pays 90 denars per influence point", text);
        Assert.EndsWith("You will be at war with: Vlandia.", text);
    }

    [Fact]
    public void SwitchConfirmationWithNothingOwed()
    {
        var c = Contract();
        c.OwedInfluence = 0.2f;
        Assert.Contains("Nothing is owed to you right now.", BoardText.ConfirmSwitch(c, ContractRulesTests.Hiring()));
    }

    [Fact]
    public void FirstConfirmation() =>
        Assert.Equal(
            "Sign a mercenary contract with Battania, ruled by Caladog?\n\nPay: 90 denars per influence point. Signing gives 5 influence.\nYou will be at war with: Vlandia.",
            BoardText.ConfirmFirst(ContractRulesTests.Hiring()));
}
