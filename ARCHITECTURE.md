# Homeland architecture

Homeland follows the Rivet.Kickback process/session split but keeps all Homeland rules in game-specific modules.

- `HomelandApplication`: process lifetime, Steam runtime, main-menu/session transitions.
- `HomelandGame`: Rivet loop adapter and authoritative host/client split.
- `HomelandNetwork`: Rivet `NetworkServerSession` / `NetworkClientSession`, using `SteamP2PTransport` or UDP.
- `HomelandSimulation`: ticket, death, detention, respawn, timeout and insurrection rules.
- `CivilianDirector`: generated civilian lives, personal/faction opinion, relationships, jobs and HLA recruitment.
- `Forensics`: dossier IDs, manual identity matching, fingerprints, blood and ballistics. It stores facts; players infer guilt.
- `ScheduleDirector`: asynchronous mini-objectives whose rewards change resources/intelligence rather than replacing ticket victory.
- `HomelandWorld`: three districts and strategic sites. It prefers third-party CC0 models and falls back to Rivet primitives.

## Knowledge model

Personal observations live with a player identity. Shared intelligence exists only after a player enters it into the CISF database. A dead CISF character loses private recognition; the corpse/notepad can be recovered or destroyed. Dossiers are deterministic from generated identity/appearance. Changing clothes hides a known identity from observers who have not personally learned the face; masks hide the face unless the observer saw the known person mask up.

## V0.1 gameplay targets

The code includes the authoritative scaffolding for: CISF/HLA team bias, generated identities, finite CISF tickets, Rebel-backed HLA tickets, downed/bleed-out, detention/release, wrongful detention anger, recruitment, district readiness, organic insurrection, schedules and 45-minute peacekeeping timeout. Physical interaction/gameplay presentation for every action is the next layer on top of these domain rules.
