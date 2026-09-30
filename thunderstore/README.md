# HostOwner

The hosting player takes ownership of chosen objects that lie in its active
area: stations, bosses, or any list of prefabs and components. Their logic then
runs on the host, and so do the host's mods for them.

![hostowner status: how many objects the host owns and took over](https://raw.githubusercontent.com/tbsj1ga/HostOwnerValheim/main/docs/media/status.png)

*`hostowner status`: how many objects the host owns and took over*

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

## Compatibility

Tested with **Valheim 1.0.16** (network version 40), **BepInEx 5.4.23.5** (BepInExPack_Valheim 5.4.2351).

## Who needs it

| Who | What |
|---|---|
| Hosting player (game started with "Start server") | installs the mod — objects of the chosen kinds in its area move to it |
| Dedicated server | **does not work**: a server has no player and no active area, the mod stays inert |
| Other players | not needed; with the mod or without, everything works as usual |

## Known conflicts

- Mods that manage object ownership themselves will argue with the host. Mods that claim an
  object once (a taunt, a cart) get along: the host yields for `YieldSeconds`.

## Bugs and feedback

GitHub Issues: https://github.com/tbsj1ga/HostOwnerValheim/issues — please attach `BepInEx/LogOutput.log`.

## More mods by j1gA

| | Mod |
|---|---|
| [![LivingMap](https://raw.githubusercontent.com/tbsj1ga/LivingMapValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/LivingMap/) | **[LivingMap](https://thunderstore.io/c/valheim/p/j1gA/LivingMap/)** — Your buildings, roads and cleared forest on the map and the minimap — and a detailed map when you zoom in. |
| [![StationSpeed](https://raw.githubusercontent.com/tbsj1ga/StationSpeedValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/StationSpeed/) | **[StationSpeed](https://thunderstore.io/c/valheim/p/j1gA/StationSpeed/)** — Faster smelters, kilns, fermenters and crops — consistent even for players without the mod. |
| [![WeaponArts](https://raw.githubusercontent.com/tbsj1ga/WeaponArtsValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/WeaponArts/) | **[WeaponArts](https://thunderstore.io/c/valheim/p/j1gA/WeaponArts/)** — One key, one active ability per weapon: stagger, taunt, heals, berserk, crits. |
| [![ExtendedBosses](https://raw.githubusercontent.com/tbsj1ga/ExtendedBossesValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/ExtendedBosses/) | **[ExtendedBosses](https://thunderstore.io/c/valheim/p/j1gA/ExtendedBosses/)** — Raid-style boss fights: phases, adds, nests, shields, marks — built from vanilla parts. |
| [![HudLayout](https://raw.githubusercontent.com/tbsj1ga/HudLayoutValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/HudLayout/) | **[HudLayout](https://thunderstore.io/c/valheim/p/j1gA/HudLayout/)** — Move, resize and restyle your HUD with the mouse: bars, food, hotbar, minimap, even other mods' HUD. |

Source, documentation and the changelog: https://github.com/tbsj1ga/HostOwnerValheim

*Developed with the help of an AI assistant (Claude by Anthropic); the design
decisions, verification against the game code and in-game testing are the
author's.*
