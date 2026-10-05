# Homeland

Homeland is an asymmetric top-down insurgency / counter-insurgency game built on Rivet. Liberation is the first mode: CISF players try to keep order and build an intelligence picture while embedded HLA Insurgents recruit locals, hide among the population and eventually turn local anger into an organic insurrection.

## First playable scope

- 5–16 player target, but low counts including 1v1 are supported.
- Roughly 70/30 CISF/HLA team bias.
- 45 minute default Liberation timeout. CISF wins at timeout only when there is no active insurrection; an active insurrection must finish.
- Three connected districts: rural/village, bazaar and CBD.
- CISF FOB plus police-station spawn choices. FOB communications tower is the strategic sabotage target.
- Finite CISF reinforcement tickets, increased by reinforcement schedules.
- HLA reinforcement tickets are actual recruited Rebel civilians. Respawn assumes a random available Rebel identity wherever that NPC was.
- Downed, bleed-out, capture, cuffs, detention, release and ticket-cost abandonment of an imprisoned life.
- Civilian alignment, fear, anger, individual opinion and relationship propagation.
- Quiet HLA recruitment and district-level readiness leading to an organic insurrection objective: **SURVIVE THE INSURRECTION**.
- Deterministic generated identity/dossier data, manual CISF database entry and player-authored notes.
- Fingerprint, blood and ballistic signatures. Evidence records facts rather than automatically declaring guilt.
- Schedule system for convoys, checkpoints, patrols, briefings, HVT transfers and reinforcement trucks.
- Steam P2P networking via Rivet's `SteamP2PTransport`, with UDP retained for local testing.
- Main menu / lobby / Homeland settings structure based on Rivet.Kickback.
- CC0 third-party asset discovery for characters/clothes, vehicles, buildings and roads, with primitive fallback.

## Checkout

Place Homeland beside Rivet:

```text
workspace/
  Rivet/
  Rivet.Homeland/
```

Run:

```powershell
dotnet run -c Release
```

Steam host:

```powershell
dotnet run -c Release -- --steam --steam-app-id 480
```

Steam join by SteamID64 during development:

```powershell
dotnet run -c Release -- --steam-connect HOST_STEAMID64 --steam-app-id 480
```

UDP test host/client:

```powershell
dotnet run -c Release -- --port 7777
dotnet run -c Release -- --connect 127.0.0.1:7777
```

## Assets

Run `tools/import-kickback-assets.ps1` when `Rivet.Samples` exists beside this repo to copy the already-used Quaternius CC0 asset set as a starting point. See `assets/README.md` for the intended character/outfit/vehicle/building/road packs.

## Current implementation boundary

This first repository pass establishes the authoritative systems, menus/networking and three-district world. The next implementation pass should bind physical interactions to the domain rules: character controller/combat, searches and dialogue, disguise swaps, CCTV terminals/printed photos, CISF laptop/database UI, radio voice, prisoner vehicle seats, jail doors/lockpicks, GOAP navigation, inventory persistence and spawn-at-current-NPC-location.
