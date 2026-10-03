# Changelog

**English** · [Русский](CHANGELOG-RU.md)

The version is set in one place — `HostOwnerPlugin.Version` in `src/HostOwnerPlugin.cs`.

## 0.2.0 — Chests

- New group `Chests` (off by default): every placed chest — a building piece with a
  `Container`, not carts, ships or graves. For mods that take items from chests through
  their owner: Runic Crafting 1.x asks the owner to hand a chest over, and a player
  without the mod never answers, so crafting from the chests near them got stuck. With
  `Chests` on, the chests in the host's area are the host's.
- An object someone has open (`inUse` in its ZDO) is never taken, so a player looking
  into a chest does not lose what they put in or took out.
- `hostowner status` also shows how many objects were left alone because they are open.

## 0.1.0

- First release. Every `Interval` seconds (2) the hosting player walks its near
  simulation area the way the game does in `ReleaseNearbyZDOS` and takes ownership
  (`ZDO.SetOwner`) of persistent objects of the chosen kinds that belong to someone else.
- Kinds: the `Stations` group (components `Smelter`, `CookingStation`, `Beehive`,
  `SapCollector`, `Fermenter`), `Bosses` (`Character.m_boss`), and the `Components` and
  `Prefabs` lists. `Player` is never taken.
- `HostOwnerApi` for other mods via reflection: `AddPrefab`, `AddComponent`,
  `IsOwnedKind`.
- Yields to explicit claims: when a player's client takes an object back right after the
  host took it (`ClaimOwnership` from a mod or the game — a taunt, a cart), the object is
  left alone for `YieldSeconds` (30 s).
- Console: `hostowner status | list | now`.
- Inert on clients and on a dedicated server. No Harmony patches.
