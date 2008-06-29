﻿/*
Copyright (C) 2007 Martin Richards
(C) 2008 Sam Reed

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
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

using System.Text.RegularExpressions;
using System.Xml;
using System.IO;
using System.Web;

namespace WikiFunctions.Lists
{
    /// <summary>
    /// Gets the list of pages on the Named Special Pages
    /// </summary>
    public partial class SpecialPageListMakerProvider : Form, IListProvider
    {
        public SpecialPageListMakerProvider()
        {
            InitializeComponent();
        }

        public List<Article> MakeList(params string[] searchCriteria)
        {
            //searchCriteria = Tools.FirstToUpperAndRemoveHashOnArray(searchCriteria);
            txtSource.Text = "";
            foreach (string crit in searchCriteria)
            {
                txtSource.Text += crit + "|";
            }

            txtSource.Text = txtSource.Text.Substring(0, txtSource.Text.LastIndexOf('|'));

            List<Article> list = new List<Article>();

            if (this.ShowDialog() == DialogResult.OK)
            {
                searchCriteria = txtSource.Text.Split(new char[] { '|' });
                if (radAllPages.Checked)
                {
                    list = new AllPagesSpecialPageProvider().MakeList(Tools.CalculateNS(cboNamespaces.Text), searchCriteria);
                }
                else if (radPrefixIndex.Checked)
                {
                    list = new PrefixIndexSpecialPageProvider().MakeList(Tools.CalculateNS(cboNamespaces.Text), searchCriteria);
                }
            }
            
            return Tools.FilterSomeArticles(list);
        }

        public string DisplayText
        { get { return "Special page"; } }

        public string UserInputTextBoxText
        { get { return "Target page(s):"; } }

        public bool UserInputTextBoxEnabled
        { get { return true; } }

        public void Selected() { }

        public bool RunOnSeparateThread
        { get { return true; } }

        private void btnOk_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void SpecialPageListMakerProvider_Load(object sender, EventArgs e)
        {
            foreach (string name in Variables.Namespaces.Values)
            {
                cboNamespaces.Items.Add(name);
            }
        }
    }

    interface ISpecialPageProvider
    {
        List<Article> MakeList(int Namespace, params string[] searchCriteria);
    }

    public class AllPagesSpecialPageProvider : ISpecialPageProvider
    {
        protected string from = "apfrom";

        #region ISpecialPageProvider Members

        public virtual List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            List<Article> list = new List<Article>();

            foreach (string s in searchCriteria)
            {
                string url = Variables.URLLong + "api.php?action=query&list=allpages&" + from + "=" + s + "&apnamespace=" + Namespace + "&aplimit=500&format=xml";
                while (true)
                {
                    string html = Tools.GetHTML(url);

                    bool more = false;

                    using (XmlTextReader reader = new XmlTextReader(new StringReader(html)))
                    {
                        while (reader.Read())
                        {
                            if (reader.Name.Equals("p"))
                            {
                                if (reader.MoveToAttribute("title"))
                                {
                                    list.Add(new WikiFunctions.Article(reader.Value.ToString(), 0));
                                }
                            }
                            else if (reader.Name.Equals("allpages") && from != "apfrom") //dont want all pages loading EVERYTHING
                            {
                                reader.MoveToAttribute("apfrom");
                                if (reader.Value.Length > 0)
                                {
                                    string continueFrom = Tools.WikiEncode(reader.Value.ToString());
                                    url = Variables.URLLong + "api.php?action=query&list=allpages&" + from + "=" + s + "&apnamespace=" + Namespace + "&aplimit=50&format=xml&apfrom=" + continueFrom;
                                    more = true;
                                }
                            }
                        }
                    }

                    if (!more)
                        break;
                }
            }

            return list;
        }

        #endregion
    }

    public class PrefixIndexSpecialPageProvider : AllPagesSpecialPageProvider
    {
        public PrefixIndexSpecialPageProvider()
        {
            from = "apprefix";
        }

        public override List<Article> MakeList(int Namespace, params string[] searchCriteria)
        {
            return base.MakeList(Namespace, searchCriteria);
        }
    }
}
