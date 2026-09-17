# HostOwner

Мод для Valheim: хост забирает себе владение выбранными объектами в своей
активной зоне — станциями, боссами, чем угодно по списку компонентов или
префабов. Ставится только на хостящего игрока; на клиентах и выделенном
сервере бездействует. Без Harmony-патчей.

Это не библиотека: другим модам он не нужен как сборка. Он закрывает общую для
них дыру — «логика объекта выполняется у того, кто им владеет, а владеет
игрок без мода» — одним общим способом, чтобы не дублировать её в каждом
(StationSpeed, BossMeter, AI-моды). У кого есть что добавить программно — есть
крошечный статический API через рефлексию, см. ниже.

Состояние и план — в `ROADMAP.md`, история версий — в `CHANGELOG.md`.

## Зачем

У каждого сетевого объекта один владелец — клиент, который исполняет его
логику и пишет результат в ZDO. Плавильню тикает владелец, босс ходит и бьёт
на владельце, бочка принимает закладку на владельце. Любой мод, который меняет
поведение объекта на клиенте, действует ровно тогда, когда этот клиент —
владелец.

С Valheim 1.0 владение раздаёт **сервер** (`ZDOMan.ReleaseZDOS`, раз в 2 с):
сначала себе — за то, что в его собственной активной зоне, потом каждому пиру
по очереди — за то, что в его; и только объектам без владельца или тем, чей
владелец из своей зоны ушёл. Кто получил объект, держит его, пока не отойдёт.
Клиенты сами владение не берут.

Отсюда дыра: если Дарк без мода пришёл к печи первым, печь его, и ускорение
хоста на неё не действует, пока Дарк рядом; если он один бьёт босса — ударов
никто из модовых не видит.

## Как работает

Раз в `Interval` секунд (2, как у игры) на хосте:

1. Берётся ближняя область симуляции вокруг хоста — те же сектора, что игра
   перебирает для него самого в `ReleaseNearbyZDOS` (`ZDOMan.FindSectorObjects`
   с `NearSimulationDistance`).
2. Для каждого объекта из списка видов (по хешу префаба), персистентного и
   внутри активной зоны (`ZNetScene.InActiveArea`), с чужим владельцем —
   `ZDO.SetOwner(сессия хоста)`. Ревизия владельца растёт, пир получает её со
   следующей посылкой и перестаёт симулировать; хост начинает.
3. Ваниль объект назад не отдаст, пока хост в зоне: владелец «в своей области».
   Когда хост уходит, срабатывает его же ванильный release (`SetOwner(0)`), и
   объект достаётся, кому положено.

Непересистентные объекты (игроки, снаряды) не трогаются, как и в ванильном
раздатчике; префабы с компонентом `Player` исключены всегда.

**Границы.** Только активная зона хоста (±NearSimulationDistance зон, по
умолчанию ±2, ~130–190 м); далёкую печь Дарка хост не подхватит — её и
симулирует только тот, кто рядом. На краю зоны объект может «мигать» между
хостом и гостем, как между любыми двумя игроками в ванилле. Выделенный сервер
не имеет игрока и активной зоны — там мод ничего не делает; на клиенте тоже.

## Установка

`build/HostOwner.dll` → `%AppData%\r2modmanPlus-local\Valheim\profiles\Valheim\BepInEx\plugins\HostOwner\`
или `build.ps1 -Install`. Только на хосте.

## Настройки

`BepInEx\config\j1ga.hostowner.cfg`. Список видов пересобирается при любой
правке без перезахода.

| Раздел | Ключ | По умолчанию | Смысл |
|---|---|---|---|
| General | `Enabled` | `true` | выключатель; уже взятое остаётся у хоста, пока он не отойдёт |
| General | `Debug` | `false` | лог каждого забранного объекта с координатами и прежним владельцем |
| General | `Interval` | `2` | секунд между проходами (0.5…30) |
| Objects | `Stations` | `true` | всё с `Smelter`, `CookingStation`, `Beehive`, `SapCollector`, `Fermenter` — плавильни всех видов, жаровни, печь, ульи, смолосборники, бочки; модовые тоже |
| Objects | `Bosses` | `false` | все существа с `Character.m_boss` |
| Objects | `Components` | пусто | имена типов компонентов игры через запятую: `Plant,Fireplace,Tameable` |
| Objects | `Prefabs` | пусто | имена префабов через запятую: `piece_bathtub,Eikthyr` |

Консоль (F5): `hostowner status` — сколько видов, сколько таких объектов в
зоне, сколько у хоста, сколько забрано; `hostowner list` — виды; `hostowner
now` — проход сейчас.

## API для других модов

Без ссылки на сборку, через рефлексию; если мода нет — ничего не делать:

```csharp
Type api = Type.GetType("HostOwner.HostOwnerApi, HostOwner");
if (api != null)
{
    api.GetMethod("AddPrefab").Invoke(null, new object[] { "fermenter" });
    api.GetMethod("AddComponent").Invoke(null, new object[] { "Plant" });
    bool owned = (bool)api.GetMethod("IsOwnedKind").Invoke(null, new object[] { "smelter".GetStableHashCode() });
}
```

`AddPrefab` / `AddComponent` — добавить вид к списку из конфига (действует до
перезапуска игры, список пересобирается на следующем проходе); `IsOwnedKind` —
забирает ли хост объекты такого префаба в этом мире.

## Где что лежит

| Файл | Что в нём |
|---|---|
| `src\HostOwnerPlugin.cs` | `Awake`/`Update`, проход `Pass`, помощники, обработка ошибок |
| `src\HostOwnerPlugin.Config.cs` | `ConfigEntry`, сборка списка видов `BuildIndex` |
| `src\HostOwnerPlugin.Api.cs` | `HostOwnerApi` для других модов |
| `src\HostOwnerPlugin.Commands.cs` | консольная команда `hostowner` |
| `build\HostOwner.dll` | сборка |
| `thunderstore\` | manifest, icon 256×256, README для пакета |

## Сборка

```
powershell -ExecutionPolicy Bypass -File .\build.ps1            # собрать и проверить ссылки
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Install   # ... и положить в plugins
powershell -ExecutionPolicy Bypass -File .\build.ps1 -Package   # ... и собрать zip для Thunderstore
```

Компилятор — `csc.exe` из .NET Framework (C# 5), ссылки — из папки игры и
`BepInEx\core` профиля r2modman; пути в начале `build.ps1`, `check-refs.ps1` и
`src\HostOwner.csproj`. `check-refs.ps1` после сборки сверяет с игрой каждую
ссылку на тип и член. Устройство то же, что у StationSpeed.

## Репозиторий

Локальный git-репозиторий, ветка `main`. Под версионированием: исходники,
`.csproj`, скрипты, документация, заготовка Thunderstore и `build\HostOwner.dll`.

## AI assistance

This mod was developed with the help of an AI assistant (Claude by Anthropic).
The code and the documentation were written together with it and checked
against the game's IL; the design decisions, in-game testing and releases are
the author's.
