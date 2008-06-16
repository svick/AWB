using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Text.RegularExpressions;

namespace WikiFunctions
{
    /// <summary>
    /// This class provides helper functions for handling errors and displaying them to users
    /// </summary>
    public partial class ErrorHandler : Form
    {
        /// <summary>
        /// Title of the page currently being processed
        /// </summary>
        public static string CurrentPage;

        /// <summary>
        /// Revision of the page currently being processed
        /// </summary>
        public static int CurrentRevision;

        /// <summary>
        /// Current text that the list is being made from in ListMaker
        /// </summary>
        public static string ListMakerText;

        protected ErrorHandler()
        {
            InitializeComponent();
        }

        private void ErrorHandler_Load(object sender, EventArgs e)
        {
            Text = Application.ProductName;
        }

        /// <summary>
        /// Displays exception information. Should be called from try...catch handlers
        /// </summary>
        /// <param name="ex">Exception object to handle</param>
        new public static void Handle(Exception ex)
        {
            if (ex != null)
            {
                // invalid regex - only ArgumentException, without subclasses
                if (ex.GetType().ToString().Equals("System.ArgumentException")
                    && ex.StackTrace.Contains("System.Text.RegularExpressions"))
                {
                    MessageBox.Show(ex.Message, "Invalid regular expression",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                // network access error
                else if (ex is System.Net.WebException)
                {
                    MessageBox.Show(ex.Message, "Network access error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                // out of memory error
                else if (ex is System.OutOfMemoryException)
                {
                    MessageBox.Show(ex.Message, "Out of Memory error",
                        MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
                else // suggest a bug report for other exceptions
                {
                    ErrorHandler handler = new ErrorHandler();

                    handler.txtError.Text = ex.Message;

                    StringBuilder errorMessage = new StringBuilder("{{AWB bug\r\n | status      = new <!-- when fixed replace with \"fixed\" -->\r\n | description = ");

                    if (Thread.CurrentThread.Name != "Main thread")
                        errorMessage.Append("\r\nThread: " + Thread.CurrentThread.Name);

                    errorMessage.Append("<table>");
                    FormatException(ex, errorMessage, true);
                    errorMessage.Append("</table>\r\n~~~~\r\n | OS          = " + Environment.OSVersion.ToString() + "\r\n | version     = " + Assembly.GetExecutingAssembly().GetName().Version.ToString());

                    if (!Variables.Revision.Contains("?")) errorMessage.Append(", revision " + Variables.Revision);

                    if (!string.IsNullOrEmpty(CurrentPage))
                    {
                        string link = "[" + Variables.URLLong + "index.php?title=" + Tools.WikiEncode(CurrentPage) + "&oldid=" + CurrentRevision.ToString() + "]";

                        errorMessage.Append("\r\n | duplicate = [encountered while processing page ''" + link + "'']");
                    } else if (!string.IsNullOrEmpty(ListMakerText))
                        errorMessage.Append("\r\n | duplicate = '''ListMaker Text:''' " + ListMakerText);

                    errorMessage.Append("\r\n}}");

                    handler.txtDetails.Text = errorMessage.ToString();

                    handler.txtSubject.Text = ex.GetType().Name + " in " + Thrower(ex);

                    MethodNames(ex);

                    handler.ShowDialog();
                }
            }
        }

        /// <summary>
        /// Formats exception information for bug report
        /// </summary>
        /// <param name="ex">Exception to process</param>
        /// <param name="sb">StringBuilder used for output</param>
        /// <param name="topLevel">false if exception is nested, true otherwise</param>
        private static void FormatException(Exception ex, StringBuilder sb, bool topLevel)
        {
            sb.Append("<tr><td>" + (topLevel ? "Exception" : "Inner exception") + ":<td><code>" 
                + ex.GetType().Name + "</code><tr><td>Message:<td><code>"
                + ex.Message + "</code><tr><td>Call stack:<td><pre>" + ex.StackTrace + "</pre></tr>\r\n");

            if (ex.InnerException != null)
            {
                FormatException(ex.InnerException, sb, false);
            }
        }

        /// <summary>
        /// Returns names of functions in stack trace of an exception
        /// </summary>
        /// <param name="ex">Exception to process</param>
        /// <returns>List of fully qualified function names</returns>
        private static string[] MethodNames(Exception ex)
        {
            MatchCollection mc = Regex.Matches(ex.StackTrace, @"([a-zA-Z_0-9.]+)(?=\()");

            string[] res = new string[mc.Count];

            for (int i = 0; i < res.Length; i++) res[i] = mc[i].Groups[1].Value;

            return res;
        }

        static readonly string[] PresetNamespaces =
            new string[] { "System", "Microsoft", "Mono" };

        /// <summary>
        /// Returns the name of our function where supposedly error resides;
        /// it's the last non-framework function in the stack
        /// </summary>
        /// <param name="ex">Exception to process</param>
        /// <returns>Function names without namespace</returns>
        private static string Thrower(Exception ex)
        {
            string[] trace = MethodNames(ex);

            int i;
            for (i = 0; i < trace.Length; i++)
            {
                bool l = false;
                foreach (string ns in PresetNamespaces)
                {
                    if (trace[i].StartsWith(ns)) l = true;
                }
                if (!l) break;
            }

            // strip namespace for clarity
            string res = Regex.Match(trace[i], @"\w+\.\w+$").Value;

            return res;
        }


        private void btnCopy_Click(object sender, EventArgs e)
        {
            try
            {
                Clipboard.Clear();
                System.Threading.Thread.Sleep(1000);
                Clipboard.SetText(txtDetails.Text);
            }
            catch { }
        }

        private void linkLabel1_LinkClicked(object sender, LinkLabelLinkClickedEventArgs e)
        {
            linkLabel1.LinkVisited = true;
            Tools.OpenURLInBrowser("http://en.wikipedia.org/w/index.php?title=Wikipedia_talk:AutoWikiBrowser/Bugs&action=edit&section=new");
        }
    }
}
