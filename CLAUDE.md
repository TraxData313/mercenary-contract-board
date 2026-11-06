# Mercenary Contract Board

Bannerlord 1.4.8 (+ War Sails) mod: every town menu gets "Mercenary Contract Board", a vanilla selection pop-up
listing the mercenary contracts each kingdom would offer the player right now (pay, ruler, wars), greyed lines with
the reason for the rest, the current contract at the top, and Accept (switching asks first). It is only a VIEW of
vanilla's offers: same conditions, same award numbers, same actions as the lord conversation.

Sister mod of `..\troop-training-requirement` - same layout, tooling, code style and rules; when in doubt, do what it
does. Status: `.manager\progress.md` (committed). Built, 40 tests, tested in game by Toni (screenshots in `docs/screenshots`). v1.0.0 published 2026-11-06: GitHub
release, Steam item 3834681925 (hidden by the Steam block); Nexus mods/14539. Player docs:
`README.md`.

## Vanilla, as found in the 1.4.8 decompile (`%TEMP%\trax_decomp\full_TaleWorlds.CampaignSystem`)

The player-initiated path, `CampaignBehaviors\LordConversationsCampaignBehavior.cs`:
- "I would like to enter {FACTION_SERVICE_TERM}." (`conversation_player_want_to_join_faction_as_mercenary_or_vassal_on_condition`):
  the lord is a Lord of a kingdom, not a prisoner, the player is not in a kingdom OR is a mercenary, and the player's
  map faction is not at war with that kingdom.
- "My sword is yours. For the right sum." shows only when the player is in NO kingdom
  (`conversation_player_is_offering_mercenary_on_condition`: `!Hero.MainHero.MapFaction.IsKingdomFaction`) - so in
  vanilla a mercenary must end the contract first ("I would like to end my contract", needs `MainParty.Army == null`;
  consequence: influence set to 0, `ChangeKingdomAction.ApplyByLeaveKingdomAsMercenary`). The board's switch does
  exactly those two steps in a row.
- Clickable only if `Helpers\FactionHelper.CanPlayerOfferMercenaryService`: `PlayerClan.Kingdom == null`, not at war
  with the kingdom, `Tier >= ClanTierModel.MercenaryEligibleTier` (1), ruler's relation >=
  `DiplomacyModel.MinimumRelationWithConversationCharacterToJoinKingdom` (-10), every kingdom the player is at war
  with whose strength > `GetStrengthThresholdForNonMutualWarsToBeIgnoredToJoinKingdom` (5% of the kingdom's) is also
  its enemy, and the clan owns no settlement. (The hint text names the conversation hero's relation, the check uses
  the RULER's - we follow the check.)
- The lord's answer, first match (lines 834-838): no enemy kingdoms -> "not at war"; `FactionHelper.CanPlayerEnterFaction`
  false (renown + min(10, renown)/10 * 0.2 * clan strength + ruler relation * 2 must be > 25; it reads
  `Hero.OneToOneConversationHero`, so we re-implement it in Core) -> "hardly heard of you";
  `GetTotalEnemyKingdomPower < 1` -> "not at war"; `GetPowerRatioToEnemies > 3` -> "we don't need hired swords";
  else the pitch with `GetMercenaryAwardFactor()` = `MinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom(PlayerClan, kingdom)`.
- Accept (`conversation_mercenary_player_accepts_lord_answer_on_consequence`): `ChangeKingdomAction.ApplyByJoinFactionAsMercenary(PlayerClan,
  kingdom, default, award)` + `GainKingdomInfluenceAction.ApplyForJoiningFaction(MainHero, 5f)`.
- So **only kingdoms at war offer contracts** (verified: two of the lord's refusals are "not at war").

Award (`GameComponents\DefaultMinorFactionsModel.GetMercenaryAwardFactorToJoinKingdom`, conversation case
`neededAmountForClanToJoinCalculation: false`): ceil(clamp(1, 80, clanCountFactor * powerRatioFactor * tributeFactor
* (1 + 0.15 * tier) * lordWealthFactor * lordPovertyFactor * fiefCountFactor * 6)) * 10, i.e. 10..800 gold per
influence point; the boosts are damped by the number of mercenary clans already hired; +15% for the Vlandian
mercenary culture feat. We call the MODEL (so a mod's model is respected), never re-implement it.

Pay (`DefaultClanFinanceModel.AddMercenaryIncome` + `DefaultClanPoliticsModel`): each day ceil(influence / 5)
influence is removed and paid as that * `Clan.MercenaryAwardMultiplier` gold. `ClanVariablesCampaignBehavior` re-rates
the player's multiplier with the same model call every 30 days (`KingdomManager.PlayerMercenaryServiceNextRenewalDay`,
in campaign hours). `LeaveAsMercenary` makes peace with every faction the clan is at war with (not constant wars) -
which is why a mercenary's hypothetical "after leaving" has no wars.

Other vanilla offer paths (not used by the board): `VassalAndMercenaryOfferCampaignBehavior` - a courier offer, 2%
a day, only at clan tier == MercenaryEligibleTier, same `CanPlayerOfferMercenaryService` plus the ruler not a prisoner
/ fugitive, priced with `neededAmountForClanToJoinCalculation: true` (a DIFFERENT number: (1 + 0.15 tier) * (20 minus
low-gold penalties) * 10, relation-free). `lord_request_mission_ask` ("I promised {FACTION_LEADER} I would hire a
company of mercenaries", fixed award 50 + an advance) has no input token anywhere: dead dialogue in 1.4.8.

## Layout

- `src\MercenaryContractBoard.Core` - netstandard2.0, **no game references**: `ContractRules` (vanilla's conditions
  in vanilla's order, reasons, the renown score), `BoardText` (every string of the board, list order), settings and
  the MCM menu description.
- `src\MercenaryContractBoard.Module` - net472 module (assembly `MercenaryContractBoard.dll`, namespace
  `MercenaryContractBoard`): `SubModule`, `ModLog`, `McmSettings`, `Board` (reads the game into `KingdomFacts`; signs
  with vanilla's actions; cross-checks `FactionHelper.CanPlayerOfferMercenaryService` for an independent clan and logs
  a disagreement once), `ContractBoardBehavior` (town menu option, the pop-ups).
  No Harmony, no models, nothing saved (`SyncData` empty). MCM referenced, never shipped.
- `tests\MercenaryContractBoard.Core.Tests` - xunit, net8.0.
- `module\` - SubModule.xml. Build output lands in `module\bin` (git-ignored).
- Machine paths: `Directory.Build.props.user` (git-ignored) sets `GameFolder` and `McmBinFolder` (falls back to
  `Modules\Bannerlord.MBOptionScreen`).

## UI

Vanilla `MultiSelectionInquiryData` (min 1, max 1; disabled elements = greyed lines, `InquiryElement.Hint` = the
tooltip; `BannerImageIdentifier` for the kingdom banners), then `InquiryData` to confirm. Pop-ups opened from a
pop-up callback are queued (`GauntletQueryManager`: the callback runs before `CloseQuery`, which then shows the queued
one) - so never pass `prioritize: true`. Towns only: War Sails' port is a sub-menu of the town menu.

## Commands

```
dotnet build MercenaryContractBoard.sln -c Release
dotnet test tests\MercenaryContractBoard.Core.Tests -c Release
powershell -ExecutionPolicy Bypass -File tools\deploy.ps1     # builds + installs Modules\MercenaryContractBoard.Dev, "Mercenary Contract Board (dev)"
powershell -ExecutionPolicy Bypass -File tools\package.ps1    # dist\MercenaryContractBoard + zip (release identity)
dotnet run --project ..\trax_combat_enhancements\tools\AssemblyGuard -c Release -- module\bin\Win64_Shipping_Client\MercenaryContractBoard.dll MCMv5
```

Release identity in `module\SubModule.xml` is Id `MercenaryContractBoard`; only deploy.ps1 re-badges it `.Dev`.
Never install the non-dev module into the game's Modules (it would load beside `MercenaryContractBoard.Dev`).

deploy.ps1 refuses if the DLLs are locked (game running). Never launch, restart or kill the game without asking Toni.
Mod log: `Documents\Mount and Blade II Bannerlord\Configs\MercenaryContractBoard\mercenary_contract_board.log`.

## Design rules

- **Decorator models only** if a model is ever touched (none now): subclass the ABSTRACT model, wrap the previously
  registered one. **Never subclass `Default*` / `Sandbox*` models.**
- Code that runs inside game callbacks (menu condition / consequence, pop-up buttons) must never throw: try/catch +
  `ModLog.ErrorOnce`.
- No Harmony unless truly needed; behaviours and game menus first.
- No saved types: the mod must stay removable mid-campaign. The contract is vanilla's state on the clan.
- **MCM is optional**: MCM types only inside method bodies of `McmSettings` (fluent builder, `Action<object>` instance
  methods - never a type deriving from / a field of / a lambda over an MCM type), else the game's `GetTypes()` refuses
  the whole mod without MCM. Run the AssemblyGuard check above after touching it.
- Same numbers as vanilla: call the game's models and helpers for anything they compute; re-implement in Core only
  what vanilla can't be called for (CanPlayerEnterFaction reads the conversation hero), with a test.
- Pure logic goes in Core with tests; the module only reads numbers from the game and feeds them in.
- Never commit `Directory.Build.props.user`, logs or `dist\`.
