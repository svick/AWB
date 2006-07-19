/*
Autowikibrowser
Copyright (C) 2006 Martin Richards

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
using System.Collections;
using System.Text.RegularExpressions;
using WikiFunctions;

namespace AutoWikiBrowser
{
    public partial class specialFilter : Form
    {
        ListBox lb;
        public specialFilter(ListBox listbox)
        {
            InitializeComponent();
            lb = listbox;
            UpdateText();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            bool does = (chkContains.Checked && txtContains.Text != "");
            bool doesnot = (chkNotContains.Checked && txtDoesNotContain.Text != "");

            if (lbRemove.Items.Count > 0)
                FilterList();

            if (does || doesnot)
                FilterMatches(does, doesnot);

            FilterNamespace();

            this.Close();
        }

        private void FilterNamespace()
        {
            int j = 0;
            int i = 0;

            while (i < lb.Items.Count)
            {
                j = Tools.CalculateNS(lb.Items[i].ToString());

                if (j == 0)
                {
                    if (chkArticle.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 1)
                {
                    if (chkArticleTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 2)
                {
                    if (chkUser.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 3)
                {
                    if (chkUserTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 4)
                {
                    if (chkWikipedia.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 5)
                {
                    if (chkWikipediaTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 6)
                {
                    if (chkImage.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 7)
                {
                    if (chkImageTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 8)
                {
                    if (chkMediaWiki.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 9)
                {
                    if (chkMediaWikiTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 10)
                {
                    if (chkTemplate.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 11)
                {
                    if (chkTemplateTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 12)
                {
                    if (chkHelp.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 13)
                {
                    if (chkHelpTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 14)
                {
                    if (chkCategory.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 15)
                {
                    if (chkCategoryTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 100)
                {
                    if (chkPortal.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else if (j == 101)
                {
                    if (chkPortalTalk.Checked)
                    {
                        i++;
                        continue;
                    }
                    else
                        lb.Items.RemoveAt(i);
                }
                else
                    i++;
            }
        }

        private void FilterMatches(bool does, bool doesnot)
        {
            string strMatch = txtContains.Text;
            string strNotMatch = txtDoesNotContain.Text;

            if (!chkIsRegex.Checked)
            {
                strMatch = Regex.Escape(strMatch);
                strNotMatch = Regex.Escape(strNotMatch);
            }

            Regex match = new Regex(strMatch);
            Regex notMatch = new Regex(strNotMatch);

            string s = "";
            int i = 0;

            while (i < lb.Items.Count)
            {
                s = lb.Items[i].ToString();
                if (does && match.IsMatch(s))
                    lb.Items.RemoveAt(i);
                else if (doesnot && !notMatch.IsMatch(s))
                    lb.Items.RemoveAt(i);
                else
                    i++;
            }
        }

        private void FilterList()
        {
            string s = "";
            int i = 0;

            while (i < lbRemove.Items.Count)
            {
                s = lbRemove.Items[i].ToString();
                lb.Items.Remove(s);
                i++;
            }
        }

        private void btnGetList_Click(object sender, EventArgs e)
        {
            OpenFileDialog of = new OpenFileDialog();
            GetLists gl = new GetLists();
            ArrayList list = new ArrayList();

            if (of.ShowDialog() == DialogResult.OK)
            {
                list = gl.FromTextFile(of.FileName);

                foreach (string s in list)
                    lbRemove.Items.Add(s);
            }
        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            lbRemove.Items.Clear();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void chkContains_CheckedChanged(object sender, EventArgs e)
        {
            txtContains.Enabled = chkContains.Checked;
            chkIsRegex.Enabled = chkContains.Checked || chkNotContains.Checked;
        }

        private void chkNotContains_CheckedChanged(object sender, EventArgs e)
        {
            txtDoesNotContain.Enabled = chkNotContains.Checked;
            chkIsRegex.Enabled = chkContains.Checked || chkNotContains.Checked;
        }

        public void UpdateText()
        {
            chkArticleTalk.Text = Variables.Namespaces[1];
            chkUser.Text = Variables.Namespaces[2];
            chkUserTalk.Text = Variables.Namespaces[3];
            chkWikipedia.Text = Variables.Namespaces[4];
            chkWikipediaTalk.Text = Variables.Namespaces[5];
            chkImage.Text = Variables.Namespaces[6];
            chkImageTalk.Text = Variables.Namespaces[7];
            chkMediaWiki.Text = Variables.Namespaces[8];
            chkMediaWikiTalk.Text = Variables.Namespaces[9];
            chkTemplate.Text = Variables.Namespaces[10];
            chkTemplateTalk.Text = Variables.Namespaces[11];
            chkHelp.Text = Variables.Namespaces[12];
            chkHelpTalk.Text = Variables.Namespaces[13];
            chkCategory.Text = Variables.Namespaces[14];
            chkCategoryTalk.Text = Variables.Namespaces[15];
            chkPortal.Text = Variables.Namespaces[100];
            chkPortalTalk.Text = Variables.Namespaces[101];

        }

        #region contextMenu

        private void nonTalkOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CheckBox cb in groupBox1.Controls)
            {
                if (cb.Name.Contains("Talk"))
                    cb.Checked = false;
                else
                    cb.Checked = true;

            }
        }

        private void talkSpaceOnlyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CheckBox cb in this.groupBox1.Controls)
            {
                if (cb.Name.Contains("Talk"))
                    cb.Checked = true;
                else
                    cb.Checked = false;

            }
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CheckBox cb in this.groupBox1.Controls)
            {
                cb.Checked = true;
            }
        }

        private void deselectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            foreach (CheckBox cb in this.groupBox1.Controls)
            {
                cb.Checked = false;
            }
        }

        #endregion
               

    }
}