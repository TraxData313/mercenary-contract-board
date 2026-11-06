using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;

namespace MercenaryContractBoard.Core
{
    /// <summary>The player's current mercenary contract, as the clan stores it (vanilla's own state).</summary>
    public sealed class CurrentContract
    {
        public string Name { get; set; } = "";
        public string RulerName { get; set; } = "";

        /// <summary>Clan.MercenaryAwardMultiplier: the rate the contract pays now.</summary>
        public int Award { get; set; }

        /// <summary>The model's rate today: what the next monthly pay review would set.</summary>
        public int AwardToday { get; set; }

        /// <summary>Whole days until the monthly pay review (KingdomManager.PlayerMercenaryServiceNextRenewalDay);
        /// 0 or less = at the next daily update.</summary>
        public int DaysToReview { get; set; }

        /// <summary>The clan's influence: for a mercenary, pay not yet paid out (1/5 a day at the contract's rate).</summary>
        public float OwedInfluence { get; set; }

        public IReadOnlyList<string> Enemies { get; set; } = Array.Empty<string>();

        /// <summary>About how many denars the owed influence turns into.</summary>
        public int OwedGold => (int)Math.Round(Math.Max(0f, OwedInfluence) * Award);
    }

    /// <summary>One kingdom on the board: its facts and its verdict.</summary>
    public sealed class BoardEntry
    {
        public BoardEntry(KingdomFacts kingdom, Verdict verdict)
        {
            Kingdom = kingdom;
            Verdict = verdict;
        }

        public KingdomFacts Kingdom { get; }
        public Verdict Verdict { get; }
    }

    /// <summary>
    /// Every word the board shows (English only for now), and the list's order. Game-free and unit-tested; the module
    /// puts these strings into vanilla's selection pop-up and inquiry boxes.
    /// </summary>
    public static class BoardText
    {
        /// <summary>Vanilla's conversation gives this much influence on signing (GainKingdomInfluenceAction.ApplyForJoiningFaction).</summary>
        public const int SigningInfluence = 5;

        public const string Title = "Mercenary Contract Board";
        public const string MenuOption = "Mercenary Contract Board";
        public const string Accept = "Accept";
        public const string Close = "Close";
        public const string Yes = "Yes";
        public const string No = "No";
        public const string ConfirmFirstTitle = "Sign a contract";
        public const string ConfirmSwitchTitle = "Switch contract";

        private static readonly string PayExplained =
            "Pay is in denars for every point of influence you earn in their service, by fighting their enemies. Signing gives "
            + SigningInfluence + " influence. Hover a kingdom for its wars.";

        /// <summary>"1,234" (invariant, digits always).</summary>
        public static string Number(int value) => value.ToString("#,0", CultureInfo.InvariantCulture);

        /// <summary>"A", "A and B", "A, B and C"; "nobody" when empty.</summary>
        public static string JoinNames(IReadOnlyList<string> names)
        {
            if (names == null || names.Count == 0) return "nobody";
            if (names.Count == 1) return names[0];
            return string.Join(", ", names.Take(names.Count - 1)) + " and " + names[names.Count - 1];
        }

        /// <summary>The board's order: contracts on offer first (best pay first, or by name), then - when shown - the
        /// kingdoms that would not hire, by name.</summary>
        public static List<BoardEntry> Arrange(IEnumerable<BoardEntry> entries, BoardSettings settings)
        {
            var all = entries.ToList();
            var offered = all.Where(e => e.Verdict.Offered);
            offered = settings.SortByPay
                ? offered.OrderByDescending(e => e.Kingdom.Award).ThenBy(e => e.Kingdom.Name, StringComparer.Ordinal)
                : offered.OrderBy(e => e.Kingdom.Name, StringComparer.Ordinal);
            var list = offered.ToList();
            if (settings.ShowUnavailable)
                list.AddRange(all.Where(e => !e.Verdict.Offered).OrderBy(e => e.Kingdom.Name, StringComparer.Ordinal));
            return list;
        }

        /// <summary>The text above the list: the current contract (or standing), then a line about the offers.</summary>
        public static string Description(PlayerStanding standing, string employer, CurrentContract? current, int offeredCount)
        {
            var sb = new StringBuilder();
            switch (standing)
            {
                case PlayerStanding.Vassal:
                    return "You are a vassal of " + employer + ". Mercenary contracts are for clans outside a kingdom: "
                        + "nothing on this board is open to you.";
                case PlayerStanding.Ruler:
                    return "You rule " + employer + ". Mercenary contracts are for clans outside a kingdom: "
                        + "nothing on this board is open to you.";
                case PlayerStanding.Mercenary when current != null:
                    sb.Append(CurrentContractBlock(current));
                    sb.Append("\n\n");
                    sb.Append(offeredCount == 0
                        ? "No other kingdom would hire you right now."
                        : "Other contracts on offer: " + offeredCount + ". Signing one ends this contract first.");
                    break;
                default:
                    sb.Append("You are not under contract.\n\n");
                    sb.Append(offeredCount == 0
                        ? "No kingdom would hire you right now."
                        : "Contracts on offer: " + offeredCount + ".");
                    break;
            }
            if (offeredCount > 0) sb.Append(' ').Append(PayExplained);
            return sb.ToString();
        }

        /// <summary>The current contract, at the top of the board.</summary>
        public static string CurrentContractBlock(CurrentContract c)
        {
            var sb = new StringBuilder();
            sb.Append("YOUR CONTRACT: ").Append(c.Name);
            if (!string.IsNullOrEmpty(c.RulerName)) sb.Append(", ruled by ").Append(c.RulerName);
            sb.Append("\nPay: ").Append(Number(c.Award)).Append(" denars per influence point");
            sb.Append("\nPay review ").Append(c.DaysToReview <= 0 ? "at the next day's turn"
                : "in " + c.DaysToReview + (c.DaysToReview == 1 ? " day" : " days"));
            if (c.AwardToday != c.Award) sb.Append(" (the rate today would be ").Append(Number(c.AwardToday)).Append(')');
            sb.Append("\nOwed to you: ");
            int owed = (int)Math.Round(Math.Max(0f, c.OwedInfluence));
            sb.Append(owed == 0 ? "nothing yet"
                : owed + " influence, about " + Number(c.OwedGold) + " denars, paid out 1/5 a day");
            sb.Append("\nAt war with: ").Append(JoinNames(c.Enemies));
            return sb.ToString();
        }

        /// <summary>One list line: "Battania (Caladog): 90 per influence" or "Sturgia (Raganvad): not at war".</summary>
        public static string EntryTitle(BoardEntry e)
        {
            var k = e.Kingdom;
            string who = string.IsNullOrEmpty(k.RulerName) ? k.Name : k.Name + " (" + k.RulerName + ")";
            return e.Verdict.Offered
                ? who + ": " + Number(k.Award) + " per influence"
                : who + ": " + ContractRules.ShortLabel(e.Verdict.Refusal);
        }

        /// <summary>The tooltip of a list line: ruler, pay (or the reason), the signing value, the wars.</summary>
        public static string EntryHint(BoardEntry e)
        {
            var k = e.Kingdom;
            var sb = new StringBuilder();
            sb.Append(k.Name);
            if (!string.IsNullOrEmpty(k.RulerName)) sb.Append(", ruled by ").Append(k.RulerName);
            if (e.Verdict.Offered)
            {
                sb.Append("\nPay: ").Append(Number(k.Award)).Append(" denars per influence point");
                sb.Append("\nSigning: ").Append(SigningInfluence).Append(" influence = ")
                    .Append(Number(SigningInfluence * k.Award)).Append(" denars");
            }
            else
            {
                sb.Append("\nNot offered: ").Append(e.Verdict.Reason);
            }
            sb.Append("\nAt war with: ").Append(JoinNames(k.Enemies));
            return sb.ToString();
        }

        /// <summary>The "are you sure" for a first contract.</summary>
        public static string ConfirmFirst(KingdomFacts k)
        {
            string who = string.IsNullOrEmpty(k.RulerName) ? k.Name : k.Name + ", ruled by " + k.RulerName;
            return "Sign a mercenary contract with " + who + "?\n\n"
                + "Pay: " + Number(k.Award) + " denars per influence point. Signing gives " + SigningInfluence + " influence.\n"
                + "You will be at war with: " + JoinNames(k.Enemies) + ".";
        }

        /// <summary>The switch warning: leaving the current contract first, and what that costs.</summary>
        public static string ConfirmSwitch(CurrentContract current, KingdomFacts k)
        {
            var sb = new StringBuilder();
            sb.Append("Leave your contract with ").Append(current.Name).Append(" and sign with ").Append(k.Name).Append("?\n\n");
            sb.Append("Leaving ").Append(current.Name).Append(" ends your wars as their mercenary. ");
            int owed = (int)Math.Round(Math.Max(0f, current.OwedInfluence));
            if (owed > 0)
                sb.Append("The ").Append(owed).Append(" influence still owed to you (about ").Append(Number(current.OwedGold))
                    .Append(" denars) is lost, as when you end a contract by talking to a lord.");
            else
                sb.Append("Nothing is owed to you right now.");
            sb.Append("\n\n").Append(k.Name).Append(" pays ").Append(Number(k.Award))
                .Append(" denars per influence point (worked out again as you sign). Signing gives ")
                .Append(SigningInfluence).Append(" influence.\n");
            sb.Append("You will be at war with: ").Append(JoinNames(k.Enemies)).Append('.');
            return sb.ToString();
        }

        /// <summary>The message after signing.</summary>
        public static string Signed(string kingdom, int award) =>
            "You are now a mercenary of " + kingdom + ": " + Number(award) + " denars per influence point.";
    }
}
