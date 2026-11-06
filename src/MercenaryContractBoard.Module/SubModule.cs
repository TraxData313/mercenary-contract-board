using System;
using MercenaryContractBoard.Core;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;

namespace MercenaryContractBoard
{
    /// <summary>
    /// Adds the contract board behaviour at every campaign start, plus the optional MCM page. No Harmony, no models.
    /// </summary>
    public class SubModule : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            base.OnSubModuleLoad();
            ModLog.Info("load", ModInfo.DisplayName + " loaded (" + typeof(SubModule).Assembly.GetName().Version + ")");
        }

        // MCM's own module registers its services in OnSubModuleLoad; the main menu is the first safe point for a page.
        protected override void OnBeforeInitialModuleScreenSetAsRoot()
        {
            base.OnBeforeInitialModuleScreenSetAsRoot();
            McmSettings.TryRegister("main menu");
        }

        protected override void OnGameStart(Game game, IGameStarter gameStarterObject)
        {
            base.OnGameStart(game, gameStarterObject);
            McmSettings.TryRegister("game start"); // no-op once registered (normally at the main menu)
            try
            {
                if (!(game?.GameType is Campaign) || !(gameStarterObject is CampaignGameStarter starter)) return;
                ModLog.Info("load", "campaign start: " + game.GameType.GetType().Name + "; settings " + McmSettings.Describe(Board.Settings));
                starter.AddBehavior(new ContractBoardBehavior());
            }
            catch (Exception e)
            {
                ModLog.Error("load", "OnGameStart", e);
            }
        }
    }
}
