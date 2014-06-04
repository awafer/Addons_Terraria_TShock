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
    using System.Runtime.InteropServices;

    public static class ByteArrayExtensions
    {
        /// <summary>
        /// Converts a data array to a raw string for Lua usage.
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        public static string GetRawString(this byte[] data)
        {
            // Copy data to a memory buffer..
            var alloc = Marshal.AllocHGlobal(data.Length);
            Marshal.Copy(data, 0, alloc, data.Length);

            // Convert the data to a raw string..
            var raw = Marshal.PtrToStringAnsi(alloc, data.Length);
            Marshal.FreeHGlobal(alloc);
            return raw;
        }
    }
}
