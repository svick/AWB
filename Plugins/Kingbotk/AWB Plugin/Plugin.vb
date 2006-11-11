' We might want to load the article and check it's not a redirect, not redlinked, what categories it is in etc
' (If so, should probably do before processing the talk page, and should probably be in base class)

' TODO: WPBio: Skip if contains WPBeatles/TheBeatlesArticle/KLF, so that don't need to have ''any'' settings in AWB
' TODO: Don't skip if contains an **empty** importance=param. Replace it with priority=.
' TODO: Should priority be placed immediately after class, instead of at the end?

Namespace AWB
    Public Enum Namespaces As Integer
        Media = -2
        Special = -1
        Main = 0
        Talk
        User
        UserTalk
        Project
        ProjectTalk
        Image
        ImageTalk
        Mediawiki
        MediawikiTalk
        Template
        TemplateTalk
        Help
        HelpTalk
        Category
        CategoryTalk
        Portal = 100
        PortalTalk
    End Enum
End Namespace

Namespace AWB.Plugins.SDKSoftware.Kingbotk
    ''' <summary>
    ''' An object representing an article which may or may not contain the targetted template
    ''' </summary>
    Public Class Article
        Friend Skip As Boolean = True
        Friend FoundTemplate As Boolean = False, Major As Boolean = False
        Friend EditSummary As String = "([[User:Kingbotk#FAQ|FAQ]], [[User:Kingbotk/" ' save the first bracket in the AWB edit summary box, so it doesn't complain
        ' length=61, maxlength=200, remaining=139
        Friend TemplateParameters As New Dictionary(Of String, TemplateParametersObject)
        Private PreferredTemplateNameRegex As Regex, PreferredTemplateNameWiki As String

        Friend Sub FinaliseEditSummary()
            If Skip Then
                EditSummary = "This article should have been skipped"
            Else
                EditSummary = Regex.Replace(EditSummary, ", $", ".")
            End If
        End Sub
        Friend Sub CheckTemplateCall(ByVal TemplateCall As String)
            If Not TemplateCall = "" Then ' we have "template:"
                Skip = False
                EditSummary += "Remove ""template:"", "
            End If
        End Sub
        Friend Sub CheckTemplateName(ByVal TemplateName As String)
            If Not PreferredTemplateNameRegex.Match(TemplateName).Success Then
                Skip = False
                EditSummary += TemplateName + "→" + PreferredTemplateNameWiki + ", "
            End If
        End Sub
        Friend Sub AddTemplateParm(ByVal NewParm As TemplateParametersObject)
            On Error Resume Next
            ' TODO: Can we make this more robust for duplicate parms? Or is there no answer to such things?
            TemplateParameters.Add(NewParm.Name, NewParm)
        End Sub

        Friend Class TemplateParametersObject
            Public Name As String
            Public Value As String

            Friend Sub New(ByVal ParameterName As String, ByVal ParameterValue As String)
                Name = ParameterName
                Value = ParameterValue
            End Sub
        End Class

        Friend Sub New(ByVal objPreferredTemplateName As Regex, ByVal strPreferredTemplateNameWiki As String, _
        ByVal PluginWikiShortcut As String, ByVal JobSummary As String)
            PreferredTemplateNameRegex = objPreferredTemplateName
            PreferredTemplateNameWiki = strPreferredTemplateNameWiki
            EditSummary += PluginWikiShortcut + "|Plugin]]) " + JobSummary
        End Sub
    End Class

    ''' <summary>
    ''' SDK Software's base class for template-manipulating AWB plugins
    ''' </summary>
    Public MustInherit Class SDKAWBTemplatesPluginBase
        ' this could quite possibly become a generic base class, as so far the only difference is the template name
        ' which could be an overridable constant?
        ' what about <nowiki> and <pre>?? hmm

        ' AWB objects:
        Protected webcontrol As WikiFunctions.Browser.WebControl, contextmenu As System.Windows.Forms.ContextMenuStrip, _
           listmaker As WikiFunctions.Lists.ListMaker
        Protected WithEvents ourmenuitem As ToolStripMenuItem

        ' Plugin state:
        Protected blnEnabled As Boolean

        ' Article-edit state:
        Protected Article As Article

        ' Regular expression
        Protected regexp As Regex
        Protected Const conRegexpLeft As String = "\{\{\s*(?<tl>template\s*:)?\s*(?<tlname>" ' WPBiography|BioWikiProject
        Protected Const conRegexpRight As String = _
           ")[\s\n\r]*(([\s\n\r]*\|[\s\n\r]*(?<parm>[-a-z0-9&]*)[\s\n\r]*)+(=[\s\n\r]*(?<val>[-a-z0-9]*)[\s\n\r]*)?)*\}\}[\s\n\r]*"
        Protected Const regexpoptions As RegexOptions = _
           RegexOptions.Compiled Or RegexOptions.Multiline Or RegexOptions.IgnoreCase Or RegexOptions.ExplicitCapture

        Protected Overridable Sub Initialise(ByVal list As WikiFunctions.Lists.ListMaker, _
        ByVal web As WikiFunctions.Browser.WebControl, ByVal tsmi As System.Windows.Forms.ToolStripMenuItem, _
        ByVal cms As System.Windows.Forms.ContextMenuStrip)
            InitMenuItem()
            tsmi.DropDownItems.Add(ourmenuitem)
            webcontrol = web
            contextmenu = cms
            listmaker = list
        End Sub

        Protected Function FindTemplate(ByVal ArticleText As String, ByVal PreferredTemplateNameRegex As Regex, _
        ByVal PreferredTemplateNameWiki As String, ByVal PluginWikiShortcut As String, ByVal JobSummary As String) As String
            Article = New Article(PreferredTemplateNameRegex, PreferredTemplateNameWiki, PluginWikiShortcut, JobSummary)

            FindTemplate = regexp.Replace(ArticleText, AddressOf Me.MatchEvaluator)

            ' Pass over to inherited classes to do their specific jobs:
            FindTemplate = ParseArticle(FindTemplate)

            Article.FinaliseEditSummary()
        End Function

        Protected Function MatchEvaluator(ByVal match As Match) As String
            If Not match.Groups("parm").Captures.Count = match.Groups("val").Captures.Count Then
                MessageBox.Show("Parms and val don't match")
                Throw New Exception("Bug? Parameters and value count don't match")
            End If

            Article.FoundTemplate = True
            Article.CheckTemplateCall(match.Groups("tl").Value)
            Article.CheckTemplateName(match.Groups("tlname").Value)

            If match.Groups("parm").Captures.Count > 0 Then
                For i As Integer = 0 To match.Groups("parm").Captures.Count - 1
                    'Str += "Parm " & i.ToString & ": " & match.Groups("parm").Captures(i).Value & " = /" & _
                    '   match.Groups("val").Captures(i).Value & "/" & microsoft.visualbasic.vbcrlf
                    If Not match.Groups("val").Captures(i).Value = "" Then
                        With match.Groups("parm").Captures(i)
                            Article.AddTemplateParm(New Article.TemplateParametersObject( _
                               match.Groups("parm").Captures(i).Value, match.Groups("val").Captures(i).Value))
                        End With
                    End If
                Next
            End If

            Return "" ' Always return an empty string; if we don't skip we'll add our own template instance
        End Function

        Protected Function TNF(ByVal ArticleText As String) As String
            Article.Major = True
            Article.Skip = False
            Return TemplateNotFound(ArticleText)
        End Function

        Protected Function ValidNamespace(ByVal Nmespace As Namespaces, ByVal AllowProject As Boolean, _
        ByVal AllowMain As Boolean, ByVal AllowAllTalk As Boolean) As Boolean
            Select Case Nmespace
                Case Namespaces.Talk
                    Return True
                Case Namespaces.Main
                    Return AllowMain
                Case Namespaces.Project
                    Return AllowProject
                Case Namespaces.ProjectTalk, Namespaces.PortalTalk, Namespaces.CategoryTalk, Namespaces.UserTalk, _
                Namespaces.HelpTalk, Namespaces.ImageTalk, Namespaces.MediawikiTalk
                    Return AllowAllTalk
            End Select
        End Function

        Protected MustOverride Sub InitMenuItem()
        Protected MustOverride Function ParseArticle(ByVal ArticleText As String) As String
        Protected MustOverride Function TemplateNotFound(ByVal ArticleText As String) As String

        Protected Sub New()
        End Sub
    End Class

    ''' <summary>
    ''' The base class for plugins which manipulate the WPBiography template
    ''' </summary>
    Public MustInherit Class WPBioBase
        Inherits SDKAWBTemplatesPluginBase
        Private TemplateNameRegex As New Regex("[Ww]PBiography", RegexOptions.Compiled)
        Private BLPRegex As New Regex("\{\{\s*(template\s*:\s*|)\s*blp\s*\}\}[\s\n\r]*", _
           RegexOptions.IgnoreCase Or RegexOptions.Compiled Or RegexOptions.ExplicitCapture)
        Private SkipRegex As New Regex("WPBeatles|\{\{KLF", RegexOptions.IgnoreCase Or RegexOptions.Compiled)

        Protected Function ProcessArticle(ByVal ArticleText As String, ByVal ArticleTitle As String, _
        ByVal [Namespace] As Integer, ByRef Summary As String, ByRef Skip As Boolean, ByVal JobSummary As String, _
        ByVal ForceAddition As Boolean, ByVal AllowProject As Boolean, ByVal AllowAllTalk As Boolean) As String
            If Not ValidNamespace(DirectCast([Namespace], Namespaces), AllowProject, False, AllowAllTalk) Then
                Skip = True
                Return ArticleText
                ' TODO: Log skipped articles
            Else
                ' Skip if contains {{WPBeatles}} or {{KLF}}
                If SkipRegex.Matches(ArticleText).Count > 0 Then
                    Skip = True
                    Return ArticleText
                End If

                ' Look for the template and parameters
                ProcessArticle = MyBase.FindTemplate(ArticleText, TemplateNameRegex, "WPBiography", _
                   "BLP", JobSummary)
                Summary = Article.EditSummary
                Skip = Article.Skip

                With Article
                    If (Not .FoundTemplate) And ForceAddition Then
                        ArticleText = TNF(ProcessArticle) ' TODO: If a {{blp}} only doesn't work, this might have to be TNF(ProcessArticle)
                        Skip = False
                    ElseIf Not Skip Then
                        ArticleText = "{{WPBiography" & Microsoft.VisualBasic.vbCrLf
                        If .TemplateParameters.ContainsKey("living") Then
                            ArticleText += "|living=" + .TemplateParameters("living").Value + _
                               Microsoft.VisualBasic.vbCrLf
                        End If
                        ArticleText += "|class="
                        If .TemplateParameters.ContainsKey("class") Then
                            ArticleText += .TemplateParameters("class").Value
                        End If
                        ArticleText += Microsoft.VisualBasic.vbCrLf

                        For Each o As KeyValuePair(Of String, Article.TemplateParametersObject) In .TemplateParameters
                            With o
                                Select Case .Key
                                    Case "living"
                                    Case "class"
                                        Exit Select
                                    Case Else
                                        ArticleText += "|" + .Key + "=" + .Value.Value + Microsoft.VisualBasic.vbCrLf
                                End Select
                            End With
                        Next

                        ArticleText += "}}" + Microsoft.VisualBasic.vbCrLf + ProcessArticle
                    End If

                    webcontrol.SetMinor(Not .Major) ' This probably ought to be in the base-base class
                End With

                Return ArticleText
            End If
        End Function

        Protected Function ParseBioArticle(ByVal Living As Boolean, ByVal ArticleText As String) As String
            ' The plugin base class has got any template parms, now we need to do our bit (generic WPBio jobs)
            With Article
                If .FoundTemplate Then
                    If .TemplateParameters.ContainsKey("importance") Then
                        If .TemplateParameters.ContainsKey("priority") Then
                            .EditSummary += "rm importance param, has priority=, "
                        Else
                            .TemplateParameters.Add("priority", _
                               New Article.TemplateParametersObject("priority", .TemplateParameters("importance").Value))
                            .EditSummary += "importance→priority, "
                        End If
                        .TemplateParameters.Remove("importance")
                        .Skip = False
                    End If
                End If

                ' this is based on PerformFindAndReplace() from AWB, and could be turned into a function if need be
                If (BLPRegex.Matches(ArticleText).Count > 0) Then
                    ArticleText = BLPRegex.Replace(ArticleText, "")
                    .EditSummary += "{{[[Template:Blp|Blp]]}} removed, "
                    Living = True
                    .Skip = False
                End If

                If Living Then
                    If .TemplateParameters.ContainsKey("living") Then
                        If Not .TemplateParameters("living").Value = "yes" Then
                            .TemplateParameters("living").Value = "yes"
                            .Skip = False
                            .Major = True
                            ' if living=yes then no change needed here
                        End If
                    Else
                        .TemplateParameters.Add("living", New Article.TemplateParametersObject("living", "yes"))
                        .Skip = False
                        .Major = True
                    End If
                End If
            End With

            Return ArticleText
        End Function

        Protected Sub New()
            MyBase.new()
            regexp = New Regex(conRegexpLeft & "WPBiography|BioWikiProject" & conRegexpRight, regexpoptions)
            ' could do this in a renamed InitMenuItem() or in the base class by using a protected var for the template
            ' names. This way is fine for now :)
        End Sub
    End Class

    ''' <summary>
    ''' An AWB plugin which ensures that a talk page contains WPBiography|living=yes
    ''' </summary>
    Public NotInheritable Class BioLivingPeoplePlugin
        Inherits WPBioBase
        Implements IAWBPlugin

        Private Const conPluginName As String = "Living People Biographies Plugin"

        Public Shadows Sub Initialise(ByVal list As WikiFunctions.Lists.ListMaker, _
        ByVal web As WikiFunctions.Browser.WebControl, ByVal tsmi As System.Windows.Forms.ToolStripMenuItem, _
        ByVal cms As System.Windows.Forms.ContextMenuStrip) Implements WikiFunctions.Plugin.IAWBPlugin.Initialise
            MyBase.Initialise(list, web, tsmi, cms)
        End Sub

        Protected Overrides Sub InitMenuItem()
            ourmenuitem = New ToolStripMenuItem(conPluginName)
            MessageBox.Show(conPluginName)
        End Sub

        Public ReadOnly Property Name() As String Implements WikiFunctions.Plugin.IAWBPlugin.Name
            Get
                Return conPluginName
            End Get
        End Property

        Public Shadows Function ProcessArticle(ByVal ArticleText As String, ByVal ArticleTitle As String, _
        ByVal [Namespace] As Integer, ByRef Summary As String, ByRef Skip As Boolean) As String _
        Implements WikiFunctions.Plugin.IAWBPlugin.ProcessArticle
            ProcessArticle = MyBase.ProcessArticle(ArticleText, ArticleTitle, [Namespace], Summary, Skip, _
               "Tag [[Category:Living people]] articles with {{[[Template:WPBiography|WPBiography]]}}. ", _
               True, False, False)
        End Function

        Protected Overrides Function ParseArticle(ByVal ArticleText As String) As String
            ' The base classes have got any template parms, now we need to do our bit
            Return MyBase.ParseBioArticle(True, ArticleText)
            ' Anything specific to this plugin can follow here:
        End Function

        Protected Overrides Function TemplateNotFound(ByVal ArticleText As String) As String
            Return "{{WPBiography" & Microsoft.VisualBasic.vbCrLf & "|living=yes" & _
               Microsoft.VisualBasic.vbCrLf & "|class=" & Microsoft.VisualBasic.vbCrLf & "}}" & _
               Microsoft.VisualBasic.vbCrLf & ArticleText
        End Function

        Private Sub MenuItem_Click(ByVal sender As Object, ByVal e As System.EventArgs) Handles ourmenuitem.Click
            MessageBox.Show("Click")
        End Sub

        Public Sub ReadXML(ByVal Reader As System.Xml.XmlTextReader) Implements WikiFunctions.Plugin.IAWBPlugin.ReadXML

        End Sub

        Public Sub Reset() Implements WikiFunctions.Plugin.IAWBPlugin.Reset

        End Sub

        Public Sub WriteXML(ByVal Writer As System.Xml.XmlTextWriter) Implements WikiFunctions.Plugin.IAWBPlugin.WriteXML

        End Sub

        Public Sub New()
            MyBase.New()
        End Sub
    End Class
End Namespace