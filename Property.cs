/* 
// Xml Documentation Generator (XDG)
// Copyright (C) 2012, Ilmar Kruis
//
// This Source Code Form is subject to the terms of the Mozilla Public License, v. 2.0. 
// If a copy of the MPL was not distributed with this file, You can obtain one 
// at http://mozilla.org/MPL/2.0/.
//
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using Mono.Cecil;

namespace XDG
{
    class Property
    {
        public string Name { get; set; }
        public string Type { get; set; }
        public string GetAccess { get; set; }
        public string SetAccess { get; set; }
        public string Summary { get; set; }

        public Property(PropertyDefinition def)
        {
            Name = def.Name;
            Type = def.PropertyType.ToXdgUrl();
 
            XmlNode doc = Xdg.FindXmlDoc(def);
            if (doc != null)
            {
                Summary = doc.GetElementContent("summary");
            }
        }
    }
}
