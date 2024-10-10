using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CommunityToolkit.Mvvm.Messaging;
using ControlzEx.Standard;
using CTCommonUI;
using CTModel;
using CTService;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Threading.Tasks.Dataflow;
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
    /// SpotDefectView.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class SpotDefectView : UserControl, IRecipient<RollInfo>, IRecipient<DefectParam[]>
    {
        private readonly ILogger<SpotDefectView> _logger;

        public SpotDefectView(SpotConfig config)
        {
            var provider = (App.Current as IContainer)!;
            DefectView = new DefectInfoView(config, provider.GetService<IQueryDefectService>()!, provider.GetService<DefectImgHelper>()!, this, provider.GetService<IDateRangeService>()!);
            _logger = provider.GetService<ILogger<SpotDefectView>>()!;
            CurrentRollCount = $"当前卷近{config.CurrentRollDefectCount}个";
            CurrentDayCount = $"当日检出近{config.CurrentDayDefectCount}个";
            SpotTitle = config.SpotName;
            LeftPositionTitle = config.LeftLable;
            RightPositionTitle = config.RightLable;
            WeakReferenceMessenger.Default.Register<RollInfo>(this);
            WeakReferenceMessenger.Default.Register<DefectParam[]>(this);
            new Task(async () =>
            {
                await RefreshDefectMap();
            }, TaskCreationOptions.LongRunning | TaskCreationOptions.RunContinuationsAsynchronously).Start();
            InitializeComponent();
        }

        private async Task RefreshDefectMap()
        {
            using var timer = new PeriodicTimer(TimeSpan.FromMilliseconds(40));
            while (await timer.WaitForNextTickAsync())
            {
                if (_defectMapItems.Count > 0)
                {
                    UpdateDefectMap();
                }
            }
        }

        public DefectInfoView DefectView { get; private set; }

        public string SpotTitle { get; private set; }

        public string LeftPositionTitle { get; private set; }

        public string RightPositionTitle { get; private set; }

        [ObservableProperty]
        private string? _currentRollNo;

        [ObservableProperty]
        private string? _remainLength;

        [ObservableProperty]
        private string? _currentSpeed;

        [ObservableProperty]
        private WriteableBitmap? _defectMap;

        private double _mapHeight;

        private readonly ConcurrentQueue<DefectMapItem> _defectMapItems = new ConcurrentQueue<DefectMapItem>();

        [RelayCommand]
        private void Init()
        {
            _mapHeight = this.Height - 66;
            UpdateDefectMap();
        }

        public void Receive(RollInfo message)
        {
            CurrentRollNo = message.RollNo;
            RemainLength = message.RemainLength;
            CurrentSpeed = message.Speed;
        }

        public string CurrentRollCount { get; }

        public string CurrentDayCount { get; }

        public unsafe void UpdateDefectMap()
        {
            Task.Factory.StartNew(() =>
            {
                if (_mapHeight <= 0)
                {
                    return (0, 0, Array.Empty<byte>(), 0, 0);
                }
                int width = 260;
                int height = (int)_mapHeight;
                var minHeight = 0;
                var maxHeight = 0;
                foreach (var item in _defectMapItems)
                {
                    minHeight = Math.Min(item.Height - 12, minHeight);
                    maxHeight = Math.Max(item.Height + 16, maxHeight);
                    item.Height += 2;
                }

                while (_defectMapItems.TryPeek(out var d))
                {
                    if (d.Height + 90 > height)
                    {
                        _defectMapItems.TryDequeue(out _);
                        continue;
                    }
                    break;
                }
                using (var bitmap = new SKBitmap(width, height))
                using (var canvas = new SKCanvas(bitmap))
                using (var surface = SKSurface.Create(new SKImageInfo(width, height)))
                {
                    using (var paint = new SKPaint())
                    {
                        var items = _defectMapItems.ToList();
                        foreach (var item in items)
                        {
                            if (item.Shape == Shape.Circle)
                            {
                                using (var circlePaint = new SKPaint())
                                {
                                    circlePaint.Color = item.FillColor;
                                    circlePaint.IsAntialias = true;
                                    canvas.DrawCircle(item.Alignment == HorizontalAlignment.Left ? 52 : width - 52, item.Height + 90, 12, circlePaint);
                                }
                            }
                            else if (item.Shape == Shape.Rectangle)
                            {
                                using (var rectPaint = new SKPaint())
                                {
                                    rectPaint.Color = item.FillColor;
                                    rectPaint.IsAntialias = true;
                                    canvas.DrawRoundRect(item.Alignment == HorizontalAlignment.Left ? 52 : width - 52 - 100, item.Height + 90, 100, 6, 3, 3, rectPaint);
                                }
                            }
                        }
                    }
                    var arr = new byte[bitmap.Bytes.Length];
                    Buffer.BlockCopy(bitmap.Bytes, 0, arr, 0, bitmap.Bytes.Length);
                    return (bitmap.Width, bitmap.Height, arr, minHeight, maxHeight);
                }
            }).ContinueWith(t =>
            {
                if (t.IsCanceled || t.IsFaulted)
                {
                    _logger.LogError("{0}:绘制缺陷地图异常:{1}", SpotTitle, t.Exception);
                    return;
                }
                App.Current.Dispatcher.Invoke(() =>
                {
                    if (t.Result.Item1 > 0 && t.Result.Item2 > 0 && t.Result.Item3.Length > 0)
                    {
                        if (DefectMap == null)
                        {
                            DefectMap = new WriteableBitmap(t.Result.Item1, t.Result.Item2, 96, 96, PixelFormats.Bgra32, null);
                            DefectMap.Lock();

                            fixed (byte* ptr = t.Result.Item3)
                            {
                                var p = new IntPtr(ptr);
                                CopyMemory(DefectMap.BackBuffer, new IntPtr(ptr), (uint)t.Result.Item3.Length);
                            }

                            DefectMap.AddDirtyRect(new Int32Rect(0, 0, t.Result.Item1, t.Result.Item2));
                            DefectMap.Unlock();
                        }
                        else
                        {
                            DefectMap.Lock();

                            fixed (byte* ptr = t.Result.Item3)
                            {
                                CopyMemory(DefectMap.BackBuffer + (90 + t.Result.Item4) * 260 * 4, new IntPtr(ptr) + (90 + t.Result.Item4) * 260 * 4, (uint)(t.Result.Item5 + 90 > t.Result.Item2 ? t.Result.Item2 - 90 - t.Result.Item4 : t.Result.Item5 - t.Result.Item4) * 260 * 4);
                            }

                            DefectMap.AddDirtyRect(new Int32Rect(0, 0, t.Result.Item1, t.Result.Item2));
                            DefectMap.Unlock();
                        }
                    }
                });
            });
        }

        [DllImport("kernel32.dll", EntryPoint = "RtlMoveMemory")]
        private static extern void CopyMemory(IntPtr destination, IntPtr source, uint length);

        public void Receive(DefectParam[] message)
        {
            foreach (var defect in message.Where(x => x.SpotName == SpotTitle))
            {
                var mapItem = new DefectMapItem();
                var defectDisplay = defect.GetDefectTitle();
                if (defect.Position == LeftPositionTitle)
                {
                    mapItem.Alignment = HorizontalAlignment.Left;
                }
                else if (defect.Position == RightPositionTitle)
                {
                    mapItem.Alignment = HorizontalAlignment.Right;
                }
                else
                {
                    mapItem.Alignment = HorizontalAlignment.Stretch;
                    continue;
                }
                if (defectDisplay.DefectTypeName == "折印")
                {
                    mapItem.Shape = Shape.Rectangle;
                }
                else if (AppSettings.DrawDefectTypeSet.Contains(defectDisplay.DefectTypeName))
                {
                    mapItem.Shape = Shape.Circle;
                }
                else
                {
                    continue;
                }
                mapItem.FillColor = new SKColor(defectDisplay.ReportColor.R, defectDisplay.ReportColor.G, defectDisplay.ReportColor.B, defectDisplay.ReportColor.A);

                _defectMapItems.Enqueue(mapItem);
            }
        }
    }

    public class DefectMapItem
    {
        public int Height { get; set; }

        public SKColor FillColor { get; set; }

        public HorizontalAlignment Alignment { get; set; }

        public Shape Shape { get; set; }
    }

    public enum Shape
    {
        None,
        Circle,
        Rectangle
    }
}