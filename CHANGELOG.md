# История изменений

Версия задаётся в одном месте — `HostOwnerPlugin.Version` в `src/HostOwnerPlugin.cs`.

## 0.1.0

- Первая версия. Хост раз в `Interval` с (2) проходит ближнюю область
  симуляции вокруг себя тем же способом, что игра в `ReleaseNearbyZDOS`, и
  забирает владение (`ZDO.SetOwner`) персистентными объектами выбранных видов
  с чужим владельцем.
- Виды: группа `Stations` (компоненты `Smelter`, `CookingStation`, `Beehive`,
  `SapCollector`, `Fermenter`), `Bosses` (`Character.m_boss`), списки
  `Components` и `Prefabs`. `Player` исключён всегда.
- `HostOwnerApi` для других модов через рефлексию: `AddPrefab`,
  `AddComponent`, `IsOwnedKind`.
- Консоль `hostowner status | list | now`.
- Бездействует на клиентах и выделенном сервере. Harmony-патчей нет.
