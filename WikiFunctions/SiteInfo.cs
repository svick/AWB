/*
Copyright (C) 2009 Max Semenik

This program is free software; you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation; either version 2 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 51 Franklin St, Fifth Floor, Boston, MA  02110-1301  USA
*/

using System;
using System.Collections.Generic;
using System.Xml;
using System.Xml.Serialization;
using WikiFunctions.API;

namespace WikiFunctions
{
    /// <summary>
    /// This class holds all basic information about a wiki
    /// </summary>
    [Serializable]
    public class SiteInfo : IXmlSerializable
    {
        private readonly IApiEdit Editor;

        private string scriptPath;
        private readonly Dictionary<int, string> namespaces = new Dictionary<int, string>();
        private Dictionary<int, List<string>> namespaceAliases = new Dictionary<int, List<string>>();
        //private Dictionary<string, string> messageCache = new Dictionary<string, string>();
        private readonly Dictionary<string, List<string>> magicWords = new Dictionary<string, List<string>>();

        internal SiteInfo()
        { }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="scriptPath"></param>
        /// <param name="namespaces"></param>
        public SiteInfo(string scriptPath, Dictionary<int, string> namespaces)
        {
            ScriptPath = scriptPath;
            this.namespaces = namespaces;
        }

        /// <summary>
        /// Creates an instance of the class
        /// </summary>
        public SiteInfo(IApiEdit editor)
        {
            Editor = editor;
            ScriptPath = editor.URL;

            try
            {
                if (!LoadSiteInfo())
                    throw new WikiUrlException();
            }
            catch (WikiUrlException)
            {
                throw;
            }
            catch (Exception ex)
            {
                throw new WikiUrlException(ex);
            }
        }

        private static string Key(string scriptPath)
        {
            return "SiteInfo[" + scriptPath + "]@";
        }

        public static SiteInfo CreateOrLoad(IApiEdit editor)
        {
            SiteInfo si = (SiteInfo)ObjectCache.Global.Get<SiteInfo>(Key(editor.URL));
            if (si != null) return si;

            si = new SiteInfo(editor);
            ObjectCache.Global[Key(editor.URL)] = si;

            return si;
        }

        private string ApiPath
        {
            get { return scriptPath + "api.php" + ((Editor.PHP5) ? "5" : ""); }
        }

        public static string NormalizeURL(string url)
        {
            return (!url.EndsWith("/")) ? url + "/" : url;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <returns></returns>
        public bool LoadSiteInfo()
        {
            string output = Editor.HttpGet(ApiPath + "?action=query&meta=siteinfo&siprop=general|namespaces|namespacealiases|statistics|magicwords&format=xml");

            XmlDocument xd = new XmlDocument();
            xd.LoadXml(output);

            var general = xd["general"];
            if (general == null) return false;

            Language = general.Attributes["lang"].Value;

            if (xd["api"] == null || xd["api"]["query"] == null
                || xd["api"]["query"]["namespaces"] == null || xd["api"]["query"]["namespacealiases"] == null)
                return false;

            foreach (XmlNode xn in xd["api"]["query"]["namespaces"].GetElementsByTagName("ns"))
            {
                int id = int.Parse(xn.Attributes["id"].Value);

                if (id != 0) namespaces[id] = xn.InnerText + ":";
            }

            namespaceAliases = Variables.PrepareAliases(namespaces);

            foreach (XmlNode xn in xd["api"]["query"]["namespacealiases"].GetElementsByTagName("ns"))
            {
                int id = int.Parse(xn.Attributes["id"].Value);

                if (id != 0) namespaceAliases[id].Add(xn.InnerText);
            }

            if (xd["api"]["query"]["magicwords"] == null)
                return false;

            foreach (XmlNode xn in xd["api"]["query"]["magicwords"].GetElementsByTagName("magicword"))
            {
                List<string> alias = new List<string>();

                foreach (XmlNode xin in xn["aliases"].GetElementsByTagName("alias"))
                {
                    alias.Add(xin.InnerText);
                }

                magicWords.Add(xn.Attributes["name"].Value, alias);
            }

            return true;
        }

        //[XmlAttribute(AttributeName = "url")]
        public string ScriptPath
        {
            get { return scriptPath; }
            set //Must stay public otherwise Serialiser for ObjectCache isn't happy =(
            {
                scriptPath = NormalizeURL(value);
            }
        }

        public Dictionary<int, string> Namespaces
        { get { return namespaces; } }

        public Dictionary<int, List<string>> NamespaceAliases
        { get { return namespaceAliases; } }

        public Dictionary<string, List<string>> MagicWords
        { get { return magicWords; } }

        public string Language
        { get; private set; }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="names"></param>
        /// <returns></returns>
        public Dictionary<string, string> GetMessages(params string[] names)
        {
            string output = Editor.HttpGet(ApiPath + "?format=xml&action=query&meta=allmessages&ammessages=" + string.Join("|", names));

            XmlDocument xd = new XmlDocument();
            xd.LoadXml(output);

            Dictionary<string, string> result = new Dictionary<string, string>(names.Length);

            foreach (XmlNode xn in xd.GetElementsByTagName("message"))
            {
                result[xn.Attributes["name"].Value] = xn.InnerText;
            }

            return result;
        }

        #region Service functions
        #endregion

        #region IXmlSerializable Members

        public System.Xml.Schema.XmlSchema GetSchema()
        {
            return null;
        }

        public void ReadXml(XmlReader reader)
        {
            throw new Exception("The method or operation is not implemented.");
        }

        public void WriteXml(XmlWriter writer)
        {
            //writer.WriteStartElement("site");
            writer.WriteAttributeString("url", scriptPath);
            writer.WriteAttributeString("php5", Editor.PHP5 ? "1" : "0");
            {
                writer.WriteStartElement("Namespaces");
                {
                    foreach (KeyValuePair<int, string> p in namespaces)
                    {
                        writer.WriteStartElement("Namespace");
                        writer.WriteAttributeString("id", p.Key.ToString());
                        writer.WriteValue(p.Value);
                        writer.WriteEndElement();
                    }
                }
            }
            //writer.WriteEndElement();
        }
        #endregion
    }

    public class WikiUrlException : Exception
    {
        public WikiUrlException()
            : base("Can't connect to given wiki site.")
        { }

        public WikiUrlException(Exception innerException)
            : base("Can't connect to given wiki site.", innerException)
        { }
    }
}
