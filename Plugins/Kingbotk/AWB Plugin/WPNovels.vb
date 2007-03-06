Namespace AutoWikiBrowser.Plugins.SDKSoftware.Kingbotk.Plugins

    Friend NotInheritable Class WPNovelSettings
        Implements IGenericSettings

        Private Const conAttentionParm As String = "NovAttention"
        Private Const conNeedsInfoBoxParm As String = "NovNeedsInfoBox"
        Private Const conIncompInfoBoxParm As String = "NovIncompInfoBox"
        Private Const conNeedInfoBoxCoverParm As String = "NovNeedInfoBoxCover"
        Private Const conCollabCandParm As String = "NovCollabCand"
        Private Const conPastCollabParm As String = "NovPastCollab"
        Private Const conPeerReviewParm As String = "NovPeerReview"
        Private Const conOldPeerReviewParm As String = "NovOldPeerReview"
        Private Const conCrimeWGParm As String = "NovCrimeWG"
        Private Const conShortStoryWGParm As String = "NovShortStoryWG"
        Private Const conSFWGParm As String = "NovSFWG"

        ' UI:
        Private txtEdit As TextBox

#Region "XML interface"
        Public Sub ReadXML(ByVal Reader As System.Xml.XmlTextReader) Implements IGenericSettings.ReadXML
            'AutoStub = PluginManager.XMLReadBoolean(Reader, conAutoStubParm, AutoStub)
            'StubClass = PluginManager.XMLReadBoolean(Reader, conStubClassParm, StubClass)
            Attention = PluginManager.XMLReadBoolean(Reader, conAttentionParm, Attention)
            IncompleteInfoBox = PluginManager.XMLReadBoolean(Reader, conIncompInfoBoxParm, IncompleteInfoBox)
            NeedCover = PluginManager.XMLReadBoolean(Reader, conNeedInfoBoxCoverParm, NeedCover)
            CrimeWG = PluginManager.XMLReadBoolean(Reader, conCrimeWGParm, CrimeWG)
            ShortStoryWG = PluginManager.XMLReadBoolean(Reader, conShortStoryWGParm, ShortStoryWG)
            SFWG = PluginManager.XMLReadBoolean(Reader, conSFWGParm, SFWG)
            NeedsInfoBox = PluginManager.XMLReadBoolean(Reader, conNeedsInfoBoxParm, NeedsInfoBox)
            Collab = PluginManager.XMLReadBoolean(Reader, conCollabCandParm, Collab)
            PastCollab = PluginManager.XMLReadBoolean(Reader, conPastCollabParm, PastCollab)
            PeerReview = PluginManager.XMLReadBoolean(Reader, conPeerReviewParm, PeerReview)
            OldPeerReview = PluginManager.XMLReadBoolean(Reader, conOldPeerReviewParm, OldPeerReview)
        End Sub
        Public Sub WriteXML(ByVal Writer As System.Xml.XmlTextWriter) Implements IGenericSettings.WriteXML
            With Writer
                .WriteAttributeString(conAttentionParm, Attention.ToString)
                .WriteAttributeString(conIncompInfoBoxParm, IncompleteInfoBox.ToString)
                .WriteAttributeString(conNeedInfoBoxCoverParm, NeedCover.ToString)
                .WriteAttributeString(conCrimeWGParm, CrimeWG.ToString)
                .WriteAttributeString(conShortStoryWGParm, ShortStoryWG.ToString)
                .WriteAttributeString(conSFWGParm, SFWG.ToString)
                .WriteAttributeString(conNeedsInfoBoxParm, NeedsInfoBox.ToString)
                .WriteAttributeString(conCollabCandParm, Collab.ToString)
                .WriteAttributeString(conPastCollabParm, PastCollab.ToString)
                .WriteAttributeString(conPeerReviewParm, PeerReview.ToString)
                .WriteAttributeString(conOldPeerReviewParm, OldPeerReview.ToString)
            End With
        End Sub
        Public Sub Reset() Implements IGenericSettings.XMLReset

        End Sub
#End Region

#Region "Properties"
        Public Property Attention() As Boolean
            Get
                Return AttentionCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                AttentionCheckBox.Checked = value
            End Set
        End Property
        Public Property IncompleteInfoBox() As Boolean
            Get
                Return InCompInfoCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                InCompInfoCheckBox.Checked = value
            End Set
        End Property
        Public Property NeedCover() As Boolean
            Get
                Return NeedsInfoCoverCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                NeedsInfoCoverCheckBox.Checked = value
            End Set
        End Property
        Public Property CrimeWG() As Boolean
            Get
                Return CrimeCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                CrimeCheckBox.Checked = value
            End Set
        End Property
        Public Property ShortStoryWG() As Boolean
            Get
                Return ShortStoryCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                ShortStoryCheckBox.Checked = value
            End Set
        End Property
        Public Property SFWG() As Boolean
            Get
                Return SFCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                SFCheckBox.Checked = value
            End Set
        End Property
        Public Property NeedsInfoBox() As Boolean
            Get
                Return NeedInfoCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                NeedInfoCheckBox.Checked = value
            End Set
        End Property
        Public Property Collab() As Boolean
            Get
                Return CollaborationCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                CollaborationCheckBox.Checked = value
            End Set
        End Property
        Public Property PastCollab() As Boolean
            Get
                Return PastCollaborationCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                PastCollaborationCheckBox.Checked = value
            End Set
        End Property
        Public Property PeerReview() As Boolean
            Get
                Return PeerReviewCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                PeerReviewCheckBox.Checked = value
            End Set
        End Property
        Public Property OldPeerReview() As Boolean
            Get
                Return OldPeerCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                OldPeerCheckBox.Checked = value
            End Set
        End Property
        'Friend Property AutoStub() As Boolean Implements IGenericSettings.AutoStub
        '    Get
        '        Return AutoStubCheckBox.Checked
        '    End Get
        '    Set(ByVal value As Boolean)
        '        AutoStubCheckBox.Checked = value
        '    End Set
        'End Property
        'Friend Property StubClass() As Boolean Implements IGenericSettings.StubClass
        '    Get
        '        Return StubClassCheckBox.Checked
        '    End Get
        '    Set(ByVal value As Boolean)
        '        StubClassCheckBox.Checked = value
        '    End Set
        'End Property
        WriteOnly Property StubClassModeAllowed() As Boolean Implements IGenericSettings.StubClassModeAllowed
            Set(ByVal value As Boolean)
                InCompInfoCheckBox.Enabled = value
            End Set
        End Property
        Public WriteOnly Property EditTextBox() As TextBox Implements IGenericSettings.EditTextBox
            Set(ByVal value As TextBox)
                txtEdit = value
            End Set
        End Property
        Public ReadOnly Property TextInsertContextMenuStripItems() As ToolStripItemCollection _
        Implements IGenericSettings.TextInsertContextMenuStripItems
            Get
                Return TextInsertContextMenuStrip.Items
            End Get
        End Property
#End Region

        ' Event handlers:
        Private Sub LinkClicked(ByVal sender As Object, ByVal e As LinkLabelLinkClickedEventArgs) Handles LinkLabel1.LinkClicked
            System.Diagnostics.Process.Start(PluginManager.ENWiki + "Template:NovelsWikiProject")
        End Sub

#Region "TextInsertHandlers"

#End Region
    End Class

    Friend NotInheritable Class WPNovels
        Inherits PluginBase

        ' Regular expressions:
        Private Shared BLPRegex As New Regex("\{\{\s*(template\s*:\s*|)\s*blp\s*\}\}[\s\n\r]*", _
           RegexOptions.IgnoreCase Or RegexOptions.Compiled Or RegexOptions.ExplicitCapture)
        'Private Shared DefaultSortRegex As New Regex("\{\{\s*(template\s*:\s*|)\s*DEFAULTSORT[\s\n\r]*(\||:)", _
        '   RegexOptions.IgnoreCase Or RegexOptions.Compiled Or RegexOptions.ExplicitCapture)

        ' Strings:
        Private Const conStringsShortStoryWorkGroup As String = "short-story-task-force"
        Private Const conStringsCrimeWorkGroup As String = "crime-task-force"
        Private Const conStringsSFWorkGroup As String = "sf-task-force"

        ' Settings:
        Private OurTab As New TabPage("Novels")
        Private WithEvents OurSettingsControl As New WPNovelSettings
        Private Const conEnabled As String = "NovEnabled"
        Protected Friend Overrides ReadOnly Property conPluginShortName() As String
            Get
                Return "Novels"
            End Get
        End Property
        Protected Overrides ReadOnly Property PreferredTemplateNameWiki() As String
            Get
                Return "WPNovels"
            End Get
        End Property
        Protected Overrides ReadOnly Property ParameterBreak() As String
            Get
                Return Microsoft.VisualBasic.vbCrLf
            End Get
        End Property
        Protected Overrides Sub ImportanceParameter(ByVal Importance As Importance)
            Template.NewOrReplaceTemplateParm("priority", Importance.ToString, Me.Article, False, False)
        End Sub
        Protected Overrides ReadOnly Property OurTemplateHasAlternateNames() As Boolean
            Get
                Return True
            End Get
        End Property
        Protected Friend Overrides ReadOnly Property GenericSettings() As IGenericSettings
            Get
                Return OurSettingsControl
            End Get
        End Property
        Protected Overrides ReadOnly Property CategoryTalkClassParm() As String
            Get
                Return "Cat"
            End Get
        End Property
        Protected Overrides ReadOnly Property TemplateTalkClassParm() As String
            Get
                Return "Template"
            End Get
        End Property
        Friend Overrides ReadOnly Property HasSharedLogLocation() As Boolean
            Get
                Return False
            End Get
        End Property
        Friend Overrides ReadOnly Property SharedLogLocation() As String
            Get
                Return ""
            End Get
        End Property

        ' Initialisation:
        Protected Friend Sub New(ByVal Manager As PluginManager)
            MyBase.New(Manager)
            Const RegexpMiddle As String = "WPBiography|BioWikiProject|Musician|WikiProject Biography|broy"
            MainRegex = New Regex(conRegexpLeft & RegexpMiddle & conRegexpRight, conRegexpOptions)
            PreferredTemplateNameRegex = New Regex("^[Ww]PBiography$", RegexOptions.Compiled)
            SecondChanceRegex = New Regex(conRegexpLeft & RegexpMiddle & conRegexpRightNotStrict, conRegexpOptions)
        End Sub
        Protected Friend Overrides Sub Initialise(ByVal AWBPluginsMenu As ToolStripMenuItem, ByVal txt As TextBox)
            OurMenuItem = New ToolStripMenuItem("Novels Plugin")
            MyBase.InitialiseBase(AWBPluginsMenu, txt) ' must set menu item object first
            OurTab.UseVisualStyleBackColor = True
            OurTab.Controls.Add(OurSettingsControl)
        End Sub

        ' Article processing:
        Protected Overrides ReadOnly Property InspectUnsetParameters() As Boolean
            Get
                Return OurSettingsControl.ForcePriorityParm
            End Get
        End Property
        Protected Overrides Sub InspectUnsetParameter(ByVal Param As String)
            ' We only get called if InspectUnsetParameters is True
            If String.Equals(Param, "importance", StringComparison.CurrentCultureIgnoreCase) Then
                Template.NewTemplateParm("priority", "")
                Article.DoneReplacement("importance", "priority", True, conPluginShortName)
            End If
        End Sub
        Protected Overrides Function SkipIfContains() As Boolean
            ' Skip if contains {{WPBeatles}} or {{KLF}}
            Return (BeatlesKLFSkipRegex.Matches(Article.AlteredArticleText).Count > 0)
        End Function
        Protected Overrides Sub ProcessArticleFinish()
            With Article
                If (BLPRegex.Matches(.AlteredArticleText).Count > 0) Then
                    .AlteredArticleText = BLPRegex.Replace(.AlteredArticleText, "")
                    .DoneReplacement("{{[[Template:Blp|Blp]]}}", "living=yes", True, conPluginShortName)
                    .ArticleHasAMinorChange()
                End If
            End With

            StubClass()

            With OurSettingsControl

            End With
        End Sub
        Protected Overrides Function TemplateFound() As Boolean
            With Template
                If .Parameters.ContainsKey("importance") Then
                    If .Parameters.ContainsKey("priority") Then
                        Article.EditSummary += "rm importance param, has priority=, "
                        PluginSettingsControl.MyTrace.WriteArticleActionLine( _
                           "importance parameter removed, has priority=", conPluginShortName)
                    Else
                        .Parameters.Add("priority", _
                           New Templating.TemplateParametersObject("priority", _
                           .Parameters("importance").Value))
                        Article.DoneReplacement("importance", "priority", True, conPluginShortName)
                    End If
                    .Parameters.Remove("importance")
                    Article.ArticleHasAMinorChange()
                End If
                '' Not tested yet:
                'If .Parameters.ContainsKey("listas") Then
                '    If .Parameters("listas").Value = "" Then
                '        PluginSettingsControl.MyTrace.WriteArticleActionLine( _
                '           "empty listas parameter removed", conPluginShortName)
                '    Else
                '        If DefaultSortRegex.IsMatch(Article.AlteredArticleText) Then
                '            PluginSettingsControl.MyTrace.WriteArticleActionLine( _
                '               "listas parameter removed; DEFAULTSORT already present", conPluginShortName)
                '        Else
                '            Article.AlteredArticleTextPrependLine("{{DEFAULTSORT:" & .Parameters("listas").Value & "}}")
                '            Article.DoneReplacement("listas", "{{DEFAULTSORT}}", True, conPluginShortName)
                '        End If
                '        .Parameters.Remove("listas")
                '        Article.ArticleHasAMajorChange()
                '    End If
                'End If
            End With
        End Function
        Protected Overrides Sub GotTemplateNotPreferredName(ByVal TemplateName As String)
            If TemplateName.ToLower = "musician" Then AddAndLogNewParamWithAYesValue("musician-work-group")
        End Sub
        Protected Overrides Function WriteTemplateHeader(ByRef PutTemplateAtTop As Boolean) As String
            WriteTemplateHeader = "{{NovelsWikiProject" & Microsoft.VisualBasic.vbCrLf

            With Template
                WriteTemplateHeader += WriteOutParameterToHeader("class") & _
                   WriteOutParameterToHeader("priority")
            End With
        End Function

        'User interface:
        Protected Overrides Sub ShowHideOurObjects(ByVal Visible As Boolean)
            Manager.ShowHidePluginTab(OurTab, Visible)
        End Sub

        ' XML settings:
        Protected Friend Overrides Sub ReadXML(ByVal Reader As System.Xml.XmlTextReader)
            Dim blnNewVal As Boolean = PluginManager.XMLReadBoolean(Reader, conEnabled, Enabled)
            If Not blnNewVal = Enabled Then Enabled = blnNewVal ' Mustn't set if the same or we get extra tabs
            OurSettingsControl.ReadXML(Reader)
        End Sub
        Protected Friend Overrides Sub Reset()
            OurSettingsControl.Reset()
        End Sub
        Protected Friend Overrides Sub WriteXML(ByVal Writer As System.Xml.XmlTextWriter)
            Writer.WriteAttributeString(conEnabled, Enabled.ToString)
            OurSettingsControl.WriteXML(Writer)
        End Sub
    End Class
End Namespace