# HostOwner — status and plan

**English** · [Русский](ROADMAP-RU.md)

The host takes ownership of objects of chosen kinds in its active area, so their logic
(and the mods acting on it) runs on the host. No patches: a pass over
`ZDOMan.FindSectorObjects` every `Interval` s and `ZDO.SetOwner`.

Current version: **0.1.0** — built and checked by `check-refs.ps1`.

---

## Done

- [x] A pass over the host's near simulation area, as in `ZDOMan.ReleaseNearbyZDOS`:
      `FindSectorObjects(zone, SimulationDistance(near, 0, classic))`, `Persistent` and
      `ZNetScene.InActiveArea` filters, `SetOwner` on objects owned by others.
- [x] Kinds of objects: the `Stations` group (by components `Smelter`, `CookingStation`,
      `Beehive`, `SapCollector`, `Fermenter`), `Bosses` (`Character.m_boss`), free
      `Components` and `Prefabs` lists; `Player` always excluded. Rebuilt on a config edit.
- [x] `HostOwnerApi.AddPrefab / AddComponent / IsOwnedKind` for other mods via reflection.
- [x] Yield to explicit claims: an object the host took that is back with a peer on the next
      pass (`ClaimOwnership` from a client — the WeaponArts taunt, a cart) is left alone for
      `YieldSeconds`. `ZDOID → pass` records live two passes, so a return after the host
      left the area does not count as a claim.
- [x] Console `hostowner status | list | now`.
- [x] Inert on clients (`!IsServer`) and on a dedicated server (`IsDedicated`, no
      `Player.m_localPlayer`).

## Checked against the game's IL (1.0.14)

- [x] `ZDOMan.Update`: `ReleaseZDOS` runs only on the server; in it, every 2 s,
      `ReleaseNearbyZDOS(server refPos, sessionID)`, then for each peer `(m_refPos, m_uid)`
      in `m_peers` order. Clients never take ownership — it can only be taken from them on
      the server, and nobody disputes it.
- [x] `ReleaseNearbyZDOS`: an object is given to a peer only if it has no owner or
      `!IsInPeerActiveArea(pos, owner)`; taken from the peer itself (`SetOwner(0)`) when the
      peer has left its area. So while the host is in the area, what it took stays with it.
- [x] `ZDO.SetOwner`: does nothing for the same owner, otherwise `SetOwnerInternal` +
      `IncreaseOwnerRevision`; `RPC_ZDOData` on receipt applies the owner with the higher
      revision.
- [x] `FindSectorObjects` accepts `null` instead of the far objects list.

## To check in game

- [ ] A guest without the mod reaches a smelter first → within ≤2 s the host's `Debug` log
      shows `smelter (…): taken from <guest>`, and the host's StationSpeed speeds it up.
- [ ] The guest stays at the smelter, the host stands near — the object does not flip
      between them (`hostowner status`: `taken just now` = 0 on the following passes).
- [ ] The host walks out of the area — the smelter goes back to the guest (vanilla release),
      and is taken again when the host returns.
- [ ] `Bosses=true`: a guest's fight with a boss in the host's area — the host's BossMeter
      sees the hits, the boss does not stutter on the owner change.
- [ ] `Bosses=true` + a guest with WeaponArts taunts the boss: after the first return the
      `Debug` log shows `… claimed back by <guest>, left alone for 30 s`, ownership does not
      flip after that, the taunt holds the target.
- [ ] Cost of a pass: `FindSectorObjects` over 25 sectors + a dictionary lookup per object —
      expected to be fractions of a millisecond every 2 s; confirm.
- [ ] Compatibility with mods that move ownership themselves.

## Next

- [ ] A radius smaller than the active area (e.g. ±1 zone only), so as not to take from a
      guest what is near them and far from the host.
- [ ] Exclusions: `ExcludePrefabs`, to drop, say, fermenters from the `Stations` group.
- [ ] Thunderstore icon — a placeholder for now; a prompt in the set's shared style is ready.
- [ ] A server mode (working on a dedicated server) — a separate task, not started.

## Ideas

- Give ownership back to a guest while the host is AFK / in a menu (`Player.IsSleeping` or
  pause) — so a boss's AI does not freeze.
- Show an object's current owner in its hover text (debugging).
