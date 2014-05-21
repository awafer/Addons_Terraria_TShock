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
    public enum AddonState
    {
        /// <summary>
        /// Addon is in an erroneous state and will not execute any further events.
        /// </summary>
        Error,

        /// <summary>
        /// Addon is loaded and ready.
        /// </summary>
        Ok,

        /// <summary>
        /// Addon is currently loading.
        /// </summary>
        Loading,

        /// <summary>
        /// Addon is currently released.
        /// </summary>
        Released,

        /// <summary>
        /// Addon is currently being reloaded.
        /// </summary>
        Reloading
    }
}
