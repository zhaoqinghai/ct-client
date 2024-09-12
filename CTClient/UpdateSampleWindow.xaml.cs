using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CTCommonUI;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace CTClient
{
    /// <summary>
    /// UpdateSampleWindow.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class UpdateSampleWindow : Window
    {
        readonly ILogger<UpdateSampleWindow> _logger;

        public UpdateSampleWindow()
        {
            _logger = (App.Current as IContainer)!.GetService<ILogger<UpdateSampleWindow>>()!;
            Items = AppSettings.DefectDefineDict.Where(x => x.Value.DefectDefineType.HasFlag(DefectDefineType.Search)).ToList();
            DefectType = Items.First().Value;
            DefectTime = DateTime.Now;
            Positions = new List<string>() { "工作侧", "传动侧" };
            Position = Positions.First();
            InitializeComponent();
        }

        [RelayCommand]
        private void Commit()
        {
            _logger.LogInformation("卷号:{0} 类型:{1} 宽度:{2} 深度:{3} 时间:{4} 方位:{5}", RollNo, DefectType?.DefectTypeName, DefectWidth, DefectDepth, DefectTime, Position);
            Close();
        }

        [ObservableProperty]
        private string? _rollNo;

        [ObservableProperty]
        private double _defectWidth;

        [ObservableProperty]
        private double _defectDepth;

        [ObservableProperty]
        private DateTime _defectTime;

        [ObservableProperty]
        private string? _position;

        [ObservableProperty]
        private DefectDefineV? _defectType;

        public List<KeyValuePair<string, DefectDefineV>> Items { get; private init; } = null!;

        public List<string> Positions { get; private init; } = null!;
    }
}