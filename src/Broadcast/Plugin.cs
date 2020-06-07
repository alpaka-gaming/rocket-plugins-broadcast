using Newtonsoft.Json;
using Rocket.API;
using Rocket.API.Collections;
using Rocket.Core;
using Rocket.Core.Logging;
using Rocket.Core.Plugins;
using Rocket.Plugins.Broadcast.Commands;
using Rocket.Plugins.Broadcast.Models;
using Rocket.Unturned;
using Rocket.Unturned.Chat;
using Rocket.Unturned.Player;
using SDG.Unturned;
using Steamworks;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using Color = UnityEngine.Color;
using P = Rocket.Unturned.Events.UnturnedPlayerEvents;

// ReSharper disable CollectionNeverQueried.Global

namespace Rocket.Plugins.Broadcast
{
    public class Plugin : RocketPlugin<Config>
    {
        public Plugin()
        {
            name = "Broadcast";
            Instance = this;
            Config = Instance.Configuration.Instance;
        }

        internal static Plugin Instance { get; private set; }
        internal static Config Config { get; private set; }

        internal static List<TextCommand> Command { get; private set; }
        internal static int LastIndex { get; private set; }
        internal static DateTime? LastMessage { get; private set; }

        private void FixedUpdate()
        {
            PrintMessage();
        }

        protected override void Load()
        {
            if (Config.JoinMessageEnable)
                U.Events.OnPlayerConnected += Events_OnPlayerConnected;
            if (Config.LeaveMessageEnable)
                U.Events.OnPlayerDisconnected += Events_OnPlayerDisconnected;
            if (Config.DeathMessageEnable)
                P.OnPlayerDeath += Events_OnPlayerDeath;

            if (Config.AnnouncementsEnable && Config.Commands != null)
            {
                Command = new List<TextCommand>();
                foreach (var item in Config.Commands)
                {
                    var command = new TextCommand(item.Name, item.Help, item.Text);
                    Command.Add(command);
                    R.Commands.Register(command);
                }
            }

            Instance.Configuration.Save();
            Logger.Log($"[{name}] Successfully Loaded!");
        }

        protected override void Unload()
        {
            if (Config.JoinMessageEnable)
                U.Events.OnPlayerConnected -= Events_OnPlayerConnected;
            if (Config.LeaveMessageEnable)
                U.Events.OnPlayerDisconnected -= Events_OnPlayerDisconnected;
            if (Config.DeathMessageEnable)
                P.OnPlayerDeath -= Events_OnPlayerDeath;
            if (Config.AnnouncementsEnable)
            {
                R.Commands.DeregisterFromAssembly(Assembly);
                Command.Clear();
            }

            Logger.Log($"[{name}] Successfully Unloaded!");
        }

        public override TranslationList DefaultTranslations
        {
            get
            {
                var result = new TranslationList()
                {
                    // TL1

                    { "connect_message", "{0} connected to the server." },
                    { "disconnect_message", "{0} disconnected from the server." },
                    { "connect_group_message", "{0}{1} connected to the server." },
                    { "disconnect_group_message", "{0}{1} disconnected from the server." },

                    { "connect_message_extended", "{0} [{1}] ({2}) connected to the server." },
                    { "disconnect_message_extended", "{0} [{1}] ({2}) disconnected from the server." },
                    { "connect_group_message_extended", "{0}{1} [{2}] ({3}) connected to the server." },
                    { "disconnect_group_message_extended", "{0}{1} [{2}] ({3}) disconnected from the server." },

                    { "connect_message_country", "{0} connected to the server from {1}."},
                    { "connect_group_message_country", "{0}{1} connected to the server from {2}." },
                    { "connect_message_country_extended", "{0} [{1}] ({2}) connected to the server from {3}." },
                    { "connect_group_message_country_extended", "{0}{1} [{2}] ({3}) connected to the server from {4}." },

                    // TL2
                    {"gun_headshot_death_message","{0} [GUN - {3}] {2} {1}"},
                    {"gun_death_message","{0} [GUN - {2}] {1}"},
                    {"food_death_message","[FOOD] {0}"},
                    {"arena_death_message","[ARENA] {0}"},
                    {"shred_death_message","[SHRED] {0}"},
                    {"punch_headshot_death_message","{0} [PUNCH] {2} {1}"},
                    {"punch_death_message","{0} [PUNCH] {1}"},
                    {"bones_death_message","[BONES] {0}"},
                    {"melee_headshot_death_message","{0} [MELEE - {3}] {2} {1}"},
                    {"melee_death_message","{0} [MELEE- {2}] {1}"},
                    {"water_death_message","[WATER] {0}"},
                    {"breath_death_message","[BREATH] {0}"},
                    {"zombie_death_message","[ZOMBIE] {0}"},
                    {"animal_death_message","[ANIMAL] {0}"},
                    {"grenade_death_message","[GRENADE] {0}"},
                    {"vehicle_death_message","[VEHICLE] {0}"},
                    {"suicide_death_message","[SUICIDE] {0}"},
                    {"burning_death_message","[BURNING] {0}"},
                    {"headshot_death_message","+ [HEADSHOT]" },
                    {"landmine_death_message","[LANDMINE] {0}"},
                    {"roadkill_death_message","{0} [ROADKILL] {1}"},
                    {"bleeding_death_message","[BLEEDING] {0}"},
                    {"freezing_death_message","[FREEZING] {0}"},
                    {"sentry_death_message","[SENTRY] {0}"},
                    {"charge_death_message","[CHARGE] {0}"},
                    {"missile_death_message","[MISSILE] {0}"},
                    {"splash_death_message","[SPLASH] {0}"},
                    {"acid_death_message","[ACID] {0}"},
                    {"spark_death_message", "[SPARK] {0}"},
                    {"infection_death_message", "[INFECTION] {0}"},
                    {"spit_death_message","[SPIT] {0}"},
                    {"kill_death_message","[ADMIN KILL] {0}"},
                    {"boulder_death_message","[BOULDER] {0}"},
                };

                return result;
            }
        }

        #region Methods

        private void Message(UnturnedPlayer player, bool @join)
        {
            try
            {
                if (State != PluginState.Loaded) return;

                IGeoIP geoip = null;
                if (Config.ShowJoinCountry && @join)
                {
                    P2PSessionState_t val = default;
                    SteamGameServerNetworking.GetP2PSessionState(player.CSteamID, out val);
                    string iPFromUInt = Parser.getIPFromUInt32(val.m_nRemoteIP);

                    try
                    {
#if DEBUG
                        iPFromUInt = new System.Net.WebClient().DownloadString("https://ipinfo.io/ip").Replace("\n", "").Trim();
#endif
                        var url = string.Empty;
                        if (Config.GeoIpProvider.Contains("ipinfo"))
                            url = $"http://ipinfo.io/{iPFromUInt}/json";
                        else if (Config.GeoIpProvider.Contains("ipapi"))
                            url = $"http://ip-api.com/json/{iPFromUInt}";

                        var request = (HttpWebRequest)WebRequest.Create(url);
                        request.Method = "GET";
                        request.UserAgent = "Unturned/Rocket";
                        request.AutomaticDecompression = DecompressionMethods.Deflate | DecompressionMethods.GZip;

                        var content = string.Empty;
                        using (var response = (HttpWebResponse)request.GetResponse())
                        using (var stream = response.GetResponseStream())
                        using (var sr = new StreamReader(stream))
                            content = sr.ReadToEnd();

                        if (!string.IsNullOrWhiteSpace(content))
                        {
                            if (Config.GeoIpProvider.Contains("ipinfo"))
                                geoip = JsonConvert.DeserializeObject<IpInfo>(content);
                            else if (Config.GeoIpProvider.Contains("ipapi"))
                                geoip = JsonConvert.DeserializeObject<IpApi>(content);
                        }
                    }
                    catch (Exception)
                    {
#if DEBUG
                        throw;
#endif
                    }

                    if (geoip != null && geoip.Country.Length == 2 && Config.Countries.Any(m => m.Key == geoip.Country))
                        geoip.Country = Config.Countries.First(m => m.Key == geoip.Country).Value;

#if DEBUG
                    if (geoip != null)
                        UnturnedChat.Say($"Country: {geoip.Country}");
#endif
                }

                var message = string.Empty;

                if (!R.Permissions.HasPermission(player, "broadcast.vanish"))
                {
                    if ((R.Permissions.HasPermission(player, "broadcast.group") || player.IsAdmin) && Config.GroupMessages)
                    {
                        var group = R.Permissions.GetGroups(player, false).FirstOrDefault();
                        if (!Config.ExtendedMessages)
                        {
                            message = Translate(@join ? "connect_group_message" : "disconnect_group_message", group != null ? group.DisplayName + ": " : "", player.CharacterName);
                            if (geoip != null && @join)
                                message = Translate("connect_group_message_country", group != null ? group.DisplayName + ": " : "", player.CharacterName, geoip.Country);

                            UnturnedChat.Say(message, @join ? Config.JoinMessage : Config.LeaveMessage);
                        }
                        else
                        {
                            foreach (var sdgPlayer in Provider.clients)
                            {
                                if (sdgPlayer != null)
                                {
                                    if (R.Permissions.HasPermission(new RocketPlayer(sdgPlayer.playerID.steamID.ToString()), "broadcast.extended") || sdgPlayer.isAdmin)
                                    {
                                        message = Translate(@join ? "connect_group_message_extended" : "disconnect_group_message_extended", group != null ? group.DisplayName + ": " : "", player.CharacterName, player.SteamName, player.CSteamID.ToString());
                                        if (geoip != null && @join)
                                            message = Translate("connect_group_message_country_extended", group != null ? group.DisplayName + ": " : "", player.CharacterName, player.SteamName, player.CSteamID.ToString(), geoip.Country);
                                    }
                                    else
                                    {
                                        message = Translate(@join ? "connect_group_message" : "disconnect_group_message", group != null ? group.DisplayName + ": " : "", player.CharacterName);
                                        if (geoip != null && @join)
                                            message = Translate("connect_group_message_country", group != null ? group.DisplayName + ": " : "", player.CharacterName, geoip.Country);
                                    }

                                    UnturnedChat.Say(sdgPlayer.playerID.steamID, message, @join ? Config.JoinMessage : Config.LeaveMessage);
                                }
                            }
                        }
                    }
                    else
                    {
                        if (!Config.ExtendedMessages)
                        {
                            message = Translate(@join ? "connect_message" : "disconnect_message", player.CharacterName);
                            if (geoip != null && @join)
                                message = Translate("connect_message_country", player.CharacterName, geoip.Country);
                            UnturnedChat.Say(message, @join ? Config.JoinMessage : Config.LeaveMessage);
                        }
                        else
                        {
                            foreach (var sdgPlayer in Provider.clients)
                            {
                                if (sdgPlayer != null)
                                {
                                    if (R.Permissions.HasPermission(new RocketPlayer(sdgPlayer.playerID.steamID.ToString()), "broadcast.extended") || sdgPlayer.isAdmin)
                                    {
                                        message = Translate(@join ? "connect_message_extended" : "disconnect_message_extended", player.CharacterName, player.SteamName, player.CSteamID.ToString());
                                        if (geoip != null && @join)
                                            message = Translate("connect_message_country_extended", player.CharacterName, player.SteamName, player.CSteamID.ToString(), geoip.Country);
                                    }
                                    else
                                    {
                                        message = Translate(@join ? "connect_message" : "disconnect_message", player.CharacterName);
                                        if (geoip != null && @join)
                                            message = Translate("connect_message_country", player.CharacterName, geoip.Country);
                                    }
                                    UnturnedChat.Say(sdgPlayer.playerID.steamID, message, @join ? Config.JoinMessage : Config.LeaveMessage);
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        private void PrintMessage()
        {
            try
            {
                if (State == PluginState.Loaded && Config.Messages != null && (LastMessage == null || ((DateTime.Now - LastMessage.Value).TotalSeconds > Config.AnnouncementsInterval)))
                {
                    if (LastIndex > (Config.Messages.Count - 1)) LastIndex = 0;
                    var message = Config.Messages[LastIndex];
                    UnturnedChat.Say(message.Text, UnturnedChat.GetColorFromName(message.Color, Color.green));
                    Logger.Log(message.Text);
                    LastMessage = DateTime.Now;
                    LastIndex++;
                }
            }
            catch (Exception ex)
            {
                Logger.LogException(ex);
            }
        }

        #endregion Methods

        #region Events

        private void Events_OnPlayerConnected(UnturnedPlayer player)
        {
            Message(player, true);
        }

        private void Events_OnPlayerDisconnected(UnturnedPlayer player)
        {
            if (player != null)
                Message(player, false);
            else
                Logger.LogWarning("Warning: DC message for a player didn't run as the player data was null.");
        }

        private void Events_OnPlayerDeath(UnturnedPlayer player, EDeathCause cause, ELimb limb, Steamworks.CSteamID murderer)
        {
            var killer = UnturnedPlayer.FromCSteamID(murderer);

            var headshot = Translate("headshot_death_message");

            if (cause.ToString() == "GUN")
            {
                if (limb == ELimb.SKULL)
                    UnturnedChat.Say(Translate("gun_headshot_death_message", killer.DisplayName, player.DisplayName, headshot, UnturnedPlayer.FromCSteamID(murderer).Player.equipment.asset.itemName), Config.DeathMessage);
                else
                    UnturnedChat.Say(Translate("gun_death_message", killer.DisplayName, player.DisplayName, UnturnedPlayer.FromCSteamID(murderer).Player.equipment.asset.itemName), Config.DeathMessage);
            }
            else if (cause.ToString() == "MELEE")
            {
                if (limb == ELimb.SKULL)
                    UnturnedChat.Say(Translate("melee_headshot_death_message", killer.DisplayName, player.DisplayName, headshot, UnturnedPlayer.FromCSteamID(murderer).Player.equipment.asset.itemName), Config.DeathMessage);
                else
                    UnturnedChat.Say(Translate("melee_death_message", killer.DisplayName, player.DisplayName, UnturnedPlayer.FromCSteamID(murderer).Player.equipment.asset.itemName), Config.DeathMessage);
            }
            else if (cause.ToString() == "PUNCH")
            {
                if (limb == ELimb.SKULL)
                    UnturnedChat.Say(Translate("punch_headshot_death_message", killer.DisplayName, player.DisplayName, headshot), Config.DeathMessage);
                else
                    UnturnedChat.Say(Translate("punch_death_message", killer.DisplayName, player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "SHRED")
            {
                UnturnedChat.Say(Translate("shred_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "ZOMBIE")
            {
                UnturnedChat.Say(Translate("zombie_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "ANIMAL")
            {
                UnturnedChat.Say(Translate("animal_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "ROADKILL")
            {
                UnturnedChat.Say(Translate("roadkill_death_message", killer.DisplayName, player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "SPARK")
            {
                UnturnedChat.Say(Translate("spark_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "VEHICLE")
            {
                UnturnedChat.Say(Translate("vehicle_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "FOOD")
            {
                UnturnedChat.Say(Translate("food_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "WATER")
            {
                UnturnedChat.Say(Translate("water_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "INFECTION")
            {
                UnturnedChat.Say(Translate("infection_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "BLEEDING")
            {
                UnturnedChat.Say(Translate("bleeding_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "LANDMINE")
            {
                UnturnedChat.Say(Translate("landmine_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "BREATH")
            {
                UnturnedChat.Say(Translate("breath_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "KILL")
            {
                UnturnedChat.Say(Translate("kill_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "FREEZING")
            {
                UnturnedChat.Say(Translate("freezing_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "SENTRY")
            {
                UnturnedChat.Say(Translate("sentry_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "CHARGE")
            {
                UnturnedChat.Say(Translate("charge_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "MISSILE")
            {
                UnturnedChat.Say(Translate("missile_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "BONES")
            {
                UnturnedChat.Say(Translate("bones_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "SPLASH")
            {
                UnturnedChat.Say(Translate("splash_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "ACID")
            {
                UnturnedChat.Say(Translate("acid_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "SPIT")
            {
                UnturnedChat.Say(Translate("spit_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "BURNING")
            {
                UnturnedChat.Say(Translate("burning_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "BURNER")
            {
                UnturnedChat.Say(Translate("burner_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "BOULDER")
            {
                UnturnedChat.Say(Translate("boulder_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "ARENA")
            {
                UnturnedChat.Say(Translate("arena_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "GRENADE")
            {
                UnturnedChat.Say(Translate("grenade_death_message", player.DisplayName), Config.DeathMessage);
            }
            else if (cause.ToString() == "SUICIDE" && Configuration.Instance.SuicideMessages)
            {
                UnturnedChat.Say(Translate("suicide_death_message", player.DisplayName), Config.DeathMessage);
            }
        }

        #endregion Events
    }
}