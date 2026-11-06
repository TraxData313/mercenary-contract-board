# Manager progress: Mercenary Contract Board

## Task (Toni, 2026-11-06)
New separate Bannerlord mod `mercenary-contract-board` (folder `Documents\BannerlordMods\mercenary-contract-board`,
module Id `MercenaryContractBoard`, display "Mercenary Contract Board", dev deploy `Modules\MercenaryContractBoard.Dev`).
Sister mod of `troop-training-requirement` - copy its conventions, tooling and code style.

- Every town menu: "Mercenary Contract Board". Lists each kingdom's mercenary contract: kingdom, ruler, pay (vanilla's
  award, gold per influence point, + an honest practical figure), its wars. Kingdoms not offering: greyed with reason.
- Under contract: the current contract at the top (kingdom, rate, wars). Accepting another asks "you will leave your
  contract with X" - Yes = leave as mercenary, then join, with vanilla's actions; No = back.
- Vassal / ruler: the board says so, offers nothing.
- Vanilla's conditions and award numbers exactly (the board is a view of vanilla's offers).
- Vanilla UI (selection pop-up), orange #FFA028 accent where a colour is used. MCM optional.
- No Harmony if possible, nothing saved, decorator-only models, callbacks never throw, AssemblyGuard.

## Steps
1. [x] Study vanilla (LordConversationsCampaignBehavior, FactionHelper, DefaultMinorFactionsModel, ChangeKingdomAction,
   ClanFinance / ClanPolitics, VassalAndMercenaryOfferCampaignBehavior) - findings in CLAUDE.md.
2. [x] Scaffold from the sister (Core / Module / tests, tools, .gitignore, LICENSE).
3. [x] Core: ContractRules, BoardText, settings + tests (40).
4. [x] Module: Board (game reads, Sign), ContractBoardBehavior (town menu, pop-ups), McmSettings, SubModule.
5. [x] Docs: README, CLAUDE.md, store text drafts in tools\. Private GitHub repo, deploy.
6. [ ] Toni tests in game; screenshots; thumbnail; publish when he says so.

## Log
- 2026-11-06 steps 1-5: built 0/0, 40 tests pass, AssemblyGuard OK (MCMv5), deployed to Modules\MercenaryContractBoard.Dev.
  No Harmony, no models, nothing saved. Not tested in game. Nothing uploaded.
  Open: the courier offer uses a different vanilla award (neededAmountForClanToJoinCalculation: true) - the board shows
  the conversation's; switching while in a town of the kingdom you leave leaves you in an enemy town (vanilla allows
  the same via a lord in a tavern).
