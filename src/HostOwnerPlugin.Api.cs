using System;
using System.Collections.Generic;

namespace HostOwner
{
    // For other mods: kinds of objects to own on the host besides the config, registered at
    // any time; the list is rebuilt at the next pass. Nothing needs to reference this
    // assembly - reach it through reflection, and do nothing when it is not there:
    //
    //   Type api = Type.GetType("HostOwner.HostOwnerApi, HostOwner");
    //   if (api != null) api.GetMethod("AddPrefab").Invoke(null, new object[] { "fermenter" });
    //
    public static class HostOwnerApi
    {
        internal static readonly HashSet<string> Prefabs = new HashSet<string>(StringComparer.Ordinal);
        internal static readonly HashSet<string> Components = new HashSet<string>(StringComparer.Ordinal);

        // A prefab by its registered name, e.g. "fermenter".
        public static void AddPrefab(string prefabName)
        {
            if (string.IsNullOrEmpty(prefabName)) return;
            if (Prefabs.Add(prefabName.Trim())) HostOwnerPlugin.Invalidate();
        }

        // Every prefab carrying a component of this game type, e.g. "Plant".
        public static void AddComponent(string typeName)
        {
            if (string.IsNullOrEmpty(typeName)) return;
            if (Components.Add(typeName.Trim())) HostOwnerPlugin.Invalidate();
        }

        // Whether objects of this prefab are taken over by the host in the current world.
        public static bool IsOwnedKind(int prefabHash)
        {
            HostOwnerPlugin p = HostOwnerPlugin.Instance;
            return p != null && p.IsWanted(prefabHash);
        }
    }

    public partial class HostOwnerPlugin
    {
        internal bool IsWanted(int prefabHash)
        {
            return _wanted.ContainsKey(prefabHash);
        }
    }
}
