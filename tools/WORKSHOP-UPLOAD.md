# Uploading to the Steam Workshop and Nexus Mods

Published 2026-11-06: Steam item **[3834681925](https://steamcommunity.com/sharedfiles/filedetails/?id=3834681925)** (hidden at once by the
Steam block, like the other mods since 2026-10-10 - see bannerlord-mods `MODS-STATUS.md`; full description sent
with the first update the same day), GitHub repo public with release v1.0.0. Nexus: [mods/14539](https://www.nexusmods.com/mountandblade2bannerlord/mods/14539), published the same day.

Same uploader and quirks as the sister mods (`..\troop-training-requirement\tools\WORKSHOP-UPLOAD.md`): Bannerlord's own
`TaleWorlds.MountAndBlade.SteamWorkshop.exe`, riding the logged-in Steam client (start Steam first).

## Before either upload

1. Pick the release version in `module\SubModule.xml` (`<Version value="v1.0.0" />` today).
2. Package and refresh the thumbnail if the screenshots changed:

```powershell
powershell -ExecutionPolicy Bypass -File tools\package.ps1 -Force
python tools\make_thumbnail.py
```

`package.ps1` writes `dist\MercenaryContractBoard\` (the Workshop folder) and
`dist\MercenaryContractBoard_v<version>.zip` (the Nexus / GitHub release file). `dist\` is gitignored.

## Nexus Mods (by hand on the site)

1. Add a mod: game Mount & Blade II: Bannerlord, name "Mercenary Contract Board", category Gameplay.
2. Upload `dist\MercenaryContractBoard_v<version>.zip` as the main file, version as in SubModule.xml.
3. Description: paste `tools\NEXUS-DESCRIPTION.bbcode`. Short summary: the first line of it.
4. Images: `docs\thumbnail.jpg` as the primary image, then `docs\screenshots\` in their numbered order.
5. Requirements: none. Mod Configuration Menu (612) is optional - mention it, do not require it. No Harmony.
6. Then put the Nexus link into README's Install section.

## Steam Workshop: first upload (creates the item - once only)

```powershell
& "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.MountAndBlade.SteamWorkshop.exe" "C:\Users\Asus ROG\Documents\BannerlordMods\mercenary-contract-board\tools\WorkshopCreate.xml"
```

Run it from a scratch folder (it drops `steam_appid.txt` and a log where it runs). Success = "Uploading done!" in
the output, never the exit code. Then:

1. Put the new item ID into `WorkshopUpdate.xml` (replace `ITEM_ID`) and here, with the page link, and
   mark `WorkshopCreate.xml` "ALREADY RUN - NEVER RUN AGAIN".
2. Send the full description right away (the create task only has a short one):
   `python tools\make_workshop_update.py --notes "v<version> - first release."` and run the uploader on the printed path.
3. Put the Steam link into README's Install section.

## Steam Workshop: updating

```powershell
powershell -ExecutionPolicy Bypass -File tools\package.ps1 -Force
$task = python tools\make_workshop_update.py --notes "vX.Y.Z - what changed."
& "C:\Program Files (x86)\Steam\steamapps\common\Mount & Blade II Bannerlord\bin\Win64_Shipping_Client\TaleWorlds.MountAndBlade.SteamWorkshop.exe" $task
```

Run it from a scratch folder. Success = "Uploading done!" in the output, never the exit code.
The script sends `dist\MercenaryContractBoard`, `STEAM-DESCRIPTION.bbcode` and the change notes
(`--visibility` optional). It refuses while `WorkshopUpdate.xml` still has `ITEM_ID`.

## By hand on the Steam item page

No required items (no Harmony; MCM is optional, so do not add it). The uploader cannot add the
screenshots in `docs\screenshots` under "Add/edit images & videos" (in their numbered order).
