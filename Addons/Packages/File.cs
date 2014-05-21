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

namespace Addons.Packages
{
    using Extensions;
    using KeraLua;
    using System.Collections.Generic;
    using TShockAPI;

    public class File
    {
        /// <summary>
        /// Internal package dictionary of function delegates.
        /// 
        /// Important!
        /// Because the .NET garbage collection is a data Nazi, we must keep a local
        /// reference of these delegates to ensure they are not cleaned up.
        /// </summary>
        private readonly Dictionary<string, LuaNativeFunction> m_FunctionDelegates;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public File()
        {
            this.m_FunctionDelegates = new Dictionary<string, LuaNativeFunction>
                {
                    { "create_dir", this.CreateDirectory }, 
                    { "dir_exists", this.DirectoryExists }, 
                    { "file_exists", this.FileExists }, 
                    { "get_dir", this.GetDirectoryFiles }, 
                    { "get_dirs", this.GetDirectoryDirs }
                };
        }

        /// <summary>
        /// Registers this package to the given Lua state.
        /// </summary>
        /// <param name="state"></param>
        public void RegisterPackage(NLua.Lua state)
        {
            NLua.LuaLib.LuaNewTable(state.GetState());
            this.m_FunctionDelegates.ForEach(f => state.RegisterTableFunction(f.Key, f.Value));
            NLua.LuaLib.LuaSetGlobal(state.GetState(), "file");
        }

        /// <summary>
        /// Attempts to create a directory.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        public int CreateDirectory(LuaState state)
        {
            if (NLua.LuaLib.LuaGetTop(state) != 1)
            {
                NLua.LuaLib.LuaLError(state, "Invalid arguments for 'create_dir'.");
                return 0;
            }

            try
            {
                var dir = NLua.LuaLib.LuaToString(state, -1);
                NLua.LuaLib.LuaPushBoolean(state, System.IO.Directory.CreateDirectory(dir).Exists);
                return 1;
            }
            catch
            {
                NLua.LuaLib.LuaPushBoolean(state, false);
                return 1;
            }
        }

        /// <summary>
        /// Determines if a directory exists.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        public int DirectoryExists(LuaState state)
        {
            var dir = NLua.LuaLib.LuaToString(state, -1);
            NLua.LuaLib.LuaPushBoolean(state, System.IO.Directory.Exists(dir));
            return 1;
        }

        /// <summary>
        /// Determines if a file exists.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        public int FileExists(LuaState state)
        {
            var f = NLua.LuaLib.LuaToString(state, -1);
            NLua.LuaLib.LuaPushBoolean(state, System.IO.File.Exists(f));
            return 1;
        }

        /// <summary>
        /// Obtains a directories root folder files.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        public int GetDirectoryFiles(LuaState state)
        {
            var dir = NLua.LuaLib.LuaToString(state, -1);
            if (!System.IO.Directory.Exists(dir))
            {
                NLua.LuaLib.LuaPushNil(state);
                return 1;
            }

            NLua.LuaLib.LuaNewTable(state);
            var top = NLua.LuaLib.LuaGetTop(state);
            var index = 1;

            var files = System.IO.Directory.EnumerateFiles(dir, "*", System.IO.SearchOption.TopDirectoryOnly);
            foreach (var f in files)
            {
                NLua.LuaLib.LuaPushNumber(state, index);
                NLua.LuaLib.LuaPushString(state, f);
                NLua.LuaLib.LuaSetTable(state, top);
                index++;
            }

            return 1;
        }

        /// <summary>
        /// Obtains a directories root folder directories.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        public int GetDirectoryDirs(LuaState state)
        {
            var dir = NLua.LuaLib.LuaToString(state, -1);
            if (!System.IO.Directory.Exists(dir))
            {
                NLua.LuaLib.LuaPushNil(state);
                return 1;
            }

            NLua.LuaLib.LuaNewTable(state);
            var top = NLua.LuaLib.LuaGetTop(state);
            var index = 1;

            var dirs = System.IO.Directory.EnumerateDirectories(dir, "*", System.IO.SearchOption.TopDirectoryOnly);
            foreach (var d in dirs)
            {
                NLua.LuaLib.LuaPushNumber(state, index);
                NLua.LuaLib.LuaPushString(state, d);
                NLua.LuaLib.LuaSetTable(state, top);
                index++;
            }

            return 1;
        }
    }
}
