﻿/*
Autowikibrowser
Copyright (C) 2007 Martin Richards
(C) 2007 Stephen Kennedy (Kingboyk) http://www.sdk-software.com/

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
using WikiFunctions.Plugin;
using WikiFunctions.Parse;
using WikiFunctions.Lists;
using WikiFunctions.Logging;
using WikiFunctions.Properties;
using WikiFunctions.Browser;
using WikiFunctions.Controls;
using System.Collections.Specialized;
using WikiFunctions.Background;
using System.Security.Permissions;
using WikiFunctions.Controls.Lists;
using AutoWikiBrowser.Plugins;

[assembly: CLSCompliant(true)]
namespace AutoWikiBrowser
{
    [PermissionSet(SecurityAction.Demand, Name = "FullTrust")]
    [System.Runtime.InteropServices.ComVisibleAttribute(true)]
    public sealed partial class MainForm : Form, IAutoWikiBrowser
    { // this class needs to be public, otherwise we get an exception which recommends setting ComVisibleAttribute to true (which we've already done)
        #region Fields
            private static Splash splash = new Splash();
            private static WikiFunctions.AWBProfiles.AWBProfilesForm profiles;
        
            private static bool Abort = false;

            private Profiler prof = new Profiler();
        
            private static string LastArticle = "";
            private static string SettingsFile = "";
            private static string LastMove = "";
            private static string LastDelete = "";
            private static string LastProtect = "";

            private static int oldselection = 0;
            private static int retries = 0;

            private static bool PageReload = false;
            private static int mnudges = 0;
            private static int sameArticleNudges = 0;

            private static bool boolSaved = true;
            private static HideText RemoveText = new HideText(false, true, false);
            private static List<string> noParse = new List<string>();
            private static FindandReplace findAndReplace = new FindandReplace();
            private static SubstTemplates substTemplates = new SubstTemplates();
            private static RegExTypoFix RegexTypos;
            private static SkipOptions Skip = new SkipOptions();
            private static WikiFunctions.MWB.ReplaceSpecial replaceSpecial = 
                new WikiFunctions.MWB.ReplaceSpecial();
            private static Parsers parsers;
            private static TimeSpan StartTime = 
                new TimeSpan(DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            private static StringCollection RecentList = new StringCollection();
            private static CustomModule cModule = new CustomModule();
            internal static RegexTester regexTester = new RegexTester();
            private static bool userTalkWarningsLoaded = false;
            private static Regex userTalkTemplatesRegex;
            private static bool mErrorGettingLogInStatus;
            private static bool skippable = true;

            private ListComparer lc;
            private ListSplitter splitter;

            private static readonly Regex DiffIdParser = new Regex(@"[a-z](-?\d*)x(-?\d*)");
            private static Help h = new Help();
        #endregion

        #region Constructor and MainForm load/resize
            public MainForm()
            {
                splash.Show(this);
                RightToLeft = System.Globalization.CultureInfo.CurrentCulture.TextInfo.IsRightToLeft 
                    ? RightToLeft.Yes : RightToLeft.No;
                InitializeComponent();
                splash.setProgress(5);
                try
                {
                    lblUserName.Alignment = ToolStripItemAlignment.Right;
                    lblProject.Alignment = ToolStripItemAlignment.Right;
                    lblTimer.Alignment = ToolStripItemAlignment.Right;
                    lblEditsPerMin.Alignment = ToolStripItemAlignment.Right;
                    lblIgnoredArticles.Alignment = ToolStripItemAlignment.Right;
                    lblEditCount.Alignment = ToolStripItemAlignment.Right;

                    btntsShowHide.Image = Res.btnshowhide_image;
                    btntsShowHideParameters.Image = Res.btnshowhideparameters_image;
                    btntsSave.Image = Res.btntssave_image;
                    btntsIgnore.Image = Res.GoLtr;
                    btntsStop.Image = Res.Stop;
                    btntsPreview.Image = Res.preview;
                    btntsChanges.Image = Res.changes;
                    btntsFalsePositive.Image = Res.RolledBack;
                    btntsStart.Image = Res.Run;

                    //btnSave.Image = Res.btntssave_image;
                    //btnIgnore.Image = Res.GoLtr;

                    //btnDiff.Image = Res.changes;
                    //btnPreview.Image = Res.preview;
                    splash.setProgress(15);
                    int stubcount = 500;
                    bool catkey = false;
                    try
                    {
                        stubcount = AutoWikiBrowser.Properties.Settings.Default.StubMaxWordCount;
                        catkey = AutoWikiBrowser.Properties.Settings.Default.AddHummanKeyToCats;
                        parsers = new Parsers(stubcount, catkey);
                    }
                    catch (Exception ex)
                    {
                        parsers = new Parsers();
                        ErrorHandler.Handle(ex);
                    }

                    toolStripComboOnLoad.SelectedIndex = 0;
                    cmboCategorise.SelectedIndex = 0;
                    cmboImages.SelectedIndex = 0;
                    lblStatusText.AutoSize = true;
                    lblBotTimer.AutoSize = true;

                    Variables.User.UserNameChanged += UpdateUserName;
                    Variables.User.BotStatusChanged += UpdateBotStatus;
                    Variables.User.AdminStatusChanged += UpdateAdminStatus;
                    Variables.User.WikiStatusChanged += UpdateWikiStatus; 

                    Variables.User.webBrowserLogin.DocumentCompleted += web4Completed;
                    Variables.User.webBrowserLogin.Navigating += web4Starting;

                    webBrowserEdit.Loaded += CaseWasLoad;
                    webBrowserEdit.Saved += CaseWasSaved;
                    webBrowserEdit.None += CaseWasNull;
                    webBrowserEdit.Fault += StartDelayedRestartTimer;
                    webBrowserEdit.StatusChanged += UpdateWebBrowserStatus;
                    splash.setProgress(60);
                    listMaker1.BusyStateChanged += SetProgressBar;
                    listMaker1.NoOfArticlesChanged += UpdateButtons;
                    listMaker1.StatusTextChanged += UpdateListStatus;
                    Text = "AutoWikiBrowser - Default.xml";
                    splash.setProgress(25);

                    WikiFunctions.AWBProfiles.AWBProfiles.ResetTempPassword();
                }
                catch (Exception ex)
                {
                    ErrorHandler.Handle(ex);
                }
            }
            
            private void MainForm_Load(object sender, EventArgs e)
            {
                splash.setProgress(60);
                lblStatusText.Text = "Initialising...";
                Application.DoEvents();
                Variables.MainForm = this;
                updateUpdater();

                GlobalObjects.MyTrace.LS = loggingSettings1;

                try
                {
                    //check that we are not using an old OS. 98 seems to mangled some unicode
                    if (Environment.OSVersion.Version.Major < 5)
                    {
                        MessageBox.Show("You appear to be using an older operating system, this software may have trouble with some unicode fonts on operating systems older than Windows 2000, the start button has been disabled.", "Operating system", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                        SetStartButton(false);
                    }
                    else
                        listMaker1.MakeListEnabled = true;

                    splash.setProgress(80);
                    if (AutoWikiBrowser.Properties.Settings.Default.LogInOnStart)
                        CheckStatus(false);

                    LogControl1.Initialise(listMaker1);

                    if (Properties.Settings.Default.WindowLocation != null)
                        this.Location = Properties.Settings.Default.WindowLocation;

                    if (Properties.Settings.Default.WindowSize != null)
                        this.Size = Properties.Settings.Default.WindowSize;

                    Debug();
                    pluginsToolStripMenuItem.Visible = Plugin.LoadPlugins(this);
                    LoadPrefs();
                    UpdateButtons();
                    LoadRecentSettingsList();
                    splash.setProgress(90);
                    if (Variables.User.checkEnabled() == WikiStatusResult.OldVersion)
                        oldVersion();

                    webBrowserDiff.Navigate("about:blank");
                    webBrowserDiff.ObjectForScripting = this;
                }
                catch (Exception ex)
                {
                    ErrorHandler.Handle(ex);
                }
                    
                lblStatusText.Text = "";
                splash.Close();
            }

            private void MainForm_Resize(object sender, EventArgs e)
            {
                if ((Minimize) && (this.WindowState == FormWindowState.Minimized))
                    this.Visible = false;
            }
        #endregion

        #region Properties

        ArticleEx stredittingarticle; // = new ArticleWithLogging(""); 
        internal ArticleEx TheArticle
        {
            get { return stredittingarticle; }
            private set { stredittingarticle = value; }
        }

        private bool BotMode
        {
            get { return chkAutoMode.Checked; }
            set { chkAutoMode.Checked = value; }
        }

        bool bLowThreadPriority = false;
        private bool LowThreadPriority
        {
            get { return bLowThreadPriority; }
            set
            {
                bLowThreadPriority = value;
                if (value)
                    Thread.CurrentThread.Priority = ThreadPriority.Lowest;
                else
                    Thread.CurrentThread.Priority = ThreadPriority.Normal;
            }
        }

        bool bFlash = false;
        private bool Flash
        {
            get { return bFlash; }
            set { bFlash = value; }
        }

        bool bBeep = false;
        private bool Beep
        {
            get { return bBeep; }
            set { bBeep = value; }
        }

        bool bMinimize = false;
        private bool Minimize
        {
            get { return bMinimize; }
            set { bMinimize = value; }
        }

        decimal dTimeOut = 30;
        private decimal TimeOut
        {
            get { return dTimeOut; }
            set
            {
                dTimeOut = value;
                webBrowserEdit.TimeoutLimit = int.Parse(value.ToString());
            }
        }

        bool bSaveArticleList = false;
        private bool SaveArticleList
        {
            get { return bSaveArticleList; }
            set { bSaveArticleList = value; }
        }

        bool bAutoSaveEdit = false;
        private bool AutoSaveEditBoxEnabled
        {
            get { return bAutoSaveEdit; }
            set { bAutoSaveEdit = value; }
        }

        string sAutoSaveEditFile = "Edit Box.txt";
        private string AutoSaveEditBoxFile
        {
            get { return sAutoSaveEditFile; }
            set { sAutoSaveEditFile = value; }
        }

        decimal dAutoSaveEditPeriod = 60;
        private decimal AutoSaveEditBoxPeriod
        {
            get { return dAutoSaveEditPeriod; }
            set { dAutoSaveEditPeriod = value; EditBoxSaveTimer.Interval = int.Parse((value * 1000).ToString()); }
        }

        List<String> lCustomWiki = new List<string>();
        private List<String> customWikis
        {
            get { return lCustomWiki; }
            set { lCustomWiki = value; }
        }

        #endregion

        #region MainProcess

        /// <summary>
        /// checks if we are still logged in
        /// asks to relogin if needed
        /// </summary>
        bool CheckLoginStatus()
        {
            if (webBrowserEdit.UserName != Variables.User.Name)
            {
                MessageBox.Show("You've been logged off, probably due to loss of session data.\r\n" +
                    "Please relogin.", "Logged off", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                Stop();
                CheckStatus(true);
                return false;
            }

            return true;
        }

        private void Start()
        {
            try
            {
                Tools.WriteDebug(this.Name, "Starting");

                CanShutdown();

                //check edit summary
                txtEdit.Enabled = true;
                SetToolBarEnabled(true);
                txtEdit.Text = "";
                webBrowserEdit.BringToFront();
                if (cmboEditSummary.Text == "" && Plugin.Items.Count == 0)
                    MessageBox.Show("Please enter an edit summary.", "Edit summary", MessageBoxButtons.OK, 
                        MessageBoxIcon.Exclamation);

                StopDelayedRestartTimer();
                DisableButtons();
                //TheArticle.EditSummary = "";
                skippable = true;
                txtEdit.Clear();

                if (webBrowserEdit.IsBusy)
                    webBrowserEdit.Stop();

                if (webBrowserEdit.Document != null)
                    webBrowserEdit.Document.Write("");

                //check we are logged in
                if (!Variables.User.WikiStatus && !CheckStatus(false))
                    return;

                ArticleInfo(true);

                if (listMaker1.NumberOfArticles < 1)
                {
                    webBrowserEdit.Busy = false;
                    stopSaveInterval();
                    lblTimer.Text = "";
                    lblStatusText.Text = "No articles in list, you need to use the Make list";
                    this.Text = "AutoWikiBrowser";
                    webBrowserEdit.Document.Write("");
                    listMaker1.MakeListEnabled = true;
                    return;
                }
                else
                    webBrowserEdit.Busy = true;

                TheArticle = new ArticleEx(listMaker1.SelectedArticle().Name);
                ErrorHandler.CurrentArticle = TheArticle.Name;
                NewHistory();

                if (!Tools.IsValidTitle(TheArticle.Name))
                {
                    SkipPage("Invalid page title");
                    return;
                }
                if (BotMode)
                    NudgeTimer.StartMe();

                EditBoxSaveTimer.Enabled = AutoSaveEditBoxEnabled;

                //Navigate to edit page
                webBrowserEdit.LoadEditPage(TheArticle.Name);
            }
            catch (Exception ex)
            {
                Tools.WriteDebug(this.Name, "Start() error: " + ex.Message);
                StartDelayedRestartTimer();
            }
            
            if (GlobalObjects.MyTrace.StoppedWithConfigError)
            {
                try
                { GlobalObjects.MyTrace.ValidateUploadProfile(); }
                catch (Exception ex)
                { GlobalObjects.MyTrace.ConfigError(ex); }
            }
        }

        private void CaseWasLoad()
        {
            if (!loadSuccess())
                return;

            if (!CheckLoginStatus()) return;

            if (GlobalObjects.MyTrace.HaveOpenFile)
                GlobalObjects.MyTrace.WriteBulletedLine("AWB started processing", true, true, true);
            else
                GlobalObjects.MyTrace.Initialise();

            string strTemp = webBrowserEdit.GetArticleText();

            this.Text = "AutoWikiBrowser " + SettingsFile + " - " + TheArticle.Name;

            //check for redirect
            if (bypassRedirectsToolStripMenuItem.Checked && Tools.IsRedirect(strTemp) && !PageReload)
            {
                ArticleEx Redirect = new ArticleEx(Tools.RedirectTarget(strTemp));

                if (Redirect.Name == TheArticle.Name)
                {//ignore recursive redirects
                    SkipPage("Recursive redirect");
                    return;
                }

                listMaker1.ReplaceArticle(TheArticle, Redirect);
                TheArticle = new ArticleEx(Redirect.Name);

                webBrowserEdit.LoadEditPage(Redirect.Name);
                return;
            }

            TheArticle.OriginalArticleText = strTemp;

            int.TryParse(webBrowserEdit.GetScriptingVar("wgCurRevisionId"), out ErrorHandler.CurrentRevision);

            if (PageReload)
            {
                PageReload = false;
                GetDiff();
                return;
            }

            if (chkSkipIfContains.Checked && TheArticle.SkipIfContains(txtSkipIfContains.Text, 
                chkSkipIsRegex.Checked, chkSkipCaseSensitive.Checked, true))
            {
                SkipPage("Article contains: " + txtSkipIfContains.Text);
                return;
            }

            if (chkSkipIfNotContains.Checked && TheArticle.SkipIfContains(txtSkipIfNotContains.Text,
                chkSkipIsRegex.Checked, chkSkipCaseSensitive.Checked, false))
            {
                SkipPage("Article does not contain: " + txtSkipIfNotContains.Text);
                return;
            }

            if (!Skip.skipIf(TheArticle.OriginalArticleText))
            {                
                SkipPage("skipIf custom code"); 
                return;
            }

            //check not in use
            if (TheArticle.IsInUse && !BotMode)
                MessageBox.Show("This page has the \"Inuse\" tag, consider skipping it");

            if (!doNotAutomaticallyDoAnythingToolStripMenuItem.Checked)
            {
                ProcessPage();

                if (!Abort && skippable && chkSkipNoChanges.Checked && 
                    TheArticle.ArticleText == TheArticle.OriginalArticleText)
                {
                    SkipPage("No change");
                    return;
                }
                else if (!Abort && TheArticle.SkipArticle)
                {
                    SkipPageReasonAlreadyProvided(); // Don't send a reason; ProcessPage() should already have logged one
                    return;
                }
            }

            webBrowserEdit.SetArticleText(TheArticle.ArticleText);
            TheArticle.SaveSummary();
            txtEdit.Text = TheArticle.ArticleText;

            //Update statistics and alerts
            ArticleInfo(false);

            if (!Abort)
            {
                if (BotMode && chkQuickSave.Checked)
                    startDelayedAutoSaveTimer();
                else if (toolStripComboOnLoad.SelectedIndex == 0)
                    GetDiff();
                else if (toolStripComboOnLoad.SelectedIndex == 1)
                    GetPreview();
                else if (toolStripComboOnLoad.SelectedIndex == 2)
                {
                    if (BotMode)
                    {
                        startDelayedAutoSaveTimer();
                        return;
                    }

                    bleepflash();

                    this.Focus();
                    txtEdit.Focus();
                    txtEdit.SelectionLength = 0;

                    EnableButtons();
                }
            }
            else
            {
                EnableButtons();
                Abort = false;
            }
        }

        private void bleepflash()
        {
            if (!this.ContainsFocus)
            {
                if (Flash) Tools.FlashWindow(this);
                if (Beep) Tools.Beep1();
            }
        }

        private bool loadSuccess()
        {
            try
            {
                string HTML = webBrowserEdit.Document.Body.InnerHtml;
                if (HTML.Contains("The Wikipedia database is temporarily in read-only mode for the following reason"))
                {//http://en.wikipedia.org/wiki/MediaWiki:Readonlytext

                    if (retries < 10)
                    {
                        StartDelayedRestartTimer();
                        retries++;
                        Start();
                        return false;
                    }
                    else
                    {
                        retries = 0;
                        SkipPage("Database is locked, tried 10 times");
                        return false;
                    }
                }
                else if (HTML.Contains("Sorry! We could not process your edit due to a loss of session data. Please try again. If it still doesn't work, try logging out and logging back in."))
                {
                    Save();
                    return false;
                }

                if (HTML.Contains("readOnly"))
                {
                    if (Variables.User.IsAdmin)
                        return true;
                    else
                    {
                        NudgeTimer.Stop();
                        SkipPage("Page is protected");
                        return false;
                    }
                }
                //check we are still logged in
                try
                {
                    if (!webBrowserEdit.GetLogInStatus())
                    {
                        Variables.User.LoggedIn = false;
                        NudgeTimer.Stop();
                        Start();
                        return false;
                    }
                }
                catch
                {
                    // No point writing to log listener I think, as it gets destroyed when we Stop?
                    if (mErrorGettingLogInStatus)
                    {
                        MessageBox.Show("Error getting login status", "Error", MessageBoxButtons.OK,
                            MessageBoxIcon.Error);
                        // Stop AWB and reset the state variable:
                        Stop(); mErrorGettingLogInStatus = false;
                    }
                    else
                    {
                        mErrorGettingLogInStatus = true; // prevent start / error / start / error loop
                        Stop();
                        Start();
                    }
                    return false;
                }

                mErrorGettingLogInStatus = false;

                if (webBrowserEdit.NewMessage)
                {//check if we have any messages
                    NudgeTimer.Stop();
                    Variables.User.WikiStatus = false;
                    UpdateButtons();
                    webBrowserEdit.Document.Write("");
                    this.Focus();

                    dlgTalk DlgTalk = new dlgTalk();
                    if (DlgTalk.ShowDialog() == DialogResult.Yes)
                        Tools.OpenUserTalkInBrowser();
                    else
                    {
                        try
                        {
                            System.Diagnostics.Process.Start("IExplore", Variables.GetUserTalkURL());
                        }
                        catch { }
                    }

                    DlgTalk = null;
                    return false;
                }
                if (!webBrowserEdit.HasArticleTextBox)
                {
                    if (!BotMode)
                    {
                        MessageBox.Show("There was a problem loading the page. Re-start the process", "Problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        return false;
                    }

                    lblStatusText.Text = "There was a problem loading the page. Re-starting.";
                    StartDelayedRestartTimer();
                    return false;
                }
                if (webBrowserEdit.Document.GetElementById("wpTextbox1").InnerText == null && chkSkipNonExistent.Checked)
                {//check if it is a non-existent page, if so then skip it automatically.
                    SkipPage("Non-existent page");
                    return false;
                }
                if (webBrowserEdit.Document.GetElementById("wpTextbox1").InnerText != null && chkSkipExistent.Checked)
                {
                    SkipPage("Existing page");
                    return false;
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
            NudgeTimer.Reset();
            return true;
        }

        private void CaseWasDiff()
        {
            if (BotMode)
            {
                startDelayedAutoSaveTimer();
                return;
            }

            bleepflash();

            this.Focus();
            txtEdit.Focus();
            txtEdit.SelectionLength = 0;

            EnableButtons();
        }

        private void CaseWasSaved()
        {
            if (webBrowserEdit.Document.Body.InnerHtml.Contains("<H1 class=firstHeading>Edit conflict: "))
            {//if session data is lost, if data is lost then save after delay with tmrAutoSaveDelay
                MessageBox.Show("There has been an Edit Conflict. AWB will now re-apply its changes on the updated page. \n\r Please re-review the changes before saving. Any Custom edits will be lost, and have to be re-added manually.", "Edit Conflict");
                NudgeTimer.Stop();
                Start();
                return;
            }
            else if (!BotMode && webBrowserEdit.Document.Body.InnerHtml.Contains("<A class=extiw title=m:spam_blacklist href=\"http://meta.wikimedia.org/wiki/spam_blacklist\">"))
            {//check edit wasn't blocked due to spam filter
                if (!chkSkipSpamFilter.Checked && MessageBox.Show("Edit has been blocked by spam blacklist. Try and edit again?", "Spam blacklist", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) == DialogResult.Yes)
                    Start();
                else
                    SkipPage("Edit blocked by spam protection filter");
                    
                return;
            }
            else if (webBrowserEdit.Document.Body.InnerHtml.Contains("<DIV CLASS=PREVIEWNOTE"))
            {//if session data is lost, if data is lost then save after delay with tmrAutoSaveDelay
                StartDelayedRestartTimer();
                return;
            }

            //lower restart delay
            if (intRestartDelay > 5)
                intRestartDelay -= 1;

            NumberOfEdits++;

            LastArticle = "";
            listMaker1.Remove(TheArticle);
            NudgeTimer.Stop();
            sameArticleNudges = 0;
            if (tabControl2.SelectedTab == tpHistory)
                tabControl2.SelectedTab = tpEdit;
            LogControl1.AddLog(false, TheArticle.LogListener);

            if (listMaker1.Count == 0)
                if (AutoSaveEditBoxEnabled)
                    EditBoxSaveTimer.Enabled = false;
            retries = 0;
            Start();
        }

        private void CaseWasNull()
        {
            if (webBrowserEdit.Document.Body.InnerHtml.Contains("<B>You have successfully signed in to Wikipedia as"))
            {
                lblStatusText.Text = "Signed in, now re-starting";

                if (!Variables.User.WikiStatus)
                    CheckStatus(false);
            }
        }

        private void SkipPageReasonAlreadyProvided()
        {
            try
            {
                //reset timer.
                NumberOfIgnoredEdits++;
                stopDelayedAutoSaveTimer();
                NudgeTimer.Stop();
                listMaker1.Remove(TheArticle);
                sameArticleNudges = 0;
                LogControl1.AddLog(true, TheArticle.LogListener);
                retries = 0;
                Start();
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        private void SkipPage(string reason)
        {
            switch (reason)
            {
                case "user":
                    TheArticle.Trace.UserSkipped();
                    break;

                case "plugin":
                    TheArticle.Trace.PluginSkipped();
                    break;

                case "":
                    break;

                default:
                    TheArticle.Trace.AWBSkipped(reason);
                    break;
            }

            SkipPageReasonAlreadyProvided();
        }

        private void ProcessPage()
        {
            ProcessPage(TheArticle);
        }

        private void ProcessPage(ArticleEx TheArticle)
        {
            bool process = true;

            prof.AddLog("--------------------------------------");
            prof.Start("ProcessPage(\"" + TheArticle.Name + "\")");
            try
            {
                if (noParse.Contains(TheArticle.Name))
                    process = false;

                if (!ignoreNoBotsToolStripMenuItem.Checked &&
                    !Parsers.CheckNoBots(TheArticle.ArticleText, Variables.User.Name))
                {
                    TheArticle.AWBSkip("Restricted by {{bots}}/{{nobots}}");
                    return;
                }

                prof.Profile("Initial skip checks");

                if (cModule.ModuleEnabled && cModule.Module != null)
                {
                    TheArticle.SendPageToCustomModule(cModule.Module);
                    if (TheArticle.SkipArticle) return;
                }

                prof.Profile("Custom module");

                if (Plugin.Items.Count > 0)
                {
                    foreach (KeyValuePair<string, IAWBPlugin> a in Plugin.Items)
                    {
                        TheArticle.SendPageToPlugin(a.Value, this);
                        if (TheArticle.SkipArticle) return;
                    }
                }
                prof.Profile("Plugins");

                if (chkUnicodifyWhole.Checked && process)
                {
                    TheArticle.HideMoreText(RemoveText);
                    prof.Profile("HideMoreText");

                    TheArticle.Unicodify(Skip.SkipNoUnicode, parsers);
                    prof.Profile("Unicodify");

                    TheArticle.UnHideMoreText(RemoveText);
                    prof.Profile("UnHideMoreText");
                }

                if (cmboImages.SelectedIndex != 0)
                {
                    TheArticle.UpdateImages((WikiFunctions.Options.ImageReplaceOptions)cmboImages.SelectedIndex,
                        parsers, txtImageReplace.Text, txtImageWith.Text, chkSkipNoImgChange.Checked);
                    if (TheArticle.SkipArticle) return;
                }

                prof.Profile("Images");

                if (cmboCategorise.SelectedIndex != 0)
                {
                    TheArticle.Categorisation((WikiFunctions.Options.CategorisationOptions)
                        cmboCategorise.SelectedIndex, parsers, chkSkipNoCatChange.Checked, txtNewCategory.Text.Trim(),
                        txtNewCategory2.Text.Trim());
                    if (TheArticle.SkipArticle) return;
                    else if (!chkGeneralFixes.Checked) TheArticle.AWBChangeArticleText("Fix categories", parsers.FixCategories(TheArticle.ArticleText), true);
                }

                prof.Profile("Categories");

                if (chkFindandReplace.Checked && !findAndReplace.AfterOtherFixes)
                {
                    TheArticle.PerformFindAndReplace(findAndReplace, substTemplates, replaceSpecial,
                        chkSkipWhenNoFAR.Checked);
                    if (TheArticle.SkipArticle) return;
                }

                prof.Profile("F&R");

                if (chkRegExTypo.Checked && RegexTypos != null && !BotMode && !Tools.IsTalkPage(TheArticle.NameSpaceKey))
                {
                    TheArticle.PerformTypoFixes(RegexTypos, chkSkipIfNoRegexTypo.Checked);
                    prof.Profile("Typos");
                    if (TheArticle.SkipArticle) return;
                }

                if (TheArticle.CanDoGeneralFixes)
                {
                    if (process && chkAutoTagger.Checked)
                    {
                        TheArticle.AutoTag(parsers, Skip.SkipNoTag);
                        if (TheArticle.SkipArticle) return;
                    }

                    prof.Profile("Auto-tagger");

                    if (process && chkGeneralFixes.Checked)
                    {
                        TheArticle.HideText(RemoveText);

                        prof.Profile("HideText");

                        TheArticle.FixHeaderErrors(parsers, Variables.LangCode, Skip.SkipNoHeaderError);
                        prof.Profile("FixHeaderErrors");
                        TheArticle.SetDefaultSort(parsers, Variables.LangCode, Skip.SkipNoDefaultSortAdded);
                        prof.Profile("SetDefaultSort");

                        TheArticle.AWBChangeArticleText("Fix categories", parsers.FixCategories(TheArticle.ArticleText), true);
                        prof.Profile("FixCategories");
                        TheArticle.AWBChangeArticleText("Fix images", parsers.FixImages(TheArticle.ArticleText), true);
                        prof.Profile("FixImages");
                        TheArticle.AWBChangeArticleText("Fix syntax", parsers.FixSyntax(TheArticle.ArticleText), true);
                        prof.Profile("FixSyntax");
                        TheArticle.AWBChangeArticleText("Fix temperatures", parsers.FixTemperatures(TheArticle.ArticleText), true);
                        prof.Profile("FixTemperatures");

                        TheArticle.AWBChangeArticleText("Fix main article", parsers.FixMainArticle(TheArticle.ArticleText), true);
                        prof.Profile("FixMainArticle");

                        TheArticle.AWBChangeArticleText("Fix empty links and templates", parsers.FixEmptyLinksAndTemplates(TheArticle.ArticleText), true);
                        prof.Profile("FixEmptyLinksAndTemplates");

                        TheArticle.FixLinks(parsers, Skip.SkipNoBadLink);
                        prof.Profile("FixLinks");
                        TheArticle.BulletExternalLinks(parsers, Skip.SkipNoBulletedLink);
                        prof.Profile("BulletExternalLinks");

                        prof.Profile("Links");

                        TheArticle.AWBChangeArticleText("Sort meta data",
                            parsers.SortMetaData(TheArticle.ArticleText, TheArticle.Name), true);

                        prof.Profile("Metadata");

                        TheArticle.EmboldenTitles(parsers, Skip.SkipNoBoldTitle);

                        TheArticle.AWBChangeArticleText("Format sticky links",
                            parsers.StickyLinks(parsers.SimplifyLinks(TheArticle.ArticleText)), true);

                        //TheArticle.AWBChangeArticleText("Remove duplicate wikilink", parsers.RemoveDuplicateWikiLinks(TheArticle.ArticleText), true);

                        TheArticle.UnHideText(RemoveText);

                        prof.Profile("End of general fixes");
                    }
                }
                else if (process && chkGeneralFixes.Checked && TheArticle.NameSpaceKey == 3)
                {
                    TheArticle.HideText(RemoveText);

                    prof.Profile("HideText");

                    if (!userTalkWarningsLoaded)
                    {
                        loadUserTalkWarnings();
                        prof.Profile("loadUserTalkWarnings");
                    }

                    TheArticle.AWBChangeArticleText("Subst user talk warnings",
                        parsers.SubstUserTemplates(TheArticle.ArticleText, TheArticle.Name, userTalkTemplatesRegex), true);

                    prof.Profile("SubstUserTemplates");

                    TheArticle.UnHideText(RemoveText);

                    prof.Profile("UnHideText");
                }

                if (chkAppend.Checked)
                {
                    if (rdoAppend.Checked)
                        TheArticle.AWBChangeArticleText("Appended your message",
                            TheArticle.ArticleText + "\r\n\r\n" + txtAppendMessage.Text, false);
                    else
                        TheArticle.AWBChangeArticleText("Prepended your message",
                            txtAppendMessage.Text + "\r\n\r\n" + TheArticle.ArticleText, false);
                }

                if (chkFindandReplace.Checked && findAndReplace.AfterOtherFixes)
                {
                    TheArticle.PerformFindAndReplace(findAndReplace, substTemplates, replaceSpecial,
                        chkSkipWhenNoFAR.Checked);

                    prof.Profile("F&R (2nd)");

                    if (TheArticle.SkipArticle) return;
                }

                if (chkEnableDab.Checked && txtDabLink.Text.Trim() != "" &&
                    txtDabVariants.Text.Trim() != "")
                {
                    if (TheArticle.Disambiguate(txtDabLink.Text.Trim(), txtDabVariants.Lines, BotMode,
                        (int)udContextChars.Value, chkSkipNoDab.Checked))
                    {
                        if (TheArticle.SkipArticle) return;
                    }
                    else
                    {
                        Stop();
                        return;
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex); 
                TheArticle.Trace.AWBSkipped("Exception:" + ex.Message);
            }
            finally
            {
                prof.Flush();
            }
        }

        private void GetDiff()
        {
            try
            {
                webBrowserDiff.BringToFront();
                webBrowserDiff.Document.OpenNew(false);

                if (TheArticle.OriginalArticleText == txtEdit.Text)
                {
                    webBrowserDiff.Document.Write(@"<h2 style='padding-top: .5em;
padding-bottom: .17em;
border-bottom: 1px solid #aaa;
font-size: 150%;'>No changes</h2><p>Press the ""Ignore"" button below to skip to the next page.</p>");
                }
                else
                {
                    webBrowserDiff.Document.Write("<html><head>" +
                        WikiDiff.DiffHead() + @"</head><body>" + WikiDiff.TableHeader() +
                        WikiDiff.GetDiff(TheArticle.OriginalArticleText, txtEdit.Text, 1) +
                        @"</table></body></html>");
                }
                
                CaseWasDiff();
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        private void GetPreview()
        {
            webBrowserEdit.BringToFront();
            webBrowserEdit.SetArticleText(txtEdit.Text);

            LastArticle = txtEdit.Text;

            skippable = false;
            webBrowserEdit.ShowPreview();
            EnableButtons();
            bleepflash();
        }

        private void Save()
        {
            DisableButtons();
            if (txtEdit.Text.Length > 0)
                SaveArticle();
            else if (MessageBox.Show("Do you really want to save a blank page?", "Save?", 
                MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.Yes)
                SaveArticle();
            else
                SkipPage("Nothing to save - blank page");
        }

        private void SaveArticle()
        {
            webBrowserEdit.BringToFront();

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
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        #endregion

        #region extra stuff

        public void DiffDblClicked(string id)
        {
            try
            {
                Match m = DiffIdParser.Match(id);
                int SrcLine = int.Parse(m.Groups[1].Value) - 1;
                int DestLine = int.Parse(m.Groups[2].Value) - 1;

                if (SrcLine < 0 || DestLine < 0) return;

                string[] Src = TheArticle.OriginalArticleText.Split(new string[] { "\r\n" }, StringSplitOptions.None);
                string[] Dest = txtEdit.Text.Split(new string[] { "\r\n" }, StringSplitOptions.None);

                List<string> Lst = new List<string>(Dest);
                switch (id[0])
                {
                    case 'a':
                        Lst.RemoveAt(DestLine);
                        Dest = Lst.ToArray();
                        break;
                    case 'd':
                        Lst.Insert(DestLine, Src[Math.Min(SrcLine, Src.Length - 1)]);
                        Dest = Lst.ToArray();
                        break;
                    case 'r':
                        Dest[DestLine] = Src[SrcLine];
                        break;
                    default:
                        return;
                }
                txtEdit.Text = string.Join("\r\n", Dest);
                GetDiff();
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex); 
                return;
            }
        }

        public void DiffClicked(string id)
        {
            try
            {
                tabControl2.SelectedTab = tpEdit;
                txtEdit.Select();
                Match m = DiffIdParser.Match(id);
                int DestLine = int.Parse(m.Groups[2].Value) - 1;
                if (DestLine < 0) return;

                MatchCollection mc = Regex.Matches(txtEdit.Text, "\r\n");
                DestLine = Math.Min(mc.Count, DestLine);

                if (DestLine == 0) txtEdit.Select(0, 0);
                else
                    txtEdit.Select(mc[DestLine - 1].Index + 2, 0);
                txtEdit.ScrollToCaret();
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        private void panelShowHide()
        {
            if (panel1.Visible) panel1.Hide();
            else panel1.Show();

            setBrowserSize();
        }

        Point oldPosition = new Point();
        Size oldSize = new Size();
        private void parametersShowHide()
        {
            enlargeEditAreaToolStripMenuItem.Checked = !enlargeEditAreaToolStripMenuItem.Checked;
            if (groupBox2.Visible)
            {
                btntsShowHideParameters.Image = Res.btnshowhideparameters2_image;

                oldPosition = tabControl2.Location;;
                tabControl2.Location = new Point(groupBox2.Location.X, groupBox2.Location.Y - 5);

                oldSize = tabControl2.Size;
                tabControl2.Size = new Size((tabControl2.Size.Width + tabControl1.Size.Width + groupBox2.Size.Width + 8), tabControl2.Size.Height); 
            }
            else
            {
                btntsShowHideParameters.Image = Res.btnshowhideparameters_image;

                tabControl2.Location = oldPosition;
                tabControl2.Size = oldSize;
            }
            groupBox2.Visible = tabControl1.Visible = !groupBox2.Visible;
        }
        
        private void UpdateUserName(object sender, EventArgs e)
        {
            lblUserName.Text = Variables.User.Name;
        }

        private void UpdateWebBrowserStatus()
        {
            lblStatusText.Text = webBrowserEdit.Status;
        }

        private void UpdateListStatus()
        {
            lblStatusText.Text = listMaker1.Status;
        }

        private void MainForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            WebControl.Shutdown = true;

            Properties.Settings.Default.WindowLocation = this.Location;

            if (this.WindowState == FormWindowState.Normal)
                Properties.Settings.Default.WindowSize = this.Size;
            else
                Properties.Settings.Default.WindowSize = this.RestoreBounds.Size;

            Properties.Settings.Default.Save();

            if (AutoWikiBrowser.Properties.Settings.Default.DontAskForTerminate)
            {
                // save user persistent settings
                AutoWikiBrowser.Properties.Settings.Default.Save();
                return;
            }
            string msg = "";
            if (boolSaved == false)
                msg = "You have changed the list since last saving it!\r\n";

            TimeSpan Time = new TimeSpan(DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, DateTime.Now.Second);
            Time = Time.Subtract(StartTime);
            ExitQuestion dlg = new ExitQuestion(Time, NumberOfEdits, msg);
            dlg.ShowDialog();
            if (dlg.DialogResult == DialogResult.OK)
            {
                AutoWikiBrowser.Properties.Settings.Default.DontAskForTerminate = dlg.checkBoxDontAskAgain;

                // save user persistent settings
                AutoWikiBrowser.Properties.Settings.Default.Save();
            }
            else
            {
                e.Cancel = true;
                dlg = null;
                return;
            }

            if (webBrowserEdit.IsBusy)
                webBrowserEdit.Stop2();
            if (Variables.User.webBrowserLogin.IsBusy)
                Variables.User.webBrowserLogin.Stop();

            SaveRecentSettingsList();
        }

        private void setCheckBoxes()
        {
            if (webBrowserEdit.Document.Body.InnerHtml.Contains("wpMinoredit"))
            {
                webBrowserEdit.SetMinor(markAllAsMinorToolStripMenuItem.Checked);
                webBrowserEdit.SetWatch(addAllToWatchlistToolStripMenuItem.Checked);
                webBrowserEdit.SetSummary(MakeSummary());
            }
        }

        private string MakeSummary()
        {
            string tag = cmboEditSummary.Text + TheArticle.SavedSummary;
            if (!BotMode || !chkSuppressTag.Checked) tag += " " + Variables.SummaryTag;

            return tag;
        }

        private void chkFindandReplace_CheckedChanged(object sender, EventArgs e)
        {
            btnMoreFindAndReplce.Enabled = chkFindandReplace.Checked;
            btnFindAndReplaceAdvanced.Enabled = chkFindandReplace.Checked;
            chkSkipWhenNoFAR.Enabled = chkFindandReplace.Checked;
            btnSubst.Enabled = chkFindandReplace.Checked;
        }

        private void cmboCategorise_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (cmboCategorise.SelectedIndex > 0)
            {
                txtNewCategory.Enabled = true;
                if (cmboCategorise.SelectedIndex == 1)
                {
                    label1.Text = "with Category:";
                    txtNewCategory2.Enabled = true;
                }
                else
                {
                    label1.Text = "";
                    txtNewCategory2.Enabled = false;
                }
                chkSkipNoCatChange.Enabled = true;
            }
            else
            {
                chkSkipNoCatChange.Enabled = false;
                label1.Text = "";
                txtNewCategory2.Enabled = false;
                txtNewCategory.Enabled = false;
            }
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

        private void UpdateBotStatus(object sender, EventArgs e)
        {
            chkAutoMode.Enabled = Variables.User.IsBot;
            if (BotMode)
                BotMode = Variables.User.IsBot;
            if (chkQuickSave.Checked)
                chkQuickSave.Checked = Variables.User.IsBot;
            lblOnlyBots.Visible = !Variables.User.IsBot;
        }

         private void UpdateAdminStatus(object sender, EventArgs e)   
          { }   
     
          private void UpdateWikiStatus(object sender, EventArgs e)   
          { } 

        private void chkAutoMode_CheckedChanged(object sender, EventArgs e)
        {
            if (BotMode)
            {
                SetBotModeEnabled(true);
                chkNudge.Checked = true;
                chkNudgeSkip.Checked = false; // default to false until such time as the settings file has this! mets! :P

                if (chkRegExTypo.Checked)
                {
                    MessageBox.Show("Auto cannot be used with RegExTypoFix.\r\nRegExTypoFix will now be turned off", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    chkRegExTypo.Checked = false;
                }
            }
            else
            {
                SetBotModeEnabled(false);
                stopDelayedAutoSaveTimer();
            }
        }

        private void SetBotModeEnabled(bool Enabled)
        {
            label2.Enabled = chkSuppressTag.Enabled = chkQuickSave.Enabled = nudBotSpeed.Enabled
            = lblAutoDelay.Enabled = btnResetNudges.Enabled = lblNudges.Enabled = chkNudge.Enabled 
            = chkNudgeSkip.Enabled = chkNudge.Checked = chkShutdown.Enabled = Enabled;
        }

        private void aboutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            TimeSpan Time = new TimeSpan(DateTime.Now.Day, DateTime.Now.Hour, DateTime.Now.Minute, 
                DateTime.Now.Second).Subtract(StartTime);
            new AboutBox(webBrowserEdit.Version.ToString(), Time, NumberOfEdits).Show();
        }

        private void loginToolStripMenuItem_Click(object sender, EventArgs e)
        { CheckStatus(true); }

        private void logOutToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Would you really like to logout?", "Logout", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                chkAutoMode.Enabled = false;
                BotMode = false;
                chkQuickSave.Checked = false;
                lblOnlyBots.Visible = true;
                webBrowserEdit.BringToFront();
                webBrowserEdit.LoadLogOut();
                webBrowserEdit.Wait();
                Variables.User.UpdateWikiStatus();
            }
        }

        public bool CheckStatus(bool Login)
        {
            lblStatusText.Text = "Loading page to check if we are logged in.";
            WikiStatusResult Result = Variables.User.UpdateWikiStatus();

            bool b = false;
            string label = "Software disabled";

            switch (Result)
            {
                case WikiStatusResult.Error:
                    lblUserName.BackColor = Color.Red;
                    MessageBox.Show("Check page failed to load.\r\n\r\nCheck your Internet Explorer is working and that the Wikipedia servers are online, also try clearing Internet Explorer cache.", "User check problem", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    break;

                case WikiStatusResult.NotLoggedIn:
                    lblUserName.BackColor = Color.Red;
                    if (!Login)
                        MessageBox.Show("You are not logged in. The log in screen will now load, enter your name and password, click \"Log in\", wait for it to complete, then start the process again.\r\n\r\nIn the future you can make sure this won't happen by logging in to Wikipedia using Microsoft Internet Explorer.", "Not logged in", MessageBoxButtons.OK, MessageBoxIcon.Information);
                    webBrowserEdit.LoadLogInPage();
                    webBrowserEdit.BringToFront();
                    break;

                case WikiStatusResult.NotRegistered:
                    lblUserName.BackColor = Color.Red;
                    MessageBox.Show(Variables.User.Name + " is not enabled to use this.", "Not enabled", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                    try
                    {
                        System.Diagnostics.Process.Start(Variables.URL + "/wiki/Project:AutoWikiBrowser/CheckPage");
                    }
                    catch { }
                    break;

                case WikiStatusResult.OldVersion:
                    oldVersion();
                    break;

                case WikiStatusResult.Registered:
                    b = true;
                    label = string.Format("Logged in, user and software enabled. Bot = {0}, Admin = {1}", Variables.User.IsBot, Variables.User.IsAdmin);
                    lblUserName.BackColor = Color.LightGreen;

                    //Get list of articles not to apply general fixes to.
                    Match n = WikiRegexes.NoGeneralFixes.Match(Variables.User.CheckPageText);
                    if (n.Success)
                    {
                        foreach (Match link in WikiRegexes.UnPipedWikiLink.Matches(n.Value))
                            if (!noParse.Contains(link.Groups[1].Value))
                                noParse.Add(link.Groups[1].Value);
                    }
                    break;
            }

            // detect writing system
            if (Variables.RTL) RightToLeft = RightToLeft.Yes;
                else RightToLeft = RightToLeft.No;

            lblStatusText.Text = label;
            UpdateButtons();

            return b;
        }

        private void oldVersion()
        {
            if (!WebControl.Shutdown)
            {
                lblUserName.BackColor = Color.Red;

                DialogResult yesnocancel = MessageBox.Show("This version is not enabled, please download the newest version. If you have the newest version, check that Wikipedia is online.\r\n\r\nPlease press \"Yes\" to run the AutoUpdater, \"No\" to load the download page and update manually, or \"Cancel\" to not update (but you will not be able to edit).", "Problem", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Information);
                if (yesnocancel == DialogResult.Yes)
                    runUpdater();
                else if (yesnocancel == DialogResult.No)
                {
                    try
                    {
                        System.Diagnostics.Process.Start("http://sourceforge.net/project/showfiles.php?group_id=158332");
                    }
                    catch { }
                }
            }
        }

        private void exitToolStripMenuItem_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
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
            txtSkipIfContains.Enabled = chkSkipIfContains.Checked;
        }

        private void chkOnlyIfContains_CheckedChanged(object sender, EventArgs e)
        {
            txtSkipIfNotContains.Enabled = chkSkipIfNotContains.Checked;
        }

        private void txtNewCategory_Leave(object sender, EventArgs e)
        {
            txtNewCategory.Text = txtNewCategory.Text.Trim('[', ']');
            txtNewCategory.Text = Regex.Replace(txtNewCategory.Text, "^" + Variables.NamespacesCaseInsensitive[14], "");
            txtNewCategory.Text = Tools.TurnFirstToUpper(txtNewCategory.Text);
        }

        private void txtNewCategory2_Leave(object sender, EventArgs e)
        {
            txtNewCategory2.Text = txtNewCategory2.Text.Trim('[', ']');
            txtNewCategory2.Text = Regex.Replace(txtNewCategory2.Text, "^" + Variables.NamespacesCaseInsensitive[14], "");
            txtNewCategory2.Text = Tools.TurnFirstToUpper(txtNewCategory2.Text);
        }

        private void ArticleInfo(bool reset)
        {
            string ArticleText = txtEdit.Text;
            int intWords = 0;
            int intCats = 0;
            int intImages = 0;
            int intLinks = 0;
            int intInterLinks = 0;
            lblWarn.Text = "";

            if (reset)
            {
                //Resets all the alerts.
                lblWords.Text = "Words: ";
                lblCats.Text = "Categories: ";
                lblImages.Text = "Images: ";
                lblLinks.Text = "Links: ";
                lblInterLinks.Text = "Interwiki links: ";

                lbDuplicateWikilinks.Items.Clear();
                lblDuplicateWikilinks.Visible = false;
                lbDuplicateWikilinks.Visible = false;
                btnRemove.Visible = false;
            }
            else
            {
                intWords = Tools.WordCount(ArticleText);

                foreach (Match m in Regex.Matches(ArticleText, "\\[\\[" + Variables.Namespaces[14], RegexOptions.IgnoreCase))
                    intCats++;

                foreach (Match m in Regex.Matches(ArticleText, "\\[\\[" + Variables.Namespaces[6], RegexOptions.IgnoreCase))
                    intImages++;

                foreach (Match m in WikiRegexes.InterWikiLinks.Matches(ArticleText))
                    intInterLinks++;

                foreach (Match m in WikiRegexes.WikiLinksOnly.Matches(ArticleText))
                    intLinks++;

                intLinks = intLinks - intInterLinks - intImages - intCats;

                if (TheArticle.NameSpaceKey == 0 && (WikiRegexes.Stub.IsMatch(ArticleText)) && (intWords > 500))
                    lblWarn.Text = "Long article with a stub tag.\r\n";

                if (!(Regex.IsMatch(ArticleText, "\\[\\[" + Variables.Namespaces[14], RegexOptions.IgnoreCase)))
                    lblWarn.Text += "No category (although one may be in a template)\r\n";

                if (ArticleText.StartsWith("=="))
                    lblWarn.Text += "Starts with heading.";

                lblWords.Text = "Words: " + intWords.ToString();
                lblCats.Text = "Categories: " + intCats.ToString();
                lblImages.Text = "Images: " + intImages.ToString();
                lblLinks.Text = "Links: " + intLinks.ToString();
                lblInterLinks.Text = "Interwiki links: " + intInterLinks.ToString();

                //Find multiple links                
                lbDuplicateWikilinks.Items.Clear();
                ArrayList ArrayLinks = new ArrayList();
                string x = "";
                //get all the links
                foreach (Match m in WikiRegexes.WikiLink.Matches(ArticleText))
                {
                    x = m.Groups[1].Value;
                    if (!WikiRegexes.Dates.IsMatch(x) && !WikiRegexes.Dates2.IsMatch(x))
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
                    btnRemove.Visible = true;
                }
            }
        }

        private void lbDuplicateWikilinks_Click(object sender, EventArgs e)
        {
            tabControl2.SelectedTab = tpEdit;
            int selection = lbDuplicateWikilinks.SelectedIndex;
            if (selection != oldselection)
                Find.resetFind();
            if (lbDuplicateWikilinks.SelectedIndex != -1)
            {
                string strLink = Regex.Escape(lbDuplicateWikilinks.SelectedItem.ToString());
                Find.find("\\[\\[" + strLink + "(\\|.*?)?\\]\\]", true, true, txtEdit, TheArticle.Name);
                btnRemove.Enabled = true;
            }
            else
            {
                Find.resetFind();
                btnRemove.Enabled = false;
            }

            ArticleInfo(false);
            try
            {
                if (lbDuplicateWikilinks.Items.Count != selection + 2)
                    lbDuplicateWikilinks.SelectedIndex = selection + 2;
                else
                    lbDuplicateWikilinks.SelectedIndex = selection + 1;
                lbDuplicateWikilinks.SelectedIndex = selection;
            }
            catch
            {
                lbDuplicateWikilinks.SelectedIndex = lbDuplicateWikilinks.Items.Count - 1;
            }
            oldselection = selection;
        }

        private void ResetFind(object sender, EventArgs e)
        {
            Find.resetFind();
        }

        private void txtEdit_TextChanged(object sender, EventArgs e)
        {
            Find.resetFind();
            try
            {
                TheArticle.EditSummary = "";
            }
            catch { }
        }

        private void btnFind_Click(object sender, EventArgs e)
        {
            tabControl2.SelectedTab = tpEdit;
            Find.find(txtFind.Text, chkFindRegex.Checked, chkFindCaseSensitive.Checked, txtEdit, TheArticle.Name);
        }
        
        private void toolStripTextBox2_Click(object sender, EventArgs e)
        {
            toolStripTextBox2.Text = "";
        }

        private void toolStripTextBox2_KeyPress(object sender, KeyPressEventArgs e)
        {
            if (!char.IsNumber(e.KeyChar) && e.KeyChar != 8)
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

        [Conditional("DEBUG")]
        public void Debug()
        {
            Tools.WriteDebugEnabled = true;
            listMaker1.Add("Wikipedia:AutoWikiBrowser/Sandbox");
            //Variables.User.WikiStatus = true; // Stop logging in and the username code doesn't work!
            Variables.User.IsBot = true;
            Variables.User.IsAdmin = true;
            chkQuickSave.Enabled = true;
            lblOnlyBots.Visible = false;
            dumpHTMLToolStripMenuItem.Visible = true;
            logOutDebugToolStripMenuItem.Visible = true;
            bypassAllRedirectsToolStripMenuItem.Enabled = true;
            webBrowserEdit.IsWebBrowserContextMenuEnabled = true;

            prof = new Profiler("profiling.txt", true);
        }

        #endregion

        #region set variables

        private void PreferencesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            MyPreferences MyPrefs = new MyPreferences(Variables.LangCode, Variables.Project, Variables.CustomProject, txtEdit.Font, LowThreadPriority, Flash, Beep, Minimize, SaveArticleList, TimeOut, AutoSaveEditBoxEnabled, AutoSaveEditBoxFile, AutoSaveEditBoxPeriod);

            if (MyPrefs.ShowDialog(this) == DialogResult.OK)
            {
                txtEdit.Font = MyPrefs.TextBoxFont;
                LowThreadPriority = MyPrefs.LowThreadPriority;
                Flash = MyPrefs.perfFlash;
                Beep = MyPrefs.perfBeep;
                Minimize = MyPrefs.perfMinimize;
                SaveArticleList = MyPrefs.perfSaveArticleList;
                TimeOut = MyPrefs.perfTimeOutLimit;
                AutoSaveEditBoxEnabled = MyPrefs.perfAutoSaveEditBoxEnabled;
                AutoSaveEditBoxPeriod = MyPrefs.perfAutoSaveEditBoxPeriod;
                AutoSaveEditBoxFile = MyPrefs.perfAutoSaveEditBoxFile;
                customWikis = MyPrefs.perfCustomWikis;

                if (MyPrefs.Language != Variables.LangCode || MyPrefs.Project != Variables.Project || MyPrefs.CustomProject != Variables.CustomProject)
                {
                    SetProject(MyPrefs.Language, MyPrefs.Project, MyPrefs.CustomProject);

                    Variables.User.WikiStatus = false;
                    chkQuickSave.Checked = false;
                    BotMode = false;
                    lblOnlyBots.Visible = true;
                    Variables.User.IsBot = false;
                    Variables.User.IsAdmin = false;
                }
            }
            MyPrefs = null;

            listMaker1.AddRemoveRedirects();
        }

        private void reloadToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //refresh typo list
            loadTypos(true);

            //refresh talk warnings list
            loadUserTalkWarnings();

            //refresh login status, and reload check list
            if (!Variables.User.WikiStatus)
            {
                if (!CheckStatus(false))
                    return;
            }
        }

        private void SetProject(LangCodeEnum Code, ProjectEnum Project, string CustomProject)
        {
            //set namespaces
            Variables.SetProject(Code, Project, CustomProject);

            //set interwikiorder
            if (Variables.LangCode == LangCodeEnum.en || Variables.LangCode == LangCodeEnum.pl ||
                Variables.LangCode == LangCodeEnum.simple)
                parsers.InterWikiOrder = InterWikiOrderEnum.LocalLanguageAlpha;
            //else if (Code == "fi")
            //    parsers.InterWikiOrder = InterWikiOrderEnum.LocalLanguageFirstWord;
            else if (Variables.LangCode == LangCodeEnum.he)
                parsers.InterWikiOrder = InterWikiOrderEnum.AlphabeticalEnFirst;
            else
                parsers.InterWikiOrder = InterWikiOrderEnum.Alphabetical;

            if (Variables.LangCode != LangCodeEnum.en || Project != ProjectEnum.wikipedia)
            {
                chkAutoTagger.Checked = false;
                chkGeneralFixes.Checked = false;
            }
            if (Variables.Project != ProjectEnum.custom && Variables.Project != ProjectEnum.wikia && Variables.Project != ProjectEnum.commons && Variables.Project != ProjectEnum.meta && Variables.Project != ProjectEnum.species)
                lblProject.Text = Variables.LangCode.ToString().ToLower() + "." + Variables.Project;
            else if (Variables.Project == ProjectEnum.commons || Variables.Project == ProjectEnum.meta || Variables.Project == ProjectEnum.species)
                lblProject.Text = Variables.Project.ToString();
            else lblProject.Text = Variables.URL;
        }

        #endregion

        #region Enabling/Disabling of buttons

        private void UpdateButtons()
        {
            bool enabled = listMaker1.NumberOfArticles > 0;
            SetStartButton(enabled);

            listMaker1.ButtonsEnabled = enabled;
            lbltsNumberofItems.Text = "Articles: " + listMaker1.NumberOfArticles.ToString();
            bypassAllRedirectsToolStripMenuItem.Enabled = Variables.User.IsAdmin;
        }

        private void SetStartButton(bool enabled)
        {
            /* Please don't remove the If statements; otherwise the EnabledChange event fires even if the button
             * button was already named. The Kingbotk plugin attaches to that event. */
            if (!btnStart.Enabled) btnStart.Enabled = enabled;
            if (!btntsStart.Enabled) btntsStart.Enabled = enabled;
        }

        private void DisableButtons()
        {
            SetStartButton(false);
            SetButtons(false);

            if (listMaker1.NumberOfArticles == 0)
                btnIgnore.Enabled = false;

            if (cmboEditSummary.Focused) txtEdit.Focus();
        }

        private void EnableButtons()
        {
            UpdateButtons();
            SetButtons(true);
        }

        private void SetButtons(bool Enabled)
        {
            btnSave.Enabled = btnIgnore.Enabled = btnPreview.Enabled = btnDiff.Enabled =
            btntsPreview.Enabled = btntsChanges.Enabled = listMaker1.MakeListEnabled =
            btntsSave.Enabled = btntsIgnore.Enabled = btnMove.Enabled = btnDelete.Enabled =
            btnProtect.Enabled = Enabled;
        }

        #endregion

        #region timers

        int intRestartDelay = 5;
        int intStartInSeconds = 5;
        private void DelayedRestart()
        {
            stopDelayedAutoSaveTimer();
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
            intRestartDelay += 5;
        }
        private void StartDelayedRestartTimer(int Delay)
        {
            intStartInSeconds = Delay;
            ticker += DelayedRestart;
        }
        private void StopDelayedRestartTimer()
        {
            ticker -= DelayedRestart;
            intStartInSeconds = intRestartDelay;
        }

        private void stopDelayedAutoSaveTimer()
        {
            ticker -= DelayedAutoSave;
            intTimer = 0;
            lblBotTimer.Text = "Bot timer: " + intTimer.ToString();
        }

        private void startDelayedAutoSaveTimer()
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
                stopDelayedAutoSaveTimer();
                SaveArticle();
            }

            lblBotTimer.Text = "Bot timer: " + intTimer.ToString();
        }

        private void showTimerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showTimer();
        }

        private void showTimer()
        {
            lblTimer.Visible = showTimerToolStripMenuItem.Checked;
            stopSaveInterval();
        }

        int intStartTimer = 0;
        private void SaveInterval()
        {
            intStartTimer++;
            lblTimer.Text = "Timer: " + intStartTimer.ToString();
        }
        private void stopSaveInterval()
        {
            intStartTimer = 0;
            lblTimer.Text = "Timer: 0";
            ticker -= SaveInterval;
        }

        public delegate void Tick();
        public event Tick ticker;
        private void Timer_Tick(object sender, EventArgs e)
        {
            if (ticker != null)
                ticker();

            seconds++;
            if (seconds == 60)
            {
                seconds = 0;
                EditsPerMin();
            }
        }

        int seconds = 0;
        int lastTotal = 0;
        private void EditsPerMin()
        {
            int editsInLastMin = NumberOfEdits - lastTotal;
            NumberOfEditsPerMinute = editsInLastMin;
            lastTotal = NumberOfEdits;
        }

        #endregion

        #region menus and buttons

        private void makeModuleToolStripMenuItem_Click(object sender, EventArgs e)
        {
            cModule.Show();
        }

        private void btnMoreSkip_Click(object sender, EventArgs e)
        {
            Skip.ShowDialog();
        }

        private void btnPreview_Click(object sender, EventArgs e)
        {
            GetPreview();
        }

        private void btnDiff_Click(object sender, EventArgs e)
        {
            GetDiff();
        }

        private void FalsePositiveClick(object sender, EventArgs e)
        {
            if (TheArticle.Name.Length > 0)
                Tools.WriteTextFile("#[[" + TheArticle.Name + "]]\r\n", @"False positives.txt", true);
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
            SkipPage("user");
        }

        private void btnMove_Click(object sender, EventArgs e)
        {
            MoveArticle();
        }

        private void btnDelete_Click(object sender, EventArgs e)
        {
            DeleteArticle();
        }

        private void btnProtect_Click(object sender, EventArgs e)
        {
            ProtectArticle();
            Start();
        }

        private void filterOutNonMainSpaceToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (filterOutNonMainSpaceToolStripMenuItem.Checked)
            {
                listMaker1.FilterNonMainAuto = true;
                listMaker1.FilterNonMainArticles();
            }
            else
                listMaker1.FilterNonMainAuto = false;
        }

        private void specialFilterToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            listMaker1.Filter();
        }

        private void convertToTalkPagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listMaker1.ConvertToTalkPages();
        }

        private void convertFromTalkPagesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listMaker1.ConvertFromTalkPages();
        }

        private void sortAlphabeticallyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (sortAlphabeticallyToolStripMenuItem.Checked)
            {
                listMaker1.AutoAlpha = true;
                listMaker1.AlphaSortList();
            }
            else
                listMaker1.AutoAlpha = false;
        }

        private void saveListToTextFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            listMaker1.SaveList();
        }

        private void launchListComparerToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (listMaker1.Count > 0 && MessageBox.Show("Would you like to copy your current Article List to the ListComparer?", "Copy Article List?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                lc = new ListComparer(listMaker1.GetArticleList());
            else
                lc = new ListComparer();

            lc.Show(this);
        }

        private void launchListSplitterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            WikiFunctions.AWBSettings.UserPrefs P = MakePrefs();

            if (listMaker1.Count > 0 && MessageBox.Show("Would you like to copy your current Article List to the ListSplitter?", "Copy Article List?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                splitter = new ListSplitter(P, savePluginSettings(P), listMaker1.GetArticleList());
            else
                splitter = new ListSplitter(P, savePluginSettings(P));

            splitter.Show(this);
        }

        private void launchDumpSearcherToolStripMenuItem_Click(object sender, EventArgs e)
        {
            launchDumpSearcher();
        }

        private void launchDumpSearcher()
        {
            WikiFunctions.DatabaseScanner.DatabaseScanner ds = new WikiFunctions.DatabaseScanner.DatabaseScanner();
            ds.Show();
            UpdateButtons();
        }

        private void addIgnoredToLogFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (addIgnoredToLogFileToolStripMenuItem.Checked)
            {
                btnFalsePositive.Visible = true;
                btntsFalsePositive.Visible = true;
                btnStop.Location = new System.Drawing.Point(217, 57);
                btnStop.Size = new System.Drawing.Size(49, 21);
            }
            else
            {
                btnFalsePositive.Visible = false;
                btntsFalsePositive.Visible = false;
                btnStop.Location = new System.Drawing.Point(165, 57);
                btnStop.Size = new System.Drawing.Size(101, 21);
            }
        }

        private void alphaSortInterwikiLinksToolStripMenuItem_CheckStateChanged(object sender, EventArgs e)
        {
            parsers.sortInterwikiOrder = alphaSortInterwikiLinksToolStripMenuItem.Checked;
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
                if (e.KeyCode == Keys.S && btnSave.Enabled)
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
                    SkipPage("user");
                    e.SuppressKeyPress = true;
                    return;
                }
                if (e.KeyCode == Keys.D && btnDiff.Enabled)
                {
                    GetDiff();
                    e.SuppressKeyPress = true;
                    return;
                }
                if (e.KeyCode == Keys.E && btnPreview.Enabled)
                {
                    GetPreview();
                    e.SuppressKeyPress = true;
                    return;
                }
                if (e.KeyCode == Keys.F)
                {
                    Find.find(txtFind.Text, chkFindRegex.Checked, chkFindCaseSensitive.Checked, 
                        txtEdit, TheArticle.Name);
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

        private void copyToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            if ((webBrowserEdit.Document != null)) webBrowserEdit.Document.ExecCommand("Copy", false, System.DBNull.Value);
        }

        private void copyToolStripMenuItem2_Click(object sender, EventArgs e)
        {
            if ((webBrowserDiff.Document != null)) webBrowserDiff.Document.ExecCommand("Copy", false, System.DBNull.Value);
        }

        private void listToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = Tools.HTMLListToWiki(txtEdit.SelectedText, "*");
        }

        private void listToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = Tools.HTMLListToWiki(txtEdit.SelectedText, "#");
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

        private void humanNameDisambigTagToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = "{{Hndis|name=" + Tools.MakeHumanCatKey(TheArticle.Name) + "}}";
        }

        private void wikifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = "{{Wikify|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}\r\n\r\n" + txtEdit.Text;
        }

        private void cleanupToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = "{{cleanup|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}\r\n\r\n" + txtEdit.Text;
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
            txtEdit.SelectedText = "{{Uncategorized|date={{subst:CURRENTMONTHNAME}} {{subst:CURRENTYEAR}}}}";
        }

        private void bypassAllRedirectsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //txtEdit.Text = parsers.BypassRedirects(txtEdit.Text);
            if (MessageBox.Show("Replacement of links to redirects with direct links is strongly discouraged, " +
                "however it could be useful in some circumstances. Are you sure you want to continue?",
                "Bypass redirects", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)

                return;

            BackgroundRequest r = new BackgroundRequest();

            Enabled = false;
            r.BypassRedirects(txtEdit.Text);
            while (!r.Done) Application.DoEvents();
            Enabled = true;

            txtEdit.Text = (string)r.Result;
        }

        private void unicodifyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            string text = txtEdit.SelectedText;
            text = parsers.Unicodify(text);
            txtEdit.SelectedText = text;
        }

        private void metadataTemplateToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = "{{Persondata\r\n|NAME=\r\n|ALTERNATIVE NAMES=\r\n|SHORT DESCRIPTION=\r\n|DATE OF BIRTH=\r\n|PLACE OF BIRTH=\r\n|DATE OF DEATH=\r\n|PLACE OF DEATH=\r\n}}";
        }

        private void humanNameCategoryKeyToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = "{{DEFAULTSORT:" + Tools.MakeHumanCatKey(TheArticle.Name) + "}}";
        }

        private void birthdeathCatsToolStripMenuItem_Click(object sender, EventArgs e)
        {
            //find first dates
            string strBirth = "";
            string strDeath = "";
            Regex RegexDates = new Regex("[1-2][0-9]{3}");

            try
            {
                MatchCollection m = RegexDates.Matches(txtEdit.Text);

                if (m.Count >= 1)
                    strBirth = m[0].Value;
                if (m.Count >= 2)
                    strDeath = m[1].Value;

                //make name, surname, firstname
                string strName = Tools.MakeHumanCatKey(TheArticle.Name);

                string Categories = "";

                if (strDeath.Length == 0 || int.Parse(strDeath) < int.Parse(strBirth) + 20)
                    Categories = "[[Category:" + strBirth + " births|" + strName + "]]";
                else
                    Categories = "[[Category:" + strBirth + " births|" + strName + "]]\r\n[[Category:" + strDeath + " deaths|" + strName + "]]";

                txtEdit.SelectedText = Categories;
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        private void stubToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = toolStripTextBox1.Text;
        }
        private void contextMenuStrip1_Opening(object sender, CancelEventArgs e)
        {
            try
            {
                txtEdit.Focus();

                cutToolStripMenuItem.Enabled = copyToolStripMenuItem.Enabled =
                    openSelectionInBrowserToolStripMenuItem.Enabled = (txtEdit.SelectedText.Length > 0);

                undoToolStripMenuItem.Enabled = txtEdit.CanUndo;

                openPageInBrowserToolStripMenuItem.Enabled = TheArticle.Name.Length > 0;
                openTalkPageInBrowserToolStripMenuItem.Enabled = TheArticle.Name.Length > 0;
                openHistoryMenuItem.Enabled = TheArticle.Name.Length > 0;
                replaceTextWithLastEditToolStripMenuItem.Enabled = LastArticle.Length > 0;
            }
            catch { }
        }

        private void openPageInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.URLLong + "index.php?title=" + TheArticle.URLEncodedName);
            }
            catch { }
        }

        private void openTalkPageInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.URLLong + "index.php?title=" + GetLists.ConvertToTalk(TheArticle));
            }
            catch { }
        }

        private void openHistoryMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.URLLong + "index.php?title=" + TheArticle.URLEncodedName + "&action=history");
            }
            catch { }
        }

        private void openSelectionInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start(Variables.URLLong + "index.php?title=" + txtEdit.SelectedText);
            }
            catch { }
        }

        private void chkGeneralParse_CheckedChanged(object sender, EventArgs e)
        {
            alphaSortInterwikiLinksToolStripMenuItem.Enabled = chkGeneralFixes.Checked;
        }

        private void btnFindAndReplaceAdvanced_Click(object sender, EventArgs e)
        {
            if (!replaceSpecial.Visible)
                replaceSpecial.Show();
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
            PageReload = false;
            NudgeTimer.Stop();
            UpdateButtons();
            if (intTimer > 0)
            {//stop and reset the bot timer.
                stopDelayedAutoSaveTimer();
                EnableButtons();
                return;
            }

            stopSaveInterval();
            StopDelayedRestartTimer();
            if (webBrowserEdit.IsBusy)
                webBrowserEdit.Stop2();
            if (Variables.User.webBrowserLogin.IsBusy)
                Variables.User.webBrowserLogin.Stop();

            listMaker1.Stop();

            if (AutoSaveEditBoxEnabled)
                EditBoxSaveTimer.Enabled = false;

            lblStatusText.Text = "Stopped";
        }

        private void helpToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            Help.ShowHelp(h);
        }

        private void reparseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            ArticleEx a = new ArticleEx(TheArticle.Name);

            a.OriginalArticleText = txtEdit.Text;
            ProcessPage(a);
            txtEdit.Text = a.ArticleText;
            GetDiff();
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
            string text = RemoveText.Hide(txtEdit.Text);
            text = parsers.RemoveAllWhiteSpace(text);
            text = RemoveText.AddBack(text);

            txtEdit.Text = text;
        }

        private void txtNewCategory_DoubleClick(object sender, EventArgs e)
        {
            txtNewCategory.SelectAll();
        }

        private void cmboEditSummary_MouseMove(object sender, MouseEventArgs e)
        {
            string EditSummary = "";

            if (TheArticle != null)
                EditSummary = TheArticle.EditSummary;

            if (EditSummary == "")
                toolTip1.SetToolTip(cmboEditSummary, "");
            else
                toolTip1.SetToolTip(cmboEditSummary, MakeSummary());
        }

        private void tabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            UpdateButtons();
        }

        private void chkRegExTypo_CheckedChanged(object sender, EventArgs e)
        {
            if (BotMode && chkRegExTypo.Checked)
            {
                MessageBox.Show("RegexTypoFix cannot be used with auto save on.\r\nAutosave will now be turned off, and Typos loaded.", "Warning", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                BotMode = false;
                //return;
            }
            loadTypos(false);
            chkSkipIfNoRegexTypo.Enabled = chkRegExTypo.Checked;
        }

        private void loadTypos(bool Reload)
        {
            if (chkRegExTypo.Checked)
            {
                lblStatusText.Text = "Loading typos";

                string s = Variables.RETFPath;

                if (!s.StartsWith("http:")) s = Variables.URL + "/wiki/" + Tools.WikiEncode(s);

                string message = @"1. Check each edit before you make it. Although this has been built to be very accurate there is always the possibility of an error which requires your attention.

2. Optional: Select [[WP:AWB/T|Typo fixing]] as the edit summary. This lets everyone know where to bring issues with the typo correction.";

                if (RegexTypos == null)
                    message += "\r\n\r\nThe newest typos will now be downloaded from " + s + " when you press OK.";

                MessageBox.Show(message, "Attention", MessageBoxButtons.OK, MessageBoxIcon.Warning);

                if (RegexTypos == null || Reload)
                {
                    RegexTypos = new RegExTypoFix();
                    lblStatusText.Text = RegexTypos.TyposCount.ToString() + " typos loaded";
                }
            }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://en.wikipedia.org/wiki/Wikipedia:AutoWikiBrowser/Typos");
            }
            catch { }
        }

        private void webBrowserEdit_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            toolStripProgressBar1.MarqueeAnimationSpeed = 0;
            toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
        }

        private void webBrowserEdit_Navigating(object sender, WebBrowserNavigatingEventArgs e)
        {
            webBrowserEdit.BringToFront();
            toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            toolStripProgressBar1.MarqueeAnimationSpeed = 100;
        }

        private void dumpHTMLToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = webBrowserEdit.Document.Body.InnerHtml;
        }

        private void logOutDebugToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Variables.User.WikiStatus = false;
        }

        private void summariesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            SummaryEditor se = new SummaryEditor();

            string[] summaries = new string[cmboEditSummary.Items.Count];
            cmboEditSummary.Items.CopyTo(summaries, 0);
            se.Summaries.Lines = summaries;
            se.Summaries.Select(0, 0);

            string PrevSummary = cmboEditSummary.SelectedText;

            if (se.ShowDialog() == DialogResult.OK)
            {
                cmboEditSummary.Items.Clear();

                foreach (string s in se.Summaries.Lines)
                {
                    if (s.Trim() == "") continue;
                    cmboEditSummary.Items.Add(s.Trim());
                }

                if (cmboEditSummary.Items.Contains(PrevSummary))
                    cmboEditSummary.SelectedText = PrevSummary;
                else cmboEditSummary.SelectedItem = 0;
            }
        }

        private void showHidePanelToolStripMenuItem_Click(object sender, EventArgs e)
        {
            panelShowHide();
        }

        private void enlargeEditAreaToolStripMenuItem_Click(object sender, EventArgs e)
        {
            parametersShowHide();
        }

        #endregion

        #region tool bar stuff

        private void btnShowHide_Click(object sender, EventArgs e)
        {
            panelShowHide();
        }

        private void btntsShowHideParameters_Click(object sender, EventArgs e)
        {
            parametersShowHide();
        }

        private void btntsStart_Click(object sender, EventArgs e)
        {
            Start();
        }

        private void btntsSave_Click(object sender, EventArgs e)
        {
            Save();
        }

        private void btntsIgnore_Click(object sender, EventArgs e)
        {
            SkipPage("user");
        }

        private void btntsStop_Click(object sender, EventArgs e)
        {
            Stop();
        }

        private void btntsPreview_Click(object sender, EventArgs e)
        {
            GetPreview();
        }

        private void btntsChanges_Click(object sender, EventArgs e)
        {
            GetDiff();
        }

        private void setBrowserSize()
        {
            if (toolStrip.Visible)
            {
                webBrowserEdit.Location = new Point(webBrowserEdit.Location.X, 48);
                if (panel1.Visible)
                    webBrowserEdit.Height = panel1.Location.Y - 48;
                else
                    webBrowserEdit.Height = statusStrip1.Location.Y - 48;
            }
            else
            {
                webBrowserEdit.Location = new Point(webBrowserEdit.Location.X, 25);
                if (panel1.Visible)
                    webBrowserEdit.Height = panel1.Location.Y - 25;
                else
                    webBrowserEdit.Height = statusStrip1.Location.Y - 25;
            }

            webBrowserDiff.Location = webBrowserEdit.Location;
            webBrowserDiff.Size = webBrowserEdit.Size;
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
                lblImageWith.Text = "";

                txtImageReplace.Enabled = false;
                txtImageWith.Enabled = false;
                chkSkipNoImgChange.Enabled = false;
            }
            else if (cmboImages.SelectedIndex == 1)
            {
                lblImageWith.Text = "With Image:";

                txtImageWith.Enabled = true;
                txtImageReplace.Enabled = true;
                chkSkipNoImgChange.Enabled = true;
            }
            else if (cmboImages.SelectedIndex == 2)
            {
                lblImageWith.Text = "";

                txtImageWith.Enabled = false;
                txtImageReplace.Enabled = true;
                chkSkipNoImgChange.Enabled = true;
            }
            else if (cmboImages.SelectedIndex == 3)
            {
                lblImageWith.Text = "Comment:";

                txtImageWith.Enabled = true;
                txtImageReplace.Enabled = true;
                chkSkipNoImgChange.Enabled = true;
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

        private void SetProgressBar()
        {
            if (listMaker1.BusyStatus)
            {
                toolStripProgressBar1.MarqueeAnimationSpeed = 100;
                toolStripProgressBar1.Style = ProgressBarStyle.Marquee;
            }
            else
            {
                toolStripProgressBar1.MarqueeAnimationSpeed = 0;
                toolStripProgressBar1.Style = ProgressBarStyle.Continuous;
            }
        }

        #endregion

        #region ArticleActions
        private void MoveArticle()
        {
            MoveDeleteDialog dlg = new MoveDeleteDialog(1);

            try
            {
                dlg.NewTitle = TheArticle.Name;
                dlg.Summary = LastMove;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LastMove = dlg.Summary;
                    webBrowserEdit.MovePage(TheArticle.Name, dlg.NewTitle, dlg.Summary);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
            finally
            {
                dlg.Dispose();
            }
        }

        private void DeleteArticle()
        {
            MoveDeleteDialog dlg = new MoveDeleteDialog(2);

            try
            {
                dlg.Summary = LastDelete;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LastDelete = dlg.Summary;
                    webBrowserEdit.DeletePage(TheArticle.Name, dlg.Summary);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
            finally
            {
                dlg.Dispose();
            }
        }

        private void ProtectArticle()
        {
            MoveDeleteDialog dlg = new MoveDeleteDialog(3);

            try
            {
                dlg.Summary = LastProtect;

                if (dlg.ShowDialog(this) == DialogResult.OK)
                {
                    LastProtect = dlg.Summary;
                    webBrowserEdit.ProtectPage(TheArticle.Name, dlg.Summary, dlg.EditProtectionLevel, dlg.MoveProtectionLevel, dlg.ProtectExpiry);
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
            finally
            {
                dlg.Dispose();
            }
        }

        #endregion

        private void btnSubst_Click(object sender, EventArgs e)
        {
            substTemplates.ShowDialog();
        }

        private void testRegexToolStripMenuItem_Click(object sender, EventArgs e)
        {
            regexTester = new RegexTester();

            if (txtEdit.SelectionLength > 0 && MessageBox.Show("Would you like to transfer the currently selected Article Text to the Regex Tester?", "Transfer Article Text?", MessageBoxButtons.YesNo) == DialogResult.Yes)
                    regexTester.ArticleText = txtEdit.SelectedText;
            
            regexTester.Show();
        }

        private void chkLock_CheckedChanged(object sender, EventArgs e)
        {
            cmboEditSummary.Visible = !chkLock.Checked;
            lblSummary.Text = cmboEditSummary.Text;
            lblSummary.Visible = chkLock.Checked;
        }

        private void txtDabLink_TextChanged(object sender, EventArgs e)
        {
            btnLoadLinks.Enabled = txtDabLink.Text.Trim() != "";
        }

        private void txtDabLink_Enter(object sender, EventArgs e)
        {
            if (txtDabLink.Text == "") txtDabLink.Text = listMaker1.SourceText;
        }

        private void chkEnableDab_CheckedChanged(object sender, EventArgs e)
        {
            panelDab.Enabled = chkEnableDab.Checked;
        }

        private void btnLoadLinks_Click(object sender, EventArgs e)
        {
            try
            {
                string name = txtDabLink.Text.Trim();
                if (name.Contains("|")) name = name.Substring(0, name.IndexOf('|') - 1);
                Article link = new WikiFunctions.Article(name);
                List<Article> l = GetLists.FromLinksOnPage(txtDabLink.Text);
                txtDabVariants.Text = "";
                foreach (Article a in l)
                {
                    uint i;
                    // exclude years
                    if (uint.TryParse(a.Name, out i) && (i < 2100)) continue;

                    // disambigs typically link to pages in the same namespace only
                    if (link.NameSpaceKey != a.NameSpaceKey) continue;

                    txtDabVariants.Text += a.Name + "\r\n";
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        private void txtDabLink_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '\r':
                    e.Handled = true;
                    btnLoadLinks_Click(this, null);
                    break;
            }
        }

        private void txtFind_KeyPress(object sender, KeyPressEventArgs e)
        {
            switch (e.KeyChar)
            {
                case '\r':
                    e.Handled = true;
                    btnFind_Click(this, null);
                    break;
            }
        }

#region NotifyTray
        private void showToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (!this.Visible)
                toolStripHide();
        }

        private void hideToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (this.Visible)
                this.Visible = false;
        }

        private void exitToolStripMenuItem1_Click(object sender, EventArgs e)
        {
            this.Close();
            Application.Exit();
        }

        private void ntfyTray_MouseDoubleClick(object sender, MouseEventArgs e)
        {
            if (!this.Visible)
                toolStripHide();
            else
                this.Visible = false;
        }

        private void mnuNotify_Opening(object sender, CancelEventArgs e)
        {
            SetMenuVisibility(this.Visible);
        }

        private void SetMenuVisibility(bool Visible)
        {
            showToolStripMenuItem.Enabled = Visible;
            hideToolStripMenuItem.Enabled = !Visible;
        }
#endregion

        private void toolStripHide()
        {
            this.Visible = true;
            this.WindowState = System.Windows.Forms.FormWindowState.Maximized;
        }

        private void updateUpdater()
        {
            Updater Updater = new Updater();
            Updater.Update();
        }

        public void NotifyBalloon(string Message, ToolTipIcon Icon)
        {
            ntfyTray.BalloonTipText = Message;
            ntfyTray.BalloonTipIcon = Icon;
            ntfyTray.ShowBalloonTip(10000);
        }

        private void btnRemove_Click(object sender, EventArgs e)
        {
            tabControl2.SelectedTab = tpEdit;
            string selectedtext = txtEdit.SelectedText;
            if (selectedtext.StartsWith("[[") && selectedtext.EndsWith("]]"))
            {
                selectedtext = selectedtext.Trim('[').Trim(']');
                if (selectedtext.EndsWith("|"))
                {
                    if (selectedtext.Contains("(") && selectedtext.Contains(")"))
                        selectedtext = selectedtext.Substring(0, selectedtext.IndexOf("("));
                    if (selectedtext.Contains(":"))
                        selectedtext = selectedtext.Substring(selectedtext.IndexOf(":")).TrimEnd('|');
                    if ("[[" + selectedtext + "]]" == txtEdit.SelectedText)
                    {
                        MessageBox.Show("The selected link could not be removed.");
                        selectedtext = "[[" + selectedtext + "]]";
                    }
                }
                else if (selectedtext.Contains("|"))
                    selectedtext = selectedtext.Substring(selectedtext.IndexOf("|") + 1);

                txtEdit.SelectedText = selectedtext;
            }
            else
                MessageBox.Show("Please select a link to remove either manually or by clicking a link in the list above.");
        }

        private void runUpdaterToolStripMenuItem_Click(object sender, EventArgs e)
        {
            runUpdater();
        }

        private void runUpdater()
        {

            if (MessageBox.Show("AWB needs to be closed. To do this now, click 'yes'. If you need to save your settings, do this now, the updater will not complete until AWB is closed.", "Close AWB?", MessageBoxButtons.YesNoCancel, MessageBoxIcon.Question) == DialogResult.Yes)
            {
                try
                {
                    System.Diagnostics.Process.Start(Path.GetDirectoryName(Application.ExecutablePath) + "\\AWBUpdater.exe");
                }
                catch { }
                this.Close();
                Application.Exit();
            }
        }

        private void btnResetNudges_Click(object sender, EventArgs e)
        {
            Nudges = 0;
            sameArticleNudges = 0;
            lblNudges.Text = NudgeTimerString + "0";
        }

        #region "Nudge timer"
        private const string NudgeTimerString = "Total nudges: ";

        private void NudgeTimer_Tick(object sender, NudgeTimer.NudgeTimerEventArgs e)
        {
            //make sure there was no error and bot mode is still enabled
            if (BotMode)
            {
                if (GlobalObjects.MyTrace.IsUploading || GlobalObjects.MyTrace.IsGettingPassword)
                {
                    // Don't nudge when a log is uploading in the background or it is attempting to get a password from the user, just wait for it
                    e.Cancel = true; return;
                }

                bool Cancel;
                // Tell plugins we're about to nudge, and give them the opportunity to cancel:
                foreach (KeyValuePair<string, IAWBPlugin> a in Plugin.Items)
                {
                    a.Value.Nudge(out Cancel);
                    if (Cancel)
                    {
                        e.Cancel = true; return;
                    }
                }

                // Update stats and nudge:
                Nudges++;
                lblNudges.Text = NudgeTimerString + Nudges;
                NudgeTimer.Stop();
                if (chkNudgeSkip.Checked && sameArticleNudges > 0)
                {
                    sameArticleNudges = 0;
                    SkipPage("There was an error saving the page twice");
                }
                else
                {
                    sameArticleNudges++;
                    Stop();
                    Start();
                }

                // Inform plugins:
                foreach (KeyValuePair<string, IAWBPlugin> a in Plugin.Items)
                { a.Value.Nudged(Nudges); }
            }
        }

        public int Nudges
        {
            get { return mnudges; }
            private set { mnudges = value; }
        }
        #endregion

        /// <summary>
        /// Save List Box to a text file
        /// </summary>
        /// <param name="listbox"></param>
        public void SaveList(ListBox listbox)
        {
            try
            {
                StringBuilder strList = new StringBuilder("");
                StreamWriter sw;
                string strListFile;
                if (saveListDialog.ShowDialog() == DialogResult.OK)
                {
                    foreach (String a in listbox.Items)
                        strList.AppendLine(a);
                    strListFile = saveListDialog.FileName;
                    sw = new StreamWriter(strListFile, false, Encoding.UTF8);
                    sw.Write(strList);
                    sw.Close();
                }
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        private void EditBoxSaveTimer_Tick(object sender, EventArgs e)
        {
            if (!System.IO.Directory.Exists(Application.StartupPath + "\\EditBoxSaves"))
                System.IO.Directory.CreateDirectory(Application.StartupPath + "\\EditBoxSaves");

            saveEditBoxText(Application.StartupPath + "\\EditBoxSaves\\" + AutoSaveEditBoxFile);
        }

        private void saveEditBoxText(string path)
        {
            try
            {
                StreamWriter sw = new StreamWriter(path.ToString(), false, Encoding.UTF8);
                sw.Write(txtEdit.Text);
                sw.Close();
            }
            catch (IOException ex)
            {
                MessageBox.Show(ex.Message, "File error", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
        }

        private void saveTextToFileToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (saveListDialog.ShowDialog() == DialogResult.OK)
                saveEditBoxText(saveListDialog.FileName);
        }

        private void loadUserTalkWarnings()
        {
            string finalRegex = "\\{\\{ ?(template:)? ?((";
            Regex userTalkTemplate = new Regex(@"# \[\[Template:(.*?)\]\]");
            try
            {
                string text = "";
                try
                {
                    text = Tools.GetHTML(Variables.URLLong + "index.php?title=Wikipedia:AutoWikiBrowser/User talk templates&action=raw&ctype=text/plain&dontcountme=s", Encoding.UTF8);
                }
                catch
                {
                }
                foreach (Match m in userTalkTemplate.Matches(text))
                {
                    try
                    {
                        finalRegex = finalRegex + m.Groups[1].Value + "|";
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorHandler.Handle(ex);
            }
            finalRegex = finalRegex.Trim('|') + ") ?(\\|.*?)?) ?\\}\\}";
            userTalkWarningsLoaded = true;
            userTalkTemplatesRegex = new Regex(finalRegex, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }

        private void undoAllChangesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            txtEdit.Text = TheArticle.OriginalArticleText;
        }

        private void reloadEditPageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            PageReload = true;
            webBrowserEdit.LoadEditPage(TheArticle.Name);
            TheArticle.OriginalArticleText = webBrowserEdit.GetArticleText();
        }

        private void chkSkipNonExistent_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSkipNonExistent.Checked)
                chkSkipExistent.Checked = false;
        }

        private void chkSkipExistent_CheckedChanged(object sender, EventArgs e)
        {
            if (chkSkipExistent.Checked)
                chkSkipNonExistent.Checked = false;
        }

        private void tabControl2_SelectedIndexChanged(object sender, EventArgs e)
        {
            NewHistory();
        }

        private void NewHistory()
        {
            try
            {
                if (tabControl2.SelectedTab == tpHistory)
                {
                    if (webBrowserHistory.Url != new System.Uri(Variables.URLLong + "index.php?title=" + TheArticle.URLEncodedName + "&action=history&useskin=myskin") && TheArticle.URLEncodedName != "")
                        webBrowserHistory.Navigate(Variables.URLLong + "index.php?title=" + TheArticle.URLEncodedName + "&action=history&useskin=myskin");
                }
            }
            catch
            {
                webBrowserHistory.Navigate("about:blank");
                webBrowserHistory.Document.Write("<html><body><p>Unable to load history</p></body></html>");
            }
        }

        private void webBrowserHistory_DocumentCompleted(object sender, WebBrowserDocumentCompletedEventArgs e)
        {
            string histHtml = webBrowserHistory.Document.Body.InnerHtml;
            string startMark = "<!-- start content -->";
            string endMark = "<!-- end content -->";
            if (histHtml.Contains(startMark) && histHtml.Contains(endMark))
                histHtml = histHtml.Substring(histHtml.IndexOf(startMark), histHtml.IndexOf(endMark) - histHtml.IndexOf(startMark));
            histHtml = histHtml.Replace("<A ", "<a target=\"blank\" ");
            histHtml = histHtml.Replace("<FORM ", "<form target=\"blank\" ");
            histHtml = histHtml.Replace("<DIV id=histlegend", "<div id=histlegend style=\"display:none;\"");
            histHtml = "<h3>" + TheArticle.Name + "</h3>" + histHtml;
            webBrowserHistory.Document.Body.InnerHtml = histHtml;
        }

        private void openInBrowserToolStripMenuItem_Click(object sender, EventArgs e)
        {
            Tools.OpenArticleHistoryInBrowser(TheArticle.Name);
        }

        private void refreshHistoryToolStripMenuItem_Click(object sender, EventArgs e)
        {
            try
            {
                webBrowserHistory.Navigate(Variables.URLLong + "index.php?title=" + TheArticle.URLEncodedName + "&action=history&useskin=myskin");
            }
            catch
            {
                webBrowserHistory.Navigate("about:blank");
                webBrowserHistory.Document.Write("<html><body><p>Unable to load history</p></body></html>");
            }
        }

        private void pictureBox1_Click(object sender, EventArgs e)
        {
            try
            {
                System.Diagnostics.Process.Start("http://commons.wikimedia.org/wiki/Image:Crystal_Clear_action_run.png");
            }
            catch { }
        }

        private void profilesToolStripMenuItem_Click(object sender, EventArgs e)
        {
            profiles = new WikiFunctions.AWBProfiles.AWBProfilesForm(webBrowserEdit);
            profiles.LoadProfile += loadProfileSettings;
            profiles.ShowDialog(this);
        }

        private void chkMinor_CheckedChanged(object sender, EventArgs e)
        {
            markAllAsMinorToolStripMenuItem.Checked = chkMinor.Checked;
        }

        private void markAllAsMinorToolStripMenuItem_Click(object sender, EventArgs e)
        {
            chkMinor.Checked = markAllAsMinorToolStripMenuItem.Checked;
        }

        private void loadProfileSettings()
        {
            LoadPrefs(profiles.SettingsToLoad);
            CheckStatus(true);
        }

        private void mnuHistory_Opening(object sender, CancelEventArgs e)
        {
            openInBrowserToolStripMenuItem.Enabled = refreshHistoryToolStripMenuItem.Enabled = (TheArticle != null);
        }

        #region Shutdown
        private void chkShutdown_CheckedChanged(object sender, EventArgs e)
        {
            EnableDisableShutdownControls(chkShutdown.Checked);
        }

        private void EnableDisableShutdownControls(bool Enabled)
        {
            radShutdown.Enabled = radStandby.Enabled = radRestart.Enabled
            = radHibernate.Enabled = radShutdown.Checked = Enabled;
        }

        private void CanShutdown()
        {
            if (chkShutdown.Checked && listMaker1.Count == 0)
            {
                ShutdownTimer.Enabled = true;
                ShutdownNotification shut = new ShutdownNotification();
                shut.ShutdownType = GetShutdownType();

                DialogResult result = shut.ShowDialog(this);

                if (result == DialogResult.Cancel)
                {
                    ShutdownTimer.Enabled = false;
                    MessageBox.Show(GetShutdownType() + " aborted!");
                }
                else if (result == DialogResult.Yes)
                    if (GetShutdownType() == "Standby" || GetShutdownType() == "Hibernate")
                    {
                        shut.Close();
                        shut.Dispose();
                        ShutdownTimer.Enabled = false;
                    }
                    ShutdownComputer();
            }
        }

        private string GetShutdownType()
        {
            if (radShutdown.Checked)
                return "Shutdown";
            else if (radStandby.Checked)
                return "Standby";
            else if (radRestart.Checked)
                return "Restart";
            else if (radHibernate.Checked)
                return "Hibernate";
            else
                return "";
        }

        private void ShutdownComputer()
        {
                if (radHibernate.Checked)
                    Application.SetSuspendState(PowerState.Hibernate, true, true);
                else if (radRestart.Checked)
                    System.Diagnostics.Process.Start("shutdown", "-r");
                else if (radShutdown.Checked)
                    System.Diagnostics.Process.Start("shutdown", "-s");
                else if (radStandby.Checked)
                    Application.SetSuspendState(PowerState.Suspend, true, true);
        }

        private void ShutdownTimer_Tick(object sender, EventArgs e)
        {
            ShutdownComputer();
        }
        #endregion

        #region EditToolbar
        private void imgBold_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "'''Bold text'''";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 12;
                txtEdit.SelectionLength = 9;
            }
            else
            {
                txtEdit.SelectedText = "'''" + txtEdit.SelectedText + "'''";
            }
        }

        private void imgItalics_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "''Italic text''";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 13;
                txtEdit.SelectionLength = 11;
            }
            else
            {
                txtEdit.SelectedText = "''" + txtEdit.SelectedText + "''";
            }
        }

        private void imgLink_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "[[Link title]]";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 12;
                txtEdit.SelectionLength = 10;
            }
            else
            {
                txtEdit.SelectedText = "[[" + txtEdit.SelectedText + "]]";
            }
        }

        private void imgExtlink_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "[http://www.example.com link title]";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 34;
                txtEdit.SelectionLength = 33;
            }
            else
            {
                txtEdit.SelectedText = "[" + txtEdit.SelectedText + "]";
            }
        }

        private void imgMath_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "<math>Insert formula here</math>";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 26;
                txtEdit.SelectionLength = 19;
            }
            else
            {
                txtEdit.SelectedText = "<math>" + txtEdit.SelectedText + "</math>";
            }
        }

        private void imgNowiki_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "<nowiki>Insert non-formatted text here</nowiki>";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 39;
                txtEdit.SelectionLength = 30;
            }
            else
            {
                txtEdit.SelectedText = "<nowiki>" + txtEdit.SelectedText + "</nowiki>";
            }
        }

        private void imgHr_Click(object sender, EventArgs e)
        {
            txtEdit.SelectedText = txtEdit.SelectedText + "\r\n----\r\n";
        }

        private void imgRedirect_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "#REDIRECT [[Insert text]]";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 13;
                txtEdit.SelectionLength = 11;
            }
            else
            {
                txtEdit.SelectedText = "#REDIRECT [[" + txtEdit.SelectedText + "]]";
            }
        }

        private void imgStrike_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "<s>Strike-through text</s>";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 23;
                txtEdit.SelectionLength = 19;
            }
            else
            {
                txtEdit.SelectedText = "<s>" + txtEdit.SelectedText + "</s>";
            }
        }

        private void imgSup_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "<sup>Superscript text</sup>";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 22;
                txtEdit.SelectionLength = 16;
            }
            else
            {
                txtEdit.SelectedText = "<sup>" + txtEdit.SelectedText + "</sup>";
            }
        }

        private void imgSub_Click(object sender, EventArgs e)
        {
            if (txtEdit.SelectionLength == 0)
            {
                txtEdit.SelectedText = "<sub>Subscript text</sub>";
                txtEdit.SelectionStart = txtEdit.SelectionStart - 20;
                txtEdit.SelectionLength = 14;
            }
            else
            {
                txtEdit.SelectedText = "<sub>" + txtEdit.SelectedText + "</sub>";
            }
        }

        private void SetToolBarEnabled(bool Enabled)
        {
            imgBold.Enabled = imgExtlink.Enabled = imgHr.Enabled = imgItalics.Enabled = imgLink.Enabled =
            imgMath.Enabled = imgNowiki.Enabled = imgRedirect.Enabled = imgStrike.Enabled = imgSub.Enabled =
            imgSup.Enabled = Enabled;
        }

        private void SetToolBarVisible(bool Visible)
        {
            if (Visible)
            {
                txtEdit.Location = new Point(txtEdit.Location.X, txtEdit.Location.Y + 32);
                txtEdit.Size = new Size(txtEdit.Size.Width, txtEdit.Size.Height - 32);
            }
            else
            {
                txtEdit.Location = new Point(txtEdit.Location.X, txtEdit.Location.Y - 32);
                txtEdit.Size = new Size(txtEdit.Size.Width, txtEdit.Size.Height + 32);
            }

            imgBold.Visible = imgExtlink.Visible = imgHr.Visible = imgItalics.Visible = imgLink.Visible =
            imgMath.Visible = imgNowiki.Visible = imgRedirect.Visible = imgStrike.Visible = imgSub.Visible =
            imgSup.Visible = Visible;
        }
#endregion

        private void showHideEditToolbarToolStripMenuItem_Click(object sender, EventArgs e)
        {
            showHideEditToolbarToolStripMenuItem.Checked = !showHideEditToolbarToolStripMenuItem.Checked;
            SetToolBarVisible(showHideEditToolbarToolStripMenuItem.Checked);
        }

        private void txtFind_MouseHover(object sender, EventArgs e)
        {
            toolTip1.SetToolTip(txtFind, txtFind.Text);
        }
    }
}
