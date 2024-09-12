using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Messaging;
using CTCommonUI;
using CTModel;
using CTService;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SkiaSharp;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Path = System.IO.Path;

namespace CTClient
{
    /// <summary>
    /// DefectInfoView.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class DefectInfoView : UserControl, IRecipient<DefectParam[]>
    {
        private readonly SpotConfig _spotConfig;
        private readonly IQueryDefectService _queryDefectService;
        private readonly DefectImgHelper _defectHelper;
        private readonly SpotDefectView _spotDefectView;
        private readonly ILogger<DefectInfoView> _logger;
        private readonly IDateRangeService _dateRangeService;

        public DefectInfoView(SpotConfig setting, IQueryDefectService queryDefectService, DefectImgHelper defectHelper, SpotDefectView parent, IDateRangeService dateRangeService)
        {
            _spotDefectView = parent;
            _spotConfig = setting;
            _dateRangeService = dateRangeService;
            _queryDefectService = queryDefectService;
            _defectHelper = defectHelper;
            _logger = (App.Current as IContainer)!.GetService<ILogger<DefectInfoView>>()!;
            _newestDefectQueue = new FixedSizeQueue<DefectInfo>(_spotConfig.NewestDefectColCount * _spotConfig.NewestDefectRowCount);
            GalleryDisplayMode = DefectGalleryDisplayMode.Newest;
            NewestDefectColCount = _spotConfig.NewestDefectColCount;
            NewestDefectRowCount = _spotConfig.NewestDefectRowCount;
            WeakReferenceMessenger.Default.Register(this);
            InitializeComponent();
        }

        [ObservableProperty]
        private FixedSizeQueue<DefectInfo> _newestDefectQueue;

        public ObservableCollection<DefectInfo> CurrentDefectList { get; } = new ObservableCollection<DefectInfo>();

        [ObservableProperty]
        private DefectGalleryDisplayMode _galleryDisplayMode = DefectGalleryDisplayMode.None;

        partial void OnGalleryDisplayModeChanged(DefectGalleryDisplayMode mode)
        {
            CurrentDefectList.Clear();
            if (mode == DefectGalleryDisplayMode.Newest)
            {
                Task.Factory.StartNew(() =>
                {
                    var datas = _queryDefectService.GetRecentDefectList(_spotConfig.SpotName, _spotConfig.NewestDefectColCount * _spotConfig.NewestDefectRowCount);
                    return Transfer(datas);
                }).ContinueWith(datas =>
                {
                    if (datas.IsFaulted || datas.IsCanceled)
                    {
                        _logger.LogError("查询最近缺陷异常:{0}", datas.Exception);
                        return;
                    }
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        if (NewestDefectQueue.Count > 0)
                        {
                            var detailId = NewestDefectQueue.MaxBy(x => x.DetailId)?.DetailId ?? 0;
                            NewestDefectQueue.Enqueue(datas.Result.Where(x => x.DetailId > detailId).ToArray());
                        }
                        else
                        {
                            NewestDefectQueue.Enqueue(datas.Result.ToArray());
                        }
                    });
                });
            }
            else if (mode == DefectGalleryDisplayMode.CurrentRoll)
            {
                Task.Factory.StartNew(() =>
                {
                    var datas = _queryDefectService.GetCurrentRollDefectList(_spotConfig.SpotName, _spotDefectView.CurrentRollNo ?? string.Empty, _spotConfig.CurrentRollDefectCount);
                    return TransferCurrent(datas);
                }).ContinueWith(datas =>
                {
                    if (datas.IsFaulted || datas.IsCanceled)
                    {
                        _logger.LogError("查询最近缺陷异常:{0}", datas.Exception);
                        return;
                    }
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var item in datas.Result)
                        {
                            CurrentDefectList.Add(item);
                        }
                    });
                });
            }
            else if (mode == DefectGalleryDisplayMode.CurrentDay)
            {
                Task.Factory.StartNew(() =>
                {
                    var datas = _queryDefectService.GetCurrentDayDefectList(_spotConfig.SpotName, _spotConfig.CurrentDayDefectCount);
                    return TransferCurrent(datas);
                }).ContinueWith(datas =>
                {
                    if (datas.IsFaulted || datas.IsCanceled)
                    {
                        _logger.LogError("查询最近缺陷异常:{0}", datas.Exception);
                        return;
                    }

                    App.Current.Dispatcher.Invoke(() =>
                    {
                        foreach (var item in datas.Result)
                        {
                            CurrentDefectList.Add(item);
                        }
                    });
                });
            }
        }

        private List<DefectInfo> Transfer(IEnumerable<DefectParam> datas)
        {
            return datas.AsParallel().Select(x =>
            {
                try
                {
                    var defectDisplay = x.GetDefectTitle();
                    var info = new DefectInfo()
                    {
                        DetailId = x.DetailId,
                        DefectWidth = x.Rect_H * _spotConfig.CameraPxSize,
                        DefectDepth = x.Rect_W * _spotConfig.CameraPxSize,
                        DefectTitle = defectDisplay.DefectTypeName,
                        BackColor = defectDisplay.BackColor,
                        ForeColor = defectDisplay.ForeColor,
                        Position = x.Position,
                        RollNo = x.RollNo,
                        CreateTime = x.CreateTime
                    };
                    var uri = _defectHelper.GetThumbnail(x.DetailId, x.CreateTime, x.RollNo, x.ImgSavePath, x.Rect_X, x.Rect_Y, x.Rect_W, x.Rect_H);
                    var bitmapImage = new BitmapImage(uri);
                    bitmapImage.Freeze();
                    info.Source = bitmapImage;
                    return info;
                }
                catch (Exception ex)
                {
                    _logger.LogError("转换当前图片异常:{0}", ex);
                }
                return null;
            }).Where(x => x != null).Select(x => x!).OrderBy(x => x.CreateTime).ToList();
        }

        private List<DefectInfo> TransferCurrent(IEnumerable<DefectParam> datas)
        {
            return datas.AsParallel().Select(x =>
            {
                try
                {
                    var defectDisplay = x.GetDefectTitle();
                    var info = new DefectInfo()
                    {
                        DetailId = x.DetailId,
                        DefectWidth = x.Rect_H * _spotConfig.CameraPxSize,
                        DefectDepth = x.Rect_W * _spotConfig.CameraPxSize,
                        DefectTitle = defectDisplay.DefectTypeName,
                        BackColor = defectDisplay.BackColor,
                        ForeColor = defectDisplay.ForeColor,
                        Position = x.Position,
                        RollNo = x.RollNo,
                        CreateTime = x.CreateTime
                    };
                    var uri = _defectHelper.GetThumbnail(x.DetailId, x.CreateTime, x.RollNo, x.ImgSavePath, x.Rect_X, x.Rect_Y, x.Rect_W, x.Rect_H);
                    var bitmapImage = new BitmapImage(uri);
                    bitmapImage.Freeze();
                    info.Source = bitmapImage;
                    return info;
                }
                catch (Exception ex)
                {
                    _logger.LogError("转换当前图片异常:{0}", ex);
                }
                return null;
            }).Where(x => x != null).Select(x => x!).OrderByDescending(x => x.CreateTime).ToList();
        }

        public int CurrentDefectColCount { get; private set; }

        public int NewestDefectColCount { get; private set; }

        public int NewestDefectRowCount { get; private set; }

        public void Receive(DefectParam[] message)
        {
            Task.Factory.StartNew(() =>
            {
                return Transfer(message.Where(x => x.SpotName == _spotConfig.SpotName).OrderBy(x => x.CreateTime).ThenBy(x => x.DetailId));
            }).ContinueWith(datas =>
            {
                App.Current.Dispatcher.Invoke(() =>
                {
                    var detailId = NewestDefectQueue.MaxBy(x => x.DetailId)?.DetailId ?? 0;
                    NewestDefectQueue.Enqueue(datas.Result.Where(x => x.DetailId > detailId).ToArray());

                    if (DefectGalleryDisplayMode.Current.HasFlag(GalleryDisplayMode))
                    {
                        var maxCount = GalleryDisplayMode == DefectGalleryDisplayMode.CurrentRoll ? _spotConfig.CurrentRollDefectCount : _spotConfig.CurrentDayDefectCount;
                        foreach (var item in datas.Result.OrderBy(x => x.DetailId))
                        {
                            if (CurrentDefectList.Count >= maxCount)
                            {
                                CurrentDefectList.RemoveAt(maxCount - 1);
                            }
                            CurrentDefectList.Insert(0, item);
                        }
                    }
                });
            });
        }
    }

    public enum DefectGalleryDisplayMode
    {
        None = 0,
        Newest = 1,
        CurrentRoll = 1 << 1,
        CurrentDay = 1 << 2,
        Current = CurrentDay | CurrentRoll,
    }

    public class FixedSizeQueue<T> : IEnumerable<T>, INotifyCollectionChanged where T : IEquatable<T>
    {
        private readonly Queue<T> _queue;
        private readonly int _maxSize;

        public FixedSizeQueue(int size) : this(size, Enumerable.Empty<T>())
        {
        }

        public FixedSizeQueue(int size, IEnumerable<T> data)
        {
            _maxSize = size;
            _queue = new Queue<T>(data.Take(size));
        }

        public event NotifyCollectionChangedEventHandler? CollectionChanged;

        public void Enqueue(T item)
        {
            if (_queue.Count >= _maxSize)
            {
                var removedItem = _queue.Dequeue();
            }

            _queue.Enqueue(item);
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Replace, item));
        }

        public void Enqueue(T[] items)
        {
            if (items.Length <= 0)
            {
                return;
            }

            if (_queue.Count > _maxSize - items.Length)
            {
                var oldItems = new List<T>();
                for (; _queue.Count > _maxSize - items.Length;)
                {
                    oldItems.Add(_queue.Dequeue());
                }
                oldItems.Reverse();
                OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, oldItems, _queue.Count));
            }
            var newItems = new List<T>();
            for (var i = 0; i < items.Length; i++)
            {
                _queue.Enqueue(items[i]);

                newItems.Add(items[i]);
            }
            newItems.Reverse();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, newItems, 0));
        }

        public int Count => _queue.Count;

        public void Clear()
        {
            _queue.Clear();
            OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }

        public bool Contains(T item)
        {
            return _queue.Any(x => x.Equals(item));
        }

        public IEnumerator<T> GetEnumerator()
        {
            return _queue.Reverse().GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        protected virtual void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
        {
            CollectionChanged?.Invoke(this, e);
        }
    }

    public class DefectInfo : IEquatable<DefectInfo>
    {
        public static DefectInfo Default => new DefectInfo()
        {
            DefectTitle = string.Empty,
            Position = string.Empty,
            RollNo = string.Empty
        };

        public DefectInfo WithDetailId(int id)
        {
            DetailId = id;
            return this;
        }

        public int DetailId { get; set; }

        public required string Position { get; set; }

        public ImageSource? Source { get; set; }

        public required string DefectTitle { get; set; }

        public DateTime CreateTime { get; set; }

        public required string RollNo { get; set; }

        public Color BackColor { get; set; }

        public Color ForeColor { get; set; }

        public double DefectWidth { get; set; }

        public double DefectDepth { get; set; }

        public bool Equals(DefectInfo? other)
        {
            return DetailId == other?.DetailId;
        }
    }

    public class DefectImgHelper(IOptions<LocalSettings> _settings)
    {
        public const int THUMBNAIL_MAX_WIDTH = 480;
        private static object _locker = new object();

        public Uri GetThumbnail(int defectId, DateTime createTime, string rollNo, string rawImgPath, int rectX, int rectY, int rectW, int rectH)
        {
            var dir = $"{_settings.Value.DefectSettings.SmallImageSaveFolder}/{createTime:yyyy-MM}/{rollNo}";
            var rawDir = $"{_settings.Value.DefectSettings.SmallImageSaveFolder}/{createTime:yyyy-MM}/{rollNo}/raw";
            var imgPath = Path.Combine(dir, $"{defectId}.jpg");
            var filePath = string.Empty;
            lock (_locker)
            {
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                if (!Directory.Exists(rawDir))
                {
                    Directory.CreateDirectory(rawDir);
                }
                if (Path.Exists(imgPath))
                {
                    return new Uri(imgPath);
                }
                filePath = SharePathToLocalPath(rawImgPath, rawDir);
            }
            using var rawBitmap = SKBitmap.Decode(filePath);
            var cropRect = SKRectI.Intersect(new SKRectI(rectX - 15, rectY - 40, rectX + rectW + 170, rectY + rectH + 100), new SKRectI(0, 0, rawBitmap.Width, rawBitmap.Height));
            using var croppedBitmap = new SKBitmap(cropRect.Width, cropRect.Height);
            using var canvas = new SKCanvas(croppedBitmap);
            canvas.DrawBitmap(rawBitmap, cropRect, new SKRect(0, 0, cropRect.Width, cropRect.Height));

            var resizedBitmap = croppedBitmap.Resize(new SKImageInfo(THUMBNAIL_MAX_WIDTH, croppedBitmap.Height * THUMBNAIL_MAX_WIDTH / croppedBitmap.Width), SKFilterQuality.High);
            lock (_locker)
            {
                if (!File.Exists(imgPath))
                {
                    using var destFs = File.OpenWrite(imgPath);
                    croppedBitmap.Encode(destFs, SKEncodedImageFormat.Jpeg, 100);
                }
            }

            return new Uri(imgPath);
        }

        public string SharePathToLocalPath(string imgPath, string rawDir)
        {
            var fileName = Path.GetFileName(imgPath);
            var filePath = Path.Combine(rawDir, fileName);
            if (File.Exists(filePath))
            {
                return filePath;
            }
            File.Copy(imgPath, filePath, true);
            return filePath;
        }

        public BitmapImage GetRawImg(string rawImgPath, int width, DateTime createTime, string rollNo)
        {
            var rawDir = $"{_settings.Value.DefectSettings.SmallImageSaveFolder}/{createTime:yyyy-MM}/{rollNo}/raw";
            var filePath = string.Empty;
            lock (_locker)
            {
                if (!Directory.Exists(rawDir))
                {
                    Directory.CreateDirectory(rawDir);
                }
                filePath = SharePathToLocalPath(rawImgPath, rawDir);
            }
            using var rawBitmap = SKBitmap.Decode(filePath);
            var resizedBitmap = rawBitmap.Resize(new SKImageInfo(width, rawBitmap.Height * width / rawBitmap.Width), SKFilterQuality.High);
            using (var image = SKImage.FromBitmap(resizedBitmap))
            {
                using (var outputStream = new MemoryStream())
                {
                    var data = image.Encode(SKEncodedImageFormat.Jpeg, 95);
                    data.SaveTo(outputStream);
                    var bitmapImage = new BitmapImage();
                    bitmapImage.BeginInit();
                    bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                    bitmapImage.StreamSource = new MemoryStream(outputStream.ToArray());
                    bitmapImage.EndInit();
                    bitmapImage.Freeze();

                    return bitmapImage;
                }
            }
        }
    }

    public static class DefectExtension
    {
        public static DefectDefineV GetDefectTitle(this DefectParam param)
        {
            return AppSettings.DefectDefineDict.FirstOrDefault(x => x.Value.DefectType == param.Type && (x.Value.RickLevel == null || x.Value.RickLevel == param.DefectGrade)).Value;
        }
    }
}