using System;
using System.Collections.Generic;
using System.Linq;
using MercenaryContractBoard.Core;
using Helpers;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.Actions;
using TaleWorlds.CampaignSystem.Party;
using TaleWorlds.Library;

namespace MercenaryContractBoard
{
    /// <summary>One kingdom on the board and the game object behind it.</summary>
    internal sealed class Offer
    {
        public Offer(Kingdom kingdom, BoardEntry entry)
        {
            Kingdom = kingdom;
            Entry = entry;
        }

        public Kingdom Kingdom { get; }
        public BoardEntry Entry { get; }
    }

    /// <summary>What the board shows right now.</summary>
    internal sealed class Snapshot
    {
        public PlayerStanding Standing { get; set; }
        public string Employer { get; set; } = "";
        public CurrentContract? Current { get; set; }
        public List<Offer> Offers { get; } = new List<Offer>();
    }

    /// <summary>
    /// The game side: reads vanilla's numbers (the same models and helpers the lord conversation uses) into
    /// <see cref="KingdomFacts"/> for <see cref="ContractRules"/>, and signs / leaves contracts with vanilla's own actions,
    /// exactly as the conversation's consequences do. Nothing of ours is stored anywhere: the contract lives on the clan.
    /// </summary>
    internal static class Board
    {
        /// <summary>The live settings (MCM writes into this object through its ProxyRefs).</summary>
        public static BoardSettings Settings { get; } = new BoardSettings();

        public static PlayerStanding StandingOf(Clan clan)
        {
            var own = clan.Kingdom;
            if (own == null) return PlayerStanding.Independent;
            if (clan.IsUnderMercenaryService) return PlayerStanding.Mercenary;
            return own.RulingClan == clan ? PlayerStanding.Ruler : PlayerStanding.Vassal;
        }

        /// <summary>Reads the board. Throws only if the campaign itself is unusable; a failing kingdom is skipped.</summary>
        public static Snapshot Read()
        {
            var clan = Clan.PlayerClan;
            var snap = new Snapshot { Standing = StandingOf(clan) };
            var own = clan.Kingdom;
            if (own != null) snap.Employer = own.Name?.ToString() ?? "";
            bool inArmy = MobileParty.MainParty?.Army != null;

            if (snap.Standing == PlayerStanding.Mercenary && own != null)
                snap.Current = ReadContract(clan, own);

            foreach (var kingdom in Kingdom.All.ToList())
            {
                if (kingdom == null || kingdom.IsEliminated || kingdom == own || kingdom.Leader == null) continue;
                try
                {
                    var facts = Facts(kingdom, clan, snap.Standing);
                    var verdict = ContractRules.Evaluate(snap.Standing, snap.Employer, inArmy, facts);
                    if (snap.Standing == PlayerStanding.Independent) CrossCheck(kingdom, verdict);
                    snap.Offers.Add(new Offer(kingdom, new BoardEntry(facts, verdict)));
                }
                catch (Exception e)
                {
                    ModLog.ErrorOnce("board.kingdom", e);
                }
            }
            return snap;
        }

        /// <summary>
        /// One kingdom, as FactionHelper.CanPlayerOfferMercenaryService and the lord's answers see it. For a player under
        /// contract the clan is taken as it would be right after leaving: ChangeKingdomAction (LeaveAsMercenary) makes
        /// peace with every faction it is at war with, so no war with this kingdom and no wars of its own.
        /// </summary>
        private static KingdomFacts Facts(Kingdom kingdom, Clan clan, PlayerStanding standing)
        {
            var models = Campaign.Current.Models;
            bool afterLeaving = standing == PlayerStanding.Mercenary;
            var facts = new KingdomFacts
            {
                Name = kingdom.Name?.ToString() ?? kingdom.StringId,
                RulerName = kingdom.Leader?.Name?.ToString() ?? "",
                PlayerAtWarWith = !afterLeaving && clan.IsAtWarWith(kingdom),
                ClanTier = clan.Tier,
                MercenaryEligibleTier = models.ClanTierModel.MercenaryEligibleTier,
                RulerRelation = kingdom.Leader!.GetRelationWithPlayer(),
                MinimumRelation = models.DiplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom,
                PlayerOwnsSettlements = clan.Settlements.Count > 0,
                Enemies = EnemyNames(kingdom),
                Strength = kingdom.CurrentTotalStrength,
                EnemyStrength = FactionHelper.GetTotalEnemyKingdomPower(kingdom),
                PlayerRenown = clan.Renown,
                PlayerClanStrength = clan.CurrentTotalStrength,
            };
            if (!afterLeaving)
            {
                float threshold = models.DiplomacyModel.GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom(kingdom);
                facts.UnsharedPlayerWars = Kingdom.All
                    .Where(x => x != null && clan.MapFaction.IsAtWarWith(x) && x.CurrentTotalStrength > threshold && !kingdom.IsAtWarWith(x))
                    .Select(x => x.Name?.ToString() ?? x.StringId)
                    .ToList();
            }
            try
            {
                facts.Award = models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(clan, kingdom);
            }
            catch (Exception e)
            {
                ModLog.ErrorOnce("board.award", e);
            }
            return facts;
        }

        private static CurrentContract ReadContract(Clan clan, Kingdom own)
        {
            var c = new CurrentContract
            {
                Name = own.Name?.ToString() ?? own.StringId,
                RulerName = own.Leader?.Name?.ToString() ?? "",
                Award = clan.MercenaryAwardMultiplier,
                OwedInfluence = clan.Influence,
                Enemies = EnemyNames(own),
            };
            c.AwardToday = c.Award;
            try
            {
                // ClanVariablesCampaignBehavior re-rates the player's contract with this same call every 30 days.
                c.AwardToday = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(clan, own);
                float hoursLeft = Campaign.Current.KingdomManager.PlayerMercenaryServiceNextRenewalDay - (float)Campaign.CurrentTime;
                c.DaysToReview = (int)Math.Ceiling(hoursLeft / CampaignTime.HoursInDay);
            }
            catch (Exception e)
            {
                ModLog.ErrorOnce("board.contract", e);
            }
            return c;
        }

        private static List<string> EnemyNames(Kingdom kingdom) =>
            FactionHelper.GetEnemyKingdoms(kingdom)
                .Where(k => k != null && !k.IsEliminated)
                .Select(k => k.Name?.ToString() ?? k.StringId)
                .OrderBy(n => n, StringComparer.Ordinal)
                .ToList();

        /// <summary>
        /// For a clan outside any kingdom, vanilla's own FactionHelper.CanPlayerOfferMercenaryService must agree with
        /// our first five checks. A disagreement (a mod changed the rule, or a patch) is logged once; the board keeps
        /// our answer, which spells out the reason.
        /// </summary>
        private static void CrossCheck(Kingdom kingdom, Verdict verdict)
        {
            try
            {
                bool vanilla = FactionHelper.CanPlayerOfferMercenaryService(kingdom, out _, out _);
                bool ours = verdict.Refusal != Refusal.AtWarWithYou && verdict.Refusal != Refusal.ClanTier
                    && verdict.Refusal != Refusal.Relation && verdict.Refusal != Refusal.UnsharedWars
                    && verdict.Refusal != Refusal.OwnsSettlement;
                if (vanilla != ours)
                    ModLog.Info("crosscheck", "FactionHelper.CanPlayerOfferMercenaryService(" + kingdom.StringId + ") = " + vanilla
                        + " but the board says " + (verdict.Offered ? "offered" : verdict.Refusal.ToString()) + " - please report");
            }
            catch (Exception e)
            {
                ModLog.ErrorOnce("board.crosscheck", e);
            }
        }

        /// <summary>
        /// Signs with <paramref name="target"/>, exactly as vanilla's conversation does: when under contract first the
        /// "end my contract" consequence (influence to 0, ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary), then the
        /// "All right. I accept" consequence (award from the model, ApplyByJoinFactionAsMercenary, 5 influence for
        /// joining). Returns the award signed for, or -1 when nothing changed.
        /// </summary>
        public static int Sign(Kingdom target)
        {
            var clan = Clan.PlayerClan;
            var standing = StandingOf(clan);
            if (standing == PlayerStanding.Vassal || standing == PlayerStanding.Ruler || target == null || target.IsEliminated)
                return -1;
            if (standing == PlayerStanding.Mercenary)
            {
                string left = clan.Kingdom?.StringId ?? "?";
                ChangeClanInfluenceAction.Apply(clan, -clan.Influence);
                ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary(clan);
                ModLog.Info("contract", "left " + left + " as a mercenary (influence set to 0, as vanilla)");
            }
            // Worked out again now: leaving can shift the strengths a little, and this is what the lord would say now.
            int award = Campaign.Current.Models.MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(clan, target);
            ChangeKingdomAction.ApplyByJoinFactionAsMercenary(clan, target, default(CampaignTime), award);
            GainKingdomInfluenceAction.ApplyForJoiningFaction(Hero.MainHero, BoardText.SigningInfluence);
            ModLog.Info("contract", "signed with " + target.StringId + " at " + award + " gold per influence point");
            return award;
        }
    }
}
