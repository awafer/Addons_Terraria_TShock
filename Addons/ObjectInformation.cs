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
    using System.Collections.Generic;

    public class ObjectInformation
    {
        private readonly List<string> m_Events = new List<string>();
        private readonly List<string> m_Fields = new List<string>();
        private readonly List<string> m_Members = new List<string>();
        private readonly List<string> m_Methods = new List<string>();
        private readonly List<string> m_Properties = new List<string>();

        /// <summary>
        /// Default Constructor
        /// </summary>
        /// <param name="o"></param>
        public ObjectInformation(object o)
        {
            var t = o.GetType();

            this.Type = t.FullName;

            foreach (var e in t.GetEvents())
                this.m_Events.Add(e.Name);
            foreach (var f in t.GetFields())
                this.m_Fields.Add(f.Name);
            foreach (var m in t.GetMembers())
                this.m_Members.Add(m.Name);
            foreach (var m in t.GetMethods())
                this.m_Members.Add(m.Name);
            foreach (var p in t.GetProperties())
                this.m_Members.Add(p.Name);
        }

        /// <summary>
        /// Gets or sets this object type.
        /// </summary>
        public string Type { get; set; }

        /// <summary>
        /// Gets this objects events.
        /// </summary>
        public IEnumerable<string> Events
        {
            get { return this.m_Events; }
        }

        /// <summary>
        /// Gets this objects fields.
        /// </summary>
        public IEnumerable<string> Fields
        {
            get { return this.m_Fields; }
        }

        /// <summary>
        /// Gets this objects methods.
        /// </summary>
        public IEnumerable<string> Members
        {
            get { return this.m_Members; }
        }

        /// <summary>
        /// Gets this objects methods.
        /// </summary>
        public IEnumerable<string> Methods
        {
            get { return this.m_Methods; }
        }

        /// <summary>
        /// Gets this objects properties.
        /// </summary>
        public IEnumerable<string> Properties
        {
            get { return this.m_Properties; }
        }
    }
}