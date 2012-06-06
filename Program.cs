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
using System.IO;
using Mono.Cecil;
using System.Xml;

namespace XDG
{
    class MenuItem
    {
        public string Ns { get; set; }
        public List<string> Types { get; set; }
    }

    static class Xdg
    {
        static XmlDocIdLib.XmlDocIdGenerator xmlIdGen;
        static XmlDocument xmlDoc;

        static SortedDictionary<string,List<Type>> typeList;

        static Xdg()
        {
            typeList = new SortedDictionary<string,List<Type>>();
        }

        /// <summary>
        /// Main entry point, check commandline arguments and settings, load data and start parsing
        /// </summary>
        /// <param name="args"></param>
        static void Main(string[] args)
        {
            if (args.Length < 1 || !File.Exists(args[0]))
            {
                Console.WriteLine("File not found!");
                return;
            }

            string docs = Path.ChangeExtension(args[0], "xml");
            if (!File.Exists(docs))
            {
                Console.WriteLine("Unable to find XML documentation");
                return;
            }

            using(FileStream s = File.OpenRead(docs))
            {
                xmlDoc = new XmlDocument();
                xmlDoc.Load(s);
            }

            xmlIdGen = new XmlDocIdLib.XmlDocIdGenerator();
            AssemblyDefinition library = AssemblyDefinition.ReadAssembly(args[0]);

            ParseAssembly(library);
        }

        /// <summary>
        /// Run through provided assembly to parse all present types, members etc.
        /// </summary>
        /// <param name="lib"></param>
        static void ParseAssembly(AssemblyDefinition lib)
        {
            List<MenuItem> menu = new List<MenuItem>();

            foreach (TypeDefinition type in lib.MainModule.Types)
            {
                if (!type.IsPublic)
                    continue;

                Type t = new Type("Xdg Test Project", type);
                t.Menu = menu;

                if (t.Name.EndsWith("Collection")) 
                {
                    int i = menu.Count;
                }

                List<Type> ns_type;
                if (typeList.TryGetValue(t.Namespace, out ns_type))
                {
                    ns_type.Add(t);
                    
                    MenuItem mi = menu.Find((MenuItem i) => { return i.Ns == t.Namespace; });
                    mi.Types.Add(type.ToXdgUrl());
                }
                else
                {
                    typeList.Add(t.Namespace, new List<Type>(new Type[] { t }));

                    MenuItem mi = new MenuItem();
                    mi.Ns = t.Namespace;
                    mi.Types = new List<string>(new string[] { type.ToXdgUrl() });
                    menu.Add(mi);
                }
            }

            menu.Sort(new Comparison<MenuItem>((MenuItem a, MenuItem b) => { return a.Ns.CompareTo(b.Ns); }));
            foreach (MenuItem mi in menu)
                mi.Types.Sort();

            foreach (KeyValuePair<string, List<Type>> kvp in typeList)
            {
                foreach (Type t in kvp.Value)
                    t.Write();
            }
        }

        /// <summary>
        /// Find documentation entries in XML file for a certain reference
        /// </summary>
        /// <param name="mr"></param>
        /// <returns></returns>
        internal static XmlNode FindXmlDoc(MemberReference mr)
        {
            string id = xmlIdGen.GetXmlDocPath(mr);
            return xmlDoc.SelectSingleNode("/doc/members/member[@name='" + id + "']");
        }

        /// <summary>
        /// Translate typename to (local) URL
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static string TypeUrl(TypeReference type)
        {
            return "type.html?" + Type.GetName(type, false, true);
        }

        static string TypeUrl(string fullname)
        {
            return "type.html?" + fullname;
        }

        /// <summary>
        /// Get a (local/MSDN) link for this TypeReference
        /// </summary>
        /// <param name="r"></param>
        /// <returns></returns>
        public static string ToXdgUrl(this TypeReference r)
        {
            string url = "";

            if (r.FullName.StartsWith("System."))
            {
                if(!r.HasGenericParameters && !(r is GenericInstanceType))
                    url = "http://msdn.microsoft.com/en-us/library/"+r.FullName.ToLower()+".aspx";
            }
            else
            {
                url = TypeUrl(r);
            }

            string href = (string.IsNullOrEmpty(url) ? "" : "href=\"" + url + "\"");

            return string.Format("<a {0}>{1}</a>", href, Type.GetName(r));
        }

        /// <summary>
        /// Get contents of a sub-element
        /// </summary>
        /// <param name="node"></param>
        /// <param name="element"></param>
        /// <returns></returns>
        public static string GetElementContent(this XmlNode node, string element)
        {
            XmlNode n = node.SelectSingleNode(element);

            if (n == null)
                return null;

            // parse <see> tags
            XmlNodeList seeTags = n.SelectNodes("see");
            foreach (XmlNode see in seeTags)
            {
                XmlElement e = xmlDoc.CreateElement("a");
                e.InnerText = see.InnerText;
                
                XmlAttribute crefAttr = see.Attributes["cref"];
                if (crefAttr != null)
                {
                    string cref = crefAttr.Value;
                    cref = cref.Split(new char[]{'`'}).ElementAt(0);
                    XmlAttribute a = xmlDoc.CreateAttribute("href");
                    
                    string text = see.InnerText;
                    if (string.IsNullOrEmpty(text))
                        text = cref.Substring(2);
                    e.InnerText = text;

                    if (cref.StartsWith("T:")) // create Type link
                    {
                        if (cref.StartsWith("T:System."))
                        {
                            a.Value = "http://msdn.microsoft.com/en-us/library/" + cref.Substring(2) + ".aspx";
                            e.Attributes.Append(a);
                        }
                        else
                        {
                            a.Value = TypeUrl(cref.Substring(2));
                            e.Attributes.Append(a);
                        }
                    }
                }

                if (see.Attributes["href"] != null)
                    e.Attributes.Append(see.Attributes["href"]);

                n.ReplaceChild(e, see);
            }

            // parse <c> tags
            XmlNodeList cTags = n.SelectNodes("c");
            foreach (XmlNode c in cTags)
            {
                XmlElement strong = xmlDoc.CreateElement("strong");
                strong.InnerText = c.InnerText;

                n.ReplaceChild(strong, c);
            }

            return n.InnerXml;
        }

        /// <summary>
        /// Append StringBuilder with content enclosed in span tags with supplied style class
        /// </summary>
        /// <param name="b"></param>
        /// <param name="style"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static StringBuilder AppendStyled(this StringBuilder b, string style, string value)
        {
            b.Append("<span class=\"").Append(style).Append("\">").Append(value).Append("</span>");
            return b;
        }
    }
}
