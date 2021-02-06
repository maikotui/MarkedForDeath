﻿using Oxide.Core;
using Oxide.Core.Configuration;
using Oxide.Core.Plugins;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Oxide.Plugins
{
    [Info("MarkedForDeath", "humanalog", "1.0")]
    [Description("One person will be marked for death. After killing this person, the killer becomes the next who is marked for death.")]
    public class MarkedForDeath : RustPlugin
    {
        #region Variables
        private const int LOCATION_SCRAMBLE_AMOUNT = 150;

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
                InfoPanel.Call("SetPanelAttribute", "MarkedForDeath", "CurrentMarkPanelText", "Content", $"'{dataFile["MarkedPlayerName"]}' is marked for death. Last seen near {dataFile["MarkedPlayerLocation"]}.");
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
                Interface.Oxide.LogError($"Could not find player with name {markName}");
                SendError(arg, $"Could not find player with name {markName}");
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
                Interface.Oxide.LogError($"Could not find player with SteamID {markID}");
                SendError(arg, $"Could not find player with SteamID {markID}");
            }
        }

        [ConsoleCommand("updatemarklocation")]
        private void UpdateMarkLocationCommand(ConsoleSystem.Arg arg)
        {
            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("MarkedForDeathData");
            ulong markID = Convert.ToUInt64(dataFile["MarkedPlayerSteamID"]);

            BasePlayer mark = null;
            foreach (BasePlayer player in BasePlayer.allPlayerList)
            {
                if (player.userID == markID)
                {
                    mark = player;
                    break;
                }
            }

            if(mark != null)
            {
                dataFile["MarkedPlayerLocation"] = GetScrambledMapCoords(mark.ServerPosition);

                dataFile.Save();

                // Update the infopanel
                if (InfoPanel)
                {
                    InfoPanel.Call("SetPanelAttribute", "MarkedForDeath", "CurrentMarkPanelText", "Content", $"'{dataFile["MarkedPlayerName"]}' is marked for death. Last seen near {dataFile["MarkedPlayerLocation"]}.");
                    InfoPanel.Call("RefreshPanel", "MarkedForDeath", "CurrentMarkPanel");
                }
            }
            else
            {
                Interface.Oxide.LogError($"Could not find player with SteamID {markID}");
                SendError(arg, $"Could not find player with SteamID {markID}");
            }
        }

        #endregion

        #region Utility

        private void ChangeMarkedPlayer(BasePlayer nextMark)
        {
            // Update the datafile
            DynamicConfigFile dataFile = Interface.Oxide.DataFileSystem.GetDatafile("MarkedForDeathData");
            dataFile["MarkedPlayerSteamID"] = nextMark.userID;
            dataFile["MarkedPlayerName"] = nextMark.displayName;
            dataFile["MarkedPlayerLocation"] = GetScrambledMapCoords(nextMark.ServerPosition);

            dataFile.Save();

            // Update the infopanel
            if (InfoPanel)
            {
                InfoPanel.Call("SetPanelAttribute", "MarkedForDeath", "CurrentMarkPanelText", "Content", $"'{dataFile["MarkedPlayerName"]}' is marked for death. Last seen near {dataFile["MarkedPlayerLocation"]}.");
                InfoPanel.Call("RefreshPanel", "MarkedForDeath", "CurrentMarkPanel");
            }
        }

        private string GetScrambledMapCoords(UnityEngine.Vector3 position)
        {
            // Scramble the location
            UnityEngine.Vector3 randomPosition = new UnityEngine.Vector3(position.x, position.y, position.z);
            randomPosition.x += Oxide.Core.Random.Range(LOCATION_SCRAMBLE_AMOUNT * -1, LOCATION_SCRAMBLE_AMOUNT);
            randomPosition.z += Oxide.Core.Random.Range(LOCATION_SCRAMBLE_AMOUNT * -1, LOCATION_SCRAMBLE_AMOUNT);

            return FormatGridReference(randomPosition);
        }

        private static string FormatGridReference(UnityEngine.Vector3 position)
        {
            UnityEngine.Vector2 roundedPos = new UnityEngine.Vector2(World.Size / 2 + position.x, World.Size / 2 - position.z);
            string grid = $"{NumberToLetter((int)(roundedPos.x / 150))}:{(int)(roundedPos.y / 150)}";
            return grid;
        }

        private static string NumberToLetter(int num)
        {
            int num2 = UnityEngine.Mathf.FloorToInt(num / 26);
            int num3 = num % 26;
            string text = string.Empty;
            if (num2 > 0)
            {
                for (int i = 0; i < num2; i++)
                {
                    text += Convert.ToChar(65 + i);
                }
            }
            return text + Convert.ToChar(65 + num3).ToString();
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
    }
}
