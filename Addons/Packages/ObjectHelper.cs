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
    using System;
    using System.Collections.Generic;
    using System.Reflection;
    using System.Runtime.ExceptionServices;
    using System.Security;
    using TShockAPI;

    public class ObjectHelper
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
        /// Binding flags to lookup objects with.
        /// </summary>
        private const BindingFlags BindFlags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

        /// <summary>
        /// Internal instance of the current addons state.
        /// </summary>
        private NLua.Lua m_Lua;

        /// <summary>
        /// Default Constructor
        /// </summary>
        public ObjectHelper()
        {
            this.m_FunctionDelegates = new Dictionary<string, LuaNativeFunction>
                {
                    { "get_field", this.lua_getfield },
                    { "set_field", this.lua_setfield }
                };
        }

        /// <summary>
        /// Registers this package to the given Lua state.
        /// </summary>
        /// <param name="state"></param>
        public void RegisterPackage(NLua.Lua state)
        {
            this.m_Lua = state;

            NLua.LuaLib.LuaNewTable(state.GetState());
            this.m_FunctionDelegates.ForEach(f => state.RegisterTableFunction(f.Key, f.Value));
            NLua.LuaLib.LuaSetGlobal(state.GetState(), "objhelper");
        }

        /// <summary>
        /// Attempts to obtain a fields value from an object.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        public int lua_getfield(LuaState state)
        {
            if (NLua.LuaLib.LuaGetTop(state) != 2)
            {
                NLua.LuaLib.LuaLError(state, "Invalid arguments for 'get_field'.");
                return 0;
            }

            var n = this.m_Lua.Pop(); // name of field
            var o = this.m_Lua.Pop(); // object

            if (!(n is string) || (o == null))
            {
                NLua.LuaLib.LuaLError(state, "Invalid arguments for 'get_field'.");
                return 0;
            }

            var f = o.GetType().GetField((string)n, ObjectHelper.BindFlags);
            if (f == null)
            {
                NLua.LuaLib.LuaPushNil(state);
                return 1;
            }

            this.m_Lua.Push(f.GetValue(o));
            return 1;
        }

        /// <summary>
        /// Attempts to set a fields value.
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        [HandleProcessCorruptedStateExceptions]
        [SecurityCritical]
        [System.Runtime.InteropServices.AllowReversePInvokeCalls]
        public int lua_setfield(LuaState state)
        {
            if (NLua.LuaLib.LuaGetTop(state) != 3)
            {
                NLua.LuaLib.LuaLError(state, "Invalid arguments for 'set_field'.");
                return 0;
            }

            var v = this.m_Lua.Pop(); // value
            var n = this.m_Lua.Pop(); // name of field
            var o = this.m_Lua.Pop(); // object

            if (!(n is string) || (o == null))
            {
                NLua.LuaLib.LuaLError(state, "Invalid arguments for 'set_field'.");
                return 0;
            }

            var f = o.GetType().GetField((string)n, ObjectHelper.BindFlags);
            if (f == null)
            {
                NLua.LuaLib.LuaPushBoolean(state, false);
                return 1;
            }

            try
            {
                f.SetValue(o, Convert.ChangeType(v, f.FieldType));
                NLua.LuaLib.LuaPushBoolean(state, true);
                return 1;
            }
            catch
            {
                NLua.LuaLib.LuaPushBoolean(state, false);
                return 1;
            }
        }
    }
}
