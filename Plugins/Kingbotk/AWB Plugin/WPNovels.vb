Namespace AutoWikiBrowser.Plugins.SDKSoftware.Kingbotk.Plugins

    Friend NotInheritable Class WPNovelSettings
        Implements IGenericSettings

        Private Const conAutoStubParm As String = "NovelsAutoStub"
        Private Const conStubClassParm As String = "NovelsStubClass"
        Private Const conOldPeerReviewParm As String = "NovelsOldPeerReview"
        Private Const conCrimeWGParm As String = "NovelsCrimeWG"
        Private Const conShortStoryWGParm As String = "NovelsShortStoryWG"
        Private Const conSFWGParm As String = "NovelsSFWG"
        ' UI:
        Private txtEdit As TextBox

#Region "XML interface"
        Public Sub ReadXML(ByVal Reader As System.Xml.XmlTextReader) Implements IGenericSettings.ReadXML
            AutoStub = PluginManager.XMLReadBoolean(Reader, conAutoStubParm, AutoStub)
            StubClass = PluginManager.XMLReadBoolean(Reader, conStubClassParm, StubClass)
            CrimeWG = PluginManager.XMLReadBoolean(Reader, conCrimeWGParm, CrimeWG)
            ShortStoryWG = PluginManager.XMLReadBoolean(Reader, conShortStoryWGParm, ShortStoryWG)
            SFWG = PluginManager.XMLReadBoolean(Reader, conSFWGParm, SFWG)
        End Sub
        Public Sub WriteXML(ByVal Writer As System.Xml.XmlTextWriter) Implements IGenericSettings.WriteXML
            With Writer
                .WriteAttributeString(conCrimeWGParm, CrimeWG.ToString)
                .WriteAttributeString(conShortStoryWGParm, ShortStoryWG.ToString)
                .WriteAttributeString(conSFWGParm, SFWG.ToString)
                .WriteAttributeString(conAutoStubParm, AutoStub.ToString)
                .WriteAttributeString(conStubClassParm, StubClass.ToString)
            End With
        End Sub
        Public Sub Reset() Implements IGenericSettings.XMLReset

        End Sub
#End Region

#Region "Properties"
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

        Friend Property AutoStub() As Boolean Implements IGenericSettings.AutoStub
            Get
                Return AutoStubCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                AutoStubCheckBox.Checked = value
            End Set
        End Property
        Friend Property StubClass() As Boolean Implements IGenericSettings.StubClass
            Get
                Return StubClassCheckBox.Checked
            End Get
            Set(ByVal value As Boolean)
                StubClassCheckBox.Checked = value
            End Set
        End Property
        WriteOnly Property StubClassModeAllowed() As Boolean Implements IGenericSettings.StubClassModeAllowed
            Set(ByVal value As Boolean)
                StubClassCheckBox.Enabled = value
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
    End Class

    Friend NotInheritable Class WPNovels
        Inherits PluginBase

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
            Template.NewOrReplaceTemplateParm("importance", Importance.ToString, Me.Article, False, False)
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
        Friend Overrides ReadOnly Property HasReqPhotoParam() As Boolean
            Get
                Return False
            End Get
        End Property
        Friend Overrides Sub ReqPhoto()
        End Sub

        ' Initialisation:
        Protected Friend Sub New(ByVal Manager As PluginManager)
            MyBase.New(Manager)
            Const RegexpMiddle As String = "NovelsWikiProject|Novels|WPNovels"
            MainRegex = New Regex(conRegexpLeft & RegexpMiddle & conRegexpRight, conRegexpOptions)
            PreferredTemplateNameRegex = New Regex("^[Nn]ovelsWikiProject$", RegexOptions.Compiled)
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
                Return False
            End Get
        End Property
        Protected Overrides Sub InspectUnsetParameter(ByVal Param As String)
        End Sub
        Protected Overrides Function SkipIfContains() As Boolean
            ' None
        End Function
        Protected Overrides Sub ProcessArticleFinish()
            StubClass()
            With OurSettingsControl
                If .CrimeWG Then AddAndLogNewParamWithAYesValue("crime-task-force")
                If .ShortStoryWG Then AddAndLogNewParamWithAYesValue("short-story-task-force")
                If .SFWG Then AddAndLogNewParamWithAYesValue("sf-task-force")
            End With
        End Sub
        Protected Overrides Function TemplateFound() As Boolean
        End Function
        Protected Overrides Sub GotTemplateNotPreferredName(ByVal TemplateName As String)
            ' Currently only WPBio does anything here (if {{musician}} add to musician-work-group)
        End Sub
        Protected Overrides Function WriteTemplateHeader(ByRef PutTemplateAtTop As Boolean) As String
            WriteTemplateHeader = "{{NovelsWikiProject" & Microsoft.VisualBasic.vbCrLf

            With Template
                WriteTemplateHeader += WriteOutParameterToHeader("class") & _
                   WriteOutParameterToHeader("importance")
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