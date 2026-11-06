using System;
using System.Collections.Generic;

namespace MercenaryContractBoard.Core
{
    /// <summary>Where the player's clan stands, for the board.</summary>
    public enum PlayerStanding
    {
        /// <summary>No kingdom: contracts are open (vanilla's own case).</summary>
        Independent,

        /// <summary>Under a mercenary contract: other contracts are open as a switch (leave, then sign).</summary>
        Mercenary,

        /// <summary>A vassal clan of a kingdom: no contracts.</summary>
        Vassal,

        /// <summary>The ruling clan of a kingdom: no contracts.</summary>
        Ruler,
    }

    /// <summary>Why a kingdom would not offer a contract, in the order vanilla checks.</summary>
    public enum Refusal
    {
        None,
        Vassal,
        Ruler,
        InArmy,
        AtWarWithYou,
        ClanTier,
        Relation,
        UnsharedWars,
        OwnsSettlement,
        NotAtWar,
        UnknownClan,
        NotNeeded,
    }

    /// <summary>
    /// Everything the rules need about one kingdom and the player, read from the game by the module. For a player under
    /// contract, the "player" fields describe the clan as it would be right after leaving the current contract
    /// (vanilla's ChangeKingdomAction.LeaveAsMercenary makes peace with everyone, so: not at war with this kingdom,
    /// no wars of its own).
    /// </summary>
    public sealed class KingdomFacts
    {
        /// <summary>Kingdom.Name, e.g. "Vlandia", "Northern Empire".</summary>
        public string Name { get; set; } = "";

        public string RulerName { get; set; } = "";

        /// <summary>The player's clan is at war with this kingdom.</summary>
        public bool PlayerAtWarWith { get; set; }

        public int ClanTier { get; set; }

        /// <summary>ClanTierModel.MercenaryEligibleTier (vanilla 1).</summary>
        public int MercenaryEligibleTier { get; set; } = 1;

        /// <summary>The ruler's relation with the player.</summary>
        public float RulerRelation { get; set; }

        /// <summary>DiplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom (vanilla -10).</summary>
        public int MinimumRelation { get; set; } = -10;

        /// <summary>Kingdoms the player's clan is at war with (and that are stronger than 5% of this kingdom), which
        /// this kingdom is NOT at war with. Vanilla refuses while any are left.</summary>
        public IReadOnlyList<string> UnsharedPlayerWars { get; set; } = Array.Empty<string>();

        public bool PlayerOwnsSettlements { get; set; }

        /// <summary>The kingdoms this kingdom is at war with (FactionHelper.GetEnemyKingdoms), by name.</summary>
        public IReadOnlyList<string> Enemies { get; set; } = Array.Empty<string>();

        /// <summary>Kingdom.CurrentTotalStrength.</summary>
        public float Strength { get; set; }

        /// <summary>FactionHelper.GetTotalEnemyKingdomPower: the summed strength of its enemy kingdoms.</summary>
        public float EnemyStrength { get; set; }

        public float PlayerRenown { get; set; }

        /// <summary>Clan.PlayerClan.CurrentTotalStrength.</summary>
        public float PlayerClanStrength { get; set; }

        /// <summary>MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom: gold per influence point.</summary>
        public int Award { get; set; }
    }

    /// <summary>A kingdom's answer: offered, or the first reason it refuses.</summary>
    public readonly struct Verdict
    {
        public Verdict(Refusal refusal, string reason)
        {
            Refusal = refusal;
            Reason = reason;
        }

        public Refusal Refusal { get; }

        /// <summary>One line, empty when offered.</summary>
        public string Reason { get; }

        public bool Offered => Refusal == Refusal.None;

        public static Verdict Yes => new Verdict(Refusal.None, "");
    }

    /// <summary>
    /// Vanilla's mercenary offer, decided the way the lord conversation decides it (Bannerlord 1.4.8,
    /// LordConversationsCampaignBehavior "I would like to enter your service" -> "My sword is yours. For the right
    /// sum." -> the lord's answer). The order matters: the first failing check is the reason shown.
    /// <list type="number">
    /// <item>The player line only shows for a kingdom the player is not at war with
    ///   (conversation_player_want_to_join_faction_as_mercenary_or_vassal_on_condition).</item>
    /// <item>It is clickable only when FactionHelper.CanPlayerOfferMercenaryService holds: clan tier &gt;=
    ///   MercenaryEligibleTier, the ruler's relation &gt;= MinimumRelationWithConversationCharacterToJoinKingdom, every
    ///   war of the player against a kingdom stronger than 5% of this one is shared by this kingdom, and the clan owns no
    ///   settlement (hint order as vanilla's).</item>
    /// <item>Then the lord answers, first match wins: no enemy kingdoms ("not at war"); FactionHelper.CanPlayerEnterFaction
    ///   fails ("I have hardly heard of you"); total enemy kingdom strength &lt; 1 ("not at war"); own strength more than
    ///   3 times the enemies' ("we do not need hired swords to win this war"); else the offer, at
    ///   MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom gold per influence point.</item>
    /// </list>
    /// </summary>
    public static class ContractRules
    {
        /// <summary>FactionHelper.CanPlayerEnterFaction (mercenary case): the score must exceed this.</summary>
        public const float RenownScoreNeeded = 25f;

        /// <summary>Lord answer: "We do not need hired swords to win this war" above this strength ratio.</summary>
        public const float NotNeededPowerRatio = 3f;

        /// <summary>
        /// FactionHelper.CanPlayerEnterFaction(asVassal: false), 1.4.8: renown + min(10, renown) / 10 * 0.2 * clan
        /// strength + ruler relation * 2. Vanilla accepts above 25.
        /// </summary>
        public static float RenownScore(float renown, float clanStrength, float rulerRelation) =>
            renown + Math.Min(10f, renown) / 10f * 0.2f * clanStrength + rulerRelation * 2f;

        /// <summary>FactionHelper.GetPowerRatioToEnemies.</summary>
        public static float PowerRatio(float strength, float enemyStrength) => strength / (enemyStrength + 0.0001f);

        /// <param name="standing">The player's clan.</param>
        /// <param name="employer">The name of the player's kingdom (vassal, ruler or current employer), or empty.</param>
        /// <param name="inArmy">The player's party is in an army (vanilla won't end a contract then).</param>
        /// <param name="k">This kingdom.</param>
        public static Verdict Evaluate(PlayerStanding standing, string employer, bool inArmy, KingdomFacts k)
        {
            switch (standing)
            {
                case PlayerStanding.Vassal:
                    return new Verdict(Refusal.Vassal, "You are a vassal of " + Plain(employer) + ": contracts are for clans outside a kingdom.");
                case PlayerStanding.Ruler:
                    return new Verdict(Refusal.Ruler, "You rule " + Plain(employer) + ": contracts are for clans outside a kingdom.");
                case PlayerStanding.Mercenary when inArmy:
                    return new Verdict(Refusal.InArmy, "Leave your army first: a contract can't be ended while you are in an army.");
            }

            if (k.PlayerAtWarWith)
                return new Verdict(Refusal.AtWarWithYou, "Your clan is at war with them.");
            if (k.ClanTier < k.MercenaryEligibleTier)
                return new Verdict(Refusal.ClanTier, "Your clan tier needs to be " + k.MercenaryEligibleTier + ".");
            if (k.RulerRelation < k.MinimumRelation)
                return new Verdict(Refusal.Relation, "You need " + k.MinimumRelation + " relation with " + k.RulerName
                    + " (you have " + (int)Math.Floor(k.RulerRelation) + ").");
            if (k.UnsharedPlayerWars.Count > 0)
                return new Verdict(Refusal.UnsharedWars, "Your clan is at war with " + BoardText.JoinNames(k.UnsharedPlayerWars)
                    + ", and they are not.");
            if (k.PlayerOwnsSettlements)
                return new Verdict(Refusal.OwnsSettlement, "Clans that own a settlement can't be mercenaries.");

            if (k.Enemies.Count == 0)
                return new Verdict(Refusal.NotAtWar, "Not at war: they have no use for mercenaries.");
            if (RenownScore(k.PlayerRenown, k.PlayerClanStrength, k.RulerRelation) <= RenownScoreNeeded)
                return new Verdict(Refusal.UnknownClan, "They have hardly heard of you: win some renown (or " + k.RulerName + "'s favour).");
            if (k.EnemyStrength < 1f)
                return new Verdict(Refusal.NotAtWar, "Not at war: they have no use for mercenaries.");
            if (PowerRatio(k.Strength, k.EnemyStrength) > NotNeededPowerRatio)
                return new Verdict(Refusal.NotNeeded, "Winning their wars without help: more than 3 times their enemies' strength.");
            return Verdict.Yes;
        }

        /// <summary>An empty name becomes "a kingdom".</summary>
        private static string Plain(string name) => string.IsNullOrEmpty(name) ? "a kingdom" : name;

        /// <summary>
        /// The short grey label after the kingdom's name in the list (the full reason is in the tooltip).
        /// </summary>
        public static string ShortLabel(Refusal refusal) => refusal switch
        {
            Refusal.None => "",
            Refusal.Vassal => "you are a vassal",
            Refusal.Ruler => "you are a ruler",
            Refusal.InArmy => "you are in an army",
            Refusal.AtWarWithYou => "at war with you",
            Refusal.ClanTier => "clan tier too low",
            Refusal.Relation => "relation too low",
            Refusal.UnsharedWars => "your wars aren't theirs",
            Refusal.OwnsSettlement => "you own a settlement",
            Refusal.NotAtWar => "not at war",
            Refusal.UnknownClan => "never heard of you",
            Refusal.NotNeeded => "not hiring",
            _ => "not hiring",
        };
    }
}
