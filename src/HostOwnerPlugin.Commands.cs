using System;
using System.Text;
using BepInEx;
using UnityEngine;

namespace HostOwner
{
    public partial class HostOwnerPlugin
    {
        // ------------------------------------------------------------------
        // console command: hostowner
        // ------------------------------------------------------------------
        private void RegisterCommands()
        {
            new Terminal.ConsoleCommand("hostowner",
                "Host Owner. 'hostowner status' - what the host owns; 'hostowner list' - the kinds; 'hostowner now' - a pass right away",
                delegate(Terminal.ConsoleEventArgs args) { RunCommand(args); });
        }

        private static void Say(Terminal.ConsoleEventArgs args, string text)
        {
            if (args != null && args.Context != null) args.Context.AddString(text);
        }

        private void RunCommand(Terminal.ConsoleEventArgs args)
        {
            try
            {
                string sub = args.Args.Length > 1 ? args.Args[1].ToLowerInvariant() : "";
                if (sub == "status") { Say(args, StatusText()); return; }
                if (sub == "list") { Say(args, _wanted.Count + " kinds: " + KindList(0)); return; }
                if (sub == "now")
                {
                    if (!Hosting) { Say(args, "Not hosting a world with a player; nothing to do."); return; }
                    _indexedScene = null;
                    _nextPass = Time.time + _cfgInterval.Value;
                    Pass();
                    Say(args, StatusText());
                    return;
                }
                Say(args, "hostowner status | list | now");
            }
            catch (Exception e)
            {
                Say(args, "hostowner: " + e.Message);
                Fail("command", e);
            }
        }

        private string StatusText()
        {
            if (_disabledByErrors) return Name + " is inert after errors, see the log.";
            if (!_cfgEnabled.Value) return Name + " is disabled in the config.";
            ZNet znet = ZNet.instance;
            if (znet == null) return Name + ": no world loaded.";
            if (!znet.IsServer()) return Name + ": this client is not the host, nothing is taken over here.";
            if (znet.IsDedicated()) return Name + ": a dedicated server has no player and no active area, nothing is taken over here.";
            StringBuilder sb = new StringBuilder();
            sb.Append(_wanted.Count).Append(" kinds; last pass: ").Append(_lastWanted).Append(" such objects in the active area, ")
              .Append(_lastOwned).Append(" owned by the host, ").Append(_lastClaimed).Append(" taken just now; ")
              .Append(_claimedTotal).Append(" taken in ").Append(_passes).Append(" passes, every ").Append(F(_cfgInterval.Value)).Append(" s.");
            return sb.ToString();
        }
    }
}
