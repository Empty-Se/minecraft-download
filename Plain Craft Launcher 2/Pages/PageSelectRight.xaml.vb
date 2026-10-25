Public Class PageSelectRight

    '窗口基础
    Private Sub PageSelectRight_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If McFolderListLoader.State <> LoadState.Loading Then RefreshFolderContext()
        RefreshFolderHeader()
        PanBack.ScrollToHome()
    End Sub
    Private Sub LoaderInit() Handles Me.Initialized
        PageLoaderInit(Load, PanLoad, PanAllBack, Nothing, McInstanceListLoader, AddressOf McInstanceListUI, AutoRun:=False)
        AddHandler McFolderListLoader.LoadingStateChanged, Sub() RunInUi(AddressOf RefreshFolderHeader)
        AddHandler McInstanceListLoader.LoadingStateChanged, Sub() RunInUi(AddressOf RefreshFolderHeader)
    End Sub

    Public Sub RefreshFolderHeader()
        If LabFolderPath Is Nothing Then Return
        Dim Root = McFolderSelected
        LabFolderPath.Text = If(String.IsNullOrWhiteSpace(Root), "正在读取 Minecraft 目录…", Root)
        LabFolderPath.ToolTip = Root
        Dim CanDownload = Not String.IsNullOrWhiteSpace(Root) AndAlso DirectoryUtils.Exists(Root) AndAlso
            Not PageSelectLeft.FolderMutationPending AndAlso McFolderListLoader.State <> LoadState.Loading AndAlso
            McInstanceListLoader.State <> LoadState.Loading AndAlso
            McFolderList.Any(Function(Folder) PathUtils.IsSame(Folder.Location, Root))
        BtnFolderDownload.IsEnabled = CanDownload
        BtnEmptyDownload.IsEnabled = CanDownload
    End Sub

    Public Sub RefreshFolderContext()
        RefreshFolderHeader()
        If String.IsNullOrWhiteSpace(McFolderSelected) OrElse Not DirectoryUtils.Exists(McFolderSelected) Then Return
        LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.RunOnUpdated, MaxDepth:=1, ExtraPath:="versions\")
    End Sub
    Private Sub Load_Click(sender As Object, e As MouseButtonEventArgs) Handles Load.Click
        If McInstanceListLoader.State = LoadState.Failed Then
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        End If
    End Sub

    '窗口属性
    ''' <summary>
    ''' 是否显示隐藏的 Minecraft 版本。
    ''' </summary>
    Public ShowHidden As Boolean = False

#Region "结果 UI 化"

    Private Sub McInstanceListUI(Loader As LoaderTask(Of String, Integer))
        Try
            RefreshFolderHeader()
            '加载 UI
            PanMain.Children.Clear()

            For Each Card As KeyValuePair(Of McInstanceCardType, List(Of McInstance)) In McInstanceList.ToArray
                '确认是否为隐藏版本显示状态
                If Card.Key = McInstanceCardType.Hidden Xor ShowHidden Then Continue For
                Dim Instances = Card.Value.Where(Function(Instance) Not McInstanceLock.IsLocked(Instance)).ToList
                If Not Instances.Any Then Continue For
#Region "确认卡片名称"
                Dim CardName As String = ""
                Select Case Card.Key
                    Case McInstanceCardType.OriginalLike
                        CardName = "常规版本"
                    Case McInstanceCardType.API
                        Dim IsForgeExists As Boolean = False
                        Dim IsNeoForgeExists As Boolean = False
                        Dim IsFabricExists As Boolean = False
                        Dim IsLiteExists As Boolean = False
                        For Each Instance As McInstance In Instances
                            If Instance.Version.ModLoaders.HasFlagF(ModLoaders.Fabric) Then IsFabricExists = True
                            If Instance.Version.ModLoaders.HasFlagF(ModLoaders.LiteLoader) Then IsLiteExists = True
                            If Instance.Version.ModLoaders.HasFlagF(ModLoaders.Forge) Then IsForgeExists = True
                            If Instance.Version.ModLoaders.HasFlagF(ModLoaders.NeoForge) Then IsNeoForgeExists = True
                        Next
                        If If(IsLiteExists, 1, 0) + If(IsForgeExists, 1, 0) + If(IsFabricExists, 1, 0) + If(IsNeoForgeExists, 1, 0) > 1 Then
                            CardName = "可安装 Mod"
                        ElseIf IsForgeExists Then
                            CardName = "Forge 版本"
                        ElseIf IsNeoForgeExists Then
                            CardName = "NeoForge 版本"
                        ElseIf IsLiteExists Then
                            CardName = "LiteLoader 版本"
                        Else
                            CardName = "Fabric 版本"
                        End If
                    Case McInstanceCardType.Error
                        CardName = "错误的版本"
                    Case McInstanceCardType.Hidden
                        CardName = "隐藏的版本"
                    Case McInstanceCardType.Rubbish
                        CardName = "不常用版本"
                    Case McInstanceCardType.Star
                        CardName = "收藏夹"
                    Case McInstanceCardType.Fool
                        CardName = "愚人节版本"
                    Case Else
                        Throw New ArgumentException("未知的卡片种类（" & Card.Key & "）")
                End Select
#End Region
                '建立控件
                Dim CardTitle As String = CardName & If(CardName = "收藏夹", "", " (" & Instances.Count & ")")
                Dim NewCard As New MyCard With {.Title = CardTitle, .Margin = New Thickness(0, 0, 0, 15)}
                Dim NewStack As New StackPanel With {.Margin = New Thickness(20, MyCard.SwapedHeight, 18, 0), .VerticalAlignment = VerticalAlignment.Top, .RenderTransform = New TranslateTransform(0, 0)}
                NewCard.Children.Add(NewStack)
                NewCard.SwapControl = NewStack
                NewCard.SetContentBuilder(Instances, Function(Instance) Instance.ToListItem(AddressOf McInstanceListContent))
                PanMain.Children.Add(NewCard)
                '确定卡片是否展开
                If Card.Key = McInstanceCardType.Rubbish OrElse Card.Key = McInstanceCardType.Error OrElse Card.Key = McInstanceCardType.Fool Then
                    NewCard.IsSwapped = True
                Else
                    NewCard.BuildContent()
                End If
            Next

            '若只有一个卡片，则强制展开
            If PanMain.Children.Count = 1 AndAlso CType(PanMain.Children(0), MyCard).IsSwapped Then
                CType(PanMain.Children(0), MyCard).IsSwapped = False
            End If

            '判断应该显示哪一个页面
            If PanMain.Children.Count = 0 Then
                PanEmpty.Visibility = Visibility.Visible
                PanBack.Visibility = Visibility.Collapsed
                If ShowHidden Then
                    LabEmptyTitle.Text = "无隐藏版本"
                    LabEmptyContent.Text = "没有版本被隐藏，你可以在版本设置的版本分类选项中隐藏版本。" & vbCrLf & "再次按下 F11 即可退出隐藏版本查看模式。"
                    BtnEmptyDownload.Visibility = Visibility.Collapsed
                Else
                    LabEmptyTitle.Text = "此目录暂无游戏版本"
                    LabEmptyContent.Text = "确认上方目录后，即可下载第一个版本。" & vbCrLf & "已有游戏？在左侧添加对应的 .minecraft 根目录。"
                    BtnEmptyDownload.Visibility = Visibility.Visible
                End If
            Else
                PanBack.Visibility = Visibility.Visible
                PanEmpty.Visibility = Visibility.Collapsed
            End If

        Catch ex As Exception
            Logger.Error(ex, "将版本列表转换显示时失败")
        End Try
    End Sub
    Public Shared Sub McInstanceListContent(sender As MyListItem, e As EventArgs)
        Dim Instance As McInstance = sender.Tag
        '注册点击事件
        AddHandler sender.Click, AddressOf Item_Click
        '图标按钮
        Dim BtnStar As New MyIconButton
        If Instance.IsStar Then
            BtnStar.ToolTip = "取消收藏"
            BtnStar.LogoScale = 1.1
            BtnStar.Logo = Logo.IconButtonLikeFill
        Else
            BtnStar.ToolTip = "收藏"
            BtnStar.LogoScale = 1.1
            BtnStar.Logo = Logo.IconButtonLikeLine
        End If
        AddHandler BtnStar.Click,
        Sub()
            WriteIni(Instance.PathVersion & "PCL\Setup.ini", "IsStar", Not Instance.IsStar)
            McInstanceListForceRefresh = True
            LoaderFolderRun(McInstanceListLoader, McFolderSelected, LoaderFolderRunType.ForceRun, MaxDepth:=1, ExtraPath:="versions\")
        End Sub
        Dim BtnOpen As New MyIconButton With {.LogoScale = 1.15, .Logo = Logo.IconButtonOpen, .ToolTip = "打开版本文件夹"}
        AddHandler BtnOpen.Click, Sub() OpenExplorer(Instance.PathVersion)
        AddHandler sender.MouseRightButtonUp, Sub() OpenExplorer(Instance.PathVersion)
        sender.Buttons = {BtnStar, BtnOpen}
    End Sub

#End Region

#Region "页面事件"

    '点击选项
    Public Shared Sub Item_Click(sender As MyListItem, e As EventArgs)
        Dim Instance As McInstance = sender.Tag
        OpenExplorer(Instance.PathVersion)
    End Sub

    Private Sub BtnDownload_Click(sender As Object, e As EventArgs) Handles BtnEmptyDownload.Click, BtnFolderDownload.Click
        RefreshFolderHeader()
        If Not BtnFolderDownload.IsEnabled Then
            Hint("请先选择 Minecraft 目录，并等待目录加载完成。", HintType.Red)
            Return
        End If
        PageDownloadInstall.ConfirmedDownloadRoot = McFolderSelected
        PageDownloadInstall.DownloadRootConfirmed = True
        FrmMain.PageChange(FormMain.PageType.Download, FormMain.PageSubType.DownloadInstall)
    End Sub

    Public Sub BtnEmptyDownload_Loaded() Handles BtnEmptyDownload.Loaded
        Dim NewVisibility = If(ShowHidden, Visibility.Collapsed, Visibility.Visible)
        If BtnEmptyDownload.Visibility <> NewVisibility Then
            BtnEmptyDownload.Visibility = NewVisibility
            PanLoad.TriggerForceResize()
        End If
    End Sub

#End Region

End Class

