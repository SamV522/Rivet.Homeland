# Homeland

Homeland is an asymmetric top-down insurgency / counter-insurgency game built on Rivet. Liberation is the first mode: CISF players try to keep order and build an intelligence picture while embedded HLA Insurgents recruit locals, hide among the population and can turn local anger into an organic insurrection.

## Current playable slice

- 5–16 player target with very small matches supported; team allocation biases roughly 70/30 toward CISF while still producing HLA opposition when multiple players are present.
- 45 minute default Liberation timeout. CISF does not receive the timeout win while an insurrection is active.
- Three connected districts: rural/village, bazaar and CBD.
- Physical CISF/HLA player actors using Rivet CharacterController.
- Server-authoritative movement, combat, damage, downed/bleed-out, stabilization, death, respawn, cuffs, detention and release.
- Persistent corpses: a player respawning as a new identity no longer removes the previous identity's body.
- Finite CISF reinforcement tickets.
- CISF FOB and village/bazaar police spawn sites. HLA can sabotage FOB communications or take police stations; CISF can retake police stations and cycle available respawn sites.
- HLA reinforcement lives are actual recruited Rebel civilians. An HLA respawn takes over a random available Rebel at that civilian's current physical location.
- A physical civilian population driven by server-authoritative GOAP and Rivet Navigation/Jolt rather than a timer-only activity loop.
- Civilian home/work/bazaar routines, danger response, fleeing, witness memory and reporting.
- Civilian alignment, fear, anger, personal opinion and relationship propagation.
- Quiet HLA recruitment immediately adds that real civilian to the future HLA reinforcement pool.
- Local Call to Arms. Recruited Rebels can retrieve weapons, rally, follow an HLA player, move to an ordered point, attack CISF or scramble back into civilian life.
- Armed Rebel NPCs navigate toward CISF and fight as deliberately mediocre/shaky combatants.
- District readiness can organically trigger SURVIVE THE INSURRECTION.
- Schedules are timed world objectives with physical markers and faction consequences; CISF reinforcement-truck success adds tickets.
- Deterministic identities/dossiers plus a factual forensics/evidence model.
- First-hand witness reports physically travel through the civilian GOAP loop and then become shared CISF intelligence.
- Steam P2P through Rivet's SteamP2PTransport, plus UDP for local testing.
- Steam host/join/invite flow, lobby and configurable Homeland match settings.
- Vendored CC0 Kenney roads, buildings, character models and vehicles with source/license metadata in assets/ThirdParty/Kenney/SOURCES.md.

## Controls

- WASD — move
- M — toggle world map (Esc also closes it); movement and combat inputs are held while viewing the map
- Mouse — aim
- Left mouse — fire
- E — interact / search / stabilize / resolve nearby strategic interaction or Schedule
- C — cuff
- X — release
- P — CISF: cycle preferred available respawn site
- R — HLA: quietly recruit nearby civilian
- T — HLA: local Call to Arms
- Q — HLA: order local armed Rebels to follow
- V — HLA: order local armed Rebels to go toward aimed position
- B — HLA: order local armed Rebels to attack CISF
- G — HLA: scramble local Rebels back into civilian life
- K — abandon a detained player life where allowed

## Practice

Choose **Practice** in the main menu, then **Practice as CISF** or **Practice as HLA**. This offline solo sandbox uses the full town and the regular movement, combat, civilian, recruitment, and strategic interaction systems. A stationary opposing actor starts east of you along the main road for shooting, downing, stabilization, cuffs, and release. Three bazaar civilians start as HLA supporters so you can also try Call to Arms and Rebel orders.

Practice has no victory condition or timeout and unlimited respawns, even if reinforcement tickets or spawn sites would normally be unavailable. F1 starts a fresh CISF session, F2 starts a fresh HLA session, and F5 resets your current side. F3 or Esc opens a paused practice menu with the same actions and **Back to main menu**. Resetting restores civilians, targets, corpses, intelligence, schedules, and strategic sites. M opens the map.

Direct launch: `dotnet run -c Release -- --practice cisf` or `dotnet run -c Release -- --practice hla`.

## Checkout

Place Homeland beside Rivet:

~~~text
workspace/
  Rivet/
  Rivet.Homeland/
~~~

Run:

~~~powershell
dotnet run -c Release
~~~

Steam host:

~~~powershell
dotnet run -c Release -- --steam --steam-app-id 480
~~~

Steam join by SteamID64 during development:

~~~powershell
dotnet run -c Release -- --steam-connect HOST_STEAMID64 --steam-app-id 480
~~~

UDP test host/client:

~~~powershell
dotnet run -c Release -- --port 7777
dotnet run -c Release -- --connect 127.0.0.1:7777
~~~

## Assets

A curated baseline of Kenney CC0 assets is committed directly to the repository, so the playable does not depend on an asset-import step. tools/import-kickback-assets.ps1 remains optional for importing additional Quaternius assets already used by Rivet.Kickback.

## Not yet implemented in this playable slice

These are still real Homeland requirements, but they are not represented as completed features:

- full clothing/disguise swapping and face/familiarity recognition
- CCTV terminals and printable surveillance photos
- full CISF laptop/database editing UI and authenticated HLA database sabotage
- physical radios and voice/radio leakage
- prisoner vehicle seats, jail doors, lockpick escape and escorted walking
- complete persistent inventory/stash/item concealability system
- physical fingerprint/blood/ballistics collection workflows beyond the evidence-domain layer
- player-authored physical notepad UI
- full civilian dialogue/interrogation UI
- de-escalation/allegiance-change workflow for incarcerated Rebels

Those should be treated as subsequent Homeland systems, not as already-finished functionality.
