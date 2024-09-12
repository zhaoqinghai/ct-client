using CommunityToolkit.Mvvm.ComponentModel;
using CTCommonUI;
using CTService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Fluent;
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
    /// CommonDialog.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class CommonAlarmDialog : Window
    {
        private readonly IContainer _container;

        public CommonAlarmDialog(int detailId, IContainer container, bool autoClose)
        {
            _container = container;
            Task.Factory.StartNew(() =>
            {
                return _container.GetService<IQueryDefectService>()?.GetDefectByDetailId(detailId);
            }).ContinueWith(t =>
            {
                if (t.IsFaulted || t.Result == null)
                {
                    container.GetService<ILogger<CommonAlarmDialog>>()?.LogError("查询缺陷详情失败:{0}, excepiton: {1}", detailId, t.Exception);
                    Application.Current.Dispatcher.Invoke(Close);
                    return;
                }
                else
                {
                    var defectParam = t.Result;
                    var imgHelper = container.GetService<DefectImgHelper>()!;
                    var defectImg = imgHelper.GetThumbnail(defectParam.DetailId, defectParam.CreateTime, defectParam.RollNo, defectParam.ImgSavePath, defectParam.Rect_X, defectParam.Rect_Y, defectParam.Rect_W, defectParam.Rect_H);
                    AppSettings.SpotSettingDict.TryGetValue(defectParam.SpotName, out var spot);
                    var defectDisplay = defectParam.GetDefectTitle();
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        CreateTime = defectParam.CreateTime;
                        RollNo = defectParam.RollNo;
                        DefectTitle = defectDisplay.DefectTypeName;
                        BackColor = defectDisplay.BackColor;
                        ForeColor = defectDisplay.ForeColor;
                        Position = defectParam.Position;
                        SpotName = defectParam.SpotName;
                        DefectWidth = defectParam.Rect_H * (spot?.CameraPxSize ?? 0.02);
                        DefectDepth = defectParam.Rect_W * (spot?.CameraPxSize ?? 0.02);
                        SmallImg = defectImg;
                        RawImg = imgHelper.GetRawImg(defectParam.ImgSavePath, 1000, defectParam.CreateTime, defectParam.RollNo);
                    });
                }
            });
            if (autoClose)
            {
                Task.Delay(TimeSpan.FromSeconds(8)).ContinueWith(_ =>
                {
                    Application.Current.Dispatcher.Invoke(Close);
                });
            }
            InitializeComponent();
        }

        [ObservableProperty]
        private DateTime _createTime;

        [ObservableProperty]
        private string? _rollNo;

        [ObservableProperty]
        private Uri? _smallImg;

        [ObservableProperty]
        private BitmapImage? _rawImg;

        [ObservableProperty]
        private string? _defectTitle;

        [ObservableProperty]
        private Color _backColor;

        [ObservableProperty]
        private Color _foreColor;

        [ObservableProperty]
        private double _defectWidth;

        [ObservableProperty]
        private double _defectDepth;

        [ObservableProperty]
        private string? _position;

        [ObservableProperty]
        private string? _spotName;
    }
}