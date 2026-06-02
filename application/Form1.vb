Imports System.Runtime.InteropServices
Imports System.IO
Imports System.Diagnostics

Public Class Main

    Private Const HWND_BOTTOM As Integer = 1
    Private Const SWP_NOSIZE As Integer = &H1
    Private Const SWP_NOMOVE As Integer = &H2
    Private Const SWP_SHOWWINDOW As Integer = &H40
    Private Const WM_WINDOWPOSCHANGING As Integer = &H46
    Private Const WM_MOVING As Integer = &H216
    Private Const WM_ERASEBKGND As Integer = &H14
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000

    <StructLayout(LayoutKind.Sequential)>
    Private Structure WINDOWPOS
        Public hwnd As IntPtr
        Public hwndInsertAfter As IntPtr
        Public x As Integer
        Public y As Integer
        Public cx As Integer
        Public cy As Integer
        Public flags As UInteger
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Private Structure RECT
        Public Left As Integer
        Public Top As Integer
        Public Right As Integer
        Public Bottom As Integer
    End Structure

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowPos(ByVal hWnd As IntPtr, ByVal hWndInsertAfter As IntPtr, ByVal X As Integer, ByVal Y As Integer, ByVal cx As Integer, ByVal cy As Integer, ByVal uFlags As UInteger) As Boolean
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function FindWindow(ByVal lpClassName As String, ByVal lpWindowName As String) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetParent(ByVal hWndChild As IntPtr, ByVal hWndNewParent As IntPtr) As IntPtr
    End Function

    Private WithEvents FastLockTimer As New Timer()
    Private WithEvents WallpaperTimer As New Timer()

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE
            Return cp
        End Get
    End Property

    Private Sub Form1_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.FormBorderStyle = FormBorderStyle.None
        Me.StartPosition = FormStartPosition.CenterScreen

        Me.SetStyle(ControlStyles.AllPaintingInWmPaint Or
                    ControlStyles.UserPaint Or
                    ControlStyles.OptimizedDoubleBuffer, True)
        Me.UpdateStyles()

        FastLockTimer.Interval = 250
        WallpaperTimer.Interval = 5000
    End Sub

    Private Sub Form1_Shown(sender As Object, e As EventArgs) Handles MyBase.Shown
        ForceCenterAndResize()
        LoadTranscodedWallpaper()

        FastLockTimer.Start()
        WallpaperTimer.Start()

        Try
            Dim progman As IntPtr = FindWindow("Progman", Nothing)
            If progman <> IntPtr.Zero Then
                SetParent(Me.Handle, progman)
            End If
        Catch ex As Exception
            SendToBottom()
        End Try
    End Sub

    Private Sub FastLockTimer_Tick(sender As Object, e As EventArgs) Handles FastLockTimer.Tick
        ForceCenterAndResize()
    End Sub

    Private Sub WallpaperTimer_Tick(sender As Object, e As EventArgs) Handles WallpaperTimer.Tick
        LoadTranscodedWallpaper()
    End Sub

    Private Sub ForceCenterAndResize()
        Dim currentScreen As Screen = Screen.FromControl(Me)

        If Me.Width <> currentScreen.Bounds.Width OrElse Me.Height <> currentScreen.Bounds.Height Then
            Me.Width = currentScreen.Bounds.Width
            Me.Height = currentScreen.Bounds.Height
        End If

        Dim centerX As Integer = currentScreen.Bounds.X + (currentScreen.Bounds.Width - Me.Width) \ 2
        Dim centerY As Integer = currentScreen.Bounds.Y + (currentScreen.Bounds.Height - Me.Height) \ 2

        SetWindowPos(Me.Handle, New IntPtr(HWND_BOTTOM), centerX, centerY, Me.Width, Me.Height, SWP_SHOWWINDOW)
    End Sub

    Private Sub LoadTranscodedWallpaper()
        Try
            Dim appDataPath As String = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
            Dim wallpaperPath As String = Path.Combine(appDataPath, "Microsoft\Windows\Themes\TranscodedWallpaper")

            If Not File.Exists(wallpaperPath) Then
                wallpaperPath = Path.Combine(appDataPath, "Microsoft\Windows\Themes\Wallpaper.jpg")
            End If

            If File.Exists(wallpaperPath) Then
                Using fs As New FileStream(wallpaperPath, FileMode.Open, FileAccess.Read)
                    Dim oldImage As Image = Me.BackgroundImage
                    Me.BackgroundImage = Image.FromStream(fs)
                    Me.BackgroundImageLayout = ImageLayout.Stretch
                    If oldImage IsNot Nothing Then oldImage.Dispose()
                End Using
            End If
        Catch ex As Exception
            MessageBox.Show("GAHHH I CANT LOAD THE DAMN WALLPAPER HERS WHY: " & ex.Message, "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
        End Try
    End Sub

    Private Sub SendToBottom()
        SetWindowPos(Me.Handle, New IntPtr(HWND_BOTTOM), 0, 0, 0, 0, SWP_NOSIZE Or SWP_NOMOVE Or SWP_SHOWWINDOW)
    End Sub

    Private Sub Form1_Activated(sender As Object, e As EventArgs) Handles MyBase.Activated
        SendToBottom()
    End Sub

    Protected Overrides Sub WndProc(ByRef m As Message)
        If m.Msg = WM_ERASEBKGND Then
            m.Result = New IntPtr(1)
            Return
        End If

        If m.Msg = WM_WINDOWPOSCHANGING Then
            Dim wp As WINDOWPOS = CType(Marshal.PtrToStructure(m.LParam, GetType(WINDOWPOS)), WINDOWPOS)
            Dim currentScreen As Screen = Screen.FromControl(Me)
            wp.x = currentScreen.Bounds.X + (currentScreen.Bounds.Width - Me.Width) \ 2
            wp.y = currentScreen.Bounds.Y + (currentScreen.Bounds.Height - Me.Height) \ 2
            wp.hwndInsertAfter = New IntPtr(HWND_BOTTOM)
            Marshal.StructureToPtr(wp, m.LParam, True)
        End If

        If m.Msg = WM_MOVING Then
            Dim r As RECT = CType(Marshal.PtrToStructure(m.LParam, GetType(RECT)), RECT)
            Dim currentScreen As Screen = Screen.FromControl(Me)
            Dim centerX As Integer = currentScreen.Bounds.X + (currentScreen.Bounds.Width - Me.Width) \ 2
            Dim centerY As Integer = currentScreen.Bounds.Y + (currentScreen.Bounds.Height - Me.Height) \ 2
            r.Left = centerX
            r.Top = centerY
            r.Right = centerX + Me.Width
            r.Bottom = centerY + Me.Height
            Marshal.StructureToPtr(r, m.LParam, True)
        End If

        MyBase.WndProc(m)
    End Sub


End Class
