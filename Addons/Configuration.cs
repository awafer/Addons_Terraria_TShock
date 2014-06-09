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
    using Newtonsoft.Json;
    using System.ComponentModel;
    using System.IO;

    public class Configuration
    {
        /// <summary>
        /// AutoloadAddons
        /// 
        /// If set to true, addons found within the Addons folder will automatically be loaded
        /// when the plugin is first loaded.
        /// </summary>
        [Description("Determines if addons should auto-load when the plugin is first loaded.")]
        public bool AutoloadAddons = true;

        /// <summary>
        /// AutoReloadAddons
        /// 
        /// If set to true, addons will attempt to auto-reload when they are set to an error state.
        /// </summary>
        [Description("Determines if addons should attempt to automatically reload if they are in an error state.")]
        public bool AutoReloadAddons = false;

        /// <summary>
        /// Attempts to parse the configuration file.
        /// </summary>
        /// <param name="file"></param>
        /// <returns></returns>
        public static Configuration Parse(string file)
        {
            // Ensure the config file exists..
            if (!System.IO.File.Exists(file))
            {
                var config = new Configuration();
                Configuration.Save(config, file);
                return config;
            }

            try
            {
                using (var sReader = new StreamReader(file))
                {
                    return JsonConvert.DeserializeObject<Configuration>(sReader.ReadToEnd());
                }
            }
            catch
            {
                return new Configuration();
            }
        }

        /// <summary>
        /// Saves the configuration file.
        /// </summary>
        public static void Save(Configuration config, string file)
        {
            try
            {
                var cfg = JsonConvert.SerializeObject(config, Formatting.Indented);
                using (var sWriter = new StreamWriter(file))
                {
                    sWriter.Write(cfg);
                }
            }
            catch
            {
            }
        }
    }
}
