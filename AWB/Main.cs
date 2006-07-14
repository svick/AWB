﻿/*
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
using System.Threading;
using System.Text.RegularExpressions;
using System.IO;
using System.Collections;
using System.Web;
using System.Xml;
using System.Reflection;
using System.Diagnostics;
using WikiFunctions;

[assembly: CLSCompliant(true)]
namespace AutoWikiBrowser
{
    public partial class MainForm : Form
    {
        #region constructor etc.

        public MainForm()
        {
            InitializeComponent();

            btntsShowHide.Image = Resources.btnshowhide_image;
            btntsSave.Image = Resources.btntssave_image;
            btntsIgnore.Image = Resources.btntsignore_image;
            btntsStop.Image = Resources.Stop;
            btntsPreview.Image = Resources.preview;
            btntsChanges.Image = Resources.changes;

            //add articles to avoid (in future may be populated from checkpage
            //noParse.Add("User:Bluemoose/Sandbox");


            //check that we are not using an old OS. 98 seems to mangled some unicode.
            if (Environment.OSVersion.Version.Major < 5)
            {
                MessageBox.Show("You appear to be using an older operating system, this software may have trouble with some unicode fonts on operating systems older than Windows 2000, the start button has been disabled.", "Operating system", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                btnStart.Enabled = false;
            }
            else
            {
                btnMakeList.Enabled = true;
            }
            Debug();

            if (AutoWikiBrowser.Properties.Settings.Default.LowThreadPriority)
                Thread.CurrentThread.Priority = ThreadPriority.Lowest;

            //read and set project from user persistent settings (was saved on last exit)
            SetProject(
                /* string strLang, */ AutoWikiBrowser.Properties.Settings.Default.Language,
                /* string strProj */  AutoWikiBrowser.Properties.Settings.Default.Project
            );

            cmboSourceSelect.SelectedIndex = 0;
            cmboCategorise.SelectedIndex = 0;
            cmboImages.SelectedIndex = 0;
            lblStatusText.AutoSize = true;
            lblBotTimer.AutoSize = true;

            UpdateButtons();

            webBrowserLogin.ScriptErrorsSuppressed = true;
            ticker += timeKeeper;
            webBrowserLogin.DocumentCompleted += web4Completed;
            webBrowserLogin.Navigating += web4Starting;

            webBrowserEdit.Loaded += CaseWasLoad;
            webBrowserEdit.Diffed += CaseWasDiff;
            webBrowserEdit.Saved += CaseWasSaved;
            webBrowserEdit.None += CaseWasNull;
            webBrowserEdit.Fault += StartDelayedRestartTimer;
            webBrowserEdit.StatusChanged += UpdateStatus;
        }

        //Active article
        readonly Regex WikiLinkRegex = new Regex("\\[\\[(.*?)(\\]\\]|\\|)", RegexOptions.Compiled);
        string EdittingArticle = "";
        string LastArticle = "";
        string strUserName = "";
        string UserName
        {
            get { return strUserName; }
            set
            {
                strUserName = value;
                lblUserName.Text = Variables.Namespaces[2] + value;
            }
        }
        string strSettingsFile = "";
        string strListFile = "";
        bool boolSaved = true;
        ArrayList noParse = new ArrayList();

        FindandReplace findAndReplace = new FindandReplace();
        WikiFunctions.MWB.ReplaceSpecial replaceSpecial = new WikiFunctions.MWB.ReplaceSpecial();
        Parsers parsers = new Parsers();
        WebControl webBrowserLogin = new WebControl();
        GetLists getLists = new GetLists();

        //true = don't check whether we are logged in, false = do check.
        private bool WikiStatusBool = false;
        internal bool wikiStatusBool
        {
            get { return WikiStatusBool; }
            set { WikiStatusBool = value; }
        }

        private int NumberOfArticles
        {
            set { lblNumberOfArticles.Text = value.ToString(); }
        }

        private void MainForm_Load(object sender, EventArgs e)
        {
            loadDefaultSettings();
        }

        #endregion

        #region MainProcess

        private void Start()
        {
            try
            {
                //check edit summary
                if (cmboEditSummary.Text == "")
                    MessageBox.Show("Please enter an edit summary.", "Edit summary", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);

                StopDelayedRestartTimer();

                DisableButtons();
                parsers.EditSummary = "";

                skippable = true;

                txtEdit.Clear();

                if (webBrowserEdit.IsBusy)
                {
                    webBrowserEdit.ProcessStage = enumProcessStage.none;
                    webBrowserEdit.Stop();
                }

                if (webBrowserEdit.Document != null)
                    webBrowserEdit.Document.Write("");

                //check we are logged in
                if (!WikiStatus())
                    return;

                ArticleInfo(true);

                if (lbArticles.Items.Count < 1)
                {
                    stopSaveInterval();
                    lblTimer.Text = "";
                    lblStatusText.Text = "No articles in list, you need to use the Make list";
                    this.Text = "AutoWikiBrowser";
                    webBrowserEdit.Document.Write("");
                    btnMakeList.Enabled = true;
                    return;
                }

                if (lbArticles.SelectedItem == null)
                    lbArticles.SelectedIndex = 0;

                EdittingArticle = lbArticles.SelectedItem.ToString();

                string Article = HttpUtility.UrlEncode(EdittingArticle);

                //Navigate to edit page
                webBrowserEdit.LoadEditPage(Article);
            }
            catch
            {
                StartDelayedRestartTimer();
            }
        }

        private void CaseWasLoad()
        {
            string strText = "";
            string strRedirect = "";

            if (!loadSuccess())
                return;

            if (webBrowserEdit.Document.GetElementById("wpTextbox1").InnerText != null)
                strText = webBrowserEdit.GetArticleText();

            this.Text = "AutoWikiBrowser" + strSettingsFile + " - " + EdittingArticle;

            //check not in use
            if (Regex.IsMatch(strText, "\\{\\{[Ii]nuse"))
            {
                if (!chkAutoMode.Checked)
                    MessageBox.Show("This page has the \"Inuse\" tag, consider skipping it");
            }

            //check for redirect
            if (bypassRedirectsToolStripMenuItem.Checked && Regex.IsMatch(strText, "^#redirect", RegexOptions.IgnoreCase))
            {
                Match m = Regex.Match(strText, "\\[\\[(.*?)\\]\\]");
                strRedirect = m.Groups[1].Value;

                int intPos = 0;
                intPos = lbArticles.Items.IndexOf(EdittingArticle);

                lbArticles.Items.Insert(intPos, strRedirect);
                RemoveEdittingArticle();
                EdittingArticle = strRedirect;
                lbArticles.ClearSelected();

                lbArticles.SelectedItem = strRedirect;
                strRedirect = HttpUtility.UrlEncode(strRedirect);
                webBrowserEdit.LoadEditPage(strRedirect);
                return;
            }

            if (chkIgnoreIfContains.Checked && IgnoreIfContains(EdittingArticle + strText,
            txtIgnoreIfContains.Text, chkIgnoreIsRegex.Checked, chkIgnoreCaseSensitive.Checked))
            {
                SkipPage();
                return;
            }

            if (chkOnlyIfContains.Checked && IgnoreIfDoesntContain(EdittingArticle + strText,
            txtOnlyIfContains.Text, chkIgnoreIsRegex.Checked, chkIgnoreCaseSensitive.Checked))
            {
                SkipPage();
                return;
            }

            if (!skipIf(strText))
            {
                SkipPage();
                return;
            }

            bool skip = false;
            if (!doNotAutomaticallyDoAnythingToolStripMenuItem.Checked)
            {
                string strOrigText = strText;
                strText = Process(strText, ref skip);

                if (skippable && chkSkipNoChanges.Checked)
                {
                    if (strText == strOrigText)
                    {
                        SkipPage();
                        return;
                    }
                }
            }

            if (skip)
            {
                SkipPage();
                return;
            }

            webBrowserEdit.SetArticleText(strText);
            txtEdit.Text = strText;
            //Update statistics and alerts
            ArticleInfo(false);

            if (chkAutoMode.Checked && chkQuickSave.Checked)
                startDelayedTimer();
            else
                GetDiff(previewInsteadOfDiffToolStripMenuItem.Checked);
        }

        private bool loadSuccess()
        {
            try
            {
                string HTML = webBrowserEdit.Document.Body.InnerHtml;

                if (HTML.Contains("<H1 class=firstHeading>View source</H1>"))
                {
                    SkipPage();
                    return false;
                }
                //check we are still logged in
                if (!webBrowserEdit.LoggedIn)
                {
                    wikiStatusBool = false;
                    Start();
                    return false;
                }
                string HTMLsub = HTML.Remove(HTML.IndexOf("<!-- start content -->"));
                if (HTMLsub.Contains("<DIV class=usermessage>"))
                {//check if we have any messages
                    wikiStatusBool = false;
                    UpdateButtons();
                    webBrowserEdit.Document.Write("");
                    this.Focus();

                    dlgTalk DlgTalk = new dlgTalk();
                    if (DlgTalk.ShowDialog() == DialogResult.Yes)
                        System.Diagnostics.Process.Start("http://en.wikipedia.org/wiki/User_talk:" + UserName);
                    else
                        System.Diagnostics.Process.Start("IExplore", "http://en.wikipedia.org/wiki/User_talk:" + UserName);

                    DlgTalk = null;
                    return false;
                }
                if (!webBrowserEdit.HasArticleTextBox)
                {
                    if (!chkAutoMode.Checked)
                    {
                        MessageBox.Show("There was a problem loading the page. Re-start the process", "Problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    lblStatusText.Text = "There was a problem loading the page. Re-starting.";
                    StartDelayedRestartTimer();
                    return false;
                }
                if (webBrowserEdit.Document.GetElementById("wpTextbox1").InnerText == null && ignoreNonexistentPagesToolStripMenuItem.Checked)
                {//check if it is a non-existent page, if so then skip it automatically.
                    SkipPage();
                    return false;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            return true;
        }

        private bool skipIf(string articleText)
        {//custom code to skip articles can be added here
            return true;

        }

        bool skippable = true;
        private void CaseWasDiff()
        {
            if (diffChecker(webBrowserEdit.Document.Body.InnerHtml))
            {//check if there are no changes and we want to skip
                SkipPage();
                return;
            }

            if (!AutoWikiBrowser.Properties.Settings.Default.DisableDiffScrollDown)
            {
                webBrowserEdit.Document.GetElementById("contentSub").ScrollIntoView(true);
            }

            if (chkAutoMode.Checked)
            {
                startDelayedTimer();
                return;
            }

            if (!this.ContainsFocus)
            {
                Tools.FlashWindow(this);
                Tools.Beep1();
            }

            EnableButtons();
        }

        private bool diffChecker(string strHTML)
        {//check diff to see if it should be skipped

            if (!skippable || !chkSkipNoChanges.Checked || previewInsteadOfDiffToolStripMenuItem.Checked || doNotAutomaticallyDoAnythingToolStripMenuItem.Checked)
                return false;

            if (!strHTML.Contains("class=diff-context") && !strHTML.Contains("class=diff-deletedline"))
                return true;

            strHTML = strHTML.Replace("<SPAN class=diffchange></SPAN>", "");
            strHTML = Regex.Match(strHTML, "<TD align=left colSpan=2.*?</DIV>", RegexOptions.Singleline).ToString();

            //check for no changes, or no new lines (that have text on the new line)
            if (strHTML.Contains("<SPAN class=diffchange>") || Regex.IsMatch(strHTML, "class=diff-deletedline>[^<]") || Regex.IsMatch(strHTML, "<TD colSpan=2>&nbsp;</TD>\r\n<TD>\\+</TD>\r\n<TD class=diff-addedline>[^<]"))
                return false;

            return true;
        }

        private void CaseWasSaved()
        {
            if (webBrowserEdit.Document.Body.InnerHtml.Contains("<H1 class=firstHeading>Edit conflict: "))
            {//if session data is lost, if data is lost then save after delay with tmrAutoSaveDelay

                if (!chkAutoMode.Checked)
                    MessageBox.Show("Edit conflict, restarting");

                Start();
                return;
            }

            if (webBrowserEdit.Document.Body.InnerHtml.Contains("<DIV CLASS=PREVIEWNOTE><P><STRONG>Sorry! We could not process your edit due to a loss of session data."))
            {//if session data is lost, if data is lost then save after delay with tmrAutoSaveDelay
                startDelayedTimer();
                return;
            }

            //lower restart delay
            if (intRestartDelay > 5)
                intRestartDelay -= 1;

            intEdits++;

            LastArticle = "";
            RemoveEdittingArticle();
            Start();
        }

        private void CaseWasNull()
        {
            if (webBrowserEdit.Document.Body.InnerHtml.Contains("<B>You have successfully signed in to Wikipedia as"))
            {
                lblStatusText.Text = "Signed in, now re-starting";
                WikiStatus();
            }
        }

        private void SkipPage()
        {
            try
            {
                //reset timer.
                stopDelayedTimer();
                webBrowserEdit.Document.Write("");
                RemoveEdittingArticle();
                Start();
            }
            catch { }
        }

        private string Process(string articleText, ref bool skip)
        {
            string testText; // use to check if changes are made
            bool process = true;

            try
            {
                if (noParse.Contains(EdittingArticle))
                    process = false;

                if (chkUnicodifyWhole.Checked && process)
                    articleText = parsers.Unicodify(articleText);

                if (cmboImages.SelectedIndex == 1)
                {
                    testText = articleText;
                    articleText = parsers.ReImager(txtImageReplace.Text, txtImageWith.Text, articleText);
                    if (testText == articleText)
                    {
                        skip = true;
                        return articleText;
                    }
                }
                else if (cmboImages.SelectedIndex == 2)
                {
                    testText = articleText;
                    articleText = parsers.RemoveImage(txtImageReplace.Text, articleText);
                    if (testText == articleText)
                    {
                        skip = true;
                        return articleText;
                    }
                }

                if (cmboCategorise.SelectedIndex == 1)
                {
                    testText = articleText;
                    articleText = parsers.ReCategoriser(txtSelectSource.Text, txtNewCategory.Text, articleText);
                    if (testText == articleText)
                    {
                        skip = true;
                        return articleText;
                    }
                }
                else if (cmboCategorise.SelectedIndex == 2 && txtNewCategory.Text.Length > 0)
                {
                    string cat = "[[" + Variables.Namespaces[14] + txtNewCategory.Text + "]]";
                    cat = cat.Replace("%%key%%", Tools.MakeHumanCatKey(EdittingArticle));

                    bool is_template = EdittingArticle.StartsWith(Variables.Namespaces[10]);
                    if (EdittingArticle.StartsWith(Variables.Namespaces[10]))
                        articleText += "<noinclude>\r\n" + cat + "\r\n</noinclude>";
                    else
                        articleText += cat;
                }
                else if (cmboCategorise.SelectedIndex == 3 && txtNewCategory.Text.Length > 0)
                {
                    testText = articleText;
                    articleText = parsers.RemoveOldCats(txtNewCategory.Text, articleText);
                    if (testText == articleText)
                    {
                        skip = true;
                        return articleText;
                    }
                }

                if (chkFindandReplace.Checked)
                {
                    testText = articleText;
                    articleText = findAndReplace.MultipleFindAndReplce(articleText, EdittingArticle);
                    articleText = replaceSpecial.ApplyRules(articleText, EdittingArticle);

                    if (chkIgnoreWhenNoFAR.Checked && (testText == articleText))
                    {
                        skip = true;
                        return articleText;
                    }
                }

                if (process && chkGeneralParse.Checked && (Tools.IsMainSpace(EdittingArticle) || (EdittingArticle.Contains("Sandbox") || EdittingArticle.Contains("sandbox"))))
                {
                    articleText = parsers.RemoveNowiki(articleText);

                    if (Variables.LangCode == "en")
                    {//en only
                        articleText = parsers.Conversions(articleText);
                        articleText = parsers.LivingPeople(articleText);
                        articleText = parsers.FixCats(articleText);
                        articleText = parsers.FixHeadings(articleText);
                    }
                    articleText = parsers.SyntaxFixer(articleText);
                    articleText = parsers.LinkFixer(articleText);
                    articleText = parsers.BulletExternalLinks(articleText);
                    articleText = parsers.SortMetaData(articleText, EdittingArticle);
                    articleText = parsers.BoldTitle(articleText, EdittingArticle);
                    articleText = parsers.LinkSimplifier(articleText);

                    articleText = parsers.AddNowiki(articleText);
                }

                if (process && chkGeneralParse.Checked && EdittingArticle.StartsWith("User talk:"))
                    articleText = parsers.SubstUserTemplates(articleText);

                if (chkAppend.Checked)
                {
                    if (Tools.IsNotTalk(EdittingArticle))
                    {
                        MessageBox.Show("Messages should only be appended to talk pages.");
                    }
                    else if (rdoAppend.Checked)
                        articleText += "\r\n\r\n" + txtAppendMessage.Text;
                    else
                        articleText = txtAppendMessage.Text + "\r\n\r\n" + articleText;
                }

                if (process && chkAutoTagger.Checked)
                    articleText = parsers.Tagger(articleText, EdittingArticle);

                return articleText;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
                return articleText;
            }
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            ShowPreview();
        }

        private void btnDiff_Click(object sender, EventArgs e)
        {
            ShowDiff();
        }

        private void ShowPreview()
        {
            DisableButtons();
            LastArticle = txtEdit.Text; // added by Adrian 2006-03-12

            skippable = false;
            GetDiff(true);
        }

        private void ShowDiff()
        {
            DisableButtons();
            LastArticle = txtEdit.Text; // added by Adrian 2006-03-12

            GetDiff(false);
        }

        private void GetDiff(bool boolPreview)
        {
            if (webBrowserEdit.Document == null)
                return;

            if (!webBrowserEdit.CanDiff && !webBrowserEdit.CanPreview)
                return;

            //get either diff or preiew.
            webBrowserEdit.SetArticleText(txtEdit.Text);

            if (boolPreview)
                webBrowserEdit.ShowPreview();
            else
                webBrowserEdit.ShowDiff();
        }

        private void Save()
        {
            DisableButtons();
            if (txtEdit.Text.Length > 0)
                SaveArticle();
            else if (MessageBox.Show("Do you really want to save a blank page?", "Really save??", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                SaveArticle();
            else
                SkipPage();
        }

        private void SaveArticle()
        {
            //remember article text in case it is lost, this is set to "" again when the article title is removed
            LastArticle = txtEdit.Text;

            if (showTimerToolStripMenuItem.Checked)
            {
                stopSaveInterval();
                ticker += SaveInterval;
            }

            try
            {
                setCheckBoxes();

                webBrowserEdit.SetArticleText(txtEdit.Text);
                webBrowserEdit.Save();
            }
            catch { }
        }

        private void RemoveEdittingArticle()
        {
            boolSaved = false;
            if (lbArticles.Items.Contains(EdittingArticle))
            {
                while (lbArticles.SelectedItems.Count > 0)
                    lbArticles.SetSelected(lbArticles.SelectedIndex, false);

                txtNewArticle.Text = EdittingArticle;

                int intPosition = lbArticles.Items.IndexOf(EdittingArticle);

                lbArticles.Items.Remove(EdittingArticle);

                if (lbArticles.Items.Count == intPosition)
                    intPosition--;

                if (lbArticles.Items.Count > 0)
                    lbArticles.SelectedIndex = intPosition;
            }

            UpdateButtons();
        }

        #endregion

        #region MakeList

        private void btnAdd_Click(object sender, EventArgs e)
        {
            boolSaved = false;
            txtNewArticle.Text = Tools.TurnFirstToUpper(txtNewArticle.Text);
            if (txtNewArticle.Text.Length > 0)
                lbArticles.Items.Add(txtNewArticle.Text);
            txtNewArticle.Text = "";

            UpdateButtons();
        }

        private void btnRemoveArticle_Click(object sender, EventArgs e)
        {
            try
            {
                boolSaved = false;

                lbArticles.BeginUpdate();
                int i = lbArticles.SelectedIndex;

                if (lbArticles.SelectedItems.Count > 0)
                    txtNewArticle.Text = lbArticles.SelectedItem.ToString();

                while (lbArticles.SelectedItems.Count > 0)
                    lbArticles.Items.Remove(lbArticles.SelectedItem);

                if (lbArticles.Items.Count > i)
                    lbArticles.SelectedIndex = i;
                else
                    lbArticles.SelectedIndex = i - 1;

                lbArticles.EndUpdate();
            }
            catch
            { }

            UpdateButtons();
        }

        private void btnArticlesListClear_Click(object sender, EventArgs e)
        {
            boolSaved = false;
            lbArticles.Items.Clear();

            UpdateButtons();
        }

        Thread ListerThread = null;
        private void btnMakeList_Click(object sender, EventArgs e)
        {
            txtSelectSource.Text = txtSelectSource.Text.Trim('[', ']');
            if (cmboSourceSelect.SelectedIndex == 0)
                txtSelectSource.Text = Regex.Replace(txtSelectSource.Text, "^" + Variables.Namespaces[14], "", RegexOptions.IgnoreCase);
            else if (cmboSourceSelect.SelectedIndex == 6)
                txtSelectSource.Text = Regex.Replace(txtSelectSource.Text, "^" + Variables.Namespaces[2], "", RegexOptions.IgnoreCase);
            else if (cmboSourceSelect.SelectedIndex == 7)
                txtSelectSource.Text = Regex.Replace(txtSelectSource.Text, "^" + Variables.Namespaces[-1], "", RegexOptions.IgnoreCase);
            else if (cmboSourceSelect.SelectedIndex == 8)
                txtSelectSource.Text = Regex.Replace(txtSelectSource.Text, "^" + Variables.Namespaces[6], "", RegexOptions.IgnoreCase);
            else if (cmboSourceSelect.SelectedIndex == 9)
            {
                launchDumpSearcher();
                return;
            }

            txtSelectSource.Text = Tools.TurnFirstToUpper(txtSelectSource.Text);
            txtSelectSource.AutoCompleteCustomSource.Add(txtSelectSource.Text);

            //make sure there is some text.
            if (txtSelectSource.Text.Length == 0 && txtSelectSource.Enabled)
            {
                MessageBox.Show("Please enter some text", "No text", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            if (!WikiStatus())
                return;

            if (cmboSourceSelect.SelectedIndex == 4)
            {
                OpenFileDialog openListDialog = new OpenFileDialog();
                openListDialog.Filter = "text files|*.txt|All files|*.*";
                this.Focus();
                if (openListDialog.ShowDialog() == DialogResult.OK)
                {
                    addToList(getLists.FromTextFile(openListDialog.FileName));
                    strListFile = openListDialog.FileName;
                }
                UpdateButtons();
                return;
            }
            else if (cmboSourceSelect.SelectedIndex == 10)
            {
                addToList(getLists.FromWatchList());
                UpdateButtons();
                return;
            }

            intSourceIndex = cmboSourceSelect.SelectedIndex;
            strSouce = txtSelectSource.Text;

            ThreadStart thr_Process = new ThreadStart(MakeList);
            ListerThread = new Thread(thr_Process);
            ListerThread.IsBackground = true;
            ListerThread.Start();
        }
        //static readonly object lockObject = new object();

        int intSourceIndex = 0;
        string strSouce = "";
        private void MakeList()
        {
            boolSaved = false;
            StartProgressBar();

            try
            {
                switch (intSourceIndex)
                {
                    case 0:
                        addDictToList(getLists.FromCategory(strSouce));
                        break;
                    case 1:
                        addDictToList(getLists.FromWhatLinksHere(strSouce, false));
                        break;
                    case 2:
                        addDictToList(getLists.FromWhatLinksHere(strSouce, true));
                        break;
                    case 3:
                        addToList(getLists.FromLinksOnPage(strSouce));
                        break;
                        //4 from text file
                    case 5:
                        addToList(getLists.FromGoogleSearch(strSouce));
                        break;
                    case 6:
                        addToList(getLists.FromUserContribs(strSouce));
                        break;
                    case 7:
                        addToList(getLists.FromSpecialPage(strSouce));
                        break;
                    case 8:
                        addDictToList(getLists.FromImageLinks(strSouce));
                        break;
                        //9 from datadump
                        //10 from watchlist
                    default:
                        break;
                }
            }
            catch (ThreadAbortException)
            {
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
            finally
            {
                StopProgressBar();
            }
        }

        private delegate void SetProgBarDelegate();
        private void StopProgressBar()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new SetProgBarDelegate(StopProgressBar));
                return;
            }
            if (!toolStripProgressBar1.IsDisposed)
            {
                btnMakeList.Enabled = true;
                lblStatusText.Text = "List complete!";
                toolStripProgressBar1.MarqueeAnimationSpeed = 0;
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
                UpdateButtons();
            }
        }

        private delegate void StartProgBarDelegate();
        private void StartProgressBar()
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new StartProgBarDelegate(StartProgressBar));
                return;
            }
            lblStatusText.Text = "Getting list";
            btnMakeList.Enabled = false;
            toolStripProgressBar1.MarqueeAnimationSpeed = 100;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
        }

        private delegate void AddToListDel(ArrayList a);
        private void addToList(ArrayList ArticleArray)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new AddToListDel(addToList), ArticleArray);
                return;
            }

            lbArticles.BeginUpdate();

            foreach (string s in ArticleArray)
            {
                if (!lbArticles.Items.Contains(s))
                    lbArticles.Items.Add(s);
            }

            lbArticles.EndUpdate();

            UpdateButtons();
        }

        private delegate void AddDictToListDel(Dictionary<string, int> a);
        private void addDictToList(Dictionary<string, int> d)
        {
            if (this.InvokeRequired)
            {
                this.Invoke(new AddDictToListDel(addDictToList), d);
                return;
            }

            lbArticles.BeginUpdate();

            foreach (KeyValuePair<string, int> kvp in d)
            {
                if (!lbArticles.Items.Contains(kvp.Key))
                    lbArticles.Items.Add(kvp.Key);
            }

            lbArticles.EndUpdate();

            UpdateButtons();
        }

        private ArrayList ArrayFromList()
        {
            ArrayList list = new ArrayList();

            int i = 0;
            while (i < lbArticles.Items.Count)
            {
                list.Add(lbArticles.Items[i]);
                i++;
            }
            return list;
        }

        private void webBrowserEdit_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            toolStripProgressBar1.MarqueeAnimationSpeed = 0;
            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
        }

        private void webBrowserEdit_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            toolStripProgressBar1.MarqueeAnimationSpeed = 100;
        }

        private void convertToTalkPagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ArrayList list = ArrayFromList();
            list = getLists.ConvertToTalk(list);
            lbArticles.Items.Clear();
            addToList(list);
        }

        private void convertFromTalkPagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ArrayList list = ArrayFromList();
            list = getLists.ConvertFromTalk(list);
            lbArticles.Items.Clear();
            addToList(list);
        }

        private void fromCategoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cmboSourceSelect.SelectedIndex = 0;
            txtSelectSource.Text = lbArticles.SelectedItem.ToString();

            btnMakeList.PerformClick();
        }

        private void fromWhatlinkshereToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cmboSourceSelect.SelectedIndex = 1;
            txtSelectSource.Text = lbArticles.SelectedItem.ToString();

            btnMakeList.PerformClick();
        }

        private void fromLinksOnPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cmboSourceSelect.SelectedIndex = 3;
            txtSelectSource.Text = lbArticles.SelectedItem.ToString();

            btnMakeList.PerformClick();
        }

        private void fromImageLinksToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cmboSourceSelect.SelectedIndex = 8;
            txtSelectSource.Text = lbArticles.SelectedItem.ToString();

            btnMakeList.PerformClick();
        }

        #endregion

        #region extra stuff

        private void UpdateStatus()
        {
            lblStatusText.Text = webBrowserEdit.Status;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (AutoWikiBrowser.Properties.Settings.Default.DontAskForTerminate)
            {
                // save user persistent settings
                AutoWikiBrowser.Properties.Settings.Default.Save();
                return;
            }
            string msg = "";
            if (boolSaved == false)
                msg = "You have changed the list since last saving it!";

            TimeSpan time = new TimeSpan(intHours, intMinutes, intSeconds);
            ExitQuestion dlg = new ExitQuestion(time, intEdits, msg);
            dlg.ShowDialog();
            if (dlg.DialogResult == DialogResult.OK)
            {
                AutoWikiBrowser.Properties.Settings.Default.DontAskForTerminate
                = dlg.checkBoxDontAskAgain;

                // save user persistent settings
                AutoWikiBrowser.Properties.Settings.Default.Save();
            }
            else
            {
                e.Cancel = true;
            }
            dlg = null;

            Stop();
        }

        private void setCheckBoxes()
        {
            if (webBrowserEdit.Document.Body.InnerHtml.Contains("wpMinoredit"))
            {
                webBrowserEdit.SetMinor(markAllAsMinorToolStripMenuItem.Checked);
                webBrowserEdit.SetWatch(addAllToWatchlistToolStripMenuItem.Checked);

                string tag = cmboEditSummary.Text;
                if (!chkSuppressTag.Enabled || !chkSuppressTag.Checked)
                    tag += " " + parsers.EditSummary + Variables.SummaryTag;

                webBrowserEdit.SetSummary(tag);
            }
        }

        private void chkFindandReplace_CheckedChanged(object sender, EventArgs e)
        {
            btnMoreFindAndReplce.Enabled = chkFindandReplace.Checked;
            btnFindAndReplaceAdvanced.Enabled = chkFindandReplace.Checked;
            chkIgnoreWhenNoFAR.Enabled = chkFindandReplace.Checked;
        }

        private void cmboCategorise_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmboCategorise.SelectedIndex > 0)
            {
                if (cmboCategorise.SelectedIndex == 1 && (txtSelectSource.Text.Length == 0 || cmboSourceSelect.SelectedIndex != 0))
                {
                    cmboCategorise.SelectedIndex = 0;
                    MessageBox.Show("Please create a list of articles from a category first", "Make list", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
                else
                    txtNewCategory.Enabled = true;
            }
            else
                txtNewCategory.Enabled = false;
        }


        private void web4Completed(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            toolStripProgressBar1.MarqueeAnimationSpeed = 0;
            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
        }

        private void web4Starting(object sender, WebBrowserNavigatingEventArgs e)
        {
            toolStripProgressBar1.MarqueeAnimationSpeed = 100;
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
        }

        private bool WikiStatus()
        {//this checks if you are logged in, registered and have the newest version. Some bits disabled.
            try
            {
                //check if we need to bother checking or not
                if (wikiStatusBool)
                    return true;

                //stop the process stage being confused when the webbrowser document completed event fires.
                webBrowserEdit.ProcessStage = enumProcessStage.none;
                string strInnerHTML;

                //don't require to log in in other languages.
                if (Variables.LangCode != "en" || Variables.Project != "wikipedia")
                {
                    lblStatusText.Text = "Loading page to check if we are logged in";
                    webBrowserLogin.Navigate(Variables.URLShort + "/wiki/Main_Page");
                    //wait to load
                    while (webBrowserLogin.ReadyState != WebBrowserReadyState.Complete) Application.DoEvents();
                    strInnerHTML = webBrowserLogin.Document.Body.InnerHtml;

                    if (!strInnerHTML.Contains("<LI id=pt-logout"))
                    {//see if we are logged in
                        MessageBox.Show("You are not logged in. The log in screen will now load, enter your name and password, click \"Log in\", wait for it to complete, then start the process again.\r\n\r\nIn the future you can make sure this won't happen by logging in to Wikipedia using Microsoft Internet Explorer.", "Problem", MessageBoxButtons.OK, MessageBoxIcon.Information);
                        webBrowserEdit.LoadLogInPage();
                        return false;
                    }
                    else
                    {
                        wikiStatusBool = true;
                        chkAutoMode.Enabled = true;
                        return true;
                    }
                }

                //load check page
                lblStatusText.Text = "Loading page to check if we are logged in and bot is enabled";
                webBrowserLogin.Navigate("http://en.wikipedia.org/w/index.php?title=Wikipedia:AutoWikiBrowser/CheckPage&action=edit");
                //wait to load
                while (webBrowserLogin.ReadyState != WebBrowserReadyState.Complete) Application.DoEvents();

                strInnerHTML = webBrowserLogin.Document.Body.InnerHtml;

                if (!webBrowserLogin.LoggedIn)
                {//see if we are logged in
                    MessageBox.Show("You are not logged in. The log in screen will now load, enter your name and password, click \"Log in\", wait for it to complete, then start the process again.\r\n\r\nIn the future you can make sure this won't happen by logging in to Wikipedia using Microsoft Internet Explorer.", "Problem", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    webBrowserEdit.LoadLogInPage();
                    return false;
                }
                else if (!strInnerHTML.Contains("enabledusersbegins"))
                {
                    MessageBox.Show("Check page failed to load.\r\n\r\nCheck your Internet Explorer is working and that the Wikipedia servers are online, also try clearing Internet Explorer cache.", "Problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return false;
                }
                else if (!strInnerHTML.Contains(Assembly.GetExecutingAssembly().GetName().Version.ToString() + " enabled"))
                {//see if this version is enabled
                    MessageBox.Show("This version is not enabled, please download the newest version. If you have the newest version, check that Wikipedia is online.", "Problem", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    System.Diagnostics.Process.Start("http://en.wikipedia.org/wiki/Wikipedia:AutoWikiBrowser");
                    return false;
                }
                else
                {//see if we are allowed to use this softare

                    UserName = webBrowserLogin.GetUserName();
                    strInnerHTML = strInnerHTML.Substring(strInnerHTML.IndexOf("enabledusersbegins"), strInnerHTML.IndexOf("enabledusersends") - strInnerHTML.IndexOf("enabledusersbegins"));
                    string strBotUsers = strInnerHTML.Substring(strInnerHTML.IndexOf("enabledbots"), strInnerHTML.IndexOf("enabledbotsends") - strInnerHTML.IndexOf("enabledbots"));

                    if (UserName.Length > 0 && strInnerHTML.Contains("* " + UserName + "\r\n") || strInnerHTML.Contains("Everybody enabled = true"))
                    {
                        if (strBotUsers.Contains("* " + UserName + "\r\n"))
                        {//enable botmode
                            chkAutoMode.Enabled = true;
                        }

                        wikiStatusBool = true;
                        lblStatusText.Text = "Logged in, user enabled and software enabled";
                        UpdateButtons();
                        return true;
                    }
                    else
                    {
                        MessageBox.Show("You are not enabled to use this.", "Problem", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                        System.Diagnostics.Process.Start("http://en.wikipedia.org/wiki/Wikipedia:AutoWikiBrowser/CheckPage");
                        return false;
                    }
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.ToString());
                return false;
            }
        }

        private void chkAutoMode_CheckedChanged(object sender, EventArgs e)
        {
            if (chkAutoMode.Checked)
            {
                chkQuickSave.Enabled = true;
                nudBotSpeed.Enabled = true;
                lblBotTimer.Enabled = true;
                chkSkipNoChanges.Checked = true;
                chkSuppressTag.Enabled = true;
            }
            else
            {
                chkQuickSave.Enabled = false;
                nudBotSpeed.Enabled = false;
                lblBotTimer.Enabled = false;
                chkSuppressTag.Enabled = false;
                stopDelayedTimer();
            }
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TimeSpan time = new TimeSpan(intHours, intMinutes, intSeconds);
            AboutBox About = new AboutBox(webBrowserEdit.Version.ToString(), time, intEdits);
            About.Show();
        }

        private void cmbSourceSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            switch (cmboSourceSelect.SelectedIndex)
            {
                case 0:
                    lblSourceSelect.Text = Variables.Namespaces[14];
                    txtSelectSource.Enabled = true;
                    chkWLHRedirects.Visible = false;
                    return;
                case 1:
                    lblSourceSelect.Text = "What links to";
                    txtSelectSource.Enabled = true;
                    chkWLHRedirects.Visible = false;
                    return;
                case 2:
                    lblSourceSelect.Text = "What embeds";
                    txtSelectSource.Enabled = true;
                    chkWLHRedirects.Visible = false;
                    return;
                case 3:
                    lblSourceSelect.Text = "Links on";
                    txtSelectSource.Enabled = true;
                    chkWLHRedirects.Visible = false;
                    return;
                case 4:
                    lblSourceSelect.Text = "From file";
                    txtSelectSource.Enabled = false;
                    chkWLHRedirects.Visible = false;
                    return;
                case 5:
                    lblSourceSelect.Text = "Google search";
                    txtSelectSource.Enabled = true;
                    chkWLHRedirects.Visible = false;
                    return;
                case 6:
                    lblSourceSelect.Text = Variables.Namespaces[2];
                    txtSelectSource.Enabled = true;
                    chkWLHRedirects.Visible = false;
                    return;
                case 7:
                    lblSourceSelect.Text = Variables.Namespaces[-1];
                    txtSelectSource.Enabled = true;
                    chkWLHRedirects.Visible = false;
                    return;
                case 8:
                    lblSourceSelect.Text = Variables.Namespaces[6];
                    txtSelectSource.Enabled = true;
                    chkWLHRedirects.Visible = false;
                    return;
                default:
                    lblSourceSelect.Text = "";
                    txtSelectSource.Enabled = false;
                    chkWLHRedirects.Visible = false;
                    return;
            }
        }


        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        private void chkAppend_CheckedChanged(object sender, EventArgs e)
        {
            txtAppendMessage.Enabled = chkAppend.Checked;
            rdoAppend.Enabled = chkAppend.Checked;
            rdoPrepend.Enabled = chkAppend.Checked;
        }

        private void wordWrapToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            txtEdit.WordWrap = wordWrapToolStripMenuItem1.Checked;
        }

        private void chkIgnoreIfContains_CheckedChanged(object sender, EventArgs e)
        {
            txtIgnoreIfContains.Enabled = chkIgnoreIfContains.Checked;
        }

        private void chkOnlyIfContains_CheckedChanged(object sender, EventArgs e)
        {
            txtOnlyIfContains.Enabled = chkOnlyIfContains.Checked;
        }

        private void lbArticles_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Delete)
                btnRemoveArticle.PerformClick();
        }

        private void lbArticles_MouseMove(object sender, MouseEventArgs e)
        {
            string strTip = "";

            //Get the item
            int nIdx = lbArticles.IndexFromPoint(e.Location);
            if ((nIdx >= 0) && (nIdx < lbArticles.Items.Count))
                strTip = lbArticles.Items[nIdx].ToString();

            toolTip1.SetToolTip(lbArticles, strTip);
        }

        private void saveListToTextFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SaveList();
        }
        private void SaveList()
        {//Save lbArticles list to text file.
            try
            {
                int i = 0;
                string s = "";
                StringBuilder strList = new StringBuilder("");

                while (i < lbArticles.Items.Count)
                {
                    s = lbArticles.Items[i].ToString();
                    strList.Append("# [[" + s + "]]" + "\r\n");
                    i++;
                }

                if (strListFile.Length > 0)
                    saveListDialog.FileName = strListFile;

                if (saveListDialog.ShowDialog() == DialogResult.OK)
                {
                    strListFile = saveListDialog.FileName;
                    StreamWriter sw = new StreamWriter(strListFile, false, Encoding.UTF8);
                    sw.Write(strList);
                    sw.Close();
                    boolSaved = true;
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void FilterArticles()
        {
            //filter out non-mainspace articles
            int i = 0;
            string s = "";

            while (i < lbArticles.Items.Count)
            {
                s = lbArticles.Items[i].ToString();

                if (!Tools.IsMainSpace(s))
                    lbArticles.Items.Remove(lbArticles.Items[i]);
                else //move on
                    i++;
            }
            UpdateButtons();
        }

        private void specialFilterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            specialFilter();
        }

        private void specialFilterToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            specialFilter();
        }

        private void btnFilter_Click(object sender, EventArgs e)
        {
            specialFilter();
        }
        
        private void specialFilter()
        {
            specialFilter SepcialFilter = new specialFilter(lbArticles);
            SepcialFilter.ShowDialog();
            UpdateButtons();
        }

        private void txtNewCategory_Leave(object sender, EventArgs e)
        {
            txtNewCategory.Text = txtNewCategory.Text.Trim('[', ']');
            txtNewCategory.Text = Regex.Replace(txtNewCategory.Text, "^[Cc]ategory:", "");
            txtNewCategory.Text = Tools.TurnFirstToUpper(txtNewCategory.Text);
        }

        private void ArticleInfo(bool reset)
        {
            string ArticleText = txtEdit.Text;
            int intLength = 0;
            int intCats = 0;
            int intImages = 0;
            int intLinks = 0;
            int intInterLinks = 0;
            lblWarn.Text = "";

            if (reset)
            {
                //Resets all the alerts.
                lblLength.Text = "Characters: ";
                lblCats.Text = "Categories: ";
                lblImages.Text = "Images: ";
                lblLinks.Text = "Links: ";
                lblInterLinks.Text = "Inter links: ";

                lbDuplicateWikilinks.Items.Clear();
                lblDuplicateWikilinks.Visible = false;
                lbDuplicateWikilinks.Visible = false;
            }
            else
            {
                intLength = ArticleText.Length;

                foreach (Match m in Regex.Matches(ArticleText, "\\[\\[" + Variables.Namespaces[14], RegexOptions.IgnoreCase))
                    intCats++;

                foreach (Match m in Regex.Matches(ArticleText, "\\[\\[" + Variables.Namespaces[6], RegexOptions.IgnoreCase))
                    intImages++;

                foreach (Match m in Regex.Matches(ArticleText, "(\\[\\[[a-z]{2,3}:.*\\]\\]|\\[\\[simple:.*\\]\\]|\\[\\[fiu-vro:.*\\]\\]|\\[\\[minnan:.*\\]\\]|\\[\\[roa-rup:.*\\]\\]|\\[\\[tokipona:.*\\]\\]|\\[\\[zh-min-nan:.*\\]\\])"))
                    intInterLinks++;

                foreach (Match m in Regex.Matches(ArticleText, "\\[\\["))
                    intLinks++;

                intLinks = intLinks - intInterLinks - intImages - intCats;

                if ((Regex.IsMatch(ArticleText, "({{|-)([Ss]tub}})")) && (intLength > 3500))
                    lblWarn.Text = "Long article with a stub tag.\r\n";

                if (!(Regex.IsMatch(ArticleText, "\\[\\[" + Variables.Namespaces[14], RegexOptions.IgnoreCase)))
                    lblWarn.Text += "No category.\r\n";

                if (ArticleText.StartsWith("=="))
                    lblWarn.Text += "Starts with heading.";

                lblLength.Text = "Characters: " + intLength.ToString();
                lblCats.Text = "Categories: " + intCats.ToString();
                lblImages.Text = "Images: " + intImages.ToString();
                lblLinks.Text = "Links: " + intLinks.ToString();
                lblInterLinks.Text = "Inter links: " + intInterLinks.ToString();

                //Find multiple links                
                lbDuplicateWikilinks.Items.Clear();
                ArrayList ArrayLinks = new ArrayList();
                string x = "";
                //get all the links
                foreach (Match m in WikiLinkRegex.Matches(ArticleText))
                {
                    x = m.Groups[1].Value;
                    if (!Regex.IsMatch(x, "^(January|February|March|April|May|June|July|August|September|October|November|December) [0-9]{1,2}$") && !Regex.IsMatch(x, "^[0-9]{1,2} (January|February|March|April|May|June|July|August|September|October|November|December)$"))
                        ArrayLinks.Add(x);
                }

                lbDuplicateWikilinks.Sorted = true;

                //add the duplicate articles to the listbox
                foreach (string z in ArrayLinks)
                {
                    if ((ArrayLinks.IndexOf(z) < ArrayLinks.LastIndexOf(z)) && (!lbDuplicateWikilinks.Items.Contains(z)))
                        lbDuplicateWikilinks.Items.Add(z);
                }
                ArrayLinks = null;

                if (lbDuplicateWikilinks.Items.Count > 0)
                {
                    lblDuplicateWikilinks.Visible = true;
                    lbDuplicateWikilinks.Visible = true;
                }
            }
        }

        private void lbDuplicateWikilinks_Click(object sender, EventArgs e)
        {
            if (lbDuplicateWikilinks.SelectedIndex != -1)
            {
                string strLink = Regex.Escape(lbDuplicateWikilinks.SelectedItem.ToString());
                find("\\[\\[" + strLink + "(\\|.*?)?\\]\\]", true, true);
            }
            else
                resetFind();

            ArticleInfo(false);
        }
        private void txtFind_TextChanged(object sender, EventArgs e)
        {
            resetFind();
        }

        private void chkFindRegex_CheckedChanged(object sender, EventArgs e)
        {
            resetFind();
        }
        private void txtEdit_TextChanged(object sender, EventArgs e)
        {
            resetFind();
        }
        private void chkFindCaseSensitive_CheckedChanged(object sender, EventArgs e)
        {
            resetFind();
        }
        private void resetFind()
        {
            regexObj = null;
            matchObj = null;
        }
        private void btnFind_Click(object sender, EventArgs e)
        {
            find(txtFind.Text, chkFindRegex.Checked, chkFindCaseSensitive.Checked);
        }

        private Regex regexObj;
        private Match matchObj;
        private void find(string strRegex, bool isRegex, bool caseSensive)
        {
            string ArticleText = txtEdit.Text;

            RegexOptions regOptions;

            if (caseSensive)
                regOptions = RegexOptions.None;
            else
                regOptions = RegexOptions.IgnoreCase;

            strRegex = strRegex.Replace("%%title%%", EdittingArticle);

            if (!isRegex)
                strRegex = Regex.Escape(strRegex);

            if (matchObj == null || regexObj == null)
            {
                int findStart = txtEdit.SelectionStart;

                regexObj = new Regex(strRegex, regOptions);
                matchObj = regexObj.Match(ArticleText, findStart);
                txtEdit.SelectionStart = matchObj.Index;
                txtEdit.SelectionLength = matchObj.Length;
                txtEdit.Focus();
                txtEdit.ScrollToCaret();
                return;
            }
            else
            {
                if (matchObj.NextMatch().Success)
                {
                    matchObj = matchObj.NextMatch();
                    txtEdit.SelectionStart = matchObj.Index;
                    txtEdit.SelectionLength = matchObj.Length;
                    txtEdit.Focus();
                    txtEdit.ScrollToCaret();
                }
                else
                    resetFind();
            }
        }

        private void toolStripTextBox2_Click(object sender, EventArgs e)
        {
            toolStripTextBox2.Text = "";
        }

        private void toolStripTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!Regex.IsMatch(e.KeyChar.ToString(), "[0-9]") && e.KeyChar != 8)
                e.Handled = true;

            if (e.KeyChar == '\r' && toolStripTextBox2.Text.Length > 0)
            {
                e.Handled = true;
                GoToLine();
                mnuTextBox.Hide();
            }
        }

        private void GoToLine()
        {
            int i = 1;
            int intLine = int.Parse(toolStripTextBox2.Text);
            int intStart = 0;
            int intEnd = 0;

            foreach (Match m in Regex.Matches(txtEdit.Text, "^.*?$", RegexOptions.Multiline))
            {
                if (i == intLine)
                {
                    intStart = m.Index;
                    intEnd = intStart + m.Length;
                    break;
                }
                i++;
            }

            txtEdit.Select(intStart, intEnd - intStart);
            txtEdit.ScrollToCaret();
            txtEdit.Focus();
        }

        private bool IgnoreIfContains(string strArticle, string strFind, bool Regexe, bool caseSensitive)
        {
            if (strFind.Length > 0)
            {
                RegexOptions RegOptions;

                if (caseSensitive)
                    RegOptions = RegexOptions.None;
                else
                    RegOptions = RegexOptions.IgnoreCase;

                strFind = strFind.Replace("%%title%%", EdittingArticle);

                if (!Regexe)
                    strFind = Regex.Escape(strFind);

                if (Regex.IsMatch(strArticle, strFind, RegOptions))
                    return true;
                else
                    return false;
            }
            else
                return false;
        }


        private bool IgnoreIfDoesntContain(string strArticle, string strFind, bool Regexe, bool caseSensitive)
        {
            if (strFind.Length > 0)
            {
                RegexOptions RegOptions;

                if (caseSensitive)
                    RegOptions = RegexOptions.None;
                else
                    RegOptions = RegexOptions.IgnoreCase;

                strFind = strFind.Replace("%%title%%", EdittingArticle);

                if (!Regexe)
                    strFind = Regex.Escape(strFind);

                if (Regex.IsMatch(strArticle, strFind, RegOptions))
                    return false;
                else
                    return true;
            }
            else
                return false;
        }

        [Conditional("DEBUG")]
        public void Debug()
        {//stop logging in when de-bugging
            lbArticles.Items.Add("User:Bluemoose/Sandbox");
            wikiStatusBool = true;
            chkAutoMode.Enabled = true;
            chkQuickSave.Enabled = true;
        }

        #endregion

        #region set variables

        private void selectProjectToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ProjectSelect Proj = new ProjectSelect(Variables.LangCode, Variables.Project);
            Proj.ShowDialog();
            if (Proj.DialogResult == DialogResult.OK)
            {
                wikiStatusBool = false;
                chkQuickSave.Checked = false;
                chkQuickSave.Enabled = false;
                chkAutoMode.Checked = false;
                chkAutoMode.Enabled = false;

                SetProject(Proj.Language, Proj.Project);
                if (Proj.SetAsDefault)
                {
                    AutoWikiBrowser.Properties.Settings.Default.Language = Variables.LangCode;
                    AutoWikiBrowser.Properties.Settings.Default.Project = Variables.Project;
                    AutoWikiBrowser.Properties.Settings.Default.Save();
                }
            }
            Proj = null;
        }

        private void SetProject(string LangCode, string Project)
        {
            //set namespaces
            Variables.SetProject(LangCode, Project);

            //set interwikiorder
            if (LangCode == "en" || LangCode == "pl")
                parsers.InterWikiOrder = InterWikiOrderEnum.LocalLanguageAlpha;
            else if (LangCode == "fi")
                parsers.InterWikiOrder = InterWikiOrderEnum.LocalLanguageFirstWord;
            else
                parsers.InterWikiOrder = InterWikiOrderEnum.Alphabetical;

            if (Variables.LangCode != "en" || Variables.Project != "wikipedia")
            {
                chkAutoTagger.Checked = false;
                chkGeneralParse.Checked = false;
            }
            lblProject.Text = Variables.LangCode + "." + Variables.Project;
        }

        #endregion

        #region Enabling/Disabling of buttons

        private void UpdateButtons()
        {
            NumberOfArticles = lbArticles.Items.Count;
            bool enabled = lbArticles.Items.Count > 0;
            btnStart.Enabled = enabled;
            btnFilter.Enabled = enabled;
            btnRemoveArticle.Enabled = enabled;
            btnArticlesListClear.Enabled = enabled;
            btnArticlesListSave.Enabled = enabled;
        }

        private void DisableStartButton()
        {
            btnStart.Enabled = false;
        }

        private void DisableButtons()
        {
            DisableStartButton();
            btnApply.Enabled = false;

            if (lbArticles.Items.Count == 0)
                btnIgnore.Enabled = false;

            btnPreview.Enabled = false;
            btnDiff.Enabled = false;
            btntsPreview.Enabled = false;
            btntsChanges.Enabled = false;

            btnMakeList.Enabled = false;

            btntsSave.Enabled = false;
            btntsIgnore.Enabled = false;
        }

        private void EnableButtons()
        {
            UpdateButtons();
            btnApply.Enabled = true;
            btnIgnore.Enabled = true;
            btnPreview.Enabled = true;
            btnDiff.Enabled = true;
            btntsPreview.Enabled = true;
            btntsChanges.Enabled = true;

            btnMakeList.Enabled = true;

            btntsSave.Enabled = true;
            btntsIgnore.Enabled = true;

        }

        #endregion

        #region timers

        int intHours = 0;
        int intMinutes = 0;
        int intSeconds = 0;
        int intEdits = 0;
        private void timeKeeper()
        {
            intSeconds++;
            if (intSeconds == 60)
            {
                intMinutes++;
                intSeconds = 0;
            }
            if (intMinutes == 60)
            {
                intHours++;
                intMinutes = 0;
            }
        }

        int intRestartDelay = 5;
        int intStartInSeconds = 5;
        private void DelayedRestart()
        {
            stopDelayedTimer();
            lblStatusText.Text = "Restarting in " + intStartInSeconds.ToString();

            if (intStartInSeconds == 0)
            {
                StopDelayedRestartTimer();
                Start();
            }
            else
                intStartInSeconds--;
        }
        private void StartDelayedRestartTimer()
        {
            intStartInSeconds = intRestartDelay;
            ticker += DelayedRestart;
            //increase the restart delay each time, this is decreased by 1 on each successfull save
            intRestartDelay += 3;
        }
        private void StopDelayedRestartTimer()
        {
            ticker -= DelayedRestart;
            intStartInSeconds = intRestartDelay;
        }

        private void stopDelayedTimer()
        {
            ticker -= DelayedAutoSave;
            intTimer = 0;
            lblBotTimer.Text = "Bot timer: " + intTimer.ToString();
        }

        private void startDelayedTimer()
        {
            ticker += DelayedAutoSave;
        }

        int intTimer = 0;
        private void DelayedAutoSave()
        {
            if (intTimer < nudBotSpeed.Value)
            {
                intTimer++;
                if (intTimer == 1)
                    lblBotTimer.BackColor = Color.Red;
                else
                    lblBotTimer.BackColor = DefaultBackColor;
            }
            else
            {
                stopDelayedTimer();
                SaveArticle();
            }

            lblBotTimer.Text = "Bot timer: " + intTimer.ToString();
        }

        private void showTimerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            stopSaveInterval();
        }
        int intStartTimer = 0;
        private void SaveInterval()
        {
            intStartTimer++;
            lblTimer.Text = intStartTimer.ToString();
        }
        private void stopSaveInterval()
        {
            intStartTimer = 0;
            lblTimer.Text = intStartTimer.ToString();
            ticker -= SaveInterval;
        }

        public delegate void Tick();
        public event Tick ticker;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (ticker != null)
            {
                ticker();
            }
        }

        #endregion

        #region menus and buttons

        private void launchListComparerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ListComparer lc = new ListComparer();
            lc.Show();
        }

        private void launchDumpSearcherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            launchDumpSearcher();
        }

        private void launchDumpSearcher()
        {
            DumpSearcher ds = new DumpSearcher();
            ds.foundarticle += addTo;
            ds.Show();
        }

        private delegate void AddTo(string Article);
        private void addTo(string Article)
        {
            if (this.InvokeRequired)
            {
                this.BeginInvoke(new AddTo(addTo), Article);
                return;
            }

            lbArticles.Items.Add(Article);
            UpdateButtons();
        }

        private void addIgnoredToLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnFalsePositive.Visible = addIgnoredToLogFileToolStripMenuItem.Checked;
        }

        private void alphaSortInterwikiLinksToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            parsers.sortInterwikiOrder = alphaSortInterwikiLinksToolStripMenuItem.Checked;
        }


        private void btnFalsePositive_Click(object sender, EventArgs e)
        {
            if (EdittingArticle.Length > 0)
                Tools.WriteLog("#[[" + EdittingArticle + "]]\r\n", @"False positives.txt");
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            Start();
        }

        private void btnStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void btnIgnore_Click(object sender, EventArgs e)
        {
            SkipPage();
        }

        private void MainForm_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Escape && btnStop.Enabled)
            {
                Stop();
                e.SuppressKeyPress = true;
                return;
            }

            if (e.Modifiers == Keys.Control)
            {
                if (e.KeyCode == Keys.S && btnApply.Enabled)
                {
                    Save();
                    e.SuppressKeyPress = true;
                    return;
                }
                else if (e.KeyCode == Keys.S && btnStart.Enabled)
                {
                    Start();
                    e.SuppressKeyPress = true;
                    return;
                }
                if (e.KeyCode == Keys.I && btnIgnore.Enabled)
                {
                    SkipPage();
                    e.SuppressKeyPress = true;
                    return;
                }
                if (e.KeyCode == Keys.F)
                {
                    find(txtFind.Text, chkFindRegex.Checked, chkFindCaseSensitive.Checked);
                    e.SuppressKeyPress = true;
                    return;
                }
            }
        }

        private void cmbEditSummary_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter && !cmboEditSummary.Items.Contains(cmboEditSummary.Text))
            {
                e.SuppressKeyPress = true;
                cmboEditSummary.Items.Add(cmboEditSummary.Text);
            }
        }

        private void txtSelectSource_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                e.Handled = true;
                btnMakeList.PerformClick();
            }
        }
        private void txtNewArticle_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (e.KeyChar == '\r')
            {
                e.Handled = true;
                btnAdd.PerformClick();
            }
        }

        private void listToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = Tools.HTMLToWiki(txtEdit.SelectedText, "*");
        }

        private void listToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = Tools.HTMLToWiki(txtEdit.SelectedText, "#");
        }

        private void cutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Cut();
        }

        private void copyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Copy();
        }

        private void pasteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Paste();
        }

        private void selectAllToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectAll();
        }

        private void undoToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Undo();
        }

        private void wikifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = "{{Wikify-date|{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}\r\n\r\n" + txtEdit.Text;
        }

        private void cleanupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = "{{cleanup-date|{{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}\r\n\r\n" + txtEdit.Text;
        }

        private void expandToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = "{{Expand}}\r\n\r\n" + txtEdit.Text;
        }

        private void speedyDeleteToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = "{{Delete}}\r\n\r\n" + txtEdit.Text;
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = "{{subst:clear}}";
        }

        private void uncategorisedToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = "[[Category:Category needed]]";
        }

        private void unicodifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string text = txtEdit.SelectedText;
            text = parsers.Unicodify(text);
            txtEdit.SelectedText = text;
        }

        private void btnFilterList_Click(object sender, EventArgs e)
        {
            FilterArticles();
        }

        private void filterOutNonMainSpaceArticlesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FilterArticles();
        }

        private void sortAlphebeticallyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lbArticles.Sorted = true;
            lbArticles.Sorted = false;
        }

        private void clearTheListToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lbArticles.Items.Clear();            
            UpdateButtons();
        }

        private void saveListToTextFileToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            SaveList();
        }

        private void saveListToTextFileToolStripMenuItem_Click_1(object sender, EventArgs e)
        {
            SaveList();
        }

        private void filterOutNonMainSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            FilterArticles();
        }

        private void sortAlphabeticallyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            lbArticles.Sorted = true;
            lbArticles.Sorted = false;
        }

        private void removeToolStripMenuItem_Click(object sender, EventArgs e)
        {
            btnRemoveArticle.PerformClick();
        }

        private void mnuListBox_Opening(object sender, CancelEventArgs e)
        {
            bool boolEnabled = lbArticles.Items.Count > 0;

            if (lbArticles.SelectedItems.Count == 1)
            {
                addSelectedToListToolStripMenuItem.Enabled = true;

                if (lbArticles.SelectedItem.ToString().StartsWith(Variables.Namespaces[14]))
                    fromCategoryToolStripMenuItem.Enabled = true;
                else
                    fromCategoryToolStripMenuItem.Enabled = false;

                if (lbArticles.SelectedItem.ToString().StartsWith(Variables.Namespaces[6]))
                    fromImageLinksToolStripMenuItem.Enabled = true;
                else
                    fromImageLinksToolStripMenuItem.Enabled = false;
            }
            else
                addSelectedToListToolStripMenuItem.Enabled = false;

            removeToolStripMenuItem.Enabled = lbArticles.SelectedItem != null;
            clearToolStripMenuItem1.Enabled = boolEnabled;
            filterOutNonMainSpaceArticlesToolStripMenuItem.Enabled = boolEnabled;
            convertToTalkPagesToolStripMenuItem.Enabled = boolEnabled;
            convertFromTalkPagesToolStripMenuItem.Enabled = boolEnabled;
            sortAlphebeticallyMenuItem.Enabled = boolEnabled;
            saveListToTextFileToolStripMenuItem1.Enabled = boolEnabled;
            specialFilterToolStripMenuItem.Enabled = boolEnabled;
        }

        private void clearToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            btnArticlesListClear.PerformClick();
        }

        private void metadataTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = "{{Persondata\r\n|NAME=\r\n|ALTERNATIVE NAMES=\r\n|SHORT DESCRIPTION=\r\n|DATE OF BIRTH=\r\n|PLACE OF BIRTH=\r\n|DATE OF DEATH=\r\n|PLACE OF DEATH=\r\n}}";
        }

        private void humanNameCategoryKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = Tools.MakeHumanCatKey(EdittingArticle);
        }

        private void birthdeathCatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //find first dates
            string strBirth = "";
            string strDeath = "";
            ArrayList dates = new ArrayList();
            string pattern = "[1-2][0-9]{3}";

            try
            {
                foreach (Match m in Regex.Matches(txtEdit.Text, pattern))
                {
                    string s = m.ToString();
                    dates.Add(s);
                }

                if (dates.Count >= 1)
                    strBirth = dates[0].ToString();
                if (dates.Count >= 2)
                    strDeath = dates[1].ToString();

                //make name, surname, firstname
                string strName = Tools.MakeHumanCatKey(EdittingArticle);

                string Categories = "";

                if (strDeath.Length == 0 || int.Parse(strDeath) < int.Parse(strBirth) + 20)
                    Categories = "[[Category:" + strBirth + " births|" + strName + "]]";
                else
                    Categories = "[[Category:" + strBirth + " births|" + strName + "]]\r\n[[Category:" + strDeath + " deaths|" + strName + "]]";

                txtEdit.SelectedText = Categories;
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void stubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = toolStripTextBox1.Text;
        }
        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            if (txtEdit.SelectedText.Length > 0)
            {
                cutToolStripMenuItem.Enabled = true;
                copyToolStripMenuItem.Enabled = true;
            }
            else
            {
                cutToolStripMenuItem.Enabled = false;
                copyToolStripMenuItem.Enabled = false;
            }

            undoToolStripMenuItem.Enabled = txtEdit.CanUndo;

            if (EdittingArticle.Length > 0)
                openPageInBrowserToolStripMenuItem.Enabled = true;
            else
                openPageInBrowserToolStripMenuItem.Enabled = false;

            if (LastArticle.Length > 0)
                replaceTextWithLastEditToolStripMenuItem.Enabled = true;
            else
                replaceTextWithLastEditToolStripMenuItem.Enabled = false;
        }

        private void openPageInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start(Variables.URL + "index.php?title=" + EdittingArticle);
        }

        private void previewInsteadOfDiffToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chkSkipNoChanges.Enabled = !previewInsteadOfDiffToolStripMenuItem.Checked;
        }

        private void chkGeneralParse_CheckedChanged(object sender, EventArgs e)
        {
            alphaSortInterwikiLinksToolStripMenuItem.Enabled = chkGeneralParse.Checked;
        }

        private void btnFindAndReplaceAdvanced_Click(object sender, EventArgs e)
        {
            if (!replaceSpecial.Visible)
                replaceSpecial.ShowDialog();
            else
                replaceSpecial.Hide();
        }

        private void btnMoreFindAndReplce_Click(object sender, EventArgs e)
        {
            if (!findAndReplace.Visible)
                findAndReplace.ShowDialog();
            else
                findAndReplace.Hide();
        }

        private void Stop()
        {
            UpdateButtons();
            if (intTimer > 0)
            {//stop and reset the bot timer.
                stopDelayedTimer();
                EnableButtons();
                return;
            }

            if (ListerThread != null)
                ListerThread.Abort();

            stopSaveInterval();
            StopDelayedRestartTimer();
            if (webBrowserEdit.IsBusy)
                webBrowserEdit.Stop();

            webBrowserLogin.Stop();
            lblStatusText.Text = "Stopped";

            StopProgressBar();

            UpdateButtons();
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            System.Diagnostics.Process.Start("http://en.wikipedia.org/wiki/Wikipedia:AutoWikiBrowser#User_manual");
        }

        private void reparseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            bool b = true;
            txtEdit.Text = Process(txtEdit.Text, ref b);
        }

        private void replaceTextWithLastEditToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (LastArticle.Length > 0)
                txtEdit.Text = LastArticle;
        }

        private void PasteMore1_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore1.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore2_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore2.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore3_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore3.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore4_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore4.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore5_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore5.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore6_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore6.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore7_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore7.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore8_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore8.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore9_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore9.Text;
            mnuTextBox.Hide();
        }

        private void PasteMore10_DoubleClick(object sender, EventArgs e)
        {
            txtEdit.SelectedText = PasteMore10.Text;
            mnuTextBox.Hide();
        }

        private void removeAllExcessWhitespaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = parsers.RemoveAllWhiteSpace(txtEdit.Text);
        }

        private void txtSelectSource_DoubleClick(object sender, EventArgs e)
        {
            txtSelectSource.SelectAll();
        }

        private void txtNewArticle_DoubleClick(object sender, EventArgs e)
        {
            txtNewArticle.SelectAll();
        }

        private void txtNewCategory_DoubleClick(object sender, EventArgs e)
        {
            txtNewCategory.SelectAll();
        }

        private void btnArticlesListSave_Click(object sender, EventArgs e)
        {
            SaveList();
        }

        #endregion

        #region save and load settings
        private void saveSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            saveSettings();
        }

        private void loadSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            loadSettingsDialog();
        }

        private void loadDefaultSettingsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ResetSettings();
        }

        private void ResetSettings()
        {
            findAndReplace.Clear();
            replaceSpecial.Clear();
            cmboSourceSelect.SelectedIndex = 0;
            txtSelectSource.Text = "";

            chkGeneralParse.Checked = true;
            chkAutoTagger.Checked = true;
            chkUnicodifyWhole.Checked = true;

            chkFindandReplace.Checked = false;
            chkIgnoreWhenNoFAR.Checked = true;
            findAndReplace.isRegex = false;
            findAndReplace.caseSensitive = false;
            findAndReplace.isMulti = false;
            findAndReplace.isSingle = false;

            cmboCategorise.SelectedIndex = 0;
            txtNewCategory.Text = "";

            chkIgnoreIfContains.Checked = false;
            chkOnlyIfContains.Checked = false;
            chkIgnoreIsRegex.Checked = false;
            chkIgnoreCaseSensitive.Checked = false;
            txtIgnoreIfContains.Text = "";
            txtOnlyIfContains.Text = "";

            chkAppend.Checked = false;
            rdoAppend.Checked = true;
            txtAppendMessage.Text = "";

            cmboImages.SelectedIndex = 0;
            txtImageReplace.Text = "";
            txtImageWith.Text = "";

            txtFind.Text = "";
            chkFindRegex.Checked = false;
            chkFindCaseSensitive.Checked = false;

            cmboEditSummary.SelectedIndex = 0;

            wordWrapToolStripMenuItem1.Checked = true;
            panel2.Show();
            enableToolBar = false;
            bypassRedirectsToolStripMenuItem.Checked = true;
            ignoreNonexistentPagesToolStripMenuItem.Checked = true;
            doNotAutomaticallyDoAnythingToolStripMenuItem.Checked = false;
            chkSkipNoChanges.Checked = false;
            previewInsteadOfDiffToolStripMenuItem.Checked = false;
            markAllAsMinorToolStripMenuItem.Checked = false;
            addAllToWatchlistToolStripMenuItem.Checked = false;
            showTimerToolStripMenuItem.Checked = false;
            alphaSortInterwikiLinksToolStripMenuItem.Checked = true;
            addIgnoredToLogFileToolStripMenuItem.Checked = false;

            PasteMore1.Text = "";
            PasteMore2.Text = "";
            PasteMore3.Text = "";
            PasteMore4.Text = "";
            PasteMore5.Text = "";
            PasteMore6.Text = "";
            PasteMore7.Text = "";
            PasteMore8.Text = "";
            PasteMore9.Text = "";
            PasteMore10.Text = "";

            chkAutoMode.Checked = false;
            chkQuickSave.Checked = false;
            nudBotSpeed.Value = 15;

            lblStatusText.Text = "Default settings loaded.";
        }

        private void loadSettingsDialog()
        {
            if (openXML.ShowDialog() != DialogResult.OK)
                return;
            loadSettings(openXML.FileName);
        }

        private void loadDefaultSettings()
        {//load Default.xml file if it exists
            try
            {
                string filename = Environment.CurrentDirectory + "\\Default.xml";

                if (File.Exists(filename))
                    loadSettings(filename);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void loadSettings(string filename)
        {
            try
            {
                strSettingsFile = " - " + filename.Remove(0, filename.LastIndexOf("\\") + 1);
                this.Text = "AutoWikiBrowser" + strSettingsFile;

                Stream stream = new FileStream(filename, FileMode.Open);
                findAndReplace.Clear();
                cmboEditSummary.Items.Clear();

                using (XmlTextReader reader = new XmlTextReader(stream))
                {
                    reader.WhitespaceHandling = WhitespaceHandling.None;
                    while (reader.Read())
                    {
                        if (reader.Name == "datagridFAR" && reader.HasAttributes)
                        {
                            string find = "";
                            string replace = "";

                            reader.MoveToAttribute("find");
                            find = reader.Value;
                            reader.MoveToAttribute("replacewith");
                            replace = reader.Value;

                            if (find.Length > 0)
                                findAndReplace.AddNew(find, replace);

                            continue;
                        }

                        if (reader.Name == WikiFunctions.MWB.ReplaceSpecial.XmlName)
                        {
                            bool enabled = false;
                            replaceSpecial.ReadFromXml(reader, ref enabled);
                            continue;
                        }

                        if (reader.Name == "projectlang" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("proj");
                            string project = reader.Value;
                            reader.MoveToAttribute("lang");
                            string language = reader.Value;
                            SetProject(language, project);

                            continue;
                        }
                        if (reader.Name == "selectsource" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("index");
                            cmboSourceSelect.SelectedIndex = int.Parse(reader.Value);
                            reader.MoveToAttribute("text");
                            txtSelectSource.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "general" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("general");
                            chkGeneralParse.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("tagger");
                            chkAutoTagger.Checked = bool.Parse(reader.Value);
                            //reader.MoveToAttribute("whitespace");
                            //chkRemoveWhiteSpace.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("unicodifyer");
                            chkUnicodifyWhole.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "findandreplace" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            chkFindandReplace.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("regex");
                            findAndReplace.isRegex = bool.Parse(reader.Value);
                            reader.MoveToAttribute("casesensitive");
                            findAndReplace.caseSensitive = bool.Parse(reader.Value);
                            reader.MoveToAttribute("multiline");
                            findAndReplace.isMulti = bool.Parse(reader.Value);
                            reader.MoveToAttribute("singleline");
                            findAndReplace.isSingle = bool.Parse(reader.Value);
                            if (reader.AttributeCount > 5)
                            {
                                reader.MoveToAttribute("ignorenofar");
                                chkIgnoreWhenNoFAR.Checked = bool.Parse(reader.Value);
                            }

                            continue;
                        }
                        if (reader.Name == "categorisation" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("index");
                            cmboCategorise.SelectedIndex = int.Parse(reader.Value);
                            reader.MoveToAttribute("text");
                            txtNewCategory.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "skip" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("does");
                            chkIgnoreIfContains.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("doesnot");
                            chkOnlyIfContains.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("regex");
                            chkIgnoreIsRegex.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("casesensitive");
                            chkIgnoreCaseSensitive.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("doestext");
                            txtIgnoreIfContains.Text = reader.Value;
                            reader.MoveToAttribute("doesnottext");
                            txtOnlyIfContains.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "message" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            chkAppend.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("text");
                            txtAppendMessage.Text = reader.Value;
                            if (reader.AttributeCount > 2)
                            {
                                reader.MoveToAttribute("append");
                                rdoAppend.Checked = bool.Parse(reader.Value);
                            }

                            continue;
                        }
                        if (reader.Name == "imager" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("index");
                            cmboImages.SelectedIndex = int.Parse(reader.Value);
                            reader.MoveToAttribute("replace");
                            txtImageReplace.Text = reader.Value;
                            reader.MoveToAttribute("with");
                            txtImageWith.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "find" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            txtFind.Text = reader.Value;
                            reader.MoveToAttribute("regex");
                            chkFindRegex.Checked = bool.Parse(reader.Value);
                            reader.MoveToAttribute("casesensitive");
                            chkFindCaseSensitive.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "summary" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            if (!cmboEditSummary.Items.Contains(reader.Value) && reader.Value.Length > 0)
                                cmboEditSummary.Items.Add(reader.Value);

                            continue;
                        }
                        if (reader.Name == "summaryindex" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("index");
                            cmboEditSummary.Text = reader.Value;

                            continue;
                        }

                        //menu
                        if (reader.Name == "wordwrap" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            wordWrapToolStripMenuItem1.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "toolbar" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            enableToolBar = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "bypass" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            bypassRedirectsToolStripMenuItem.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "ingnorenonexistent" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            ignoreNonexistentPagesToolStripMenuItem.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "noautochanges" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            doNotAutomaticallyDoAnythingToolStripMenuItem.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "skipnochanges" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            chkSkipNoChanges.Checked = bool.Parse(reader.Value);
                        }
                        if (reader.Name == "preview" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            previewInsteadOfDiffToolStripMenuItem.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "minor" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            markAllAsMinorToolStripMenuItem.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "watch" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            addAllToWatchlistToolStripMenuItem.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "timer" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            showTimerToolStripMenuItem.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "sortinterwiki" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            alphaSortInterwikiLinksToolStripMenuItem.Checked = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "addignoredtolog" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("enabled");
                            addIgnoredToLogFileToolStripMenuItem.Checked = bool.Parse(reader.Value);
                            btnFalsePositive.Visible = bool.Parse(reader.Value);

                            continue;
                        }
                        if (reader.Name == "pastemore1" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore1.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore2" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore2.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore3" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore3.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore4" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore4.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore5" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore5.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore6" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore6.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore7" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore7.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore8" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore8.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore9" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore9.Text = reader.Value;

                            continue;
                        }
                        if (reader.Name == "pastemore10" && reader.HasAttributes)
                        {
                            reader.MoveToAttribute("text");
                            PasteMore10.Text = reader.Value;

                            continue;
                        }
                    }
                    stream.Close();
                    lblStatusText.Text = "Settings successfully loaded";
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void saveSettings()
        {
            try
            {
                if (saveXML.ShowDialog() != DialogResult.OK)
                    return;

                strSettingsFile = " - " + saveXML.FileName.Remove(0, saveXML.FileName.LastIndexOf("\\") + 1);
                this.Text = "AutoWikiBrowser" + strSettingsFile;

                XmlTextWriter textWriter = new XmlTextWriter(saveXML.FileName, UTF8Encoding.UTF8);
                // Opens the document
                textWriter.Formatting = Formatting.Indented;
                textWriter.WriteStartDocument();

                // Write first element
                textWriter.WriteStartElement("Settings");
                textWriter.WriteAttributeString("program", "AWB");
                textWriter.WriteAttributeString("schema", "1");

                textWriter.WriteStartElement("Project");
                textWriter.WriteStartElement("projectlang");
                textWriter.WriteAttributeString("proj", Variables.Project);
                textWriter.WriteAttributeString("lang", Variables.LangCode);
                textWriter.WriteEndElement();
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("Options");

                textWriter.WriteStartElement("selectsource");
                textWriter.WriteAttributeString("index", cmboSourceSelect.SelectedIndex.ToString());
                textWriter.WriteAttributeString("text", txtSelectSource.Text);
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("general");
                textWriter.WriteAttributeString("general", chkGeneralParse.Checked.ToString());
                textWriter.WriteAttributeString("tagger", chkAutoTagger.Checked.ToString());
                //textWriter.WriteAttributeString("whitespace", chkRemoveWhiteSpace.Checked.ToString());
                textWriter.WriteAttributeString("unicodifyer", chkUnicodifyWhole.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("categorisation");
                textWriter.WriteAttributeString("index", cmboCategorise.SelectedIndex.ToString());
                textWriter.WriteAttributeString("text", txtNewCategory.Text);
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("skip");
                textWriter.WriteAttributeString("does", chkIgnoreIfContains.Checked.ToString());
                textWriter.WriteAttributeString("doesnot", chkOnlyIfContains.Checked.ToString());
                textWriter.WriteAttributeString("regex", chkIgnoreIsRegex.Checked.ToString());
                textWriter.WriteAttributeString("casesensitive", chkIgnoreCaseSensitive.Checked.ToString());
                textWriter.WriteAttributeString("doestext", txtIgnoreIfContains.Text);
                textWriter.WriteAttributeString("doesnottext", txtOnlyIfContains.Text);
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("message");
                textWriter.WriteAttributeString("enabled", chkAppend.Checked.ToString());
                textWriter.WriteAttributeString("text", txtAppendMessage.Text);
                textWriter.WriteAttributeString("append", rdoAppend.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("imager");
                textWriter.WriteAttributeString("index", cmboImages.SelectedIndex.ToString());
                textWriter.WriteAttributeString("replace", txtImageReplace.Text);
                textWriter.WriteAttributeString("with", txtImageWith.Text);
                textWriter.WriteEndElement();
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("FindAndReplaceSettings");
                findAndReplace.WriteToXml(textWriter, chkFindandReplace.Checked, chkIgnoreWhenNoFAR.Checked);
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("FindAndReplace");
                replaceSpecial.WriteToXml(textWriter, chkFindandReplace.Checked);
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("startoptions");
                int j = 0;
                while (j < cmboEditSummary.Items.Count)
                {
                    textWriter.WriteStartElement("summary");
                    textWriter.WriteAttributeString("text", cmboEditSummary.Items[j].ToString());
                    textWriter.WriteEndElement();
                    j++;
                }

                if (!cmboEditSummary.Items.Contains(cmboEditSummary.Text))
                {
                    textWriter.WriteStartElement("summary");
                    textWriter.WriteAttributeString("text", cmboEditSummary.Text);
                    textWriter.WriteEndElement();
                }

                textWriter.WriteStartElement("summaryindex");
                textWriter.WriteAttributeString("index", cmboEditSummary.Text);
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("find");
                textWriter.WriteAttributeString("text", txtFind.Text);
                textWriter.WriteAttributeString("regex", chkFindRegex.Checked.ToString());
                textWriter.WriteAttributeString("casesensitive", chkFindCaseSensitive.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("menu");

                textWriter.WriteStartElement("wordwrap");
                textWriter.WriteAttributeString("enabled", wordWrapToolStripMenuItem1.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("toolbar");
                textWriter.WriteAttributeString("enabled", enableToolBar.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("bypass");
                textWriter.WriteAttributeString("enabled", bypassRedirectsToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("ingnorenonexistent");
                textWriter.WriteAttributeString("enabled", ignoreNonexistentPagesToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("noautochanges");
                textWriter.WriteAttributeString("enabled", doNotAutomaticallyDoAnythingToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("skipnochanges");
                textWriter.WriteAttributeString("enabled", chkSkipNoChanges.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("preview");
                textWriter.WriteAttributeString("enabled", previewInsteadOfDiffToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("minor");
                textWriter.WriteAttributeString("enabled", markAllAsMinorToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("watch");
                textWriter.WriteAttributeString("enabled", addAllToWatchlistToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("timer");
                textWriter.WriteAttributeString("enabled", showTimerToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("sortinterwiki");
                textWriter.WriteAttributeString("enabled", alphaSortInterwikiLinksToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("addignoredtolog");
                textWriter.WriteAttributeString("enabled", addIgnoredToLogFileToolStripMenuItem.Checked.ToString());
                textWriter.WriteEndElement();
                textWriter.WriteEndElement();
                textWriter.WriteEndElement();

                textWriter.WriteStartElement("pastemore");

                textWriter.WriteStartElement("pastemore1");
                textWriter.WriteAttributeString("text", PasteMore1.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore2");
                textWriter.WriteAttributeString("text", PasteMore2.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore3");
                textWriter.WriteAttributeString("text", PasteMore3.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore4");
                textWriter.WriteAttributeString("text", PasteMore4.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore5");
                textWriter.WriteAttributeString("text", PasteMore5.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore6");
                textWriter.WriteAttributeString("text", PasteMore6.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore7");
                textWriter.WriteAttributeString("text", PasteMore7.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore8");
                textWriter.WriteAttributeString("text", PasteMore8.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore9");
                textWriter.WriteAttributeString("text", PasteMore9.Text);
                textWriter.WriteEndElement();
                textWriter.WriteStartElement("pastemore10");
                textWriter.WriteAttributeString("text", PasteMore10.Text);
                textWriter.WriteEndElement();

                textWriter.WriteEndElement();
                textWriter.WriteEndElement();

                // Ends the document.
                textWriter.WriteEndDocument();
                // close writer
                textWriter.Close();
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            lblStatusText.Text = "Settings successfully saved";
        }

        #endregion

        #region tool bar stuff

        private void btnShowHide_Click(object sender, EventArgs e)
        {
            if (panel2.Visible)
            {
                panel2.Hide();
            }
            else
            {
                panel2.Show();
            }
            setBrowserSize();
        }

        private void btntsSave_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void btntsIgnore_Click(object sender, EventArgs e)
        {
            SkipPage();
        }

        private void btntsStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void btntsPreview_Click(object sender, EventArgs e)
        {
            ShowPreview();
        }

        private void btntsChanges_Click(object sender, EventArgs e)
        {
            ShowDiff();
        }

        private void setBrowserSize()
        {
            if (toolStrip.Visible)
            {
                webBrowserEdit.Location = new Point(webBrowserEdit.Location.X, 48);
                if (panel2.Visible)
                    webBrowserEdit.Height = panel2.Location.Y - 48;
                else
                    webBrowserEdit.Height = statusStrip1.Location.Y - 48;

            }
            else
            {
                webBrowserEdit.Location = new Point(webBrowserEdit.Location.X, 25);
                if (panel2.Visible)
                    webBrowserEdit.Height = panel2.Location.Y - 25;
                else
                    webBrowserEdit.Height = statusStrip1.Location.Y - 25;
            }
        }

        private void enableTheToolbarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            enableToolBar = enableTheToolbarToolStripMenuItem.Checked;
        }

        private bool boolEnableToolbar = false;
        private bool enableToolBar
        {
            get { return boolEnableToolbar; }
            set
            {
                if (value == true)
                    toolStrip.Show();
                else
                    toolStrip.Hide();
                setBrowserSize();
                enableTheToolbarToolStripMenuItem.Checked = value;
                boolEnableToolbar = value;
            }
        }

        #endregion

        #region Images

        private void cmboImages_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmboImages.SelectedIndex == 0)
            {
                lblImageReplace.Text = "";
                lblImageWith.Text = "";
                txtImageWith.Visible = true;
                txtImageReplace.Enabled = false;
                txtImageWith.Enabled = false;
            }
            else if (cmboImages.SelectedIndex == 1)
            {
                lblImageReplace.Text = "Replace image:";
                lblImageWith.Text = "With Image:";

                txtImageWith.Visible = true;
                txtImageReplace.Enabled = true;
                txtImageWith.Enabled = true;

            }
            else
            {
                lblImageReplace.Text = "Remove image:";
                lblImageWith.Text = "";
                txtImageWith.Visible = false;
                txtImageReplace.Enabled = true;
            }
        }

        private void txtImageReplace_Leave(object sender, EventArgs e)
        {
            txtImageReplace.Text = Regex.Replace(txtImageReplace.Text, "^" + Variables.Namespaces[6], "", RegexOptions.IgnoreCase);
        }

        private void txtImageWith_Leave(object sender, EventArgs e)
        {
            txtImageWith.Text = Regex.Replace(txtImageWith.Text, "^" + Variables.Namespaces[6], "", RegexOptions.IgnoreCase);
        }

        #endregion
    }
}