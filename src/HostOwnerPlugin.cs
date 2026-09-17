using System;
using System.Collections.Generic;
using System.Globalization;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace HostOwner
{
    // Ownership of chosen objects on the hosting player.
    //
    // Every networked object has one owner: the client that runs its logic and writes the
    // results into the ZDO. A smelter is ticked by its owner, a boss is moved and attacks on
    // its owner, a fermenter accepts items on its owner. Since Valheim 1.0 the server hands
    // ownership out (ZDOMan.ReleaseZDOS, every 2 s): first to itself for what lies in its own
    // active area, then to each peer for what lies in theirs, and only for objects that have
    // no owner or whose owner has left the area. Whoever got an object keeps it until they
    // leave.
    //
    // This plugin, on the hosting player, takes over the objects of the listed kinds that lie
    // in the host's active area and belong to somebody else. From then on the host simulates
    // them: a station runs at the host's speed (StationSpeed), a boss's hits are counted on the
    // host (BossMeter), an AI mod on the host applies to it. Vanilla does not give them back
    // while the host stays in range, because their owner is in its area; when the host walks
    // away, its own release runs and the usual rules apply again.
    //
    // No Harmony patches: one pass over the active area on a timer and ZDO.SetOwner. Inert on
    // clients and on a dedicated server, which has no player and no active area.
    [BepInPlugin(Guid, Name, Version)]
    public partial class HostOwnerPlugin : BaseUnityPlugin
    {
        public const string Guid = "j1ga.hostowner";
        public const string Name = "Host Owner";
        public const string Version = "0.1.0";

        public static HostOwnerPlugin Instance;

        // prefab hash -> prefab name, for every kind of object to own; built per world
        private readonly Dictionary<int, string> _wanted = new Dictionary<int, string>();
        private ZNetScene _indexedScene;
        private bool _indexDirty;

        private readonly List<ZDO> _near = new List<ZDO>();
        private float _nextPass;

        // last pass and totals, for the console
        private int _lastWanted;
        private int _lastOwned;
        private int _lastClaimed;
        private int _claimedTotal;
        private int _passes;

        private int _errorCount;
        private bool _disabledByErrors;
        private const int MaxErrors = 25;
        private readonly HashSet<string> _loggedErrors = new HashSet<string>();

        // ------------------------------------------------------------------
        // lifecycle
        // ------------------------------------------------------------------
        private void Awake()
        {
            try
            {
                Instance = this;
                BindConfig();
                RegisterCommands();
                Logger.LogInfo(Name + " " + Version + " loaded. No Harmony patches are applied.");
            }
            catch (Exception e)
            {
                Logger.LogError("Awake failed, mod is inert: " + e);
                _disabledByErrors = true;
            }
        }

        private void Update()
        {
            if (_disabledByErrors) return;
            try
            {
                if (_indexDirty)
                {
                    _indexDirty = false;
                    _indexedScene = null;
                }
                if (Time.time < _nextPass) return;
                _nextPass = Time.time + _cfgInterval.Value;
                Pass();
            }
            catch (Exception e)
            {
                Fail("Update", e);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        // Rebuild the list of kinds at the next pass: a setting or a registration changed.
        internal static void Invalidate()
        {
            if (Instance != null) Instance._indexDirty = true;
        }

        // ------------------------------------------------------------------
        // the pass
        // ------------------------------------------------------------------

        // True only where the plugin has anything to do: on the hosting player, in a world.
        private bool Hosting
        {
            get
            {
                ZNet znet = ZNet.instance;
                return znet != null && znet.IsServer() && !znet.IsDedicated()
                    && Player.m_localPlayer != null && ZDOMan.instance != null && ZNetScene.instance != null;
            }
        }

        private void Pass()
        {
            if (!_cfgEnabled.Value || !Hosting) return;
            ZNet znet = ZNet.instance;
            ZDOMan man = ZDOMan.instance;
            ZNetScene zs = ZNetScene.instance;
            if (zs != _indexedScene) BuildIndex(zs);
            if (_wanted.Count == 0) return;

            // the same objects vanilla considers for the host: the near simulation area around
            // the reference position (the local player)
            Vector3 refPos = znet.GetReferencePosition();
            Vector2s zone = ZoneSystem.GetZone(refPos);
            SimulationDistance synced = znet.GetSyncedSimulationDistance();
            _near.Clear();
            man.FindSectorObjects(zone, new SimulationDistance(synced.NearSimulationDistance, 0, synced.IsClassic), _near, null);

            long me = ZDOMan.GetSessionID();
            int wanted = 0, owned = 0, claimed = 0;
            foreach (ZDO zdo in _near)
            {
                // non-persistent objects (players, projectiles) are owned by whoever made them
                if (zdo == null || !zdo.Persistent) continue;
                string name;
                if (!_wanted.TryGetValue(zdo.GetPrefab(), out name)) continue;
                wanted++;
                long owner = zdo.GetOwner();
                if (owner == me) { owned++; continue; }
                if (!ZNetScene.InActiveArea(zdo.GetPosition(), zone)) continue;
                zdo.SetOwner(me);
                claimed++;
                if (_cfgDebug.Value) Logger.LogInfo(name + " " + Pos(zdo.GetPosition()) + ": taken from " + PeerName(znet, owner));
            }
            _near.Clear();

            _lastWanted = wanted;
            _lastOwned = owned + claimed;
            _lastClaimed = claimed;
            _claimedTotal += claimed;
            _passes++;
        }

        // ------------------------------------------------------------------
        // helpers
        // ------------------------------------------------------------------
        private static string PeerName(ZNet znet, long uid)
        {
            if (uid == 0L) return "nobody";
            ZNetPeer peer = znet.GetPeer(uid);
            if (peer != null && !string.IsNullOrEmpty(peer.m_playerName)) return peer.m_playerName;
            return "peer " + uid;
        }

        private static string Pos(Vector3 p)
        {
            return "(" + p.x.ToString("0", CultureInfo.InvariantCulture) + ", " + p.y.ToString("0", CultureInfo.InvariantCulture) + ", " + p.z.ToString("0", CultureInfo.InvariantCulture) + ")";
        }

        private static string F(float v)
        {
            return v.ToString("0.##", CultureInfo.InvariantCulture);
        }

        // Errors must never take the game down with them: log each distinct message once, and
        // after too many of them switch the mod off entirely.
        private void Fail(string where, Exception e)
        {
            string key = where + ": " + e.GetType().Name + ": " + e.Message;
            if (_loggedErrors.Add(key)) Logger.LogError(key + "\n" + e.StackTrace);
            if (++_errorCount >= MaxErrors && !_disabledByErrors)
            {
                _disabledByErrors = true;
                Logger.LogError("Too many errors, " + Name + " is now inert until the game restarts.");
            }
        }
    }
}
