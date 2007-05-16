using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Text.RegularExpressions;
using WikiFunctions;

namespace AutoWikiBrowser
{
    internal sealed partial class SkipOptions : Form
    {
        public SkipOptions()
        {
            InitializeComponent();
        }

        #region Properties

        public bool SkipNoUnicode
        {
            get { return rdoNoUnicode.Checked; }
        }

        public bool SkipNoTag
        {
            get { return rdoNoTag.Checked; }
        }

        public bool SkipNoHeaderError
        {
            get { return rdoNoHeaderError.Checked; }
        }

        public bool SkipNoBoldTitle
        {
            get { return rdoNoBoldTitle.Checked; }
        }

        public bool SkipNoBulletedLink
        {
            get { return rdoNoBulletedLink.Checked; }
        }

        public bool SkipNoBadLink
        {
            get { return rdoNoBadLink.Checked; }
        }

        #endregion

        #region Methods

        public bool skipIf(string articleText)
        {//custom code to skip articles can be added here
            return true;
        }

        private void SkipOptions_FormClosing(object sender, FormClosingEventArgs e)
        {
            e.Cancel = true;
            this.Hide();
        }

        private void btnClose_Click(object sender, EventArgs e)
        {
            this.Hide();
        }

        public string SelectedItem
        {
            get
            {
                foreach (RadioButton rd in gbOptions.Controls)
                {
                    if (rd.Checked)
                        return rd.Tag.ToString();
                }

                return "0";
            }
            set
            {
                foreach (RadioButton rd in gbOptions.Controls)
                {
                    if (rd.Tag.ToString() == value)
                    {
                        rd.Checked = true;
                        return;
                    }
                }
                rdoNone.Checked = true;
            }
        }

        #endregion

    }
}