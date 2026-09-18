# HostOwner

The hosting player takes ownership of chosen objects that lie in its active
area: stations, bosses, or any list of prefabs and components. Their logic then
runs on the host, and so do the host's mods for them.

Why: every networked object is simulated by one client, its owner. Since
Valheim 1.0 the server hands ownership out, and whoever reached an object first
keeps it while they stay near. A smelter that a player without a speed mod got
to first runs at vanilla speed; a boss that one player fights alone is invisible
to a damage meter on another. With this mod the host takes such objects over
within two seconds while it is in range, and vanilla gives them back the usual
way when the host walks off.

- `Stations` (on): everything with a Smelter, CookingStation, Beehive,
  SapCollector or Fermenter component, mods included.
- `Bosses` (off): every creature flagged as a boss.
- `Components`, `Prefabs`: your own lists.
- An object a player's client claims back right after the host took it (a
  taunt mod, a cart) is left alone for `YieldSeconds`, so mods that claim
  monsters on purpose keep working.

Console: `hostowner status | list | now`. Config in
`BepInEx/config/j1ga.hostowner.cfg`; changes apply without a restart.

Host only: inert on clients and on a dedicated server (no player, no active
area). No Harmony patches. A small reflection API (`HostOwner.HostOwnerApi`)
lets other mods add kinds without referencing this assembly.

Source, issues: https://github.com/TBSjiga/HostOwner

*Developed with the help of an AI assistant (Claude by Anthropic); the design
decisions, verification against the game code and in-game testing are the
author's.*
