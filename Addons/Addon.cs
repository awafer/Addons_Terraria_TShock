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
    using KeraLua;
    using System;
    using System.IO;
    using System.Reflection;
    using NLua;
    using Terraria;
    using TShockAPI;

    public class Addon
    {
        /// <summary>
        /// _addon table __newindex callback to prevent invalid data for required keys.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        private int lua_AddonNewIndex(LuaState state)
        {
            // Ensure we have a string Key..
            if (NLua.LuaLib.LuaType(state, -2) != NLua.LuaTypes.String)
            {
                NLua.LuaLib.LuaRawSet(state, 1);
                return 0;
            }

            // Obtain the key..
            var key = NLua.LuaLib.LuaToString(state, -2);
            if (string.Compare(key, "author", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(key, "name", StringComparison.InvariantCultureIgnoreCase) == 0 ||
                string.Compare(key, "version", StringComparison.InvariantCultureIgnoreCase) == 0)
            {
                // Ensure the value is valid..
                if (NLua.LuaLib.LuaType(state, -1) != NLua.LuaTypes.String)
                {
                    NLua.LuaLib.LuaPushString(state, string.Format("Invalid value for _addon.{0}! Expected a string.", key));
                    NLua.LuaLib.LuaError(state);
                    return 0;
                }
            }

            NLua.LuaLib.LuaRawSet(state, 1);
            return 0;
        }

        /// <summary>
        /// _addon event registration to ensure our event table exists.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        private int lua_AddonNewEvent(LuaState state)
        {
            // Validate we have two arguments..
            var top = NLua.LuaLib.LuaGetTop(state);
            if (top != 2)
            {
                NLua.LuaLib.LuaPop(state, top);
                NLua.LuaLib.LuaLError(state, "Invalid arguments for events.register_event!");
                return 0;
            }

            // Validate we have the proper types..
            var type1 = NLua.LuaLib.LuaType(state, -1);
            var type2 = NLua.LuaLib.LuaType(state, -2);
            if (type1 != NLua.LuaTypes.Function || type2 != NLua.LuaTypes.String)
            {
                NLua.LuaLib.LuaPop(state, top);
                NLua.LuaLib.LuaLError(state, "Invalid arguments for events.register_event!");
                return 0;
            }

            // Ensure the event table exists..
            NLua.LuaLib.LuaGetGlobal(state, "__addon_events");
            if (NLua.LuaLib.LuaType(state, -1) != NLua.LuaTypes.Table)
            {
                // Create the missing events table..
                NLua.LuaLib.LuaPop(state, 1);
                NLua.LuaLib.LuaNewTable(state);
                NLua.LuaLib.LuaSetGlobal(state, "__addon_events");
                NLua.LuaLib.LuaGetGlobal(state, "__addon_events");
            }

            // Insert the data into the events table..
            NLua.LuaLib.LuaInsert(state, -3);
            NLua.LuaLib.LuaRawSet(state, 1);
            NLua.LuaLib.LuaPop(state, 1);

            return 0;
        }

        /// <summary>
        /// Registers a command callback for this addon.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        private int lua_AddonNewCommand(LuaState state)
        {
            // Validate we have two arguments..
            var top = NLua.LuaLib.LuaGetTop(state);
            if (top != 3)
            {
                NLua.LuaLib.LuaPop(state, top);
                NLua.LuaLib.LuaLError(state, "Invalid arguments for commands.register_command!");
                return 0;
            }

            // Validate the argument types..
            if (NLua.LuaLib.LuaType(state, -3) != LuaTypes.String ||
                NLua.LuaLib.LuaType(state, -2) != LuaTypes.String ||
                NLua.LuaLib.LuaType(state, -1) != LuaTypes.Function)
            {
                NLua.LuaLib.LuaPop(state, top);
                NLua.LuaLib.LuaLError(state, "Invalid arguments for commands.register_command!");
                return 0;
            }

            // Obtain the arguments..
            var name = NLua.LuaLib.LuaToString(state, -3);
            var permissions = NLua.LuaLib.LuaToString(state, -2);

            // Ensure the command table exists..
            NLua.LuaLib.LuaGetGlobal(state, "__addon_commands");
            if (NLua.LuaLib.LuaType(state, -1) != NLua.LuaTypes.Table)
            {
                // Create the missing command table..
                NLua.LuaLib.LuaPop(state, 1);
                NLua.LuaLib.LuaNewTable(state);
                NLua.LuaLib.LuaSetGlobal(state, "__addon_commands");
                NLua.LuaLib.LuaGetGlobal(state, "__addon_commands");
            }
            
            // Register this command to our plugin..
            if (!this.Addons.RegisterCommand(this.FileName.ToLower(), permissions, name))
            {
                NLua.LuaLib.LuaPop(state, top + 1);
                NLua.LuaLib.LuaLError(state, string.Format("Failed to register command '{0}'!", name));
                return 0;
            }

            // Remove the permission from stack..
            NLua.LuaLib.LuaRemove(state, -3);

            // Insert the data into the command table..
            NLua.LuaLib.LuaInsert(state, -3);
            NLua.LuaLib.LuaRawSet(state, 1);
            NLua.LuaLib.LuaPop(state, 1);

            return 0;
        }

        /// <summary>
        /// Lua print override.
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        private int lua_Print(params object[] args)
        {
            var oldColor = Console.ForegroundColor;
            Console.ForegroundColor = ConsoleColor.Green;
            if (args != null)
                args.ForEach(a => Console.WriteLine("[Addon] ({0}) {1}", this.FileName, a.ToString()));
            else
                Console.WriteLine("[Addon] nil");
            Console.ForegroundColor = oldColor;
            return 0;
        }

        /// <summary>
        /// Managed object dumper.
        /// </summary>
        /// <param name="o"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        private ObjectInformation lua_ObjectDump(object o)
        {
            return new ObjectInformation(o);
        }

        /// <summary>
        /// Initializes this addon instance.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="game"></param>
        /// <returns></returns>
        public bool Initialize(string name, Addons addons, Main game)
        {
            // Cleanup previous state..
            if (this.LuaState != null)
                this.Release();

            this.State = AddonState.Loading;
            this.Addons = addons;
            this.Game = game;
            this.FileName = name;

            try
            {
                // Create the new Lua state..
                this.LuaState = new NLua.Lua();
                this.LuaState.LoadCLRPackage();
                this.LuaState.RegisterFunction("print", this, typeof(Addon).GetMethod("lua_Print", BindingFlags.CreateInstance | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic));
                this.LuaState.RegisterFunction("dump", this, typeof(Addon).GetMethod("lua_ObjectDump", BindingFlags.CreateInstance | BindingFlags.IgnoreCase | BindingFlags.Instance | BindingFlags.NonPublic));
                
                // Register internal packages..
                this.FilePackage = new Packages.File();
                this.FilePackage.RegisterPackage(this.LuaState);

                //
                // Object Bindings
                //

                {
                    // Terraria Bindings
                    this.LuaState.NewTable("Terraria");
                    this.LuaState["Terraria.Main"] = game;

                    // Create TShock tables..
                    this.LuaState.NewTable("TSPlayer");
                    this.LuaState["TSPlayer.All"] = TSPlayer.All;
                    this.LuaState["TSPlayer.Server"] = TSPlayer.Server;

                    this.LuaState.NewTable("TShock");
                    this.LuaState["TShock.CharacterDB"] = TShock.CharacterDB;
                    this.LuaState["TShock.Config"] = TShock.Config;
                    this.LuaState["TShock.DB"] = TShock.DB;
                    this.LuaState["TShock.Geo"] = TShock.Geo;
                    this.LuaState["TShock.Groups"] = TShock.Groups;
                    this.LuaState["TShock.Itembans"] = TShock.Itembans;
                    this.LuaState["TShock.Players"] = TShock.Players;
                    this.LuaState["TShock.ProjectileBans"] = TShock.ProjectileBans;
                    this.LuaState["TShock.Regions"] = TShock.Regions;
                    this.LuaState["TShock.RememberedPos"] = TShock.RememberedPos;
                    this.LuaState["TShock.Users"] = TShock.Users;
                    this.LuaState["TShock.Utils"] = TShock.Utils;
                    this.LuaState["TShock.Warps"] = TShock.Warps;

                    // Build NetMessage table..
                    this.LuaState.NewTable("NetMessage");
                    this.LuaState.RegisterFunction("NetMessage.SendData", typeof(NetMessage).GetMethod("SendData", BindingFlags.Static | BindingFlags.Public));
                }

                // Build needed paths..
                var basePath = AppDomain.CurrentDomain.BaseDirectory;
                var addonPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Addons", name);
                var packagePath = string.Format(";{0}\\addons\\libs\\?.lua;{0}\\addons\\{1}\\?.lua;{0}\\addons\\{1}\\{1}.lua;{0}\\addons\\{1}\\;", basePath, name) + this.LuaState["package.path"];
                var packageCPath = string.Format(";{0}\\scripts\\addons\\libs\\?.dll;{0}\\scripts\\addons\\{1}\\?.dll;", basePath, name) + this.LuaState["package.cpath"];

                // Set base directory global..
                NLua.LuaLib.LuaPushString(this.LuaState.GetState(), basePath);
                NLua.LuaLib.LuaSetGlobal(this.LuaState.GetState(), "base_dir");

                // Build the new _addon table..
                NLua.LuaLib.LuaNewTable(this.LuaState.GetState());
                NLua.LuaLib.LuaNewTable(this.LuaState.GetState());
                NLua.LuaLib.LuaPushString(this.LuaState.GetState(), "__newindex");
                NLua.LuaLib.LuaPushStdCallCFunction(this.LuaState.GetState(), lua_AddonNewIndex);
                NLua.LuaLib.LuaRawSet(this.LuaState.GetState(), -3);
                NLua.LuaLib.LuaSetMetatable(this.LuaState.GetState(), -2);
                NLua.LuaLib.LuaPushString(this.LuaState.GetState(), "path");
                NLua.LuaLib.LuaPushString(this.LuaState.GetState(), addonPath);
                NLua.LuaLib.LuaRawSet(this.LuaState.GetState(), -3);
                NLua.LuaLib.LuaSetGlobal(this.LuaState.GetState(), "_addon");

                // Build the new events table..
                NLua.LuaLib.LuaNewTable(this.LuaState.GetState());
                NLua.LuaLib.LuaPushString(this.LuaState.GetState(), "register_event");
                NLua.LuaLib.LuaPushStdCallCFunction(this.LuaState.GetState(), lua_AddonNewEvent);
                NLua.LuaLib.LuaRawSet(this.LuaState.GetState(), -3);
                NLua.LuaLib.LuaSetGlobal(this.LuaState.GetState(), "events");

                // Build the commands table..
                NLua.LuaLib.LuaNewTable(this.LuaState.GetState());
                NLua.LuaLib.LuaPushString(this.LuaState.GetState(), "register_command");
                NLua.LuaLib.LuaPushStdCallCFunction(this.LuaState.GetState(), lua_AddonNewCommand);
                NLua.LuaLib.LuaRawSet(this.LuaState.GetState(), -3);
                NLua.LuaLib.LuaSetGlobal(this.LuaState.GetState(), "commands");

                // Set the package.path and package.cpath..
                this.LuaState["package.path"] = packagePath;
                this.LuaState["package.cpath"] = packageCPath;

                // Build path to this addon..
                var f = this.LuaState.LoadFile(Path.Combine(addonPath, name + ".lua"));
                if (f == null)
                {
                    this.Release();
                    this.State = AddonState.Error;
                    return false;
                }

                // Call the script to load it..
                f.Call();

                // Validate the addon author..
                var addonAuthor = this.LuaState["_addon.author"];
                if (addonAuthor.GetType() != typeof(string))
                    throw new Exception("_addon.author has invalid data type. Expected string.");
                this.Author = (string)addonAuthor;

                // Validate the addon name..
                var addonName = this.LuaState["_addon.name"];
                if (addonName.GetType() != typeof(string))
                    throw new Exception("_addon.name has invalid data type. Expected string.");
                this.Name = (string)addonName;

                // Validate the addon version..
                var addonVersion = this.LuaState["_addon.version"];
                if (addonVersion.GetType() != typeof(string))
                    throw new Exception("_addon.version has invalid data type. Expected string.");
                this.Version = (string)addonVersion;

                this.State = AddonState.Ok;
                return true;
            }
            catch (Exception ex)
            {
                Log.ConsoleError(ex.ToString());
                this.Release();
                this.State = AddonState.Error;
                return false;
            }
        }

        /// <summary>
        /// Cleans up this addon instance.
        /// </summary>
        public void Release()
        {
            // Cleanup the Lua state..
            if (this.LuaState != null)
                this.LuaState.Dispose();
            this.LuaState = null;

            // Set this addon to release state..
            this.State = AddonState.Released;

            // Clear the addon variables..
            this.Name = string.Empty;
            this.Author = string.Empty;
            this.Version = string.Empty;
        }

        /// <summary>
        /// Reloads this addon.
        /// </summary>
        /// <returns></returns>
        public bool Reload()
        {
            this.State = AddonState.Reloading;
            this.Release();
            return this.Initialize(this.FileName, this.Addons, this.Game);
        }

        /// <summary>
        /// Invokes the given event inside this addon.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        public object[] InvokeEvent(string name, params object[] args)
        {
            // Ensure the addon state is valid..
            if (this.State != AddonState.Ok || this.LuaState == null)
                return null;

            try
            {
                // Ensure the global __addon_events table exists..
                if (this.LuaState["__addon_events"] == null)
                    return null;

                // Attempt to locate the event function..
                var func = this.LuaState.GetFunction(string.Format("__addon_events.{0}", name));
                if (func == null)
                    return null;

                // Attempt to call the function..
                return (args.Length > 0) ? func.Call(args) : func.Call();
            }
            catch (Exception ex)
            {
                Log.ConsoleError("[Addon] Addon '{0}' failed to invoke event: {1}.\r\n{2}", this.Name, name, ex.ToString());

                // Set this addon to an error state since we have thrown an exception..
                this.State = AddonState.Error;
                return null;
            }
        }

        /// <summary>
        /// Invokes the given command inside this addon.
        /// </summary>
        /// <param name="name"></param>
        /// <param name="args"></param>
        public void InvokeCommand(string name, CommandArgs args)
        {
            // Ensure the addon state is valid..
            if (this.State != AddonState.Ok || this.LuaState == null)
                return;

            try
            {
                // Ensure the global __addon_events table exists..
                if (this.LuaState["__addon_commands"] == null)
                    return;

                // Attempt to locate the event function..
                var func = this.LuaState.GetFunction(string.Format("__addon_commands.{0}", name));
                if (func == null)
                    return;

                // Attempt to call the function..
                func.Call(args);
            }
            catch (Exception ex)
            {
                Log.ConsoleError("[Addon] Addon '{0}' failed to invoke command: {1}.\r\n{2}", this.Name, name, ex.ToString());

                // Set this addon to an error state since we have thrown an exception..
                this.State = AddonState.Error;
            }
        }

        /// <summary>
        /// Gets or sets this addons Lua state.
        /// </summary>
        public NLua.Lua LuaState { get; set; }

        /// <summary>
        /// Gets or sets this addons state.
        /// </summary>
        public AddonState State { get; set; }
        
        /// <summary>
        /// Gets or sets the this addons file name.
        /// </summary>
        public string FileName { get; set; }

        /// <summary>
        /// Gets or sets this addons name.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets this addons author.
        /// </summary>
        public string Author { get; set; }

        /// <summary>
        /// Gets or sets this addons version.
        /// </summary>
        public string Version { get; set; }

        /// <summary>
        /// Internal file package object.
        /// </summary>
        private Packages.File FilePackage { get; set; }

        /// <summary>
        /// Gets or sets the main plugin object.
        /// </summary>
        public Addons Addons { get; set; }

        /// <summary>
        /// Gets or sets this addons game object.
        /// </summary>
        public Main Game { get; set; }
    }
}
