# Mercenary Contract Board

A Mount & Blade II: Bannerlord mod (1.4.8, War Sails compatible): **every town has a board listing the mercenary
contracts on offer.** Which kingdoms would hire you, what they pay and who they are fighting, on one page. No more
riding from lord to lord asking "I would like to enter your service" to find out who pays best.

The board is only a view of vanilla's own offers. It uses the same conditions and the same pay numbers a lord would
give you in conversation, and signing or switching uses vanilla's own actions. Nothing new is invented.

## Install

1. Download the zip (not published yet).
2. Extract it into `Mount & Blade II Bannerlord\Modules\`.
3. Enable it in the launcher (below MCM if you use it). [Mod Configuration Menu](https://www.nexusmods.com/mountandblade2bannerlord/mods/612)
   is optional. No Harmony needed.

## Using the board

In any town menu, pick **Mercenary Contract Board**. A pop-up lists every kingdom with its banner:

```
Mercenary Contract Board

You are not under contract.

Contracts on offer: 2. Pay is in denars for every point of influence you earn in their service, by fighting
their enemies. Signing gives 5 influence. Hover a kingdom for its wars.

[banner] Battania (Caladog): 90 per influence
[banner] Western Empire (Garios): 60 per influence
[banner] Aserai (Unqid): not at war                    (greyed)
[banner] Sturgia (Raganvad): never heard of you        (greyed)
[banner] Vlandia (Derthert): at war with you           (greyed)

                         [Accept]   [Close]
```

Hover a line for the details:

```
Battania, ruled by Caladog
Pay: 90 denars per influence point
Signing: 5 influence = 450 denars
At war with: Sturgia and Vlandia
```

A greyed line says why that kingdom won't hire you right now:

```
Sturgia, ruled by Raganvad
Not offered: They have hardly heard of you: win some renown (or Raganvad's favour).
At war with: Khuzait
```

Pick a contract and press **Accept**. You are asked once more ("Sign a mercenary contract with Battania, ruled by
Caladog?") and Yes signs it.

### Already under contract

Your contract is shown at the top:

```
YOUR CONTRACT: Vlandia, ruled by Derthert
Pay: 70 denars per influence point
Pay review in 12 days (the rate today would be 80)
Owed to you: 14 influence, about 994 denars, paid out 1/5 a day
At war with: Battania and Sturgia

Other contracts on offer: 2. Signing one ends this contract first. ...
```

Accepting another contract asks first:

```
Switch contract

Leave your contract with Vlandia and sign with Battania?

Leaving Vlandia ends your wars as their mercenary. The 14 influence still owed to you (about 994 denars) is lost,
as when you end a contract by talking to a lord.

Battania pays 90 denars per influence point (worked out again as you sign). Signing gives 5 influence.
You will be at war with: Sturgia and Vlandia.

                         [Yes]   [No]
```

**Yes** ends your contract and signs the new one, exactly as if you had told a lord of Vlandia "Let my contract be
ended" and then asked a Battanian lord for work. **No** takes you back to the board.

You can't switch while you are in an army (vanilla won't let you end a contract then either): leave the army first.

### Vassals and rulers

If your clan is a vassal or rules a kingdom, the board says so and offers nothing. Mercenary contracts are for clans
outside a kingdom.

## How pay works (vanilla)

The pay of a contract is **denars per influence point**. As a mercenary, the influence you earn (winning battles
against the kingdom's enemies, and so on) is not kept: every day 1/5 of it is paid out as gold at your contract's
rate. A contract at 90 turns 10 influence into 900 denars over the next few days. Signing gives 5 influence, so a
new contract starts by paying 5 x the rate.

What a kingdom offers (vanilla's formula, the same number the lord says in conversation), from 10 up to 800:

- **More** when the kingdom is weaker than its enemies, has few clans or few fiefs, or its lords and treasury are
  rich; **less** when it is winning, its lords are poor, or it already has many mercenaries.
- **+15% per clan tier** of yours.
- **+15%** on top if your clan is Vlandian (their culture bonus for mercenaries).

The rate is reviewed every 30 days while you serve (vanilla): the board shows the next review and what the rate would
be today.

## Who offers a contract (vanilla's conditions)

In vanilla's order; the first that fails is the reason on the greyed line:

1. Your clan is not at war with them.
2. Your clan tier is at least 1.
3. Their ruler's relation with you is at least -10.
4. Every war you fight against a kingdom of real strength (above 5% of theirs) is also theirs.
5. Your clan owns no settlement.
6. They are at war with at least one kingdom. **Only kingdoms at war hire mercenaries.**
7. They have heard of you: renown + a share of your clan's strength + twice the ruler's relation must be above 25.
8. They are not winning on their own (no more than 3 times their enemies' strength).

When you are under contract, conditions 1 and 4 are checked as they would stand after leaving (leaving makes peace
with everyone, as in vanilla).

## Settings

With [Mod Configuration Menu](https://www.nexusmods.com/mountandblade2bannerlord/mods/612) (**optional**), under
Mod Options > Mercenary Contract Board. Changes apply at once. Without MCM the mod runs on these defaults:

| Group | Setting | Default |
|---|---|---|
| General | Enabled (the town menu option) | on |
| The board | Show kingdoms that would not hire you (greyed, with the reason) | on |
| The board | Best pay first (off = by kingdom name) | on |
| The board | Confirm a first contract (switching always asks) | on |

## Known limits

- Towns only (castles and villages have no board). War Sails' port is part of the town, so port towns have it too.
- The kingdom's courier offer (the pop-up that comes to clan tier 1 players now and then) uses a different vanilla
  formula. The board shows the conversation's number, which is what you get when you ask a lord yourself.
- Switching in a town of the kingdom you leave can leave you standing in an enemy town. You can still walk out.
- The board's text is English only for now.
- Nothing is saved into your campaign: add or remove the mod at any time. Your contract is vanilla's.

## Build

```
dotnet build MercenaryContractBoard.sln -c Release
dotnet test tests\MercenaryContractBoard.Core.Tests -c Release
powershell -ExecutionPolicy Bypass -File tools\deploy.ps1
```

Set your game path in `Directory.Build.props.user` if it differs from `Directory.Build.props`.

## License

Public domain ([Unlicense](LICENSE)).
