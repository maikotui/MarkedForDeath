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

        #region InfoPanel Integration

        [PluginReference]
        Plugin InfoPanel; // Reference to InfoPanel plugin

        private void Loaded()
        {
            if(InfoPanel)
            {
                InfoPanel.Call("SendPanelInfo", "RustCTF", new List<string> { "FlagHolderPanel" });
                InfoPanel.Call("PanelRegister", "RustCTF", "FlagHolderPanel", FlagHolderPanelCfg);
                InfoPanel.Call("RefreshPanel", "RustCTF", "FlagHolderPanel");
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
    }
}
