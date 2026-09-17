using System;
using System.Collections.Generic;
using BepInEx;
using BepInEx.Configuration;
using UnityEngine;

namespace HostOwner
{
    public partial class HostOwnerPlugin
    {
        // ------------------------------------------------------------------
        // config
        // ------------------------------------------------------------------
        private ConfigEntry<bool> _cfgEnabled;
        private ConfigEntry<bool> _cfgDebug;
        private ConfigEntry<float> _cfgInterval;

        private ConfigEntry<bool> _cfgStations;
        private ConfigEntry<bool> _cfgBosses;
        private ConfigEntry<string> _cfgComponents;
        private ConfigEntry<string> _cfgPrefabs;

        // the Stations group: everything that ticks or accepts items on its owner
        private static readonly Type[] StationTypes = { typeof(Smelter), typeof(CookingStation), typeof(Beehive), typeof(SapCollector), typeof(Fermenter) };

        private readonly HashSet<string> _warned = new HashSet<string>();

        private void BindConfig()
        {
            _cfgEnabled = Config.Bind("01 General", "Enabled", true,
                "Master switch. Off: objects already taken stay with the host until it walks away, as with any owner.");
            _cfgDebug = Config.Bind("01 General", "Debug", false, "Log every object taken over.");
            _cfgInterval = Config.Bind("01 General", "Interval", 2f,
                new ConfigDescription("Seconds between two passes over the host's active area. Vanilla hands ownership out every 2 s.",
                    new AcceptableValueRange<float>(0.5f, 30f)));

            _cfgStations = Config.Bind("02 Objects", "Stations", true,
                "Every prefab with a Smelter (smelter, blast furnace, charcoal kiln, spinning wheel, windmill, eitr refinery), CookingStation (cooking stations, oven), Beehive, SapCollector or Fermenter component, from any mod too.");
            _cfgBosses = Config.Bind("02 Objects", "Bosses", false,
                "Every creature flagged as a boss (Character.m_boss). Its AI then runs on the host, and mods that read the boss on its owner see every hit.");
            _cfgComponents = Config.Bind("02 Objects", "Components", "",
                "Component type names from the game, comma separated: every prefab carrying one of them is taken over. Example: Plant,Fireplace,Tameable. Unknown names are reported in the log once.");
            _cfgPrefabs = Config.Bind("02 Objects", "Prefabs", "",
                "Prefab names, comma separated, on top of the groups above. Example: piece_bathtub,Eikthyr. Players are never taken, whatever is listed.");

            Config.SettingChanged += delegate { _indexDirty = true; };
        }

        private static IEnumerable<string> Split(string text)
        {
            if (string.IsNullOrEmpty(text)) yield break;
            foreach (string raw in text.Split(','))
            {
                string s = raw.Trim();
                if (s.Length > 0) yield return s;
            }
        }

        private Type ResolveComponent(string name)
        {
            Type t = typeof(ZNetView).Assembly.GetType(name) ?? Type.GetType(name);
            if (t == null || !typeof(Component).IsAssignableFrom(t))
            {
                if (_warned.Add("type:" + name)) Logger.LogWarning("Component type '" + name + "' was not found in the game; ignored.");
                return null;
            }
            return t;
        }

        // Every registered prefab that matches the settings and the API registrations, once
        // per world and again after any change.
        private void BuildIndex(ZNetScene zs)
        {
            _wanted.Clear();
            _indexedScene = zs;
            if (zs.m_prefabs == null) return;

            HashSet<string> prefabs = new HashSet<string>(StringComparer.Ordinal);
            foreach (string s in Split(_cfgPrefabs.Value)) prefabs.Add(s);
            foreach (string s in HostOwnerApi.Prefabs) prefabs.Add(s);

            List<Type> components = new List<Type>();
            if (_cfgStations.Value) components.AddRange(StationTypes);
            foreach (string s in Split(_cfgComponents.Value)) { Type t = ResolveComponent(s); if (t != null && !components.Contains(t)) components.Add(t); }
            foreach (string s in HostOwnerApi.Components) { Type t = ResolveComponent(s); if (t != null && !components.Contains(t)) components.Add(t); }

            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (GameObject go in zs.m_prefabs)
            {
                if (go == null) continue;
                seen.Add(go.name);
                if (go.GetComponent<Player>() != null) continue;
                bool want = prefabs.Contains(go.name);
                if (!want)
                {
                    foreach (Type t in components)
                    {
                        if (go.GetComponent(t) != null) { want = true; break; }
                    }
                }
                if (!want && _cfgBosses.Value)
                {
                    Character c = go.GetComponent<Character>();
                    want = c != null && c.m_boss;
                }
                if (want) _wanted[zs.GetPrefabHash(go)] = go.name;
            }

            foreach (string s in prefabs)
            {
                if (!seen.Contains(s) && _warned.Add("prefab:" + s)) Logger.LogWarning("Prefab '" + s + "' is not registered in this world; ignored.");
            }

            Logger.LogInfo(_wanted.Count + " kinds of objects to own on the host: " + KindList(12));
        }

        private string KindList(int max)
        {
            List<string> names = new List<string>(_wanted.Values);
            names.Sort(StringComparer.Ordinal);
            if (names.Count == 0) return "none";
            if (max > 0 && names.Count > max)
            {
                int rest = names.Count - max;
                names.RemoveRange(max, rest);
                return string.Join(", ", names.ToArray()) + " and " + rest + " more";
            }
            return string.Join(", ", names.ToArray());
        }
    }
}
