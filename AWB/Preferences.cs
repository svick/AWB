using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using WikiFunctions;

namespace AutoWikiBrowser
{
    internal sealed partial class MyPreferences : Form
    {
        public MyPreferences(LangCodeEnum lang, ProjectEnum proj, string customproj, Font TextFont, bool LowPriority, bool Flash, bool Beep, bool Minimize, bool SaveArticleList, bool OverrideWatchlist, decimal TimeOut, bool AutoSaveEditBox, string AutoSaveEditBoxFile, decimal AutoSaveEditBoxPeriod)
        {
            InitializeComponent();

            foreach (LangCodeEnum l in Enum.GetValues(typeof(LangCodeEnum)))
                cmboLang.Items.Add(l.ToString().ToLower());

            foreach (ProjectEnum l in Enum.GetValues(typeof(ProjectEnum)))
                cmboProject.Items.Add(l);

            cmboLang.SelectedItem = lang.ToString().ToLower();
            cmboProject.SelectedItem = proj;

            cmboCustomProject.Text = customproj;

            TextBoxFont = TextFont;
            LowThreadPriority = LowPriority;
            perfFlash = Flash;
            perfBeep = Beep;
            perfMinimize = Minimize;
            perfSaveArticleList = SaveArticleList;
            perfOverrideWatchlist = OverrideWatchlist;
            perfTimeOutLimit = TimeOut;

            perfAutoSaveEditBoxEnabled = AutoSaveEditBox;
            perfAutoSaveEditBoxFile = AutoSaveEditBoxFile;
            perfAutoSaveEditBoxPeriod = AutoSaveEditBoxPeriod;

            cmboProject_SelectedIndexChanged(null, null);
        }

        #region Language and project

        public LangCodeEnum Language
        {
            get
            {
                if (cmboLang.SelectedItem.ToString() == "is") return LangCodeEnum.Is;
                LangCodeEnum l = (LangCodeEnum)Enum.Parse(typeof(LangCodeEnum), cmboLang.SelectedItem.ToString());
                return l;
            }
        }
        public ProjectEnum Project
        {
            get
            {
                ProjectEnum p = (ProjectEnum)Enum.Parse(typeof(ProjectEnum), cmboProject.SelectedItem.ToString());
                return p;
            }
        }
        public string CustomProject
        {
            get { return cmboCustomProject.Text; }
        }

        private void txtCustomProject_Leave(object sender, EventArgs e)
        {
            cmboCustomProject.Text = Regex.Replace(cmboCustomProject.Text, "^http://", "", RegexOptions.IgnoreCase);
            cmboCustomProject.Text = cmboCustomProject.Text.TrimEnd('/');
            cmboCustomProject.Text = cmboCustomProject.Text + "/";
        }

        private void cmboProject_SelectedIndexChanged(object sender, EventArgs e)
        {
            //disable language selection for single language projects
            cmboLang.Enabled = cmboProject.SelectedIndex <= 6;

            if ((ProjectEnum)Enum.Parse(typeof(ProjectEnum), cmboProject.SelectedItem.ToString()) == ProjectEnum.custom)
            {
                cmboCustomProject.Visible = true;
                cmboLang.Visible = false;
                lblLang.Text = "http://";
                cmboCustomProject_TextChanged(null, null);
            }
            else
            {
                cmboCustomProject.Visible = false;
                cmboLang.Visible = true;
                lblLang.Text = "Language:";
                btnApply.Enabled = true;
            }
        }

        private void cmboCustomProject_TextChanged(object sender, EventArgs e)
        {
            btnApply.Enabled = (cmboCustomProject.Text != "");
        }



        #endregion

        #region Other

        Font f;
        public Font TextBoxFont
        {
            get { return f; }
            set { f = value; }
        }

        private void btnTextBoxFont_Click(object sender, EventArgs e)
        {
            fontDialog.Font = TextBoxFont;
            if (fontDialog.ShowDialog() == DialogResult.OK)
                TextBoxFont = fontDialog.Font;
        }

        public bool LowThreadPriority
        {
            get { return chkLowPriority.Checked; }
            set { chkLowPriority.Checked = value; }
        }

        public bool FlashAndBeep
        {
            set { chkFlash.Checked = value; chkBeep.Checked = value; }
        }

        public bool perfFlash
        {
            get { return chkFlash.Checked; }
            set { chkFlash.Checked = value; }
        }

        public bool perfBeep
        {
            get { return chkBeep.Checked; }
            set { chkBeep.Checked = value; }
        }

        public bool perfMinimize
        {
            get { return chkMinimize.Checked; }
            set { chkMinimize.Checked = value; }
        }

        public bool perfSaveArticleList
        {
            get { return chkSaveArticleList.Checked; }
            set { chkSaveArticleList.Checked = value; }
        }

        public bool perfOverrideWatchlist
        {
            get { return chkOverrideWatchlist.Checked; }
            set { chkOverrideWatchlist.Checked = value; }
        }

        public decimal perfTimeOutLimit
        {
            get { return numTimeOutLimit.Value; }
            set { numTimeOutLimit.Value = value; }
        }

        public bool perfAutoSaveEditBoxEnabled
        {
            get { return chkAutoSaveEdit.Checked; }
            set { chkAutoSaveEdit.Checked = value; txtAutosave.Enabled = value; numEditBoxAutosave.Enabled = value; label8.Enabled = value; label9.Enabled = value; label10.Enabled = value; }
        }

        public decimal perfAutoSaveEditBoxPeriod
        {
            get { return numEditBoxAutosave.Value; }
            set { numEditBoxAutosave.Value = value; }
        }

        public List<String> perfCustomWikis
        {
            get
            {
                List<String> Temp = new List<String>();
                Temp.Add(cmboCustomProject.Text);
                foreach (object a in cmboCustomProject.Items)
                    Temp.Add(a.ToString());
                return Temp;
            }
            set
            {
                cmboCustomProject.Items.Clear();
                foreach (string Temp in value)
                    cmboCustomProject.Items.Add(Temp);
            }
        }


        public string perfAutoSaveEditBoxFile
        {
            get { return txtAutosave.Text; }
            set { txtAutosave.Text = value; }
        }
        #endregion

        private void chkAutoSaveEdit_CheckedChanged(object sender, EventArgs e)
        {
            perfAutoSaveEditBoxEnabled = chkAutoSaveEdit.Checked;
        }
    }
}