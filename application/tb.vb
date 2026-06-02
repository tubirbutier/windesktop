Imports System.Runtime.InteropServices
Imports System.Text

Public Class tb
    <DllImport("user32.dll")>
    Private Shared Function GetWindowThreadProcessId(hWnd As IntPtr, ByRef lpdwProcessId As Integer) As Integer
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SendMessageTimeout(hWnd As IntPtr, Msg As Integer, wParam As IntPtr, lParam As IntPtr, flags As Integer, timeout As Integer, ByRef result As IntPtr) As IntPtr
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function GetClassLong(hWnd As IntPtr, nIndex As Integer) As IntPtr
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetWindowCompositionAttribute(hwnd As IntPtr, ByRef data As WindowCompositionAttributeData) As Integer
    End Function

    Friend Enum WindowCompositionAttribute
        WCA_ACCENT_POLICY = 19
    End Enum

    Friend Enum AccentState
        ACCENT_ENABLE_ACRYLICBLURBEHIND = 4
    End Enum

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure AccentPolicy
        Public AccentState As AccentState
        Public AccentFlags As Integer
        Public GradientColor As Integer
        Public AnimationId As Integer
    End Structure

    <StructLayout(LayoutKind.Sequential)>
    Friend Structure WindowCompositionAttributeData
        Public Attribute As WindowCompositionAttribute
        Public Data As IntPtr
        Public SizeOfData As Integer
    End Structure

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

    Private Structure TaskbarApp
        Public Handle As IntPtr
        Public ButtonCtrl As Button
    End Structure

    Private CurrentButtons As New List(Of TaskbarApp)()

    Private Delegate Function EnumWindowsProc(hWnd As IntPtr, lParam As IntPtr) As Boolean

    <DllImport("shell32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SHAppBarMessage(dwMessage As Integer, ByRef pData As APPBARDATA) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function SetWindowPos(hWnd As IntPtr, hWndInsertAfter As IntPtr, X As Integer, Y As Integer, cx As Integer, cy As Integer, uFlags As Integer) As Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function EnumWindows(lpEnumFunc As EnumWindowsProc, lParam As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function IsWindowVisible(hWnd As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function GetWindowText(hWnd As IntPtr, lpString As StringBuilder, nMaxCount As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function GetWindowLong(hWnd As IntPtr, nIndex As Integer) As Integer
    End Function

    <DllImport("user32.dll")>
    Private Shared Function SetForegroundWindow(hWnd As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll")>
    Private Shared Function ShowWindow(hWnd As IntPtr, nCmdShow As Integer) As Boolean
    End Function

    Private Const ABM_NEW As Integer = &H0
    Private Const ABM_REMOVE As Integer = &H1
    Private Const ABM_SETPOSITION As Integer = &H2
    Private Const ABE_BOTTOM As Integer = 3

    Private Const SWP_NOSIZE As Integer = &H1
    Private Const SWP_NOMOVE As Integer = &H2
    Private Const SWP_NOACTIVATE As Integer = &H10
    Private Shared ReadOnly HWND_TOPMOST As New IntPtr(-1)
    Private Const WS_EX_NOACTIVATE As Integer = &H8000000

    Private Const GWL_EXSTYLE As Integer = -20
    Private Const WS_EX_TOOLWINDOW As Integer = &H80
    Private Const WS_EX_APPWINDOW As Integer = &H40000
    Private Const SW_RESTORE As Integer = 9

    Private Const WM_GETICON As Integer = &H7F
    Private Const ICON_SMALL2 As Integer = 2
    Private Const ICON_SMALL As Integer = 0
    Private Const ICON_BIG As Integer = 1
    Private Const GCL_HICONSM As Integer = -34
    Private Const GCL_HICON As Integer = -14
    Private Const SMTO_ABORTIFHUNG As Integer = &H2

    Private TaskbarPanel As FlowLayoutPanel

    Protected Overrides ReadOnly Property CreateParams As CreateParams
        Get
            Dim cp As CreateParams = MyBase.CreateParams
            cp.ExStyle = cp.ExStyle Or WS_EX_NOACTIVATE
            Return cp
        End Get
    End Property

    Private Sub tb_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.MinimumSize = New Size(0, 0)
        Me.MaximumSize = New Size(69420, 44)
        Me.ShowInTaskbar = False

        Me.BackColor = Color.FromArgb(30, 30, 30)

        Dim policy As New AccentPolicy()
        policy.AccentState = AccentState.ACCENT_ENABLE_ACRYLICBLURBEHIND
        policy.GradientColor = &H99222222

        Dim policySize As Integer = Marshal.SizeOf(policy)
        Dim policyPtr As IntPtr = Marshal.AllocHGlobal(policySize)
        Marshal.StructureToPtr(policy, policyPtr, False)

        Dim data As New WindowCompositionAttributeData()
        data.Attribute = WindowCompositionAttribute.WCA_ACCENT_POLICY
        data.SizeOfData = policySize
        data.Data = policyPtr

        SetWindowCompositionAttribute(Me.Handle, data)
        Marshal.FreeHGlobal(policyPtr)

        RegisterAppBar()
        SetupTaskbarPanel()

        Dim RefreshTimer As New Timer()
        RefreshTimer.Interval = 1000
        AddHandler RefreshTimer.Tick, AddressOf RefreshTaskbarApps
        RefreshTimer.Start()
    End Sub

    Private Sub SetupTaskbarPanel()
        TaskbarPanel = New FlowLayoutPanel()
        TaskbarPanel.Location = New Point(100, 0)
        TaskbarPanel.Size = New Size(Me.Width - 200, Me.Height)
        TaskbarPanel.Anchor = AnchorStyles.Left Or AnchorStyles.Right Or AnchorStyles.Top Or AnchorStyles.Bottom
        TaskbarPanel.FlowDirection = FlowDirection.LeftToRight
        TaskbarPanel.WrapContents = False
        TaskbarPanel.BackColor = Color.Transparent
        Me.Controls.Add(TaskbarPanel)
    End Sub

    Private Function GetAppIcon(hWnd As IntPtr) As Image
        Dim hIcon As IntPtr = IntPtr.Zero
        Dim res As IntPtr = IntPtr.Zero

        SendMessageTimeout(hWnd, WM_GETICON, New IntPtr(ICON_SMALL2), IntPtr.Zero, SMTO_ABORTIFHUNG, 100, res)
        hIcon = res

        If hIcon = IntPtr.Zero Then
            SendMessageTimeout(hWnd, WM_GETICON, New IntPtr(ICON_SMALL), IntPtr.Zero, SMTO_ABORTIFHUNG, 100, res)
            hIcon = res
        End If

        If hIcon = IntPtr.Zero Then
            SendMessageTimeout(hWnd, WM_GETICON, New IntPtr(ICON_BIG), IntPtr.Zero, SMTO_ABORTIFHUNG, 100, res)
            hIcon = res
        End If

        If hIcon = IntPtr.Zero Then
            hIcon = GetClassLong(hWnd, GCL_HICONSM)
        End If

        If hIcon = IntPtr.Zero Then
            hIcon = GetClassLong(hWnd, GCL_HICON)
        End If

        If hIcon <> IntPtr.Zero Then
            Try
                Dim ico As Icon = Icon.FromHandle(hIcon)
                Dim bmp As Bitmap = ico.ToBitmap()
                Return bmp
            Catch
            End Try
        End If

        Return Nothing
    End Function

    Private Sub RefreshTaskbarApps(sender As Object, e As EventArgs)
        Dim foundHandles As New List(Of IntPtr)()

        EnumWindows(Function(hWnd, lParam)
                        If hWnd = Me.Handle Then Return True
                        If Not IsWindowVisible(hWnd) Then Return True

                        Dim titleBuilder As New StringBuilder(256)
                        GetWindowText(hWnd, titleBuilder, titleBuilder.Capacity)
                        Dim title As String = titleBuilder.ToString()
                        If String.IsNullOrEmpty(title) Then Return True

                        Dim exStyle As Integer = GetWindowLong(hWnd, GWL_EXSTYLE)
                        If (exStyle And WS_EX_TOOLWINDOW) = WS_EX_TOOLWINDOW Then
                            If (exStyle And WS_EX_APPWINDOW) <> WS_EX_APPWINDOW Then Return True
                        End If

                        foundHandles.Add(hWnd)

                        If Not CurrentButtons.Any(Function(b) b.Handle = hWnd) Then
                            Dim processId As Integer = 0
                            GetWindowThreadProcessId(hWnd, processId)

                            Dim btn As New Button()
                            btn.Text = ""
                            btn.Size = New Size(TaskbarPanel.Height, TaskbarPanel.Height)
                            btn.FlatStyle = FlatStyle.Flat
                            btn.FlatAppearance.BorderSize = 0
                            btn.BackColor = Color.Transparent
                            btn.ForeColor = Color.White
                            btn.Tag = hWnd

                            Dim img As Image = GetAppIcon(hWnd)
                            If img IsNot Nothing Then
                                btn.Image = img
                                btn.ImageAlign = ContentAlignment.MiddleCenter
                            End If

                            AddHandler btn.Click, AddressOf AppButton_Click

                            TaskbarPanel.Controls.Add(btn)
                            CurrentButtons.Add(New TaskbarApp With {.Handle = hWnd, .ButtonCtrl = btn})
                        End If

                        Return True
                    End Function, IntPtr.Zero)

        For i As Integer = CurrentButtons.Count - 1 To 0 Step -1
            Dim trackedApp = CurrentButtons(i)
            If Not foundHandles.Contains(trackedApp.Handle) Then
                TaskbarPanel.Controls.Remove(trackedApp.ButtonCtrl)
                If trackedApp.ButtonCtrl.Image IsNot Nothing Then
                    trackedApp.ButtonCtrl.Image.Dispose()
                End If
                trackedApp.ButtonCtrl.Dispose()
                CurrentButtons.RemoveAt(i)
            End If
        Next
    End Sub

    Private Sub AppButton_Click(sender As Object, e As EventArgs)
        Dim btn As Button = CType(sender, Button)
        Dim targetHWnd As IntPtr = CType(btn.Tag, IntPtr)

        Dim clickedProcessId As Integer = 0
        GetWindowThreadProcessId(targetHWnd, clickedProcessId)

        Dim siblingWindows As New List(Of IntPtr)()
        For Each app In CurrentButtons
            Dim pid As Integer = 0
            GetWindowThreadProcessId(app.Handle, pid)
            If pid = clickedProcessId Then
                siblingWindows.Add(app.Handle)
            End If
        Next

        ShowWindow(targetHWnd, SW_RESTORE)
        SetForegroundWindow(targetHWnd)
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

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Dim MENU = New dashlaunch()
        MENU.Show()
    End Sub
End Class