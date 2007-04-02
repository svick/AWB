Namespace AutoWikiBrowser.Plugins.SDKSoftware.Kingbotk
    ''' <summary>
    ''' SDK Software's base class for template-manipulating AWB plugins
    ''' </summary>
    Friend MustInherit Class PluginBase
        ' Settings:
        Protected Friend MustOverride ReadOnly Property PluginShortName() As String
        Protected MustOverride ReadOnly Property InspectUnsetParameters() As Boolean
        Protected Const ForceAddition As Boolean = True ' we might want to parameterise this later
        Protected MustOverride ReadOnly Property ParameterBreak() As String
        Protected Friend Const conTemplatePlaceholder As String = "{{xxxTEMPLATExxx}}"
        Protected MustOverride ReadOnly Property OurTemplateHasAlternateNames() As Boolean
        Protected Friend MustOverride ReadOnly Property GenericSettings() As IGenericSettings
        Protected MustOverride ReadOnly Property CategoryTalkClassParm() As String
        Protected MustOverride ReadOnly Property TemplateTalkClassParm() As String
        Friend MustOverride ReadOnly Property HasSharedLogLocation() As Boolean
        Friend MustOverride ReadOnly Property SharedLogLocation() As String
        Friend MustOverride ReadOnly Property HasReqPhotoParam() As Boolean
        Friend MustOverride Sub ReqPhoto()

        ' Objects:
        Protected WithEvents OurMenuItem As ToolStripMenuItem
        Protected Manager As PluginManager
        Protected Article As Article
        Protected Template As Templating

        ' Regular expressions:
        Protected MainRegex As Regex
        Protected SecondChanceRegex As Regex
        Protected Const conRegexpLeft As String = "\{\{\s*(?<tl>template\s*:)?\s*(?<tlname>"
        'Protected Const conRegexpRight As String = _
        '   ")[\s\n\r]*(([\s\n\r]*\|[\s\n\r]*(?<parm>[-a-z0-9&]*)[\s\n\r]*)+(=[\s\n\r]*(?<val>[-a-z0-9]*)[\s\n\r]*)?)*\}\}[\s\n\r]*"
        Protected Const conRegexpRight As String = _
           ")[\s\n\r]*(([\s\n\r]*\|[\s\n\r]*(?<parm>[^}{|\s\n\r=]*)[\s\n\r]*)+(=[\s\n\r]*" & _
           "(?<val>[^}{|\n\r]*)[\s\n\r]*)?)*\}\}[\s\n\r]*"
        Protected Const conRegexpRightNotStrict As String = ")[^}]*"
        Protected Const conRegexpOptions As RegexOptions = RegexOptions.Compiled Or RegexOptions.Multiline Or _
           RegexOptions.IgnoreCase Or RegexOptions.ExplicitCapture
        Protected PreferredTemplateNameRegex As Regex
        Protected MustOverride ReadOnly Property PreferredTemplateNameWiki() As String
        Private Shared StubClassTemplateRegex As New Regex(conRegexpLeft & "Stubclass" & _
           ")[\s\n\r]*(([\s\n\r]*\|[\s\n\r]*(?<parm>[^}{|\s\n\r=]*)[\s\n\r]*)+(=[\s\n\r]*" & _
           "(?<val>[^|\n\r]*)[\s\n\r]*)?)*\}\}[\s\n\r]*", conRegexpOptions) ' value might contain {{!}} and spaces
        Protected Shared BeatlesKLFSkipRegex As _
           New Regex("WPBeatles|\{\{KLF", RegexOptions.IgnoreCase Or RegexOptions.Compiled)

        ' Enum:
        Friend Enum ProcessTalkPageMode As Integer
            Normal
            ManualAssessment
            NonStandardTalk
        End Enum

        ' Initialisation:
        Protected Friend Sub New(ByVal PM As PluginManager)
            Manager = PM
        End Sub
        Protected Function CreateStandardRegex(ByVal RegexpMiddle As String) As Regex
            Return New Regex(conRegexpLeft & RegexpMiddle & conRegexpRight, conRegexpOptions)
        End Function
        Protected Function CreateSecondChanceRegex(ByVal RegexpMiddle As String) As Regex
            Return New Regex(conRegexpLeft & RegexpMiddle & conRegexpRightNotStrict, conRegexpOptions)
        End Function

        ' AWB pass through:
        Protected Sub InitialiseBase(ByVal AWBPluginsMenu As ToolStripMenuItem, ByVal txt As TextBox)
            With OurMenuItem
                .CheckOnClick = True
                .Checked = False
                .ToolTipText = "Enable/disable the " & PluginShortName & " plugin"
            End With
            AWBPluginsMenu.DropDownItems.Add(OurMenuItem)

            GenericSettings.EditTextBox = txt

            If Not Me.IAmGeneric Then _
               Manager.AddItemToTextBoxInsertionContextMenu(GenericSettings.TextInsertContextMenuStripItems)
        End Sub
        Protected Friend MustOverride Sub Initialise(ByVal AWBPluginsMenu As ToolStripMenuItem, _
           ByVal txt As TextBox)
        Protected Friend MustOverride Sub ReadXML(ByVal Reader As XmlTextReader)
        Protected Friend MustOverride Sub Reset()
        Protected Friend MustOverride Sub WriteXML(ByVal Writer As XmlTextWriter)
        Protected Friend Sub ProcessTalkPage(ByVal A As Article, ByVal AddReqPhotoParm As Boolean)
            ProcessTalkPage(A, Classification.Code, Importance.Code, False, False, False, _
               ProcessTalkPageMode.Normal, AddReqPhotoParm)
        End Sub
        Protected Friend Sub ProcessTalkPage(ByVal A As Article, ByVal Classification As Classification, _
        ByVal Importance As Importance, ByVal ForceNeedsInfobox As Boolean, _
        ByVal ForceNeedsAttention As Boolean, ByVal RemoveAutoStub As Boolean, _
        ByVal ProcessTalkPageMode As ProcessTalkPageMode, ByVal AddReqPhotoParm As Boolean)

            Me.Article = A

            If SkipIfContains() Then
                A.PluginIHaveFinished(SkipResults.SkipMiscellaneous, PluginShortName)
            Else
                ' MAIN
                Dim OriginalArticleText As String = A.AlteredArticleText

                Template = New Templating
                A.AlteredArticleText = MainRegex.Replace(A.AlteredArticleText, AddressOf Me.MatchEvaluator)

                If Template.BadTemplate Then
                    GoTo BadTemplate
                ElseIf Template.FoundTemplate Then
                    ' Even if we've found a good template bizarrely the page could still contain a bad template too 
                    If SecondChanceRegex.IsMatch(A.AlteredArticleText) Then
                        GoTo BadTemplate
                    ElseIf TemplateFound() Then
                        GoTo BadTemplate ' (returns True if bad)
                    End If
                Else
                    If SecondChanceRegex.IsMatch(OriginalArticleText) Then
                        GoTo BadTemplate
                    ElseIf ForceAddition Then
                        TemplateNotFound()
                    End If
                End If

                If Me.HasReqPhotoParam AndAlso AddReqPhotoParm Then Me.ReqPhoto()

                ProcessArticleFinish()
                If Not ProcessTalkPageMode = PluginBase.ProcessTalkPageMode.Normal Then
                    ProcessArticleFinishNonStandardMode(Classification, Importance, ForceNeedsInfobox, _
                       ForceNeedsAttention, RemoveAutoStub, ProcessTalkPageMode)
                End If

                If Article.ProcessIt Then
                    TemplateWritingAndPlacement()
                Else
                    A.AlteredArticleText = OriginalArticleText ' New: Hopefully fixes a bug but keep an eye on this
                    A.PluginIHaveFinished(SkipResults.SkipNoChange, PluginShortName)
                End If
            End If

ExitMe:
            Article = Nothing
            Exit Sub

BadTemplate:
            A.PluginIHaveFinished(SkipResults.SkipBadTag, PluginShortName) ' TODO: We could get the template placeholder here
            Article = Nothing
            Exit Sub
        End Sub

        ' Article processing:
        Protected MustOverride Sub InspectUnsetParameter(ByVal Param As String)
        Protected MustOverride Function SkipIfContains() As Boolean
        ''' <summary>
        ''' Send the template to the plugin for preinspection
        ''' </summary>
        ''' <returns>False if OK, TRUE IF BAD TAG</returns>
        Protected MustOverride Function TemplateFound() As Boolean
        Protected MustOverride Sub ProcessArticleFinish()
        Protected MustOverride Function WriteTemplateHeader(ByRef PutTemplateAtTop As Boolean) As String
        Protected MustOverride Sub ImportanceParameter(ByVal Importance As Importance)
        Protected Function MatchEvaluator(ByVal match As Match) As String
            If Not match.Groups("parm").Captures.Count = match.Groups("val").Captures.Count Then
                Template.BadTemplate = True
            Else
                Template.FoundTemplate = True
                Article.PluginCheckTemplateCall(match.Groups("tl").Value, PluginShortName)

                If OurTemplateHasAlternateNames Then PluginCheckTemplateName(match.Groups("tlname").Value.Trim)

                If match.Groups("parm").Captures.Count > 0 Then
                    For i As Integer = 0 To match.Groups("parm").Captures.Count - 1

                        Dim value As String = match.Groups("val").Captures(i).Value
                        Dim parm As String = match.Groups("parm").Captures(i).Value

                        If value = "" Then
                            If InspectUnsetParameters Then InspectUnsetParameter(parm)
                        Else
                            Template.AddTemplateParmFromExistingTemplate(parm, value)
                        End If
                    Next
                End If
            End If

            Return conTemplatePlaceholder
        End Function
        Protected Sub PluginCheckTemplateName(ByVal TemplateName As String)
            If Not PreferredTemplateNameRegex Is Nothing Then
                If Not PreferredTemplateNameRegex.Match(TemplateName).Success Then
                    Article.DoneReplacement(TemplateName, PreferredTemplateNameWiki, False)
                    PluginSettingsControl.MyTrace.WriteArticleActionLine( _
                       String.Format("Rename template [[Template:{0}|{0}]]→[[Template:{1}|{1}]]", TemplateName, _
                       PreferredTemplateNameWiki), PluginShortName)
                    GotTemplateNotPreferredName(TemplateName)
                End If
            End If
        End Sub
        Protected MustOverride Sub GotTemplateNotPreferredName(ByVal TemplateName As String)
        Protected Overridable Sub TemplateNotFound()
            Article.ArticleHasAMajorChange()
            Template.NewTemplateParm("class", "")
            Article.TemplateAdded(PreferredTemplateNameWiki, PluginShortName)
        End Sub
        Private Sub TemplateWritingAndPlacement()
            Dim PutTemplateAtTop As Boolean
            Dim TemplateHeader As String = WriteTemplateHeader(PutTemplateAtTop)

            For Each o As KeyValuePair(Of String, Templating.TemplateParametersObject) _
            In Template.Parameters
                With o
                    TemplateHeader += "|" + .Key + "=" + .Value.Value + ParameterBreak
                End With
            Next

            TemplateHeader += "}}" + Microsoft.VisualBasic.vbCrLf

            With Me.Article
                If Not Template.FoundTemplate Then
                    .AlteredArticleTextPrepend(TemplateHeader)
                ElseIf PutTemplateAtTop Then
                    .AlteredArticleText = TemplateHeader + .AlteredArticleText.Replace(conTemplatePlaceholder, "")
                Else
                    '.AlteredArticleText = .AlteredArticleText.Replace(conTemplatePlaceholder, TemplateHeader)
                    .RestoreTemplateToPlaceholderSpot(TemplateHeader)
                End If
            End With
        End Sub
        Protected Sub AddAndLogNewParamWithAYesValue(ByVal ParamName As String)
            Template.NewOrReplaceTemplateParm(ParamName, "yes", Article, True, False, PluginName:=PluginShortName)
        End Sub
        Protected Sub AddNewParamWithAYesValue(ByVal ParamName As String)
            Template.NewOrReplaceTemplateParm(ParamName, "yes", Article, False, False, PluginName:=PluginShortName)
        End Sub
        Protected Sub AddAndLogNewParamWithAYesValue(ByVal ParamName As String, ByVal ParamAlternativeName As String)
            Template.NewOrReplaceTemplateParm(ParamName, "yes", Article, True, True, _
               ParamAlternativeName:=ParamAlternativeName, PluginName:=PluginShortName)
        End Sub
        Protected Sub AddAndLogEmptyParam(ByVal ParamName As String)
            If Not Template.Parameters.ContainsKey(ParamName) Then Template.NewTemplateParm(ParamName, "", True, _
            Article, PluginShortName)
        End Sub
        Protected Sub AddEmptyParam(ByVal ParamName As String)
            If Not Template.Parameters.ContainsKey(ParamName) Then Template.NewTemplateParm(ParamName, "", _
               False, Article, PluginShortName)
        End Sub
        Protected Sub ProcessArticleFinishNonStandardMode(ByVal Classification As Classification, _
        ByVal Importance As Importance, ByVal ForceNeedsInfobox As Boolean, _
        ByVal ForceNeedsAttention As Boolean, ByVal RemoveAutoStub As Boolean, _
        ByVal ProcessTalkPageMode As ProcessTalkPageMode)
            Select Case Classification
                Case Kingbotk.Classification.Code
                    If ProcessTalkPageMode = PluginBase.ProcessTalkPageMode.NonStandardTalk Then
                        Select Case Me.Article.Namespace
                            Case Namespaces.CategoryTalk
                                Template.NewOrReplaceTemplateParm( _
                                   "class", CategoryTalkClassParm, Me.Article, True, False, _
                                   PluginName:=PluginShortName)
                            Case Namespaces.TemplateTalk
                                Template.NewOrReplaceTemplateParm( _
                                   "class", TemplateTalkClassParm, Me.Article, True, False, _
                                   PluginName:=PluginShortName)
                            Case Namespaces.ImageTalk, Namespaces.PortalTalk, Namespaces.ProjectTalk
                                Template.NewOrReplaceTemplateParm( _
                                   "class", "NA", Me.Article, True, False, PluginName:=PluginShortName)
                        End Select
                    End If
                Case Kingbotk.Classification.Unassessed
                Case Else
                    Template.NewOrReplaceTemplateParm("class", Classification.ToString, Me.Article, False, False)
            End Select

            Select Case Importance
                Case Kingbotk.Importance.Code, Kingbotk.Importance.Unassessed
                Case Else
                    ImportanceParameter(Importance)
            End Select

            If ForceNeedsInfobox Then AddAndLogNewParamWithAYesValue("needs-infobox")

            If ForceNeedsAttention Then AddAndLogNewParamWithAYesValue("attention")

            If RemoveAutoStub Then
                With Me.Article
                    If Template.Parameters.ContainsKey("auto") Then
                        Template.Parameters.Remove("auto")
                        .ArticleHasAMajorChange()
                    End If

                    If StubClassTemplateRegex.IsMatch(.AlteredArticleText) Then
                        .AlteredArticleText = StubClassTemplateRegex.Replace(.AlteredArticleText, "")
                        .ArticleHasAMajorChange()
                    End If
                End With
            End If
        End Sub
        Protected Function WriteOutParameterToHeader(ByVal ParamName As String) As String
            With Template
                WriteOutParameterToHeader = "|" & ParamName & "="
                If .Parameters.ContainsKey(ParamName) Then
                    WriteOutParameterToHeader += .Parameters(ParamName).Value + ParameterBreak
                    .Parameters.Remove(ParamName)
                Else
                    WriteOutParameterToHeader += ParameterBreak
                End If
            End With
        End Function
        Protected Sub StubClass()
            If Me.Article.Namespace = Namespaces.Talk Then
                If GenericSettings.StubClass Then Template.NewOrReplaceTemplateParm("class", "Stub", Article, _
                   True, False, PluginName:=PluginShortName, DontChangeIfSet:=True)

                If GenericSettings.AutoStub _
                AndAlso Template.NewOrReplaceTemplateParm("class", "Stub", Article, True, False, _
                    PluginName:=PluginShortName, DontChangeIfSet:=True) _
                       Then AddAndLogNewParamWithAYesValue("auto")
                ' If add class=Stub (we don't change if set) add auto
            Else
                PluginSettingsControl.MyTrace.WriteArticleActionLine1( _
                   "Ignoring Stub-Class and Auto-Stub options; not a mainspace talk page", PluginShortName, True)
            End If
        End Sub
        Protected Sub ReplaceATemplateWithAYesParameter(ByVal R As Regex, ByVal ParamName As String, _
        ByVal TemplateCall As String, Optional ByVal Replace As Boolean = True)
            With Article
                If (R.Matches(.AlteredArticleText).Count > 0) Then
                    If Replace Then .AlteredArticleText = R.Replace(.AlteredArticleText, "")
                    .DoneReplacement(TemplateCall, ParamName & "=yes", True, PluginShortName)
                    Template.NewOrReplaceTemplateParm(ParamName, "yes", Article, False, False)
                    .ArticleHasAMinorChange()
                End If
            End With
        End Sub
        ''' <summary>
        ''' Checks if params which have two names (V8, v8) exist under both names
        ''' </summary>
        ''' <returns>True if BAD TAG</returns>
        Protected Function CheckForDoublyNamedParameters(ByVal Name1 As String, ByVal Name2 As String) As Boolean
            With Template.Parameters
                If .ContainsKey(Name1) AndAlso .ContainsKey(Name2) Then
                    If .Item(Name1).Value = .Item(Name2).Value Then
                        .Remove(Name2)
                        Article.DoneReplacement(Name2, "", True, PluginShortName)
                    Else
                        Return True
                    End If
                End If
            End With
        End Function

        ' Interraction with manager:
        Friend Property Enabled() As Boolean
            Get
                Return OurMenuItem.Checked
            End Get
            Set(ByVal IsEnabled As Boolean)
                OurMenuItem.Checked = IsEnabled
                ShowHideOurObjects(IsEnabled)
                Manager.PluginEnabledStateChanged(Me, IsEnabled)
            End Set
        End Property
        Protected Friend Overridable Sub BotModeChanged(ByVal BotMode As Boolean)
            If BotMode AndAlso GenericSettings.StubClass Then
                GenericSettings.AutoStub = True
                GenericSettings.StubClass = False
            End If
            GenericSettings.StubClassModeAllowed = Not BotMode
        End Sub
        Protected Friend Overridable ReadOnly Property IAmReady() As Boolean
            Get
                Return True
            End Get
        End Property
        Protected Friend Overridable ReadOnly Property IAmGeneric() As Boolean
            Get
                Return False
            End Get
        End Property

        ' User interface:
        Protected MustOverride Sub ShowHideOurObjects(ByVal Visible As Boolean)
        'Protected Friend ReadOnly Property TextInsertContextMenuItems(ByVal txt As TextBox) _
        'As ToolStripItemCollection
        '    Get

        '    End Get
        'End Property

        ' Event handlers:
        Private Sub ourmenuitem_CheckedChanged(ByVal sender As Object, ByVal e As System.EventArgs) _
        Handles OurMenuItem.CheckedChanged
            Enabled = OurMenuItem.Checked
        End Sub
    End Class

    Friend Interface IGenericSettings
        Property AutoStub() As Boolean
        Property StubClass() As Boolean
        WriteOnly Property StubClassModeAllowed() As Boolean
        WriteOnly Property EditTextBox() As TextBox
        ReadOnly Property TextInsertContextMenuStripItems() As ToolStripItemCollection
        Sub ReadXML(ByVal Reader As System.Xml.XmlTextReader)
        Sub WriteXML(ByVal Writer As System.Xml.XmlTextWriter)
        Sub XMLReset()
    End Interface
End Namespace