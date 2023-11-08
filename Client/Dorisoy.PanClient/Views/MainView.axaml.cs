using Avalonia;
using Avalonia.Animation;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;
using Avalonia.ReactiveUI;
using Avalonia.Styling;
using FluentAvalonia.Core;
using FluentAvalonia.UI.Media.Animation;
using FluentAvalonia.UI.Navigation;
using FluentAvalonia.UI.Windowing;
using LocalizationManager;
using Dorisoy.PanClient.ViewModels;

namespace Dorisoy.PanClient.Views;

public partial class MainView : ReactiveUserControl<MainViewViewModel>
{
    public MainView()
    {
        InitializeComponent();

        TitleBarHost.IsVisible = true;
        NavView.IsVisible = true;


        this.WhenActivated(disposables => 
        {
            var vm = ViewModel;
        });
    }


    public IStorageProvider GetStorageProvider()
    {
        return TopLevel.GetTopLevel(this).StorageProvider;
    }

    public IMutableDependencyResolver RegisterStorageProvider(IMutableDependencyResolver services)
    {
        services.RegisterLazySingleton(() => GetStorageProvider());
        return services;
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);

        //ճ����
        ClipboardService.Owner = TopLevel.GetTopLevel(this);

        // �򵥵ļ��-��Ӧ�ó������������汾������һ�����ڣ���ΪTopLevel Mobile��WASM������������
        _isDesktop = TopLevel.GetTopLevel(this) is Window;

        var vm = Locator.Current.GetService<IActivatableViewModel>("MainView") as MainViewViewModel;
        if (vm != null)
        {
            DataContext = vm;
            FrameView.NavigationPageFactory = vm.NavigationFactory;
        }

        // �������ϣ����ڽ���splashscreen�ڼ���ô�����
        if (e.Root is AppWindow aw)
        {
            var mass = aw.SplashScreen as MainAppSplashScreen;
            mass.InitApp += () =>
            {
                Dispatcher.UIThread.Post(() =>
                {
                    ////ȫ�����
                    //mass.Owner.WindowState = WindowState.Maximized;
                    ////��ʾ�����С��
                    //mass.Owner.ExtendClientAreaToDecorationsHint = true;
                });
            };
            mass.RunTasks(new CancellationToken());
        }

        NavView.Classes.Add("SinolAppNav");

        //NavView.PaneDisplayMode = NavigationViewPaneDisplayMode.LeftCompact;

        FrameView.NavigateFromObject((NavView.MenuItemsSource.ElementAt(0) as Control).Tag);

        FrameView.Navigated += OnFrameViewNavigated;
        FrameView.Navigating += Frame_Navigating;

        NavView.ItemInvoked += OnNavigationViewItemInvoked;
        NavView.BackRequested += OnNavigationViewBackRequested;

        //Ĭ�ϵ�����HomePage
        FrameView.Navigate(typeof(HomePage));
        NavigationService.Instance.SetFrame(FrameView);

        //NavigationService.Instance.SetOverlayHost(OverlayHost);
    }

    private void Frame_Navigating(object sender, NavigatingCancelEventArgs e)
    {
        UpdateNavigationLocalization();
    }

    protected override void OnLoaded(RoutedEventArgs e)
    {
        base.OnLoaded(e);
        if (VisualRoot is AppWindow aw)
        {
            TitleBarHost.ColumnDefinitions[4].Width = new GridLength(aw.TitleBar.RightInset, GridUnitType.Pixel);
            App.MainView = this;
        }
    }

    protected override void OnPointerReleased(PointerReleasedEventArgs e)
    {
        var pt = e.GetCurrentPoint(this);

        // ֡����X1->BackRequested�Զ������ǿ����ڴ˴�����X2������ǰ�򵼺�
        if (pt.Properties.PointerUpdateKind == PointerUpdateKind.XButton2Released)
        {
            if (FrameView.CanGoForward)
            {
                FrameView.GoForward();
                e.Handled = true;
            }
        }
        base.OnPointerReleased(e);
    }

    /// <summary>
    /// ����
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnNavigationViewBackRequested(object sender, NavigationViewBackRequestedEventArgs e)
    {
        FrameView.GoBack();
    }

    /// <summary>
    /// ������
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    private void OnNavigationViewItemInvoked(object sender, NavigationViewItemInvokedEventArgs e)
    {
        // ����ǰ��ѡ��Ŀ���Ļ�������SetNVIIcon�����ͷ�ΪNavigationViewItem��false����
        if (e.InvokedItemContainer is NavigationViewItem nvi)
        {
            NavigationTransitionInfo info;
            info = e.RecommendedNavigationTransitionInfo;
            NavigationService.Instance.NavigateFromContext(nvi.Tag, info, this);
        }
    }

    public MainPageViewModelBase mainPage = null;

    private void OnFrameViewNavigated(object sender, NavigationEventArgs e)
    {
        var localize = LocalizationManagerExtensions.Default;
        var page = e.Content as Control;
        var dc = page.DataContext;

        if (dc is MainPageViewModelBase mpvmb)
        {
            mainPage = mpvmb;
        }

        if (dc is PageBaseViewModel pbvm)
        {
            mainPage = pbvm.Parent;
        }

        foreach (NavigationViewItem nvi in NavView.MenuItemsSource)
        {
            var nt = nvi.Tag.ToString();
            var mn = mainPage.GetType().FullName;
            if (nt == mn)
            {
                //NavView.SelectedItem = nvi;
                SetNVIIcon(nvi, true);
            }
            else
            {
                SetNVIIcon(nvi, false);
            }
        }

        foreach (NavigationViewItem nvi in NavView.FooterMenuItemsSource)
        {
            if (nvi.Name == "SettingsPage")
            {
                nvi.Content = localize.GetValue("SettingsPage");
                //NavView.SelectedItem = nvi;
                SetNVIIcon(nvi, true);
            }
            else
            {
                SetNVIIcon(nvi, false);
            }
        }

        if (FrameView.BackStackDepth > 0 && !NavView.IsBackButtonVisible)
        {
            AnimateContentForBackButton(true);
        }
        else if (FrameView.BackStackDepth == 0 && NavView.IsBackButtonVisible)
        {
            AnimateContentForBackButton(false);
        }
    }

    /// <summary>
    /// ���µ��������ػ�
    /// </summary>
    public void UpdateNavigationLocalization()
    {
        var localize = LocalizationManagerExtensions.Default;
        foreach (NavigationViewItem item in NavView.MenuItemsSource)
        {
            var t = item.Tag;
            if (t is HomePageViewModel)
            {
                item.Content = localize.GetValue("HomePage");
            }
            else if (t is SettingsPageViewModel)
            {
                item.Content = localize.GetValue("SettingsPage");
            }
            else if (t is UserPageViewModel)
            {
                item.Content = localize.GetValue("UserPage");
            }
            else if (t is RolePageViewModel)
            {
                item.Content = localize.GetValue("RolePage");
            }
            else if (t is PermissionPageViewModel)
            {
                item.Content = localize.GetValue("PermissionPage");
            }
            else if (t is DocumentPageViewModel)
            {
                item.Content = localize.GetValue("DocumentPage");
            }
        }


        foreach (NavigationViewItem item in NavView.FooterMenuItems)
        {
            var t = item.Tag;
            if (t is SettingsPageViewModel)
            {
                item.Content = localize.GetValue("SettingsPage");
            }
        }
    }

    /// <summary>
    /// ʵ��Frame����ת
    /// </summary>
    /// <param name="view"></param>
    public void NavigateTo(object view)
    {
        var menuItems = NavView.MenuItemsSource;
        NavigationViewItem ctyp = null;
        foreach (NavigationViewItem cnvi in menuItems)
        {
            var tagName = cnvi.Tag.GetType().FullName;
            var vbtName = view.ToString();
            if (tagName == vbtName)
            {
                ctyp = cnvi;
                break;
            }
        }

        if (ctyp != null && ctyp is NavigationViewItem nvi)
        {
            var info = new SuppressNavigationTransitionInfo();
            NavView.SelectedItem = nvi;
            NavigationService.Instance.NavigateFromContext(nvi.Tag, info, this);
        }
    }


    /// <summary>
    /// ���õ����˵�ͼ��
    /// </summary>
    /// <param name="item"></param>
    /// <param name="selected"></param>
    private void SetNVIIcon(NavigationViewItem item, bool selected)
    {
        //��������ð󶨺�ת�����ȵȣ���ͼ�����ѡ��������δ���֮��仯������Ҫ�򵥵ö�
        if (item == null)
            return;
        var t = item.Tag;

        if (t is HomePageViewModel)
        {
            item.IconSource = this.TryFindResource(selected ? "HomeIconFilled" : "HomeIconFilled", out var value) ?
                (IconSource)value : null;
        }
        else if (t is SettingsPageViewModel)
        {
            item.IconSource = this.TryFindResource(selected ? "SettingsIconFilled" : "SettingsIconFilled", out var value) ?
               (IconSource)value : null;
        }
        else if (t is UserPageViewModel)
        {
            item.IconSource = this.TryFindResource(selected ? "PeopleIconFilled" : "PeopleIconFilled", out var value) ?
               (IconSource)value : null;
        }
        else if (t is RolePageViewModel)
        {
            item.IconSource = this.TryFindResource(selected ? "SpeechSolidBoldIcon" : "SpeechSolidBoldIcon", out var value) ?
               (IconSource)value : null;
        }
        else if (t is PermissionPageViewModel)
        {
            item.IconSource = this.TryFindResource(selected ? "DefenderAppIconFilled" : "DefenderAppIconFilled", out var value) ?
               (IconSource)value : null;
        }
        else if (t is DocumentPageViewModel)
        {
            item.IconSource = this.TryFindResource(selected ? "FolderIconFilled" : "FolderIconFilled", out var value) ?
               (IconSource)value : null;
        }
    }

    private async void AnimateContentForBackButton(bool show)
    {
        if (!WindowIcon.IsVisible)
            return;

        if (show)
        {
            var ani = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(250),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(MarginProperty, new Thickness(12, 4, 12, 4))
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        KeySpline = new KeySpline(0,0,0,1),
                        Setters =
                        {
                            new Setter(MarginProperty, new Thickness(48,4,12,4))
                        }
                    }
                }
            };

            await ani.RunAsync(WindowIcon);

            NavView.IsBackButtonVisible = true;
        }
        else
        {
            NavView.IsBackButtonVisible = false;

            var ani = new Animation
            {
                Duration = TimeSpan.FromMilliseconds(250),
                FillMode = FillMode.Forward,
                Children =
                {
                    new KeyFrame
                    {
                        Cue = new Cue(0d),
                        Setters =
                        {
                            new Setter(MarginProperty, new Thickness(48, 4, 12, 4))
                        }
                    },
                    new KeyFrame
                    {
                        Cue = new Cue(1d),
                        KeySpline = new KeySpline(0,0,0,1),
                        Setters =
                        {
                            new Setter(MarginProperty, new Thickness(12,4,12,4))
                        }
                    }
                }
            };

            await ani.RunAsync(WindowIcon);
        }
    }

    private bool _isDesktop;
}
