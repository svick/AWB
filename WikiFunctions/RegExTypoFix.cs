using System;
using System.Collections.Generic;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

namespace WikiFunctions
{
    public class RegExTypoFix
    {
        public RegExTypoFix()
        {
            MakeRegexes();
        }    

        Dictionary<Regex, string> TypoRegexes = new Dictionary<Regex, string>();

        public void MakeRegexes()
        {
            Dictionary<string, string> TypoStrings = new Dictionary<string, string>();

            TypoStrings.Add("(T|t)hier", "$1heir");

            Regex r;
            RegexOptions roptions = RegexOptions.None;
            foreach(KeyValuePair<string, string> k in TypoStrings)
            {
                r = new Regex(k.Key, roptions);
                TypoRegexes.Add(r, k.Value);
            }
        }

        Match findMatch;
        MatchCollection Matches;
        string summary = "";

        public string PerformTypoFixes(string ArticleText, ref bool NoChange)
        {            
            hashLinks.Clear();
            ArticleText = RemoveLinks(ArticleText);
            string OriginalText = ArticleText;
            string Replace = "";

            foreach (KeyValuePair<Regex, string> k in TypoRegexes)
            {                
                findMatch = k.Key.Match(ArticleText);

                if (findMatch.Success)
                {
                    Replace = k.Value;
                    ArticleText = k.Key.Replace(ArticleText, Replace);

                    Matches = k.Key.Matches(ArticleText);                    

                    int i = 0;
                    foreach (Group g in findMatch.Groups)
                    {
                        Replace = Replace.Replace("$" + i.ToString(), g.Value);
                        i++;
                    }

                    if (findMatch.Value != Replace)
                    {
                        summary = findMatch.Value + " → " + Replace;

                        if (Matches.Count > 1)
                            summary += " (" + Matches.Count.ToString() + ")";

                        EditSummary += summary + ", ";
                    }
                }
            }            

            if (OriginalText == ArticleText)
                NoChange = true;
            else
                NoChange = false;

            ArticleText = AddLinks(ArticleText);
            return ArticleText;
        }

        string strSummary = "";
        public string EditSummary
        {
            get { return strSummary; }
            set { strSummary = value; }
        }

        Hashtable hashLinks = new Hashtable();
        readonly Regex NoLinksRegex = new Regex("<nowiki>.*?</nowiki>|<math>.*?</math>|<!--.*?-->|[Hh]ttp://[^\\ ]*|\\[[Hh]ttp:.*?\\]|\\[\\[[Ii]mage:.*?\\]\\]|\\[\\[([a-z]{2,3}|simple|fiu-vro|minnan|roa-rup|tokipona|zh-min-nan):.*\\]\\]", RegexOptions.Singleline | RegexOptions.Compiled);
        private string RemoveLinks(string articleText)
        {
            hashLinks.Clear();
            string s = "";

            int i = 0;
            foreach (Match m in NoLinksRegex.Matches(articleText))
            {
                s = "⌊⌊⌊⌊" + i.ToString() + "⌋⌋⌋⌋";

                articleText = articleText.Replace(m.Value, s);
                hashLinks.Add(s, m.Value);
                i++;
            }

            return articleText;
        }

        private string AddLinks(string articleText)
        {
            foreach (DictionaryEntry D in hashLinks)
                articleText = articleText.Replace(D.Key.ToString(), D.Value.ToString());

            hashLinks.Clear();
            return articleText;
        }
    }
}
