# Changelog

## 0.1.0

- First release. Every `Interval` seconds (2) the hosting player walks its near
  simulation area the way the game does in `ReleaseNearbyZDOS` and takes
  ownership (`ZDO.SetOwner`) of persistent objects of the chosen kinds that
  belong to someone else.
- Kinds: the `Stations` group (components `Smelter`, `CookingStation`,
  `Beehive`, `SapCollector`, `Fermenter`), `Bosses` (`Character.m_boss`), and
  the `Components` and `Prefabs` lists. `Player` is never taken.
- `HostOwnerApi` for other mods via reflection: `AddPrefab`, `AddComponent`,
  `IsOwnedKind`.
- Console: `hostowner status | list | now`.
- Inert on clients and on a dedicated server. No Harmony patches.
