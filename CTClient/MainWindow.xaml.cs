using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using CommunityToolkit.Mvvm.Messaging.Messages;
using Microsoft.Extensions.Options;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Linq;
using CTCommonUI;
using System.ComponentModel;
using MahApps.Metro.Controls;

namespace CTClient
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    [CommunityToolkit.Mvvm.ComponentModel.ObservableObject]
    public partial class MainWindow : MetroWindow, IRecipient<DisplayDefectDialogEvent>
    {
        private readonly LocalSettings _settings;
        private readonly DispatcherTimer _timer = new DispatcherTimer() { Interval = TimeSpan.FromSeconds(1) };
        private readonly CTCommonUI.IContainer _container;

        public MainWindow(IOptions<LocalSettings> settings)
        {
            _settings = settings.Value;
            _container = (App.Current as CTCommonUI.IContainer)!;
            WeakReferenceMessenger.Default.Register(this);
            InitializeComponent();
        }

        [RelayCommand]
        private void ClearTextboxFocus(MouseButtonEventArgs e)
        {
            if (e.OriginalSource is not TextBox)
            {
                Keyboard.ClearFocus();
            }
        }

        [RelayCommand]
        public void Init()
        {
            Title = _settings.WinTitle;
            UpdateView(null, EventArgs.Empty);
            _timer.Stop();
            _timer.Tick -= UpdateView;
            _timer.Tick += UpdateView;
            _timer.Start();
        }

        [RelayCommand]
        public void CloseApp()
        {
            this.Close();
            Environment.Exit(0);
        }

        private void UpdateView(object? sender, EventArgs e)
        {
            CurrentDate = DateTime.Now.ToString("yyyy-MM-dd dddd HH:mm:ss");
        }

        public void Receive(DisplayDefectDialogEvent message)
        {
            Application.Current.Dispatcher.Invoke(() =>
            {
                if (Application.Current.Windows.OfType<CommonAlarmDialog>().Any())
                {
                    return;
                }
                try
                {
                    MaskVisible = Visibility.Visible;
                    BlurRadius = 8;
                    var dialog = new CommonAlarmDialog(message.Value.Item1, _container, message.Value.Item2) { Owner = this };
                    dialog.ShowDialog();
                }
                finally
                {
                    BlurRadius = 0;
                    MaskVisible = Visibility.Collapsed;
                }
            });
        }

        [ObservableProperty]
        private string? _currentDate;

        [ObservableProperty]
        private Visibility _maskVisible = Visibility.Collapsed;

        [ObservableProperty]
        private double _blurRadius = 0;

        [ObservableProperty]
        private PageType _pageType = PageType.Home;

        [ObservableProperty]
        private string? _subTitle;

        [RelayCommand]
        public void ChangePageType(PageType type)
        {
            PageType = type;
            SubTitle = typeof(PageType).GetField(type.ToString())?.GetCustomAttribute<DescriptionAttribute>()?.Description;
        }

        [RelayCommand]
        public void UpdateSample()
        {
            try
            {
                MaskVisible = Visibility.Visible;
                BlurRadius = 8;
                var dialog = new UpdateSampleWindow() { Owner = this };
                dialog.ShowDialog();
            }
            finally
            {
                BlurRadius = 0;
                MaskVisible = Visibility.Collapsed;
            }
        }

        private void DisplayDefectDetail_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        private void DisplayDefectDetail_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is int detailId)
            {
                WeakReferenceMessenger.Default.Send(new DisplayDefectDialogEvent(detailId));
            }
        }

        private void ShowMsg_CanExecute(object sender, CanExecuteRoutedEventArgs e)
        {
            e.CanExecute = true;
        }

        [ObservableProperty]
        private string? _tip;

        [ObservableProperty]
        private Color? _msgBkColor;

        private void ShowMsg_Executed(object sender, ExecutedRoutedEventArgs e)
        {
            if (e.Parameter is ShowMsgInfo msg)
            {
                Tip = msg.Message;
                if (msg.Error)
                {
                    MsgBkColor = (Color)ColorConverter.ConvertFromString("#ff7575");
                }
                else
                {
                    MsgBkColor = Colors.Green;
                }
                _msgFlyout.IsOpen = true;
            }
        }
    }

    public class DisplayDefectDialogEvent : ValueChangedMessage<(int, bool)>
    {
        public DisplayDefectDialogEvent(int value, bool autoClose = false) : base((value, autoClose))
        {
        }
    }

    [Flags]
    public enum PageType
    {
        [Description("")]
        Home = 1,

        [Description("/阈值设置")]
        Setting = 1 << 1,

        [Description("/检测记录")]
        History = 1 << 2,

        NoHome = Setting | History,
    }
}