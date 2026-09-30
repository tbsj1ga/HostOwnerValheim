# HostOwner

**English** · [Русский](README-RU.md)

A Valheim mod: the hosting player takes ownership of chosen objects in its active area —
stations, bosses, anything by a list of components or prefabs. Installed on the hosting
player only; inert on clients and on a dedicated server. No Harmony patches.

It is not a library: other mods do not need it as an assembly. It closes a gap they all
share — "an object's logic runs on whoever owns it, and the owner is a player without the
mod" — in one common way, so it does not have to be duplicated in each of them
(StationSpeed, ExtendedBosses, BossMeter, AI mods). For mods that want to add kinds from
code there is a tiny static API through reflection, see below.

Status and plans are in `ROADMAP.md`, version history in `CHANGELOG.md`.

## Why

Every networked object has one owner — the client that runs its logic and writes the
result into the ZDO. The owner ticks a smelter, a boss walks and strikes on its owner, a
fermenter accepts a load on its owner. Any mod that changes an object's behaviour on the
client takes effect exactly when that client is the owner.

Since Valheim 1.0 ownership is handed out by the **server** (`ZDOMan.ReleaseZDOS`, every
2 s): first to itself — for what is in its own active area, then to each peer in turn —
for what is in theirs; and only objects without an owner or whose owner has left their
area. Whoever got an object keeps it until they walk away. Clients never take ownership
themselves.

Hence the gap: if a guest without the mod reaches a smelter first, the smelter is theirs,
and the host's speed-up does not apply to it while the guest is near; if they fight a
boss alone, none of the modded players see the hits.

## How it works

Every `Interval` seconds (2, like the game) on the host:

1. Take the near simulation area around the host — the same sectors the game walks for
   the host itself in `ReleaseNearbyZDOS` (`ZDOMan.FindSectorObjects` with
   `NearSimulationDistance`).
2. For every object of a listed kind (by prefab hash), persistent and inside the active
   area (`ZNetScene.InActiveArea`), owned by someone else — `ZDO.SetOwner(host session)`.
   The owner revision grows, the peer receives it with the next packet and stops
   simulating; the host starts.
3. Vanilla will not give the object back while the host is in the area: the owner is "in
   its own area". When the host walks away, its own vanilla release (`SetOwner(0)`) fires
   and the object goes to whoever should get it.
4. If a taken object is back with the same peer on the very next pass, that is not the
   hand-out (the game never takes an object from an owner in its area) but a deliberate
   `ZNetView.ClaimOwnership` from their client: a taunt pulls a monster, a player grabs a
   cart. The host leaves such an object alone for `YieldSeconds` (30 s) and does not fight
   over it; then takes it again, and if it is claimed back again — yields again. That is
   how HostOwner gets along with the WeaponArts taunt with `Bosses=true`, and with any
   vanilla claim.

Non-persistent objects (players, projectiles) are not touched, just as by the vanilla
hand-out; prefabs with a `Player` component are always excluded.

**Limits.** Only the host's active area (±NearSimulationDistance zones, ±2 by default,
~130–190 m); a guest's far-away smelter is not picked up — only whoever is near simulates
it anyway. At the edge of the area an object may flip between the host and a guest, as
between any two players in vanilla. A dedicated server has no player and no active area —
the mod does nothing there; nor on a client.

## Screenshots

![hostowner status: how many objects the host owns and took over](https://raw.githubusercontent.com/tbsj1ga/HostOwnerValheim/main/docs/media/status.png)

*`hostowner status`: how many objects the host owns and took over*

## Compatibility

Tested with **Valheim 1.0.16** (network version 40), **BepInEx 5.4.23.5** (BepInExPack_Valheim 5.4.2351).

## Who needs it

| Who | What |
|---|---|
| Hosting player (game started with "Start server") | installs the mod — objects of the chosen kinds in its area move to it |
| Dedicated server | **does not work**: a server has no player and no active area, the mod stays inert |
| Other players | not needed; with the mod or without, everything works as usual |

## Known conflicts

- Mods that manage object ownership themselves (hand it out or keep it) will argue with the
  host. Mods that claim an object once (a taunt, a cart) get along: the host yields for
  `YieldSeconds`.

## Bugs and feedback

GitHub Issues: https://github.com/tbsj1ga/HostOwnerValheim/issues — please attach `BepInEx/LogOutput.log`.

## Installation

Through r2modman / Thunderstore, or put `build/HostOwner.dll` into
`BepInEx\plugins\HostOwner\` (or `build.ps1 -Install`). Host only.

## Settings

`BepInEx\config\j1ga.hostowner.cfg`. The list of kinds is rebuilt on any edit, without a
rejoin.

| Section | Key | Default | Meaning |
|---|---|---|---|
| General | `Enabled` | `true` | master switch; what was already taken stays with the host until it walks away |
| General | `Debug` | `false` | log every object taken, with coordinates and the previous owner |
| General | `Interval` | `2` | seconds between passes (0.5…30) |
| General | `YieldSeconds` | `30` | how long to leave alone an object a client took back right after the host (0 — never yield) |
| Objects | `Stations` | `true` | everything with `Smelter`, `CookingStation`, `Beehive`, `SapCollector`, `Fermenter` — every kind of smelter, cooking stations, the oven, beehives, sap extractors, fermenters; modded ones too |
| Objects | `Bosses` | `false` | every creature with `Character.m_boss` |
| Objects | `Components` | empty | the game's component type names, comma-separated: `Plant,Fireplace,Tameable` |
| Objects | `Prefabs` | empty | prefab names, comma-separated: `piece_bathtub,Eikthyr` |

Console (F5): `hostowner status` — how many kinds, how many such objects in the area, how
many the host owns, how many were taken, how many it yielded; `hostowner list` — the
kinds; `hostowner now` — run a pass now.

## API for other mods

No reference to the assembly, through reflection; if the mod is absent, do nothing:

```csharp
Type api = Type.GetType("HostOwner.HostOwnerApi, HostOwner");
if (api != null)
{
    api.GetMethod("AddPrefab").Invoke(null, new object[] { "fermenter" });
    api.GetMethod("AddComponent").Invoke(null, new object[] { "Plant" });
    bool owned = (bool)api.GetMethod("IsOwnedKind").Invoke(null, new object[] { "smelter".GetStableHashCode() });
}
```

`AddPrefab` / `AddComponent` — add a kind to the list from the config (lasts until the game
restarts, the list is rebuilt on the next pass); `IsOwnedKind` — whether the host takes
objects of this prefab in this world.

## Where things are

| File | Contents |
|---|---|
| `src\HostOwnerPlugin.cs` | `Awake`/`Update`, the `Pass`, helpers, error handling |
| `src\HostOwnerPlugin.Config.cs` | `ConfigEntry`, building the list of kinds `BuildIndex` |
| `src\HostOwnerPlugin.Api.cs` | `HostOwnerApi` for other mods |
| `src\HostOwnerPlugin.Commands.cs` | the `hostowner` console command |
| `build\HostOwner.dll` | build output |
| `thunderstore\` | manifest, icon 256×256, package README → `build\HostOwner-<version>.zip` |

## Building

```
powershell -ExecutionPolicy Bypass -File .\build.ps1            # build and check references
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install   # ... and copy into plugins
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Package   # ... and make the Thunderstore zip
```

The compiler is `csc.exe` from the .NET Framework (C# 5); references come from the game
folder and the r2modman profile's `BepInEx\core`; the paths are at the top of
`build.ps1`, `check-refs.ps1` and `src\HostOwner.csproj`. After the build `check-refs.ps1`
checks every type and member reference against the game.

## Repository

Branch `main` on GitHub: https://github.com/tbsj1ga/HostOwnerValheim. Versioned: sources,
`.csproj`, scripts, documentation, the Thunderstore template and `build\HostOwner.dll`.
License: MIT (`LICENSE`).

## More mods by j1gA

| | Mod |
|---|---|
| [![LivingMap](https://raw.githubusercontent.com/tbsj1ga/LivingMapValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/LivingMap/) | **[LivingMap](https://thunderstore.io/c/valheim/p/j1gA/LivingMap/)** — Your buildings, roads and cleared forest on the map and the minimap — and a detailed map when you zoom in. |
| [![StationSpeed](https://raw.githubusercontent.com/tbsj1ga/StationSpeedValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/StationSpeed/) | **[StationSpeed](https://thunderstore.io/c/valheim/p/j1gA/StationSpeed/)** — Faster smelters, kilns, fermenters and crops — consistent even for players without the mod. |
| [![WeaponArts](https://raw.githubusercontent.com/tbsj1ga/WeaponArtsValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/WeaponArts/) | **[WeaponArts](https://thunderstore.io/c/valheim/p/j1gA/WeaponArts/)** — One key, one active ability per weapon: stagger, taunt, heals, berserk, crits. |
| [![ExtendedBosses](https://raw.githubusercontent.com/tbsj1ga/ExtendedBossesValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/ExtendedBosses/) | **[ExtendedBosses](https://thunderstore.io/c/valheim/p/j1gA/ExtendedBosses/)** — Raid-style boss fights: phases, adds, nests, shields, marks — built from vanilla parts. |
| [![HudLayout](https://raw.githubusercontent.com/tbsj1ga/HudLayoutValheim/main/docs/media/icon-128.png)](https://thunderstore.io/c/valheim/p/j1gA/HudLayout/) | **[HudLayout](https://thunderstore.io/c/valheim/p/j1gA/HudLayout/)** — Move, resize and restyle your HUD with the mouse: bars, food, hotbar, minimap, even other mods' HUD. |

## AI assistance

This mod was developed with the help of an AI assistant (Claude by Anthropic). The code
and the documentation were written together with it and checked against the game's IL;
the design decisions, in-game testing and releases are the author's.
