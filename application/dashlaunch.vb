Imports System.IO
Imports System.Runtime.InteropServices

Public Class dashlaunch
    <DllImport("user32.dll")>
    Private Shared Function SetWindowCompositionAttribute(hwnd As IntPtr, ByRef data As WindowCompositionAttributeData) As Integer
    End Function

    <DllImport("shell32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SHGetFileInfo(pszPath As String, dwFileAttributes As Integer, ByRef psfi As SHFILEINFO, cbFileInfo As Integer, uFlags As Integer) As IntPtr
    End Function

    <DllImport("user32.dll", SetLastError:=True)>
    Private Shared Function DestroyIcon(hIcon As IntPtr) As Boolean
    End Function

    <DllImport("user32.dll", CharSet:=CharSet.Auto)>
    Private Shared Function SendMessage(hWnd As IntPtr, Msg As Integer, wParam As Integer, lParam As Integer) As Integer
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

    <StructLayout(LayoutKind.Sequential, CharSet:=CharSet.Auto)>
    Friend Structure SHFILEINFO
        Public hIcon As IntPtr
        Public iIcon As Integer
        Public dwAttributes As Integer
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=260)>
        Public szDisplayName As String
        <MarshalAs(UnmanagedType.ByValTStr, SizeConst:=80)>
        Public szTypeName As String
    End Structure

    Private Const SHGFI_ICON As Integer = &H100
    Private Const SHGFI_LARGEICON As Integer = &H0

    Private Const LVM_SETBKCOLOR As Integer = &H1001
    Private Const CLR_NONE As Integer = -1

    Private DynamicImages As New ImageList()

    Private Sub dashlaunch_Load(sender As Object, e As EventArgs) Handles MyBase.Load
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

        SendMessage(ListView1.Handle, LVM_SETBKCOLOR, 0, CLR_NONE)
        SendMessage(ListView2.Handle, LVM_SETBKCOLOR, 0, CLR_NONE)

        Const LVM_SETTEXTBKCOLOR As Integer = &H1026
        SendMessage(ListView1.Handle, LVM_SETTEXTBKCOLOR, 0, CLR_NONE)
        SendMessage(ListView2.Handle, LVM_SETTEXTBKCOLOR, 0, CLR_NONE)

        DynamicImages.ImageSize = New Size(32, 32)
        DynamicImages.ColorDepth = ColorDepth.Depth32Bit
        ListView1.LargeImageList = DynamicImages

        LoadDirectoryContents("C:\ProgramData\Microsoft\Windows\Start Menu\Programs")
    End Sub

    Private Sub LoadDirectoryContents(path As String)
        ListView1.Items.Clear()
        ListView2.Items.Clear()
        DynamicImages.Images.Clear()

        If Not Directory.Exists(path) Then
            MessageBox.Show("Target directory does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Warning)
            Return
        End If

        Try
            Dim files As String() = Directory.GetFiles(path)
            For Each file As String In files
                Dim fileInfo As New FileInfo(file)
                Dim fileItem As New ListViewItem(fileInfo.Name)

                Dim shfi As New SHFILEINFO()
                Dim flags As Integer = SHGFI_ICON Or SHGFI_LARGEICON
                SHGetFileInfo(file, 0, shfi, Marshal.SizeOf(shfi), flags)

                If shfi.hIcon <> IntPtr.Zero Then
                    Dim ico As Icon = Icon.FromHandle(shfi.hIcon)
                    DynamicImages.Images.Add(file, ico)
                    fileItem.ImageKey = file
                    DestroyIcon(shfi.hIcon)
                End If

                fileItem.Tag = fileInfo.FullName

                fileItem.ForeColor = Color.White

                ListView1.Items.Add(fileItem)
            Next

            Dim directories As String() = Directory.GetDirectories(path)
            For Each dir As String In directories
                Dim dirInfo As New DirectoryInfo(dir)
                Dim dirItem As New ListViewItem(dirInfo.Name)

                dirItem.Tag = dirInfo.FullName

                dirItem.ForeColor = Color.White

                ListView2.Items.Add(dirItem)
            Next

        Catch ex As UnauthorizedAccessException
            MessageBox.Show("Access denied", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        Catch ex As Exception
            MessageBox.Show($"An error occurred: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error)
        End Try
    End Sub

    Private Sub ListView2_ItemActivate(sender As Object, e As EventArgs) Handles ListView2.ItemActivate
        If ListView2.SelectedItems.Count > 0 Then
            Dim newPath As String = ListView2.SelectedItems(0).Tag.ToString()
            LoadDirectoryContents(newPath)
        End If
    End Sub

    Private Sub Label1_Click(sender As Object, e As EventArgs) Handles Label1.Click
        Me.Close()
    End Sub

    Private Sub SplitContainer1_Panel1_Paint(sender As Object, e As PaintEventArgs) Handles SplitContainer1.Panel1.Paint
    End Sub

    Private Sub SplitContainer1_Panel2_Paint(sender As Object, e As PaintEventArgs) Handles SplitContainer1.Panel2.Paint
    End Sub

    Private Sub ListView1_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView1.SelectedIndexChanged
    End Sub

    Private Sub ListView2_SelectedIndexChanged(sender As Object, e As EventArgs) Handles ListView2.SelectedIndexChanged
    End Sub

    Private Sub TableLayoutPanel1_Paint(sender As Object, e As PaintEventArgs) Handles TableLayoutPanel1.Paint

    End Sub
End Class