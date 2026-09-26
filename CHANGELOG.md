# Changelog

**English** · [Русский](CHANGELOG-RU.md)

The version is set in one place — `HostOwnerPlugin.Version` in `src/HostOwnerPlugin.cs`.

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
