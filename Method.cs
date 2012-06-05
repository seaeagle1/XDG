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
    class Method
    {
        public class Parameter
        {
            public string Name;
            public string Type;
            public string Doc;
        }

        public string Name { get; set; }
        public string NameWithTypes { get; set; }
        public string Summary { get; set; }
        public string Remarks { get; set; }
        public string Access { get; set; }
        public string ReturnType { get; set; }
        public string ReturnDocs { get; set; }
        public List<Parameter> Parameters { get; set; }

        public Method(MethodDefinition method)
        {
            Name = method.Name;
            NameWithTypes = BuildNameWithTypes(method);
            if (method.IsPublic)
                Access = "public";
            else if (method.IsFamily)
                Access = "protected";

            if(method.ReturnType.ReturnType.FullName != "System.Void")
                ReturnType = method.ReturnType.ReturnType.ToXdgUrl();

            XmlNode doc = Xdg.FindXmlDoc(method);
            if (doc != null)
            {
                Summary = doc.GetElementContent("summary");
                Remarks = doc.GetElementContent("remarks");
                ReturnDocs = doc.GetElementContent("returns");
            }

            Parameters = new List<Parameter>();
            foreach (ParameterDefinition p in method.Parameters)
            {
                Parameter param = new Parameter();
                param.Name = p.Name;
                param.Type = p.ParameterType.ToXdgUrl();
                
                if(doc != null)
                    param.Doc = doc.GetElementContent("param[@name=\""+p.Name+"\"]");

                Parameters.Add(param);
            }

        }

        string BuildNameWithTypes(MethodDefinition method)
        {
            StringBuilder sb = new StringBuilder();

            sb.Append("<strong>");
            sb.Append(Name);
            sb.Append("</strong>(");

            string comma = "";
            foreach (ParameterDefinition p in method.Parameters)
            {
                sb.Append(comma);
                sb.Append(Type.GetName(p.ParameterType));
                sb.Append("&nbsp;");
                sb.Append(p.Name);

                comma = ", ";
            }

            sb.Append(")");

            return sb.ToString();
        }
    }
}
