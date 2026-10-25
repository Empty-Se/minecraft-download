Public Class PageSelectLeft
    Implements IRefreshable

    '沿用上游 LaunchFolders / LaunchFolderSelect 记录目录，兼容同目录的 PCL。
    Public Shared Property FolderMutationPending As Boolean = False

    Private Sub PageSelectLeft_Initialized(sender As Object, e As EventArgs) Handles Me.Initialized
        AddHandler McFolderListLoader.PreviewFinish, Sub() RunInUiWait(AddressOf McFolderListUI)
    End Sub
    Private Sub PageSelectLeft_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If McFolderListLoader.State <> LoadState.Loading Then McFolderListUI()
    End Sub

    Private Sub McFolderListUI()
        Try
            If Not McFolderList.Any() Then Return
            If Not McFolderList.Any(Function(Folder) PathUtils.IsSame(Folder.Location, McFolderSelected)) Then
                McFolderSelected = McFolderList.First.Location
                PageDownloadInstall.DownloadRootConfirmed = False
            End If
            PanList.Children.Clear()
            PanList.Children.Add(New TextBlock With {.Text = "Minecraft 目录", .Margin = New Thickness(16, 20, 8, 8), .Opacity = 0.6, .FontSize = 12})
            For Each Folder As McFolder In McFolderList.ToArray()
                Dim IsCurrent = PathUtils.IsSame(Folder.Location, McFolderSelected)
                Dim NewItem As New MyListItem With {
                    .IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.RadioBox,
                    .Title = Folder.Name & If(IsCurrent, " · 当前目录", ""), .Info = Folder.Location,
                    .Height = 52, .Tag = Folder, .ToolTip = Folder.Location, .MinPaddingRight = 12
                }
                AddHandler NewItem.Changed, AddressOf Folder_Change
                PanList.Children.Add(NewItem)
                NewItem.Checked = IsCurrent
            Next
            PanList.Children.Add(New TextBlock With {.Text = "目录管理", .Margin = New Thickness(16, 24, 8, 8), .Opacity = 0.6, .FontSize = 12})
            Dim ItemAdd As New MyListItem With {
                .IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable,
                .Title = "添加 Minecraft 目录", .Height = 40, .Logo = "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20z M11 6h2v5h5v2h-5v5h-2v-5H6v-2h5z",
                .ToolTip = "选择 .minecraft 根目录；其中的多个版本共用 libraries 和 assets。"
            }
            AddHandler ItemAdd.Click, AddressOf Add_Click
            PanList.Children.Add(ItemAdd)
            Dim ItemOpen As New MyListItem With {.IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable, .Title = "打开当前目录", .Height = 40, .Logo = Logo.IconButtonOpen}
            AddHandler ItemOpen.Click, Sub() OpenExplorer(McFolderSelected)
            PanList.Children.Add(ItemOpen)
            Dim ItemRefresh As New MyListItem With {.IsScaleAnimationEnabled = False, .Type = MyListItem.CheckType.Clickable, .Title = "刷新版本列表", .Height = 40, .Logo = Logo.IconButtonRefresh}
            AddHandler ItemRefresh.Click, Sub() RefreshCurrent()
            PanList.Children.Add(ItemRefresh)
            PanList.Children.Add(New TextBlock With {
                .Text = "先选目录，再下载版本。" & vbCrLf & "一个目录可以保存多个游戏版本。",
                .Margin = New Thickness(16, 16, 16, 20), .Opacity = 0.6, .FontSize = 12, .TextWrapping = TextWrapping.Wrap
            })
        Catch ex As Exception
            Logger.Error(ex, "构建 Minecraft 目录列表失败")
        Finally
            If FrmSelectRight IsNot Nothing Then FrmSelectRight.RefreshFolderContext()
        End Try
    End Sub

    Public Sub Add_Click()
        If CannotChangeFolder() Then Return
        Try
            Dim NewFolder = Dialogs.SelectFolder("选择 .minecraft 根目录（不要选择 versions 或单个版本文件夹）", False).FirstOrDefault()
            If String.IsNullOrEmpty(NewFolder) Then Return
            NewFolder = NormalizeMinecraftFolder(NewFolder)
            If NewFolder.Contains("!") OrElse NewFolder.Contains(";") Then
                Hint("Minecraft 目录路径中不能含有感叹号或分号！", HintType.Red)
                Return
            End If
            Dim DefaultName = PathUtils.GetLastPart(NewFolder)
            If DefaultName = ".minecraft" Then DefaultName = PathUtils.GetLastPart(PathUtils.RemoveLastPart(NewFolder))
            If String.IsNullOrWhiteSpace(DefaultName) Then DefaultName = "Minecraft"
            DefaultName = DefaultName.Substring(0, Math.Min(30, DefaultName.Length))
            Dim NewName = MyMsgBoxInput("目录名称", "输入左侧列表中显示的名称；不会重命名磁盘上的文件夹。", DefaultName,
                New ObjectModel.Collection(Of Validate) From {New ValidateNullOrWhiteSpace, New ValidateLength(1, 30), New ValidateExcept({">", "|"})})
            If String.IsNullOrWhiteSpace(NewName) Then Return
            AddFolder(NewFolder, NewName, True)
        Catch ex As Exception
            Logger.Error(ex, "添加 Minecraft 目录失败", LogBehavior.Toast)
        End Try
    End Sub

    Private Shared Function NormalizeMinecraftFolder(FolderPath As String) As String
        FolderPath = PathUtils.AddSlashSuffix(FolderPath)
        If PathUtils.GetLastPart(FolderPath).Equals("versions", StringComparison.OrdinalIgnoreCase) Then
            Return PathUtils.AddSlashSuffix(PathUtils.RemoveLastPart(FolderPath))
        End If
        If Not DirectoryUtils.Exists(FolderPath & "versions\") AndAlso DirectoryUtils.Exists(FolderPath & ".minecraft\") Then Return FolderPath & ".minecraft\"
        Return FolderPath
    End Function

    Private Shared Function CannotChangeFolder() As Boolean
        If McInstanceLock.IsAnyLocked OrElse HasDownloadingTask(True) OrElse FolderMutationPending OrElse McFolderListLoader.State = LoadState.Loading Then
            Hint("请等待当前下载或目录加载完成，再切换 Minecraft 目录。", HintType.Red)
            Return True
        End If
        Return False
    End Function

    ''' <summary>保存并选中 Minecraft 根目录，不更改已有游戏文件。</summary>
    Public Shared Sub AddFolder(FolderPath As String, DisplayName As String, ShowHint As Boolean)
        If FolderMutationPending OrElse McInstanceLock.IsAnyLocked OrElse HasDownloadingTask(True) Then
            If ShowHint Then Hint("请等待当前任务完成后再添加目录。", HintType.Red)
            Return
        End If
        FolderMutationPending = True
        PageDownloadInstall.DownloadRootConfirmed = False
        RunInThread(
        Sub()
            Try
                FolderPath = NormalizeMinecraftFolder(FolderPath)
                If Not CheckPermission(FolderPath) Then Throw New UnauthorizedAccessException("没有访问该目录的权限：" & FolderPath)
                Dim Folders = Settings.Get(Of String)("LaunchFolders").Split("|"c, True).Where(Function(Item) Not String.IsNullOrWhiteSpace(Item)).ToList()
                Dim ExistingIndex = Folders.FindIndex(Function(Item) Item.Contains(">") AndAlso PathUtils.IsSame(Item.AfterFirst(">"), FolderPath))
                If ExistingIndex >= 0 Then
                    Folders(ExistingIndex) = DisplayName & ">" & FolderPath
                Else
                    Folders.Add(DisplayName & ">" & FolderPath)
                End If
                Settings.Set("LaunchFolders", Folders.Join("|"))
                McFolderSelected = FolderPath
                McFolderListLoader.Start(IsForceRestart:=True)
                If ShowHint Then Hint("已选中 Minecraft 目录：" & DisplayName, HintType.Green)
            Catch ex As Exception
                Logger.Error(ex, "添加 Minecraft 目录失败", LogBehavior.Toast)
            Finally
                FolderMutationPending = False
                RunInUi(Sub()
                            If FrmSelectRight IsNot Nothing Then FrmSelectRight.RefreshFolderHeader()
                        End Sub)
            End Try
        End Sub)
    End Sub

    '保留主程序调用的兼容入口。
    Public Shared Sub CreateMcFolderInCurrentPath()
        Dim FolderPath = Paths.Base & ".minecraft\"
        DirectoryUtils.Create(FolderPath & "versions\")
        McFolderLauncherProfilesJsonCreate(FolderPath)
        AddFolder(FolderPath, PathUtils.GetLastPart(Paths.Base), False)
    End Sub

    Public Sub RefreshCurrent() Implements IRefreshable.Refresh
        RefreshCurrent(McFolderSelected)
    End Sub
    Public Shared Sub RefreshCurrent(Folder As String)
        If McInstanceLock.IsAnyLocked OrElse HasDownloadingTask(True) OrElse FolderMutationPending Then
            Hint("请等待当前任务完成后再刷新目录。", HintType.Red)
            Return
        End If
        WriteIni(Folder & "PCL.ini", "InstanceCache", "")
        If PathUtils.IsSame(Folder, McFolderSelected) Then LoaderFolderRun(McInstanceListLoader, Folder, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
    End Sub

    Public Sub Folder_Change(sender As MyListItem, e As RouteEventArgs)
        If Not e.RaiseByMouse OrElse Not sender.Checked Then Return
        If CannotChangeFolder() Then
            e.Handled = True
            Return
        End If
        McFolderSelected = CType(sender.Tag, McFolder).Location
        PageDownloadInstall.DownloadRootConfirmed = False
        McFolderListUI()
    End Sub
End Class
