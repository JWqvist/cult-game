# Cult Rising — Sprint Backlog

## How Sprints Work
- Each sprint is one Kanban task on the board
- When Dev Tasks cron picks up a sprint task: spawn claude-code agent in /home/jewi/projects/cult-game
- Build must pass (`dotnet build` 0 errors/warnings) before commit
- PR opened from `sprint/N-name` branch → auto-merged when no 🔒 Locked comment
- After merge: next sprint task moves to backlog automatically

---

## Sprint 2 — Vehicles
**Goal:** Carjack mechanic + basic vehicle driving

Tasks:
- `scenes/Vehicle.tscn` — RigidBody2D or CharacterBody2D car scene
- `scripts/Vehicle.cs` — Drive/steer (top-down), friction, max speed
- Carjack: press E near parked car → player enters, takes control
- Exit vehicle: press E or F to get out
- Simple parked cars placed in world
- Player movement disabled while in vehicle

Acceptance: Player can walk to a car, enter, drive around, exit.

---

## Sprint 3 — NPC Pedestrians + Traffic
**Goal:** Living city feel with simple AI

Tasks:
- `scenes/Pedestrian.tscn` — NPC that wanders on sidewalks
- `scripts/Pedestrian.cs` — Wander state machine (idle → walk to point → idle)
- Flee state: if player attacks nearby, NPCs run away
- `scenes/TrafficCar.tscn` — Car that drives on roads (simple waypoint path)
- `scripts/TrafficCar.cs` — Follow road waypoints, loop
- Spawn 5-10 pedestrians + 3-5 traffic cars in World scene

Acceptance: NPCs walk around, traffic drives on roads, NPCs flee when attacked.

---

## Sprint 4 — Combat + Inventory
**Goal:** Punching, shooting, simple item system

Tasks:
- `scripts/CombatSystem.cs` — Handle melee and ranged attacks
- Melee: press F near NPC → punch animation, NPC takes damage, death state
- Ranged: equip gun → left click → raycast/projectile fires
- `scripts/Inventory.cs` — 3 slots: Fists (always), Melee weapon, Gun
- Item pickup from ground (dropped by dead NPCs or placed in world)
- NPC death: ragdoll-lite (disable AI, play death anim, drop item)
- Basic HUD: health bar, equipped item icon

Acceptance: Player can punch/shoot NPCs, pick up weapons, NPCs die.

---

## Sprint 5 — Cult Recruitment Core
**Goal:** First cult mechanics — recruiting and follower count

Tasks:
- `scripts/RecruitSystem.cs` — Talk to NPC (E) → dialogue → chance to recruit based on persuasion stat
- Recruited NPC follows player to compound area
- CultSize stat increases (GameManager)
- `scenes/Compound.tscn` — Fenced area outside city, player's base
- Compound unlocks based on CultSize thresholds (fence, tent, building)
- Money: followers donate $X per in-game day

Acceptance: Player can recruit NPCs, bring them to compound, see CultSize grow.

---

## Sprint 6 — Economy + Heat System
**Goal:** Money flows and police attention

Tasks:
- Money generation: donations from followers (passive), crimes (active)
- Crimes: mugging, theft, scam (interact with NPC → steal money)  
- `scripts/HeatSystem.cs` — heat_level 0-5, rises with crimes
- Police NPC: spawns when heat >= 2, chases player
- Heat decays over time, clears if player hides in compound
- Wanted stars HUD (1-3)
- Bribes: spend money to reduce heat

Acceptance: Money accumulates, crimes raise heat, police responds.

---

## Sprint 7 — Inner Circle + Delegation
**Goal:** Assign roles to followers, passive actions

Tasks:
- Inner Circle UI: pick up to 3 followers from cult, assign role
- Roles: Recruiter (auto-recruits 1 NPC/day), Enforcer (guards compound), Financier (boosts donations)
- `scripts/InnerCircle.cs` — manage assigned roles, trigger passive effects
- Followers can have loyalty stat — low loyalty = defection risk
- Defection: follower leaves cult, reduces CultSize, may alert police

Acceptance: Player can assign inner circle roles, see passive effects in action.

---

## Sprint 8 — Endgame Paths (Phase 1)
**Goal:** Branching story paths begin to diverge

Tasks:
- Story event system: trigger events at CultSize milestones (10, 30, 75, 150)
- At 75 members: player chooses direction (dialogue choice)
  - "We are above the law" → Waco/Jonestown path unlocks
  - "We need legitimacy" → Scientology/NXIVM path unlocks
  - "The end is near" → Heaven's Gate path unlocks
- Each choice changes NPC dialogue, police behavior, available missions
- Simple mission system: objective → reward → narrative text

---

## Sprint 9+ — Endgame + Polish
TBD based on what path mechanics need.
