Imports system.xml
''' <summary>
''' test
''' </summary>
''' <remarks></remarks>
Module Module1

    Sub Main()
        'Dim st As New IO.StringWriter
        'Dim Writer As Xml.XmlWriter = XmlTextWriter.Create(st)
        'Writer.WriteStartElement("root")
        'Writer.WriteAttributeString("test123", "456")
        'Writer.WriteEndElement()
        'Writer.Flush()
        'Return New Object() {stream}

        Dim st As New IO.StringReader("<?xml version=""1.0"" encoding=""utf-16""?>" & vbCrLf & _
           "<root test123=""456"" fuck=""off"" />")
        'st.ReadToEnd()
        Dim Reader As XmlTextReader = New XmlTextReader(st)
        'Reader.ReadStartElement("root")
        While Reader.Read()
            If Reader.NodeType = XmlNodeType.Element Then
                Debug.Print(Reader.MoveToAttribute("fuck").ToString)
                Debug.Print(Reader.Value)
            End If
        End While

        'Debug.Print(Reader.MoveToAttribute("test123").ToString)
    End Sub

End Module
