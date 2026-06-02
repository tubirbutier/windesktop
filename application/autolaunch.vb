Public Class autolaunch

    Private Sub autolaunch_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Dim Taskbar = New tb()
        Dim BG = New Main()
        Taskbar.Show()
        BG.Show()
        Me.Close()
    End Sub
End Class
