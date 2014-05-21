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

namespace Addons.Extensions
{
    using KeraLua;
    using NLua;

    public static class NLuaExtensions
    {
        /// <summary>
        /// Registers the given function to the current table on the stack top.
        /// </summary>
        /// <param name="state"></param>
        /// <param name="name"></param>
        /// <param name="func"></param>
        public static void RegisterTableFunction(this NLua.Lua state, string name, LuaNativeFunction func)
        {
            // Do not continue if we are not a table..
            var isTable = NLua.LuaLib.LuaType(state.GetState(), -1) == LuaTypes.Table;
            if (!isTable)
                return;

            // Push the name, function, and set the table..
            NLua.LuaLib.LuaPushString(state.GetState(), name);
            NLua.LuaLib.LuaPushStdCallCFunction(state.GetState(), func);
            NLua.LuaLib.LuaRawSet(state.GetState(), -3);
        }
    }
}
