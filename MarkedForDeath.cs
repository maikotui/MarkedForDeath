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
    [Info("MarkedForDeath", "humanalog", "b1.0")]
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
                dataFile.Save();
            }
        }

        #endregion

        #region Commands

        [ConsoleCommand("rollmark")] // Marks a random player
        private void RollMarkCommand(ConsoleSystem.Arg arg)
        {
            BasePlayer randomPlayer = BasePlayer.activePlayerList[Core.Random.Range(0, BasePlayer.activePlayerList.Count)];
            ChangeMarkedPlayer(randomPlayer);
            SendReply(arg, randomPlayer.displayName + " is now the flag holder.");
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
            foreach(BasePlayer player in BasePlayer.allPlayerList) {
                if (player.displayName.Equals(markName, StringComparison.OrdinalIgnoreCase))
                {
                    mark = player;
                    break;
                }
            }

            if(mark != null)
            {
                ChangeMarkedPlayer(mark);
                SendReply(arg, mark.displayName + " is now the flag holder.");
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
                SendReply(arg, mark.displayName + " is now the flag holder.");
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
            if(InfoPanel)
            {
                Puts("Info Panel loaded. Adding MarkedForDeath panels.");
                InfoPanel.Call("SendPanelInfo", "MarkedForDeath", new List<string> { "CurrentMarkPanel" });
                InfoPanel.Call("PanelRegister", "MarkedForDeath", "CurrentMarkPanel", FlagHolderPanelCfg);
                InfoPanel.Call("RefreshPanel", "MarkedForDeath", "CurrentMarkPanel");
            }
        }

        private static string FlagHolderPanelCfg = @"
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

            dataFile.Save();

            // Update the infopanel
            if (InfoPanel)
            {
                InfoPanel.Call("SetPanelAttribute", "MarkedForDeath", "CurrentMarkPanelText", "Content", nextMark.displayName);
                InfoPanel.Call("RefreshPanel", "MarkedForDeath", "CurrentMarkPanel");
            }
        }

        #endregion
    }
}
