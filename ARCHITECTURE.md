# Homeland architecture

Homeland follows the Rivet.Kickback process/session split but keeps Homeland rules in game-specific modules.

- HomelandApplication: process lifetime, Steam runtime, invites/rich presence, main-menu/session transitions.
- HomelandGame: Rivet loop adapter, authoritative host/client split, input collection and snapshot publication.
- HomelandNetwork: Rivet NetworkServerSession / NetworkClientSession, using SteamP2PTransport or UDP.
- HomelandSimulation: tickets, identity lives, damage/death, detention, respawn, strategic sites, timeout and insurrection rules.
- CivilianDirector: persistent civilian social state, relationships, alignment/fear/anger, witness memory, recruitment and Rebel commands.
- GoapPlanner: costed state-space planner over civilian world facts.
- CivilianAgentSystem: physical NPC entities using Rivet NavigationAgent + CharacterController; GOAP selects intent and navigation/Jolt performs movement.
- PlayerActorSystem: physical player bodies, combat, interactions, HLA takeover of Rebel identities and persistent corpse creation.
- Forensics: dossiers, witness statements, fingerprint/blood/ballistic fact records. Evidence records facts; players infer guilt.
- ScheduleDirector: timed mini-objectives whose outcomes affect tickets, alignment and district pressure.
- HomelandWorld: three districts, navigation space, strategic locations, schedule marker and CC0 world dressing.

## NPC authority model

Civilian AI runs on the authoritative host only.

1. CivilianDirector stores the durable social/identity state.
2. GoapPlanner chooses a plan from world facts such as danger, reportable intelligence, Rebel status, Call to Arms, armed state and scramble orders.
3. CivilianAgentSystem turns the selected action into a Rivet Navigation destination.
4. Rivet NavigationAgent steers a Jolt-backed CharacterController; game code does not teleport ordinary NPC movement.
5. The host snapshots resulting civilian/player/corpse transforms and state to clients.

Reactive Rebel combat sits above GOAP: GOAP handles arming/rallying/life behavior, while an explicit Attack order makes armed Rebels navigate toward CISF and engage.

## Identity and reinforcement model

HLA reinforcement tickets are not an abstract integer store. A recruited civilian becomes a viable Rebel identity. While that NPC is alive, free and not already player-controlled, it contributes one available HLA reinforcement life. On HLA death, the player later assumes a randomly selected available Rebel at that NPC's live position and with that identity's current armed state.

CISF deaths consume finite reinforcement tickets and generate a fresh operative identity. Available FOB/police spawn infrastructure controls where that new operative can enter the map.

Dead player identities produce persistent corpse entities that are separately replicated; the respawning player body is reused for the new identity without erasing the old corpse.

## Knowledge model

Personal identity knowledge remains separate from shared intelligence in the domain model. Current playable presentation exposes recent shared CISF evidence, including witness reports generated when a civilian actually reaches a reporting location through GOAP. Dossiers and biometric/ballistic records remain fact-oriented rather than automatically declaring someone HLA.

The complete physical-information design—player-authored paper notes, stealable printouts, CCTV photos, authenticated database editing and radio interception—is intentionally still listed as future implementation rather than described as finished.