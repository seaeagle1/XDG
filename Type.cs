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
using Mono.Cecil;
using System.Xml;
using System.Text;
using Newtonsoft.Json;
using System.IO;

namespace XDG
{
    class Type
    {
        public string Name { get; set; }
        public string LinkName { get; set; }
        public string Summary { get; set; }
        public string Remarks { get; set; }
        public string CSharpDecl { get; set; }
        public string Title { get; set; }
        public string Namespace { get; set; }
        public List<MenuItem> Menu { get; set; }
        public List<Method> Methods { get; private set; }
        public string Copyright { get; set; }

        // for debugging
        private TypeDefinition td;

        public Type(string moduleName, TypeDefinition type)
        {
            td = type;

            Name = GetName(type);
            LinkName = GetName(type, false, true);
            Title = moduleName + " Documentation - " + Name;
            Namespace = GetNamespace(type);
            Copyright = "This documentation was generated using <a href=\"https://github.com/seaeagle1/XDG\">XDG</a>.";

            CSharpDecl = CSharpTypeDecl(type);

            XmlNode doc = Xdg.FindXmlDoc(type);
            if (doc != null)
            {
                Summary = doc.GetElementContent("summary");
                Remarks = doc.GetElementContent("remarks");
            }

            Methods = new List<Method>();
            foreach (MethodDefinition m in type.Methods)
            {
                if ((!m.IsPublic && !m.IsFamily) || m.IsSetter || m.IsGetter)
                    continue;

                Methods.Add(new Method(m));
            }
            Methods.Sort( new Comparison<Method>((Method a, Method b) => {  return a.Name.CompareTo(b.Name); }));

        }

        public void Write()
        {
            using (StreamWriter sw = new StreamWriter("../../example/"+LinkName+".js"))
            using (JsonWriter writer = new JsonTextWriter(sw))
            {
                writer.Formatting = Newtonsoft.Json.Formatting.Indented;

                JsonSerializer serializer = new JsonSerializer();
                serializer.Serialize(writer, this);
            }
        }

        static string GetNamespace(TypeReference tr)
        {
            while (tr.IsNested)
                tr = tr.DeclaringType;
                
            return tr.Namespace;
        }

        public static string GetName(TypeReference tr, bool use_generics = true, bool use_namespace = false)
        {
            string[] strSlices = tr.Name.Split(new char[] { '`' });

            if (tr.HasGenericParameters && use_generics)
            {
                strSlices[0] += "&lt;";
                string comma = "";
                foreach (GenericParameter p in tr.GenericParameters)
                {
                    strSlices[0] += GetName(p) + comma;
                    comma = ",";
                }
                strSlices[0] += "&gt;";
            }

            if (tr is GenericInstanceType && use_generics)
            {
                strSlices[0] += "&lt;";
                string comma = "";
                foreach (TypeReference p in (tr as GenericInstanceType).GenericArguments)
                {
                    strSlices[0] += GetName(p) + comma;
                    comma = ",";
                }
                strSlices[0] += "&gt;";
            }

            string rv = "";
            if (use_namespace)
                rv += GetNamespace(tr) + ".";

            if (tr.IsNested)
                rv += GetName(tr.DeclaringType, use_generics) + "." + strSlices[0];
            else
                rv += strSlices[0];

            return rv;
        }

        static string CSharpTypeDecl(TypeDefinition type)
        {
            StringBuilder s = new StringBuilder();

            if (type.IsPublic)
                s.AppendStyled("keyword", "public ");

            if (type.IsEnum)
                s.AppendStyled("keyword", "enum ");
            else if (type.IsValueType)
                s.AppendStyled("keyword", "struct ");
            else if (type.IsClass)
                s.AppendStyled("keyword", "class ");

            s.Append(GetName(type));

            if ((!type.IsEnum && !type.IsInterface) &&
                (type.HasInterfaces || (type.BaseType.FullName != "System.Object" && type.BaseType.FullName != "System.ValueType")))
            {
                s.Append(" : ");

                if (type.BaseType.FullName != "System.Object" && type.BaseType.FullName != "System.ValueType")
                    s.Append(type.BaseType.ToXdgUrl() + ", ");

                foreach (TypeReference tr in type.Interfaces)
                    s.Append(tr.ToXdgUrl() + ", ");

                s.Remove(s.Length - 2, 1);
            }

            return s.ToString();
        }
    }
}
