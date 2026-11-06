using System;
using System.Collections.Generic;
using System.Linq;
using MercenaryContractBoard.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.CampaignSystem.GameMenus;
using TaleWorlds.Core;
using TaleWorlds.Core.ImageIdentifiers;
using TaleWorlds.Library;
using TaleWorlds.Localization;

namespace MercenaryContractBoard
{
    /// <summary>
    /// Adds "Mercenary Contract Board" to every town's menu and shows the board in vanilla's selection pop-up
    /// (kingdom banners, greyed lines with the reason in the tooltip), then vanilla inquiry boxes to confirm.
    /// Saves nothing: <see cref="SyncData"/> is empty, so the mod can be removed mid-campaign.
    /// </summary>
    internal sealed class ContractBoardBehavior : CampaignBehaviorBase
    {
        internal const string MenuOptionId = "mercenary_contract_board";

        private static readonly Color Orange = Color.FromUint(0xFFFFA028);

        public override void RegisterEvents()
        {
            CampaignEvents.OnSessionLaunchedEvent.AddNonSerializedListener(this, OnSessionLaunched);
        }

        public override void SyncData(IDataStore dataStore)
        {
            // Nothing saved: the contract itself is vanilla's state on the clan.
        }

        private void OnSessionLaunched(CampaignGameStarter starter)
        {
            try
            {
                // Towns only: War Sails' port is a sub-menu of the town ("Go to the port"), so the town menu covers port towns.
                starter.AddGameMenuOption("town", MenuOptionId, BoardText.MenuOption, MenuCondition, MenuConsequence, isLeave: false, index: 6);
                ModLog.Info("menu", "town menu option added");
            }
            catch (Exception e)
            {
                ModLog.Error("menu", "town menu option NOT added", e);
            }
        }

        private static bool MenuCondition(MenuCallbackArgs args)
        {
            try
            {
                if (!Board.Settings.Enabled) return false;
                args.optionLeaveType = GameMenuOption.LeaveType.Submenu;
                args.Tooltip = new TextObject("{=!}See which kingdoms would hire you as a mercenary, and for how much.");
                return true;
            }
            catch (Exception e)
            {
                ModLog.ErrorOnce("menu.condition", e);
                return false;
            }
        }

        private static void MenuConsequence(MenuCallbackArgs args)
        {
            try
            {
                ShowBoard();
            }
            catch (Exception e)
            {
                ModLog.ErrorOnce("menu.open", e);
            }
        }

        private static void ShowBoard()
        {
            var snap = Board.Read();
            var arranged = BoardText.Arrange(snap.Offers.Select(o => o.Entry), Board.Settings);
            int offered = snap.Offers.Count(o => o.Entry.Verdict.Offered);
            string description = BoardText.Description(snap.Standing, snap.Employer, snap.Current, offered);
            ModLog.Info("board", "opened: " + snap.Standing + ", " + offered + " of " + snap.Offers.Count + " kingdoms offer a contract");

            var elements = new List<InquiryElement>();
            foreach (var entry in arranged)
            {
                var offer = snap.Offers.First(o => ReferenceEquals(o.Entry, entry));
                elements.Add(new InquiryElement(offer, BoardText.EntryTitle(entry), new BannerImageIdentifier(offer.Kingdom.Banner),
                    entry.Verdict.Offered, BoardText.EntryHint(entry)));
            }

            if (elements.Count == 0)
            {
                InformationManager.ShowInquiry(new InquiryData(BoardText.Title, description, true, false, BoardText.Close, "", null, null), true);
                return;
            }
            MBInformationManager.ShowMultiSelectionInquiry(new MultiSelectionInquiryData(BoardText.Title, description, elements,
                isExitShown: true, minSelectableOptionCount: 1, maxSelectableOptionCount: 1,
                BoardText.Accept, BoardText.Close, OnPicked, null), pauseGameActiveState: true);
        }

        private static void OnPicked(List<InquiryElement> chosen)
        {
            try
            {
                if (!(chosen?.FirstOrDefault()?.Identifier is Offer picked)) return;
                // Read again: the answer must be the one vanilla would give at the moment of signing.
                var snap = Board.Read();
                var fresh = snap.Offers.FirstOrDefault(o => o.Kingdom == picked.Kingdom);
                if (fresh == null || !fresh.Entry.Verdict.Offered)
                {
                    InformationManager.DisplayMessage(new InformationMessage(picked.Entry.Kingdom.Name + " no longer offers you a contract"
                        + (fresh != null ? ": " + fresh.Entry.Verdict.Reason : "."), Orange));
                    ShowBoard();
                    return;
                }

                var kingdom = fresh.Kingdom;
                var facts = fresh.Entry.Kingdom;
                if (snap.Standing == PlayerStanding.Mercenary && snap.Current != null)
                    Confirm(BoardText.ConfirmSwitchTitle, BoardText.ConfirmSwitch(snap.Current, facts), kingdom);
                else if (Board.Settings.ConfirmFirstContract)
                    Confirm(BoardText.ConfirmFirstTitle, BoardText.ConfirmFirst(facts), kingdom);
                else
                    SignWith(kingdom);
            }
            catch (Exception e)
            {
                ModLog.ErrorOnce("board.pick", e);
            }
        }

        private static void Confirm(string title, string text, Kingdom kingdom)
        {
            var target = new SignTarget(kingdom);
            InformationManager.ShowInquiry(new InquiryData(title, text, true, true, BoardText.Yes, BoardText.No,
                target.Sign, BackToBoard), true);
        }

        private static void BackToBoard()
        {
            try
            {
                ShowBoard();
            }
            catch (Exception e)
            {
                ModLog.ErrorOnce("board.back", e);
            }
        }

        private static void SignWith(Kingdom kingdom)
        {
            try
            {
                int award = Board.Sign(kingdom);
                if (award < 0) return;
                InformationManager.DisplayMessage(new InformationMessage(BoardText.Signed(kingdom.Name.ToString(), award), Orange));
                Campaign.Current?.CurrentMenuContext?.Refresh();
            }
            catch (Exception e)
            {
                ModLog.Error("contract", "signing with " + kingdom?.StringId + " failed", e);
                InformationManager.DisplayMessage(new InformationMessage(ModInfo.DisplayName + ": signing failed, see the mod log.", Colors.Red));
            }
        }

        /// <summary>The Yes button of a confirmation (a plain Action, no closure over game state beyond the kingdom).</summary>
        private sealed class SignTarget
        {
            private readonly Kingdom _kingdom;

            public SignTarget(Kingdom kingdom) => _kingdom = kingdom;

            public void Sign() => SignWith(_kingdom);
        }
    }
}
