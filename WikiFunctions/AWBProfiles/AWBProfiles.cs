/*
AWB Profiles
Copyright (C) 2007 Sam Reed

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

namespace WikiFunctions.AWBProfiles
{
    public partial class AWBProfilesForm : Form
    {
        private WikiFunctions.Browser.WebControl Browser;
        AWBProfile AWBProfile = new AWBProfile();

        public AWBProfilesForm(WikiFunctions.Browser.WebControl browser)
        {
            InitializeComponent();
            this.Browser = browser;
        }

        private void AWBProfiles_Load(object sender, EventArgs e)
        {
            loadProfiles();
        }

        private void loadProfiles()
        {
            lvAccounts.Items.Clear();

            foreach (AWBProfile profile in AWBProfiles.GetProfiles())
            {
                ListViewItem item = new ListViewItem(profile.id.ToString());
                item.SubItems.Add(profile.Username);
                if (profile.Password != "")
                    item.SubItems.Add("Yes");
                else
                    item.SubItems.Add("No");
                item.SubItems.Add(profile.defaultsettings);
                item.SubItems.Add(profile.notes);

                lvAccounts.Items.Add(item);
            }

            WikiFunctions.Lists.ListViewColumnResize.resizeListView(lvAccounts);
        }

        private void btnLogin_Click(object sender, EventArgs e)
        {
            login();
        }

        private void loginAsThisAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            login();
        }

        private void login()
        {
            if (lvAccounts.SelectedItems.Count == 1)
            {
                if (lvAccounts.Items[lvAccounts.SelectedIndices[0]].SubItems[2].Text == "Yes")
                {//Get 'Saved' Password
                    Browser.Login(lvAccounts.Items[lvAccounts.SelectedIndices[0]].SubItems[1].Text, AWBProfiles.GetPassword(int.Parse(lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text)));
                }
                else
                {//Get Password from User
                    UserPassword password = new UserPassword();
                    password.SetText = "Enter password for: " + lvAccounts.Items[lvAccounts.SelectedIndices[0]].SubItems[1].Text;

                    if (password.ShowDialog() == DialogResult.OK)
                        Browser.Login(lvAccounts.Items[lvAccounts.SelectedIndices[1]].Text, password.GetPassword);
                }
            }
        }

        private void btnAdd_Click(object sender, EventArgs e)
        {
            Add();
        }

        private void addNewAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Add();
        }

        private void Add()
        {
            AWBProfileAdd add = new AWBProfileAdd();
            if (add.ShowDialog() == DialogResult.Yes)
                loadProfiles();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void deleteThisSavedAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Delete();
        }

        private void Delete()
        {
            AWBProfiles.DeleteProfile(int.Parse(lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text));
            loadProfiles();
        }

        private void changePasswordToolStripMenuItem_Click(object sender, EventArgs e)
        {
            UserPassword password = new UserPassword();
            password.SetText = "Set password for: " + lvAccounts.Items[lvAccounts.SelectedIndices[0]].SubItems[1].Text;

            if (password.ShowDialog() == DialogResult.OK)
                AWBProfiles.SetPassword(int.Parse(lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text), password.GetPassword);
        }

        private void editThisAccountToolStripMenuItem_Click(object sender, EventArgs e)
        {
            AWBProfileAdd add = new AWBProfileAdd(AWBProfiles.GetProfile(int.Parse(lvAccounts.Items[lvAccounts.SelectedIndices[0]].Text)));
            if (add.ShowDialog() == DialogResult.Yes)
                loadProfiles();
        }
    }
}