Imports System.Runtime.InteropServices

Public Class tb
    <StructLayout(LayoutKind.Sequential)>
    Private Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure APPBARDATA
        Public cbSize As Integer
        Public hWnd As IntPtr
        Public uCallbackMessage As Integer
        Public uEdge As Integer
        Public rc As RECT
        Public lParam As IntPtr
    End Structure

    <DllImport("shell32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SHAppBarMessage(dwMessage As Integer, ByRef pData As APPBARDATA) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer, Y As Integer, cx As Integer, cy As Integer, uFlags As Integer) As Boolean
    End Function

    Private Const ABM_NEW As Integer = &H0
    Private Const ABM_REMOVE As Integer = &H1
    Private Const ABM_SETPOSITION As Integer = &H2
    Private Const ABE_BOTTOM As Integer = 3

    Private Const SWP_NOSIZE As Integer = &H1
    Private Const SWP_NOMOVE As Integer = &H2
    Private Const SWP_NOACTIVATE As Integer = &H10
    Private HWND_TOPMOST As New IntPtr(-1)
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE
            Return cp
        End Get
    End Property

    Private Sub tb_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MinimumSize = New Size(0, 0)
        Me.MaximumSize = New Size(69420, 22)
        Me.ShowInTaskbar = False
        RegisterAppBar()
    End Sub

    Private Sub tb_FormClosing(sender As Object, e As FormClosingEventArgs) Handles MyBase.FormClosing
        UnregisterAppBar()
    End Sub

    Private Sub RegisterAppBar()
        Dim abd As New APPBARDATA()
        abd.cbSize = Marshal.SizeOf(abd)
        abd.hWnd = Me.Handle

        SHAppBarMessage(ABM_NEW, abd)
        PositionAppBar()
    End Sub

    Private Sub UnregisterAppBar()
        Dim abd As New APPBARDATA()
        abd.cbSize = Marshal.SizeOf(abd)
        abd.hWnd = Me.Handle

        SHAppBarMessage(ABM_REMOVE, abd)
    End Sub

    Private Sub PositionAppBar()
        Dim abd As New APPBARDATA()
        abd.cbSize = Marshal.SizeOf(abd)
        abd.hWnd = Me.Handle
        abd.uEdge = ABE_BOTTOM

        Dim screenBounds As Rectangle = Screen.PrimaryScreen.Bounds

        abd.rc.Left = 0
        abd.rc.Right = screenBounds.Width
        abd.rc.Bottom = screenBounds.Height
        abd.rc.Top = screenBounds.Height - Me.Height

        SHAppBarMessage(ABM_SETPOSITION, abd)

        Me.Location = New Point(abd.rc.Left, abd.rc.Top)
        Me.Size = New Size(abd.rc.Right - abd.rc.Left, abd.rc.Bottom - abd.rc.Top)

        SetWindowPos(Me.Handle, HWND_TOPMOST, 0, 0, 0, 0, SWP_NOMOVE Or SWP_NOSIZE Or SWP_NOACTIVATE)
    End Sub

    Private Sub Button2_Click(sender As Object, e As EventArgs) Handles Button2.Click
        System.Windows.Forms.Application.Exit()
    End Sub
End Class