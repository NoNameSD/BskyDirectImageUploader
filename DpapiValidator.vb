Public Module DpapiValidator

    ' The exact 20 magic bytes (SOH + Version + Windows DPAPI GUID)
    Private ReadOnly DpapiHeader As Byte() = New Byte() {
        &H1, &H0, &H0, &H0,
        &HD0, &H8C, &H9D, &HDF, &H1, &H15, &HD1, &H11,
        &H8C, &H7A, &H0, &HC0, &H4F, &HC2, &H97, &HEB
    }

    ''' <summary>
    ''' Checks if a byte array starts with the valid DPAPI magic bytes.
    ''' </summary>
    Public Function IsValidDpapiBlob(data As Byte()) As Boolean
        ' Minimum required size (20 bytes)
        If data Is Nothing OrElse data.Length < 20 Then Return False

        ' Perform a fast byte-by-byte comparison
        For i As Integer = 0 To 19
            If data(i) <> DpapiHeader(i) Then
                Return False ' Exit early on the first mismatched byte
            End If
        Next

        Return True ' All 20 bytes match perfectly
    End Function
End Module
