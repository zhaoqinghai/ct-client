using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CTCommonUI;
using CTModel;
using CTService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.TickGenerators.TimeUnits;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Effects;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CTClient
{
    /// <summary>
    /// DashboardView.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class DashboardView : UserControl, IRecipient<DefectParam[]>, IRecipient<RefreshReportEvent>
    {
        private List<SpotConfig> _spotConfigs;
        private IQueryDefectService _queryDefectService;
        private ILogger<DashboardView> _logger;

        public DashboardView()
        {
            _spotConfigs = AppSettings.SpotSettingDict.Select(x => x.Value).ToList();
            _queryDefectService = (App.Current as IContainer)!.GetService<IQueryDefectService>()!;
            _logger = (App.Current as IContainer)!.GetService<ILogger<DashboardView>>()!;
            var defectDefineSettings = AppSettings.DefectDefineDict.ToDictionary(x => x.Value.DefectTypeName, x => x.Value);
            (DefectReportItem? lowCrack, DefectReportItem? mediumCrack, DefectReportItem? highCrack, DefectReportItem? crease) = (null, null, null, null);
            if (defectDefineSettings.TryGetValue("轻微边裂", out var low))
            {
                lowCrack = new DefectReportItem
                {
                    DefectName = "轻微边裂",
                    ForeColor = low.ForeColor,
                    BackColor = low.BackColor,
                    DefectDesc = low.DefectDesc
                };
            }
            if (defectDefineSettings.TryGetValue("中等边裂", out var medium))
            {
                mediumCrack = new DefectReportItem
                {
                    DefectName = "中等边裂",
                    ForeColor = medium.ForeColor,
                    BackColor = medium.BackColor,
                    DefectDesc = medium.DefectDesc
                };
            }
            if (defectDefineSettings.TryGetValue("严重边裂", out var high))
            {
                highCrack = new DefectReportItem
                {
                    DefectName = "严重边裂",
                    ForeColor = high.ForeColor,
                    BackColor = high.BackColor,
                    DefectDesc = high.DefectDesc
                };
            }
            if (defectDefineSettings.TryGetValue("折印", out var creaseSetting))
            {
                crease = new DefectReportItem
                {
                    DefectName = "折印",
                    ForeColor = creaseSetting.ForeColor,
                    BackColor = creaseSetting.BackColor,
                    DefectDesc = string.Empty
                };
            }
            Reports = _spotConfigs.Select(x => new SpotReport()
            {
                SpotTitle = x.SpotName,
                CurrentDay = new DefectReportVM()
                {
                    Title = "当日",
                    Crease = crease?.Clone(),
                    LowCrack = lowCrack?.Clone(),
                    MediumCrack = mediumCrack?.Clone(),
                    HighCrack = highCrack?.Clone()
                },
                CurrentShift = new DefectReportVM
                {
                    Title = "当班",
                    Crease = crease?.Clone(),
                    LowCrack = lowCrack?.Clone(),
                    MediumCrack = mediumCrack?.Clone(),
                    HighCrack = highCrack?.Clone()
                },
                CurrentMonth = new DefectReportVM
                {
                    Title = "当月",
                    Crease = crease?.Clone(),
                    LowCrack = lowCrack?.Clone(),
                    MediumCrack = mediumCrack?.Clone(),
                    HighCrack = highCrack?.Clone()
                },
            }).ToDictionary(x => x.SpotTitle);
            RedrawReportTable();
            WeakReferenceMessenger.Default.Register<DefectParam[]>(this);
            WeakReferenceMessenger.Default.Register<RefreshReportEvent>(this);
            InitializeComponent();
            Loaded += DashboardView_Loaded;
        }

        private void DashboardView_Loaded(object sender, RoutedEventArgs e)
        {
            _defectGallery.Children.Clear();
            foreach (var spotConfig in _spotConfigs)
            {
                _defectGallery.Children.Add(new SpotDefectView(spotConfig)
                {
                    Height = _defectGallery.ActualHeight / _spotConfigs.Count
                });
            }
        }

        public Dictionary<string, SpotReport> Reports { get; }

        private void RedrawReportTable()
        {
            DrawMonthLineChart();
            Task.Factory.StartNew(() =>
            {
                return _queryDefectService.GetDefectCountReport();
            }).ContinueWith(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                {
                    _logger.LogError("查询缺陷统计表格失败:{0}", t.Exception);
                    return;
                }

                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var shift in t.Result.CurrentShift)
                    {
                        foreach (var defect in shift.Value)
                        {
                            UpdateReport(shift.Key, CycleType.Shift, defect.Key, defect.Value, CalcOperator.Assign);
                        }
                    }

                    foreach (var day in t.Result.CurrentDay)
                    {
                        foreach (var defect in day.Value)
                        {
                            UpdateReport(day.Key, CycleType.Day, defect.Key, defect.Value, CalcOperator.Assign);
                        }
                    }

                    foreach (var report in Reports.Values)
                    {
                        DrawDayRaidoChart(report);
                    }

                    if (t.Result.CurrentDay.Count == 0)
                    {
                    }

                    foreach (var month in t.Result.CurrentMonth)
                    {
                        foreach (var defect in month.Value)
                        {
                            UpdateReport(month.Key, CycleType.Month, defect.Key, defect.Value, CalcOperator.Assign);
                        }
                    }
                });
            });
        }

        private void DrawMonthLineChart()
        {
            Task.Factory.StartNew(() =>
            {
                var data = _queryDefectService.GetDefectDayInMonth();
                var today = DateTime.Today;
                var daysInMonth = DateTime.DaysInMonth(today.Year, today.Month);
                var dx = Enumerable.Range(1, daysInMonth + 1).ToArray();
                var ret = new Dictionary<string, byte[]>();
                var spotDict = data.GroupBy(x => x.SpotName).ToDictionary(x => x.Key);

                foreach (var spotName in AppSettings.SpotSettingDict.Keys)
                {
                    var plot = new Plot();
                    var defectTypeDict = new Dictionary<string, int[]>();
                    var maxValue = 10;
                    foreach (var defectName in AppSettings.DrawDefectTypeSet)
                    {
                        var dy = new int[daysInMonth];
                        if (spotDict.TryGetValue(spotName, out var spot))
                        {
                            var dict = spot.Where(x => x.DefectName == defectName).ToDictionary(x => x.Date.Day);
                            for (var i = 0; i < daysInMonth; i++)
                            {
                                if (dict.TryGetValue(i, out var d))
                                {
                                    dy[i] = d.Count;
                                    maxValue = Math.Max(d.Count, maxValue);
                                }
                            }
                        }

                        defectTypeDict[defectName] = dy;
                    }

                    foreach (var defectTypeItem in defectTypeDict)
                    {
                        var scatter = plot.Add.Scatter(dx, defectTypeItem.Value);
                        scatter.LegendText = defectTypeItem.Key;
                        foreach (var item in scatter.LegendItems)
                        {
                            item.LineWidth = 1;
                            item.MarkerShape = MarkerShape.FilledCircle;
                            item.MarkerSize = 6;
                        }
                        if (AppSettings.DefectDefineDict.TryGetValue(defectTypeItem.Key, out var define))
                        {
                            scatter.Color = ScottPlot.Color.FromARGB(define.ReportColor.A << 24 | define.ReportColor.R << 16 | define.ReportColor.G << 8 | define.ReportColor.B);
                        }
                    }

                    plot.Grid.MajorLineWidth = 1;
                    plot.Grid.MajorLineColor = ScottPlot.Color.FromHex("#B3B9C4");
                    plot.Grid.IsBeneathPlottables = false;
                    plot.Grid.XAxisStyle.MajorLineStyle.Pattern = LinePattern.DenselyDashed;
                    plot.Grid.YAxisStyle.MajorLineStyle.Pattern = LinePattern.DenselyDashed;

                    var yTickArr = Enumerable.Range(0, 6).Select(x => (maxValue / 50 + 1) * 10 * x).Select(x => (double)x).ToArray();
                    plot.Axes.Left.SetTicks(yTickArr, yTickArr.Select(x => x.ToString()).ToArray());
                    var xTickArr = Enumerable.Range(1, daysInMonth + 1).Select(x => (double)x).ToArray();
                    plot.Axes.Bottom.SetTicks(xTickArr, xTickArr.Select((x, i) => i % 2 == 1 ? x.ToString() : $"\n{x}").ToArray());
                    plot.Axes.SetLimits(1, xTickArr.Last(), 0, yTickArr.Last());
                    var pannel = plot.ShowLegend(Edge.Top);
                    pannel.Alignment = Alignment.LowerCenter;
                    pannel.Legend.ShadowFillStyle.IsVisible = false;
                    pannel.Legend.OutlineWidth = 0;
                    pannel.Legend.ShadowOffset = PixelOffset.NaN;
                    pannel.Legend.FontName = "Microsoft YaHei UI";
                    pannel.Legend.FontSize = 14;
                    pannel.Legend.FontColor = ScottPlot.Color.FromHex("#44546F");
                    plot.Axes.Color(ScottPlot.Color.FromHex("#44546F"));
                    plot.Layout.Fixed(new PixelPadding(30, 6, 40, 48));
                    plot.Font.Set(SKFontManager.Default.MatchCharacter('汉').FamilyName);
                    var imgBytes = plot.GetImage(430, 300).GetImageBytes(ImageFormat.Bmp);

                    ret[spotName] = imgBytes;
                }
                return ret;
            }).ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    _logger.LogError("绘制月度缺陷统计折现图异常:{0}", t.Exception);
                    return;
                }
                App.Current.Dispatcher.Invoke(() =>
                {
                    foreach (var report in Reports)
                    {
                        if (t.Result.TryGetValue(report.Key, out var d))
                        {
                            using (MemoryStream stream = new MemoryStream(d))
                            {
                                BitmapImage bitmapImage = new BitmapImage();
                                bitmapImage.BeginInit();
                                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                                bitmapImage.StreamSource = stream;
                                bitmapImage.EndInit();
                                bitmapImage.Freeze();
                                report.Value.MonthLineChart = bitmapImage;
                            }
                        }
                    }
                });
            });
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr destination, IntPtr source, uint length);

        private unsafe void DrawDayRaidoChart(SpotReport vm)
        {
            Task.Factory.StartNew(() =>
            {
                var ret = Array.Empty<byte>;

                var plot = new Plot();
                List<PieSlice> slices = new();
                List<(string, ScottPlot.Color)> LegendItems = new();
                var totalCount = 0d;

                foreach (var defectName in AppSettings.DefectDefineDict)
                {
                    if (!AppSettings.DrawDefectTypeSet.Contains(defectName.Key))
                    {
                        continue;
                    }

                    var color = ScottPlot.Color.FromARGB(defectName.Value.ReportColor.A << 24 | defectName.Value.ReportColor.R << 16 | defectName.Value.ReportColor.G << 8 | defectName.Value.ReportColor.B);
                    LegendItems.Add((defectName.Key, color));
                    var count = GetDefectMap(vm.CurrentDay, defectName.Key)?.Count ?? 0;
                    totalCount += count;
                    var value = Math.Max(.00000001, count);
                    slices.Add(new PieSlice() { Value = value, FillColor = color, LabelFontColor = color });
                }

                for (var i = 0; i < slices.Count; i++)
                {
                    if (slices[i].Value >= 1 && totalCount > 0)
                    {
                        slices[i].Label = $"{slices[i].Value / totalCount:P1}";
                    }
                    else
                    {
                        slices[i].Label = string.Empty;
                    }
                }

                plot.Layout.Fixed(new PixelPadding(0, 150, 0, 0));
                plot.HideGrid();
                plot.Axes.Frameless();
                var pie = plot.Add.Pie(slices);
                pie.ShowSliceLabels = true;
                pie.DonutFraction = .4;
                pie.LineWidth = 0;
                pie.LineColor = ScottPlot.Colors.Transparent;
                var pannel = plot.ShowLegend(Edge.Right);
                pannel.Alignment = Alignment.MiddleCenter;
                pannel.Legend.ShadowColor = ScottPlot.Colors.Transparent;
                pannel.Legend.DisplayPlottableLegendItems = false;
                pannel.Legend.ManualItems.AddRange(LegendItems.Select(x =>
                new LegendItem()
                {
                    LabelText = x.Item1,
                    LabelFontSize = 14,
                    LineWidth = 8,
                    LineColor = x.Item2,
                }));
                plot.Axes.Margins(0.01, 0.01);
                plot.Font.Set(SKFontManager.Default.MatchCharacter('汉').FamilyName);

                var imgBytes = plot.GetImageBytes(430, 300);
                return (430, 300, imgBytes);
            }).ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    _logger.LogError("绘制当日缺陷比例环状图异常:{0}", t.Exception);
                    return;
                }
                using SKBitmap bitmap = SKBitmap.Decode(t.Result.imgBytes);
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (Reports.TryGetValue(vm.SpotTitle, out var report))
                    {
                        if (report.DayPieChart == null)
                        {
                            report.DayPieChart = new WriteableBitmap(t.Result.Item1, t.Result.Item2, 96, 96, PixelFormats.Bgr32, null);
                            report.DayPieChart.Lock();

                            fixed (byte* ptr = bitmap.Bytes)
                            {
                                var p = new IntPtr(ptr);
                                CopyMemory(report.DayPieChart.BackBuffer, new IntPtr(ptr), (uint)bitmap.Bytes.Length);
                            }

                            report.DayPieChart.AddDirtyRect(new Int32Rect(0, 0, t.Result.Item1, t.Result.Item2));
                            report.DayPieChart.Unlock();
                        }
                        else
                        {
                            report.DayPieChart.Lock();

                            fixed (byte* ptr = bitmap.Bytes)
                            {
                                var p = new IntPtr(ptr);
                                CopyMemory(report.DayPieChart.BackBuffer, new IntPtr(ptr), (uint)bitmap.Bytes.Length);
                            }

                            report.DayPieChart.AddDirtyRect(new Int32Rect(0, 0, t.Result.Item1, t.Result.Item2));
                            report.DayPieChart.Unlock();
                        }
                    }
                });
            });
        }

        private enum CycleType
        {
            Shift,
            Day,
            Month
        }

        private enum CalcOperator
        {
            Increment,
            Assign
        }

        private void UpdateReport(string spotName, CycleType cycle, string defectName, int value, CalcOperator op)
        {
            if (Reports.TryGetValue(spotName, out var report))
            {
                switch (cycle)
                {
                    case CycleType.Shift:
                        var shift = GetDefectMap(report.CurrentShift, defectName);
                        if (shift != null)
                        {
                            shift.Count = op == CalcOperator.Increment ? shift.Count + value : value;
                        }
                        return;

                    case CycleType.Day:
                        var day = GetDefectMap(report.CurrentDay, defectName);
                        if (day != null)
                        {
                            day.Count = op == CalcOperator.Increment ? day.Count + value : value;
                        }
                        return;

                    case CycleType.Month:
                        var month = GetDefectMap(report.CurrentMonth, defectName);
                        if (month != null)
                        {
                            month.Count = op == CalcOperator.Increment ? month.Count + value : value;
                        }
                        break;
                }
            }
        }

        private DefectReportItem? GetDefectMap(DefectReportVM drv, string defectName)
        {
            switch (defectName)
            {
                case "轻微边裂":
                    return drv.LowCrack;

                case "中等边裂":
                    return drv.MediumCrack;

                case "严重边裂":
                    return drv.HighCrack;

                case "折印":
                    return drv.Crease;
            }
            return null;
        }

        public void Receive(DefectParam[] message)
        {
            DrawMonthLineChart();
            Application.Current.Dispatcher.Invoke(() =>
            {
                foreach (var defect in message)
                {
                    var defectDisplay = defect.GetDefectTitle();
                    UpdateReport(defect.SpotName, CycleType.Shift, defectDisplay.DefectTypeName, 1, CalcOperator.Increment);
                    UpdateReport(defect.SpotName, CycleType.Day, defectDisplay.DefectTypeName, 1, CalcOperator.Increment);
                    UpdateReport(defect.SpotName, CycleType.Month, defectDisplay.DefectTypeName, 1, CalcOperator.Increment);

                    if (Reports.TryGetValue(defect.SpotName, out var report))
                    {
                        DrawDayRaidoChart(report);
                    }
                }
            });
        }

        public void Receive(RefreshReportEvent message)
        {
            RedrawReportTable();
        }

        public partial class SpotReport : ObservableObject
        {
            public required string SpotTitle { get; init; }

            public required DefectReportVM CurrentShift { get; set; }

            public required DefectReportVM CurrentDay { get; set; }

            public required DefectReportVM CurrentMonth { get; set; }

            [ObservableProperty]
            public ImageSource? _monthLineChart;

            [ObservableProperty]
            public WriteableBitmap? _dayPieChart;
        }

        public class DefectReportTable : ObservableObject
        {
        }
    }
}