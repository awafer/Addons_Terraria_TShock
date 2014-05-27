/**
* Copyright (c) 2014 - atom0s [atom0s@live.com]
*
* Addons is free software: you can redistribute it and/or modify
* it under the terms of the GNU General Public License as published by
* the Free Software Foundation, either version 3 of the License, or
* (at your option) any later version.
*
* Addons is distributed in the hope that it will be useful,
* but WITHOUT ANY WARRANTY; without even the implied warranty of
* MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
* GNU General Public License for more details.
*
* You should have received a copy of the GNU General Public License
* along with Addons.  If not, see <http://www.gnu.org/licenses/>.
*/

namespace Addons
{
    using System;
    using System.Collections.Concurrent;
    using System.Collections.Generic;
    using System.ComponentModel;
    using System.Linq;
    using System.Reflection;
    using Terraria;
    using TerrariaApi.Server;
    using TShockAPI;

    [ApiVersion(1, 16)]
    public class Addons : TerrariaPlugin
    {
        /// <summary>
        /// Dictionary of currently loaded addons.
        /// </summary>
        private readonly ConcurrentDictionary<string, Addon> m_Addons = new ConcurrentDictionary<string, Addon>();

        /// <summary>
        /// Dictionary of current registered addon commands.
        /// </summary>
        private readonly ConcurrentDictionary<string, List<string>> m_AddonCommands = new ConcurrentDictionary<string, List<string>>();

        /// <summary>
        /// Lock object to help prevent threading races for the Lua addons.
        /// </summary>
        private readonly object m_AddonsLock = new object();

        /// <summary>
        /// Main Terraria game object.
        /// </summary>
        private readonly Main m_GameObject;

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="game"></param>
        public Addons(Main game)
            : base(game)
        {
            base.Order = int.MaxValue;
            this.m_GameObject = game;
        }

        /// <summary>
        /// Dispose override.
        /// </summary>
        /// <param name="disposing"></param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (this.m_AddonsLock)
                {
                    foreach (var a in this.m_Addons)
                    {
                        a.Value.InvokeEvent("unload");
                        a.Value.Release();
                    }

                    this.m_Addons.Clear();
                    this.m_AddonCommands.Clear();
                }
            }
            base.Dispose(disposing);
        }

        /// <summary>
        /// Initialize override.
        /// </summary>
        public override void Initialize()
        {
            // Register addon chat command..
            Commands.ChatCommands.Add(new Command("addons.manageaddons", HandleAddonCommand, "addon"));

            /**
             * Register events for Lua callbacks.
             */

            //ServerApi.Hooks.ClientChat.Register(this, null); // Does not get called.
            //ServerApi.Hooks.ClientChatReceived.Register(this, null); // Does not get called.
            //ServerApi.Hooks.GameGetKeyState.Register(this, null); // Does not get called.
            ServerApi.Hooks.GameHardmodeTileUpdate.Register(this, OnGameHardmodeTileUpdate);
            ServerApi.Hooks.GameInitialize.Register(this, OnGameInitialize);
            ServerApi.Hooks.GamePostInitialize.Register(this, OnGamePostInitialize);
            ServerApi.Hooks.GamePostUpdate.Register(this, OnGamePostUpdate);
            ServerApi.Hooks.GameStatueSpawn.Register(this, OnGameStatueSpawn);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
            ServerApi.Hooks.GameWorldConnect.Register(this, OnGameWorldConnect);
            ServerApi.Hooks.GameWorldDisconnect.Register(this, OnGameWorldDisconnect);
            ServerApi.Hooks.ItemNetDefaults.Register(this, OnItemNetDefaults);
            ServerApi.Hooks.ItemSetDefaultsInt.Register(this, OnItemSetDefaultsInt);
            ServerApi.Hooks.ItemSetDefaultsString.Register(this, OnItemSetDefaultsString);
            ServerApi.Hooks.NetGetData.Register(this, OnNetGetData);
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnNetGreetPlayer);
            ServerApi.Hooks.NetNameCollision.Register(this, OnNetNameCollision);
            ServerApi.Hooks.NetSendBytes.Register(this, OnNetSendBytes);
            ServerApi.Hooks.NetSendData.Register(this, OnNetSendData);
            ServerApi.Hooks.NpcLootDrop.Register(this, OnNpcLootDrop);
            ServerApi.Hooks.NpcNetDefaults.Register(this, OnNpcNetDefaults);
            ServerApi.Hooks.NpcSetDefaultsInt.Register(this, OnNpcSetDefaultsInt);
            ServerApi.Hooks.NpcSetDefaultsString.Register(this, OnNpcSetDefaultsString);
            ServerApi.Hooks.NpcSpawn.Register(this, OnNpcSpawn);
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
            ServerApi.Hooks.NpcTriggerPressurePlate.Register(this, OnNpcTriggerPressurePlate);
            //ServerApi.Hooks.PlayerUpdatePhysics.Register(this, null); // Does not get called.
            ServerApi.Hooks.ProjectileSetDefaults.Register(this, OnProjectileSetDefaults);
            ServerApi.Hooks.ProjectileTriggerPressurePlate.Register(this, OnProjectileTriggerPressurePlate);
            ServerApi.Hooks.ServerChat.Register(this, OnServerChat);
            ServerApi.Hooks.ServerCommand.Register(this, OnServerCommand);
            ServerApi.Hooks.ServerConnect.Register(this, OnServerConnect);
            ServerApi.Hooks.ServerJoin.Register(this, OnServerJoin);
            ServerApi.Hooks.ServerLeave.Register(this, OnServerLeave);
            ServerApi.Hooks.ServerSocketReset.Register(this, OnServerSocketReset);
            ServerApi.Hooks.WorldChristmasCheck.Register(this, OnWorldChristmasCheck);
            ServerApi.Hooks.WorldHalloweenCheck.Register(this, OnWorldHalloweenCheck);
            ServerApi.Hooks.WorldMeteorDrop.Register(this, OnWorldMeteorDrop);
            ServerApi.Hooks.WorldSave.Register(this, OnWorldSave);
            ServerApi.Hooks.WorldStartHardMode.Register(this, OnWorldStartHardMode);
        }

        /// <summary>
        /// GameHardmodeTileUpdate callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnGameHardmodeTileUpdate(HardmodeTileUpdateEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("GameHardmodeTileUpdate", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// GameInitialize callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnGameInitialize(EventArgs args)
        {
            lock (this.m_AddonsLock)
                this.m_Addons.ForEach(a => a.Value.InvokeEvent("GameInitialize", args));
        }

        /// <summary>
        /// GamePostInitialize callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnGamePostInitialize(EventArgs args)
        {
            lock (this.m_AddonsLock)
                this.m_Addons.ForEach(a => a.Value.InvokeEvent("GamePostInitialize", args));
        }

        /// <summary>
        /// GamePostUpdate callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnGamePostUpdate(EventArgs args)
        {
            lock (this.m_AddonsLock)
                this.m_Addons.ForEach(a => a.Value.InvokeEvent("GamePostUpdate", args));
        }

        /// <summary>
        /// GameStatueSpawn callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnGameStatueSpawn(StatueSpawnEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("GameStatueSpawn", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// GameUpdate callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnGameUpdate(EventArgs args)
        {
            lock (this.m_AddonsLock)
                this.m_Addons.ForEach(a => a.Value.InvokeEvent("GameUpdate", args));
        }

        /// <summary>
        /// GameWorldConnect callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnGameWorldConnect(EventArgs args)
        {
            lock (this.m_AddonsLock)
                this.m_Addons.ForEach(a => a.Value.InvokeEvent("GameWorldConnect", args));
        }

        /// <summary>
        /// GameWorldDisconnect callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnGameWorldDisconnect(EventArgs args)
        {
            lock (this.m_AddonsLock)
                this.m_Addons.ForEach(a => a.Value.InvokeEvent("GameWorldDisconnect", args));
        }

        /// <summary>
        /// ItemNetDefaults callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnItemNetDefaults(SetDefaultsEventArgs<Item, int> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ItemNetDefaults", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ItemSetDefaultsInt callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnItemSetDefaultsInt(SetDefaultsEventArgs<Item, int> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ItemSetDefaultsInt", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ItemSetDefaultsString callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnItemSetDefaultsString(SetDefaultsEventArgs<Item, string> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ItemSetDefaultsString", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NetGetData callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNetGetData(GetDataEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NetGetData", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NetGreetPlayer callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNetGreetPlayer(GreetPlayerEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NetGreetPlayer", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NetNameCollision callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNetNameCollision(NameCollisionEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NetNameCollision", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NetSendBytes callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNetSendBytes(SendBytesEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NetSendBytes", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NetSendData callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNetSendData(SendDataEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NetSendData", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NpcLootDrop callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNpcLootDrop(NpcLootDropEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NpcLootDrop", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NpcNetDefaults callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNpcNetDefaults(SetDefaultsEventArgs<NPC, int> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NpcNetDefaults", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NpcSetDefaultsInt callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNpcSetDefaultsInt(SetDefaultsEventArgs<NPC, int> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NpcSetDefaultsInt", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NpcSetDefaultsString callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNpcSetDefaultsString(SetDefaultsEventArgs<NPC, string> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NpcSetDefaultsString", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NpcSpawn callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNpcSpawn(NpcSpawnEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NpcSpawn", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NpcStrike callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NpcStrike", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// NpcTriggerPressurePlate callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnNpcTriggerPressurePlate(TriggerPressurePlateEventArgs<NPC> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("NpcTriggerPressurePlate", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ProjectileSetDefaults callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnProjectileSetDefaults(SetDefaultsEventArgs<Projectile, int> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ProjectileSetDefaults", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ProjectileTriggerPressurePlate callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnProjectileTriggerPressurePlate(TriggerPressurePlateEventArgs<Projectile> args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ProjectileTriggerPressurePlate", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ServerChat callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnServerChat(ServerChatEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ServerChat", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ServerCommand callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnServerCommand(CommandEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ServerCommand", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ServerConnect callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnServerConnect(ConnectEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ServerConnect", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ServerJoin callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnServerJoin(JoinEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("ServerJoin", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// ServerLeave callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnServerLeave(LeaveEventArgs args)
        {
            lock (this.m_AddonsLock)
                this.m_Addons.ForEach(a => a.Value.InvokeEvent("ServerLeave", args));
        }

        /// <summary>
        /// ServerSocketReset callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnServerSocketReset(SocketResetEventArgs args)
        {
            lock (this.m_AddonsLock)
                this.m_Addons.ForEach(a => a.Value.InvokeEvent("ServerSocketReset", args));
        }

        /// <summary>
        /// WorldChristmasCheck callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnWorldChristmasCheck(ChristmasCheckEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("WorldChristmasCheck", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// WorldHalloweenCheck callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnWorldHalloweenCheck(HalloweenCheckEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("WorldHalloweenCheck", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// WorldMeteorDrop callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnWorldMeteorDrop(MeteorDropEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("WorldMeteorDrop", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// WorldSave callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnWorldSave(WorldSaveEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("WorldSave", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// WorldStartHardMode callback.
        /// </summary>
        /// <param name="args"></param>
        private void OnWorldStartHardMode(HandledEventArgs args)
        {
            lock (this.m_AddonsLock)
            {
                this.m_Addons.ForEach(a =>
                    {
                        var ret = a.Value.InvokeEvent("WorldStartHardMode", args);
                        if (ret != null && ret.Length >= 1)
                        {
                            bool result;
                            if (bool.TryParse(ret[0].ToString(), out result) && result)
                                args.Handled = true;
                        }
                    });
            }
        }

        /// <summary>
        /// Handles the addon command.
        /// </summary>
        /// <param name="e"></param>
        private void HandleAddonCommand(CommandArgs e)
        {
            // Validate we have proper arguments..
            if (e.Parameters.Count >= 1)
            {
                // Handle list command..
                if (e.Parameters[0].ToLower() == "list")
                {
                    lock (this.m_AddonsLock)
                        this.m_Addons.ForEach(a => e.Player.SendSuccessMessage("[Addon] {0} v{1}, by {2} -- State: {3}", a.Value.Name, a.Value.Version, a.Value.Author, a.Value.State.ToString()));
                    return;
                }

                // Handle load command..
                if (e.Parameters[0].ToLower() == "load" && e.Parameters.Count >= 2)
                {
                    var addonName = string.Join(" ", e.Parameters.Skip(1).Take(e.Parameters.Count - 1).ToList());
                    lock (this.m_AddonsLock)
                    {
                        Addon addon;
                        this.m_Addons.TryGetValue(addonName.ToLower(), out addon);
                        if (addon != null)
                        {
                            e.Player.SendErrorMessage("[Addon] Addon '{0}' is already loaded! Cannot load!", addonName);
                            return;
                        }

                        addon = new Addon();
                        if (!addon.Initialize(addonName, this, m_GameObject))
                            return;

                        addon.InvokeEvent("load");

                        if (this.m_Addons.TryAdd(addonName.ToLower(), addon))
                        {
                            e.Player.SendSuccessMessage("[Addons] Loaded addon '{0}' v{1}, by {2}", addon.Name, addon.Version, addon.Author);
                        }
                        return;
                    }
                }

                // Handle unload command..
                if (e.Parameters[0].ToLower() == "unload" && e.Parameters.Count >= 2)
                {
                    var addonName = string.Join(" ", e.Parameters.Skip(1).Take(e.Parameters.Count - 1).ToList());
                    lock (this.m_AddonsLock)
                    {
                        Addon addon;
                        this.m_Addons.TryRemove(addonName.ToLower(), out addon);
                        if (addon == null)
                        {
                            e.Player.SendErrorMessage("[Addon] Addon '{0}' is not loaded. Cannot unload!", addonName);
                            return;
                        }

                        addon.InvokeEvent("unload");
                        addon.Release();

                        e.Player.SendSuccessMessage("[Addons] Unloaded addon '{0}'!", addonName);

                        // Find any commands by this addon and unload them..
                        this.RemoveAddonCommands(addonName.ToLower());

                        return;
                    }
                }

                // Handle unload all command..
                if (e.Parameters[0].ToLower() == "unloadall")
                {
                    lock (this.m_AddonsLock)
                    {
                        var keys = this.m_Addons.Keys;
                        foreach (var k in keys)
                        {
                            Addon addon;
                            this.m_Addons.TryRemove(k, out addon);
                            if (addon == null)
                                continue;

                            addon.InvokeEvent("unload");
                            addon.Release();
                            
                            e.Player.SendSuccessMessage("[Addons] Unloaded addon '{0}'!", k);

                            // Find any commands by this addon and unload them..
                            this.RemoveAddonCommands(k.ToLower());
                        }
                        return;
                    }
                }

                // Handle reload command..
                if (e.Parameters[0].ToLower() == "reload" && e.Parameters.Count >= 2)
                {
                    var addonName = string.Join(" ", e.Parameters.Skip(1).Take(e.Parameters.Count - 1).ToList());
                    lock (this.m_AddonsLock)
                    {
                        Addon addon;
                        this.m_Addons.TryGetValue(addonName.ToLower(), out addon);
                        if (addon == null)
                            return;

                        addon.InvokeEvent("unload");

                        // Find any commands by this addon and unload them..
                        this.RemoveAddonCommands(addonName.ToLower());

                        addon.Reload();
                        addon.InvokeEvent("load");

                        TSPlayer.Server.SendSuccessMessage("[Addons] Reloaded addon '{0}'!", addonName);
                        return;
                    }
                }
            }

            // Print out the addon usage..
            e.Player.SendErrorMessage("Invalid addon command! Commands:");
            e.Player.SendErrorMessage("/addon load [name] - Loads the given addon.");
            e.Player.SendErrorMessage("/addon unload [name] - Unloads the given addon.");
            e.Player.SendErrorMessage("/addon reload [name] - Reloads the given addon.");

            // If server, print the list command info..
            if (e.Player.Index == TSPlayer.Server.Index)
                e.Player.SendErrorMessage("/addon list - Prints the current loaded addons and their states.");
        }

        /// <summary>
        /// Registers a command from an addon.
        /// </summary>
        /// <param name="addonName"></param>
        /// <param name="permission"></param>
        /// <param name="command"></param>
        public bool RegisterCommand(string addonName, string permission, string command)
        {
            lock (this.m_AddonsLock)
            {
                // Ensure the arguments are lower-case..
                addonName = addonName.ToLower();
                command = command.ToLower();

                // Do not allow multiple addons to own the same command..
                if (this.m_AddonCommands.Any(a => a.Value.Contains(command)))
                    return false;

                // Erase any previous registered commands that TShock has with this name..
                var commands = Commands.ChatCommands.Where(c => c.HasAlias(command)).ToList();
                if (commands.Count > 0)
                {
                    foreach (var cmd in commands)
                        Commands.ChatCommands.Remove(cmd);
                }

                // Ensure we have an entry for the given addon..
                if (!this.m_AddonCommands.ContainsKey(addonName))
                    this.m_AddonCommands.TryAdd(addonName, new List<string>());

                // Prevent duplicate entries..
                if (this.m_AddonCommands[addonName].Contains(command))
                    this.m_AddonCommands[addonName].Remove(command);
                this.m_AddonCommands[addonName].Add(command);

                // Register the command..
                Commands.ChatCommands.Add(new Command(permission, HandleAddonCallbackCommand, command));
            }

            return true;
        }

        /// <summary>
        /// Removes an addons commands.
        /// </summary>
        /// <param name="addonName"></param>
        public void RemoveAddonCommands(string addonName)
        {
            // Ensure we have any entries for the given addon..
            if (this.m_AddonCommands[addonName.ToLower()] == null)
                return;

            // Remove each command from the addon..
            foreach (var cmd in this.m_AddonCommands[addonName.ToLower()].Select(command => Commands.ChatCommands.SingleOrDefault(c => c.HasAlias(command))).Where(cmd => cmd != null))
                Commands.ChatCommands.Remove(cmd);

            // Remove the addon entry..
            List<string> values;
            this.m_AddonCommands.TryRemove(addonName.ToLower(), out values);
        }

        /// <summary>
        /// Handles addon command callbacks to prevent crashes if the addon is unloaded.
        /// </summary>
        /// <param name="args"></param>
        public void HandleAddonCallbackCommand(CommandArgs args)
        {
            lock (this.m_AddonsLock)
            {
                // Obtain the command from the arguments..
                var command = args.Message.Split(' ')[0].ToLower();

                // Find the addon with this command..
                var addonEntry = this.m_AddonCommands.SingleOrDefault(c => c.Value.Contains(command));
                if (addonEntry.Key == null)
                    return;

                // Obtain the addon to invoke this command inside of..
                var addon = this.m_Addons.SingleOrDefault(a => string.Compare(a.Key, addonEntry.Key, StringComparison.InvariantCultureIgnoreCase) == 0);
                if (addon.Key == null)
                {
                    // If no addon was found, remove this command..
                    var cmd = Commands.ChatCommands.SingleOrDefault(c => c.HasAlias(command));
                    if (cmd != null)
                        Commands.ChatCommands.Remove(cmd);
                    this.m_AddonCommands[addonEntry.Key].Remove(command);
                    return;
                }

                // Invoke this command..
                addon.Value.InvokeCommand(command, args);
            }
        }

        /// <summary>
        /// Gets the author of this plugin.
        /// </summary>
        public override string Author
        {
            get { return "atom0s"; }
        }

        /// <summary>
        /// Gets the description of this plugin.
        /// </summary>
        public override string Description
        {
            get { return "Extends TShock to use Lua based addons."; }
        }

        /// <summary>
        /// Gets the name of this plugin.
        /// </summary>
        public override string Name
        {
            get { return "Addons"; }
        }

        /// <summary>
        /// Gets the version of this plugin.
        /// </summary>
        public override Version Version
        {
            get { return Assembly.GetExecutingAssembly().GetName().Version; }
        }
    }
}
