Public Class autolaunch

    Private Sub autolaunch_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim TB = New tb()
        Dim BG = New Main()
        TB.Show()
        BG.Show()
        Me.Close()
    End Sub
End Class