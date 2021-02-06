using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    [Info("MarkedForDeath", "humanalog", "v1.0")]
    [Description("One person will be marked for death. After killing this person, the killer becomes the next who is marked for death.")]
    public class MarkedForDeath : RustPlugin
    {
        #region Variables

        #endregion

        #region Server Hooks

        // Server Hooks

        protected override void LoadDefaultConfig() // Create the config file
        {
            Puts("Config for MarkedForDeath does not exist, creating one now.");
            Config["DefaultMarkedPlayerID"] = (ulong)0u;
            Config["DefaultMarkedPlayerName"] = "Unassigned";
        }

        void OnServerInitialized() // Create the plugin data file if it doesn't already exist
        {
            if (!Interface.Oxide.DataFileSystem.ExistsDatafile("MarkedForDeathData"))
            {
                DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("MarkedForDeathData");
                dataFile["MarkedPlayerSteamID"] = Convert.ToUInt64(Config["DefaultMarkedPlayerID"]);
                dataFile["MarkedPlayerName"] = Config["DefaultMarkedPlayerName"];
                dataFile["MarkedPlayerLocation"] = "A1";
                dataFile.Save();
            }
            else
            {
                DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("MarkedForDeathData");
                InfoPanel.Call("SetPanelAttribute", "MarkedForDeath", "CurrentMarkPanelText", "Content", $"Current Mark '{dataFile["MarkedPlayerName"]}' is located near {dataFile["MarkedPlayerLocation"]}.");
                InfoPanel.Call("RefreshPanel", "MarkedForDeath", "CurrentMarkPanel");
            }
        }

        // Gameplay Hooks

        object OnPlayerDeath(BasePlayer player, HitInfo info) // TODO: Send out messages on mark passing + refactor
        {
            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("MarkedForDeathData");

            // player dying is marked
            if (player.userID == Convert.ToUInt64(dataFile["MarkedPlayerSteamID"]))
            {
                Puts($"Marked player '{dataFile["MarkedPlayerName"]}' died.");

                // initiator is not an NPC (must be a player)
                if (info != null && !(info.InitiatorPlayer is NPCPlayer) && info.InitiatorPlayer.userID != player.userID)
                {
                    ChangeMarkedPlayer(info.InitiatorPlayer);
                    Puts($"Mark has been passed from {player.displayName} to {info.InitiatorPlayer.displayName}.");
                }
                else // Non-transferable 
                {
                    Puts("Marked player killed self. No mark transfer.");
                }
            }

            return null;
        }

        #endregion

        #region Commands

        [ConsoleCommand("rollmark")] // Marks a random player
        private void RollMarkCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer randomPlayer = BasePlayer.activePlayerList[Core.Random.Range(0, BasePlayer.activePlayerList.Count)];
            ChangeMarkedPlayer(randomPlayer);
            SendReply(arg, randomPlayer.displayName + " is now marked for death.");
        }

        [ConsoleCommand("whomark")] // Responds with who the current mark is
        private void WhoMarkCommand(ConsoleSystem.Arg arg)
        {
            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("MarkedForDeathData");

            SendReply(arg, dataFile["MarkedPlayerName"].ToString());
        }

        [ConsoleCommand("setmark")]
        private void SetMarkCommand(ConsoleSystem.Arg arg)
        {
            string markName = arg.GetString(0);

            BasePlayer mark = null;
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (player.displayName.Equals(markName, StringComparison.OrdinalIgnoreCase))
                {
                    mark = player;
                    break;
                }
            }

            if (mark != null)
            {
                ChangeMarkedPlayer(mark);
                SendReply(arg, mark.displayName + " is now marked for death.");
            }
            else
            {
                SendReply(arg, "Could not find player '" + markName + "'. Please try again.");
            }
        }

        [ConsoleCommand("setmarkbyid")]
        private void SetMarkByIDCommand(ConsoleSystem.Arg arg)
        {
            ulong markID = arg.GetULong(0);

            BasePlayer mark = null;
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (player.userID == markID)
                {
                    mark = player;
                    break;
                }
            }

            if (mark != null)
            {
                ChangeMarkedPlayer(mark);
                SendReply(arg, mark.displayName + " is now marked for death.");
            }
            else
            {
                SendReply(arg, "Could not find player with ID '" + markID + "'. Please try again.");
            }
        }

        #endregion

        #region InfoPanel Integration

        [PluginReference]
        Plugin InfoPanel; // Reference to InfoPanel plugin

        private void Loaded()
        {
            if (InfoPanel)
            {
                Puts("Info Panel loaded. Adding MarkedForDeath panels.");
                InfoPanel.Call("SendPanelInfo", "MarkedForDeath", new List<string> { "CurrentMarkPanel" });
                InfoPanel.Call("PanelRegister", "MarkedForDeath", "CurrentMarkPanel", CurrentMarkPanelCfg);
                InfoPanel.Call("RefreshPanel", "MarkedForDeath", "CurrentMarkPanel");
            }
        }

        private static string CurrentMarkPanelCfg = @"
        {
            ""AnchorX"": ""Right"",
            ""AnchorY"": ""Bottom"",
            ""Autoload"": true,
            ""Available"": true,
            ""BackgroundColor"": ""0 0 0 0.4"",
            ""Dock"": ""TopRightDock"",
            ""FadeOut"": 0.0,
            ""Height"": 0.95,
            ""Margin"": ""0 0 0 0.005"",
            ""Order"": 7,
            ""Text"": {
                ""Align"": ""MiddleCenter"",
                ""AnchorX"": ""Left"",
                ""AnchorY"": ""Bottom"",
                ""Available"": true,
                ""BackgroundColor"": ""0 0 0 0.4"",
                ""Content"": ""No Content"",
                ""FadeIn"": 0.0,
                ""FadeOut"": 0.0,
                ""FontColor"": ""1 1 1 1"",
                ""FontSize"": 14,
                ""Height"": 0.95,
                ""Margin"": ""0 0 0 0.005"",
                ""Order"": 0,
                ""Width"": 1.0
            },
            ""Width"": 1.0
        }";

        #endregion

        #region Helpers

        private void ChangeMarkedPlayer(BasePlayer nextMark)
        {
            // Update the datafile
            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("MarkedForDeathData");
            dataFile["MarkedPlayerSteamID"] = nextMark.userID;
            dataFile["MarkedPlayerName"] = nextMark.displayName;
            dataFile["MarkedPlayerLocation"] = ParseLocationFromVector(nextMark.ServerPosition);

            dataFile.Save();

            // Update the infopanel
            if (InfoPanel)
            {
                InfoPanel.Call("SetPanelAttribute", "MarkedForDeath", "CurrentMarkPanelText", "Content", "Current Mark '" + nextMark.displayName + "' is located near " + ParseLocationFromVector(nextMark.ServerPosition) + ".");
                InfoPanel.Call("RefreshPanel", "MarkedForDeath", "CurrentMarkPanel");
            }
        }

        private string ParseLocationFromVector(UnityEngine.Vector3 position)
        {
            // TODO: Implement parsing from vector to map string (A1), also consider implementing randomizing to get a nearby value instead
            return "A1";
        }

        #endregion
    }
}
