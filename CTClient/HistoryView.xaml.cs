using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ControlzEx.Standard;
using CTCommonUI;
using CTModel;
using CTService;
using MahApps.Metro.Controls;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Win32;
using NLog.Filters;
using OfficeOpenXml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data;
using System.Data.Common;
using System.IO;
using System.IO.Packaging;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CTClient
{
    /// <summary>
    /// HistoryView.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class HistoryView : UserControl
    {
        private readonly IQueryDefectService _queryDefectService;
        private readonly ILogger<HistoryView> _logger;
        private readonly IDateRangeService _pickDateRangeService;
        private readonly List<IFilter> _filters;

        public HistoryView()
        {
            _queryDefectService = (App.Current as IContainer)!.GetService<IQueryDefectService>()!;
            _logger = (App.Current as IContainer)!.GetService<ILogger<HistoryView>>()!;
            _pickDateRangeService = (App.Current as IContainer)!.GetService<IDateRangeService>()!;
            RollNoFilter = new RollNoFilter();
            DefectTypeFilter = new DefectTypeFilter(FilterQueryCommand);
            SpotNameFilter = new SpotNameFilter(FilterQueryCommand);
            CreateTimeFilter = new CreateTimeFilter(FilterQueryCommand);

            PagingVM = new PagingVM(QueryCommand);
            _filters = new List<IFilter>() { RollNoFilter, DefectTypeFilter, SpotNameFilter, CreateTimeFilter };

            InitializeComponent();
            PickDateRangeType = PickDateRangeType.Shift;
            IsVisibleChanged += HistoryView_IsVisibleChanged;
        }

        public void InitDataGrid()
        {
            dg.SelectedIndex = -1;
        }

        private void HistoryView_IsVisibleChanged(object sender, DependencyPropertyChangedEventArgs e)
        {
            QueryCommand.Execute(null);
        }

        [ObservableProperty]
        private PickDateRangeType _pickDateRangeType;

        partial void OnPickDateRangeTypeChanged(PickDateRangeType value)
        {
            if (value == PickDateRangeType.Shift)
            {
                (CreateTimeFilter.MinTime, CreateTimeFilter.MaxTime) = _pickDateRangeService.GetCurrentShiftDateRange();
            }
            else if (value == PickDateRangeType.Day)
            {
                (CreateTimeFilter.MinTime, CreateTimeFilter.MaxTime) = _pickDateRangeService.GetCurrentDayDateRange();
            }
            else if (value == PickDateRangeType.Month)
            {
                (CreateTimeFilter.MinTime, CreateTimeFilter.MaxTime) = _pickDateRangeService.GetCurrentMonthDateRange();
            }
        }

        [RelayCommand]
        public async Task Query()
        {
            if (!IsVisible)
            {
                return;
            }
            var data = await Task.Factory.StartNew(() => _queryDefectService.GetPageList(PagingVM.PageIndex - 1, PagingVM.PageSize, _filters.Select(f => f.GetParameter()).Where(x => x != null).SelectMany(x => x!).DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value), _filters.Select(f => f.GetSqlWhere())
                .Where(x => !string.IsNullOrEmpty(x))
                .Select(x => x!)
                .ToArray()));
            Items = data.Data.Where(x => !string.IsNullOrEmpty(x.SpotName)).Select(x =>
            {
                AppSettings.SpotSettingDict.TryGetValue(x.SpotName, out var spotConfig);
                var defectDisplay = x.GetDefectTitle();
                var ret = new DefectDetailVM()
                {
                    DetailId = x.DetailId,
                    SpotName = x.SpotName,
                    DefectWidth = x.Rect_H * (spotConfig?.CameraPxSize ?? 0.02),
                    DefectDepth = x.Rect_W * (spotConfig?.CameraPxSize ?? 0.02),
                    DefectTitle = defectDisplay.DefectTypeName,
                    BackColor = defectDisplay.BackColor,
                    ForeColor = defectDisplay.ForeColor,
                    Position = x.Position,
                    RollNo = x.RollNo,
                    CreateTime = x.CreateTime,
                    RemainLength = x.RemainLength,
                    RollSpeed = x.RollSpeed,
                    RollThickness = x.RollThickness,
                    RollWidth = x.RollWidth,
                };
                ret.DefectArea = ret.DefectDepth * ret.DefectWidth;
                return ret;
            }).ToArray();
            PagingVM.TotalCount = data.TotalCount;
        }

        [RelayCommand]
        public void FilterQuery()
        {
            PagingVM.Reset();
            QueryCommand?.Execute(null);
        }

        [RelayCommand]
        public void Export()
        {
            try
            {
                var sfd = new SaveFileDialog()
                {
                    Filter = "(*.xlsx)|*.xlsx",
                    FileName = $"缺陷详情{DateTime.Now:yyyy-MM-dd}"
                };
                if (sfd.ShowDialog() == true)
                {
                    Task.Factory.StartNew(() =>
                    {
                        var filePath = sfd.FileName;
                        if (File.Exists(filePath))
                        {
                            File.Delete(filePath);
                        }
                        ExcelPackage.LicenseContext = LicenseContext.NonCommercial;
                        using var package = new ExcelPackage(new FileInfo(sfd.FileName));
                        var sheet = package.Workbook.Worksheets.Add("Sheet1");
                        sheet.Columns[1].Width = 26;
                        sheet.Cells[1, 1].Value = "时间";
                        sheet.Columns[2].Width = 20;
                        sheet.Cells[1, 2].Value = "卷号";
                        sheet.Columns[3].Width = 12;
                        sheet.Cells[1, 3].Value = "卷宽(mm)";
                        sheet.Columns[4].Width = 12;
                        sheet.Cells[1, 4].Value = "卷厚(mm)";
                        sheet.Columns[5].Width = 12;
                        sheet.Cells[1, 5].Value = "速度(m/min)";
                        sheet.Columns[6].Width = 26;
                        sheet.Cells[1, 6].Value = "点位";
                        sheet.Columns[7].Width = 20;
                        sheet.Cells[1, 7].Value = "位置";
                        sheet.Columns[8].Width = 16;
                        sheet.Cells[1, 8].Value = "剩余长度(m)";
                        sheet.Columns[9].Width = 26;
                        sheet.Cells[1, 9].Value = "缺陷类型";
                        sheet.Columns[10].Width = 16;
                        sheet.Cells[1, 10].Value = "缺陷宽(mm)";
                        sheet.Columns[11].Width = 16;
                        sheet.Cells[1, 11].Value = "缺陷深(mm)";
                        sheet.Columns[12].Width = 16;
                        sheet.Cells[1, 12].Value = "缺陷面积(mm2)";

                        (var totalCount, _) = _queryDefectService.GetPageList(0, 0, _filters.Select(f => f.GetParameter()).Where(x => x != null).SelectMany(x => x!).DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value), _filters.Select(f => f.GetSqlWhere())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => x!)
                        .ToArray());

                        var rowOffset = 1;
                        var pageIndex = 0;
                        var pageSize = 200;

                        for (; pageIndex * pageSize < totalCount;)
                        {
                            (_, var datas) = _queryDefectService.GetPageList(pageIndex, pageSize, _filters.Select(f => f.GetParameter()).Where(x => x != null).SelectMany(x => x!).DistinctBy(x => x.Key).ToDictionary(x => x.Key, x => x.Value), _filters.Select(f => f.GetSqlWhere())
                        .Where(x => !string.IsNullOrEmpty(x))
                        .Select(x => x!)
                        .ToArray());
                            foreach (var data in datas)
                            {
                                rowOffset++;
                                AppSettings.SpotSettingDict.TryGetValue(data.SpotName, out var spotConfig);
                                var defectDisplay = data.GetDefectTitle();
                                sheet.Cells[rowOffset, 1].Value = string.Format("{0:yyyy-MM-dd HH:mm:ss}", data.CreateTime);
                                sheet.Cells[rowOffset, 2].Value = data.RollNo;
                                sheet.Cells[rowOffset, 3].Value = data.RollWidth;
                                sheet.Cells[rowOffset, 4].Value = data.RollThickness;
                                sheet.Cells[rowOffset, 5].Value = data.RollSpeed;
                                sheet.Cells[rowOffset, 6].Value = data.SpotName;
                                sheet.Cells[rowOffset, 7].Value = data.Position;
                                sheet.Cells[rowOffset, 8].Value = data.RemainLength;
                                sheet.Cells[rowOffset, 9].Value = defectDisplay.DefectTypeName;
                                var defectWidth = data.Rect_H * (spotConfig?.CameraPxSize ?? 0.02);
                                sheet.Cells[rowOffset, 10].Value = defectWidth;
                                var defectDepth = data.Rect_W * (spotConfig?.CameraPxSize ?? 0.02);
                                sheet.Cells[rowOffset, 11].Value = defectDepth;
                                sheet.Cells[rowOffset, 12].Value = defectWidth * defectDepth;
                            }
                            pageIndex++;
                        }
                        package.Save();
                    }).ContinueWith(t =>
                    {
                        if (t.IsFaulted || t.IsCanceled)
                        {
                            _logger.LogError("导出Task出现异常:{0}", t.Exception);
                        }
                        else
                        {
                            Application.Current.Dispatcher.Invoke(() =>
                            {
                                ApplicationCommands.ShowMsgCommand?.Execute((ShowMsgInfo)"导出成功", null);
                            });
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("导出异常:{0}", ex);
            }
        }

        [ObservableProperty]
        private DefectDetailVM[]? _items;

        public RollNoFilter RollNoFilter { get; private set; }

        public DefectTypeFilter DefectTypeFilter { get; private set; }

        public SpotNameFilter SpotNameFilter { get; private set; }

        public CreateTimeFilter CreateTimeFilter { get; private set; }

        public PagingVM PagingVM { get; private set; }
    }

    public interface IFilter
    {
        Dictionary<string, object?>? GetParameter();

        string? GetSqlWhere();
    }

    public partial class DefectTypeFilter : ObservableObject, IFilter
    {
        const string AllDefectName = "全部";
        private readonly ICommand _command;

        public DefectTypeFilter(ICommand command)
        {
            Items = new[] { new KeyValuePair<string, DefectDefineV>(AllDefectName, new DefectDefineV()
            {
                DefectTypeName = AllDefectName,
                DefectDesc = string.Empty
            })}.Concat(AppSettings.DefectDefineDict.Where(x => x.Value.DefectDefineType.HasFlag(DefectDefineType.Search))).ToList();
            Selected = Items.FirstOrDefault().Value;
            _command = command;
        }

        public List<KeyValuePair<string, DefectDefineV>> Items { get; set; }

        [ObservableProperty]
        private DefectDefineV _selected;

        partial void OnSelectedChanged(DefectDefineV dv)
        {
            if (_command != null && _command.CanExecute(null))
            {
                _command.Execute(null);
            }
        }

        public Dictionary<string, object?>? GetParameter()
        {
            if (string.IsNullOrEmpty(Selected.DefectTypeName) || object.ReferenceEquals(Selected.DefectTypeName, AllDefectName))
            {
                return null;
            }
            var dict = new Dictionary<string, object?>();
            dict[nameof(DefectParam.Type)] = Selected.DefectType;
            if (Selected.RickLevel.HasValue)
            {
                dict[nameof(DefectParam.DefectGrade)] = Selected.RickLevel;
            }
            return dict;
        }

        public string? GetSqlWhere()
        {
            if (string.IsNullOrEmpty(Selected.DefectTypeName) || object.ReferenceEquals(Selected.DefectTypeName, AllDefectName))
            {
                return null;
            }
            var sql = $"{nameof(DefectParam.Type)} = @{nameof(DefectParam.Type)}";
            if (Selected.RickLevel.HasValue)
            {
                sql += $" AND {nameof(DefectParam.DefectGrade)} = @{nameof(DefectParam.DefectGrade)}";
            }
            return sql;
        }
    }

    public partial class SpotNameFilter : ObservableObject, IFilter
    {
        const string AllSpotName = "全部";
        private readonly ICommand _command;

        public SpotNameFilter(ICommand command)
        {
            Items = new[] { AllSpotName }.Concat(AppSettings.SpotSettingDict.Keys).ToList();
            Selected = Items.FirstOrDefault();
            _command = command;
        }

        partial void OnSelectedChanged(string? sp)
        {
            if (_command != null && _command.CanExecute(null))
            {
                _command.Execute(null);
            }
        }

        public List<string> Items { get; set; }

        [ObservableProperty]
        private string? _selected;

        public Dictionary<string, object?>? GetParameter()
        {
            if (string.IsNullOrEmpty(Selected) || object.ReferenceEquals(Selected, AllSpotName))
            {
                return null;
            }
            var dict = new Dictionary<string, object?>();

            dict[nameof(DefectParam.SpotName)] = Selected;
            return dict;
        }

        public string? GetSqlWhere()
        {
            if (string.IsNullOrEmpty(Selected) || object.ReferenceEquals(Selected, AllSpotName))
            {
                return null;
            }
            var sql = $"{nameof(DefectParam.SpotName)} = @{nameof(DefectParam.SpotName)}";
            return sql;
        }
    }

    public partial class CreateTimeFilter : ObservableObject, IFilter
    {
        private readonly ICommand _command;

        public CreateTimeFilter(ICommand command)
        {
            MinTime = DateTime.Now.AddDays(-1);
            MaxTime = DateTime.Now;
            _command = command;
        }

        [ObservableProperty]
        private DateTime _minTime;

        [ObservableProperty]
        private DateTime _maxTime;

        partial void OnMinTimeChanged(DateTime dt)
        {
            if (_command != null && _command.CanExecute(null))
            {
                _command.Execute(null);
            }
        }

        partial void OnMaxTimeChanged(DateTime dt)
        {
            if (_command != null && _command.CanExecute(null))
            {
                _command.Execute(null);
            }
        }

        public Dictionary<string, object?>? GetParameter()
        {
            var dict = new Dictionary<string, object?>();

            dict[nameof(MinTime)] = MinTime.ToString("yyyy-MM-dd HH:mm:ss");
            dict[nameof(MaxTime)] = MaxTime.ToString("yyyy-MM-dd HH:mm:ss");
            return dict;
        }

        public string? GetSqlWhere()
        {
            var sql = $"{nameof(DefectParam.CreateTime)} >= @{nameof(MinTime)} AND {nameof(DefectParam.CreateTime)} <= @{nameof(MaxTime)}";
            return sql;
        }
    }

    public partial class RollNoFilter : ObservableObject, IFilter
    {
        [ObservableProperty]
        private string? _rollNo;

        public Dictionary<string, object?>? GetParameter()
        {
            if (string.IsNullOrEmpty(RollNo))
            {
                return null;
            }
            var dict = new Dictionary<string, object?>();

            dict[nameof(RollNo)] = $"%{RollNo}%";
            return dict;
        }

        public string? GetSqlWhere()
        {
            if (string.IsNullOrEmpty(RollNo))
            {
                return null;
            }
            var sql = $"{nameof(DefectParam.RollNo)} LIKE @{nameof(RollNo)}";
            return sql;
        }
    }

    public class DefectDetailVM
    {
        public int DetailId { get; set; }

        [Column("位置", Order = 7)]
        public required string Position { get; set; }

        [Column("缺陷类型", Order = 9)]
        public required string DefectTitle { get; set; }

        [Column("时间", Order = 1)]
        public DateTime CreateTime { get; set; }

        [Column("卷号", Order = 2)]
        public required string RollNo { get; set; }

        [Column("速度(m/min)", Order = 5)]
        public required float RollSpeed { get; set; }

        [Column("卷宽(mm)", Order = 3)]
        public required float RollWidth { get; set; }

        [Column("卷厚(mm)", Order = 4)]
        public required float RollThickness { get; set; }

        [Column("剩余长度(m)", Order = 8)]
        public required string RemainLength { get; set; }

        public Color BackColor { get; set; }

        public Color ForeColor { get; set; }

        [Column("缺陷宽(mm)", Order = 10)]
        public double DefectWidth { get; set; }

        [Column("缺陷深(mm)", Order = 11)]
        public double DefectDepth { get; set; }

        [Column("缺陷面积(mm)", Order = 12)]
        public double DefectArea { get; set; }

        [Column("点位", Order = 6)]
        public required string SpotName { get; set; }
    }

    public enum PickDateRangeType
    {
        None,
        Shift,
        Day,
        Month
    }

    public partial class PagingVM : ObservableObject
    {
        public readonly static List<int> PageSizeItems = new List<int>()
        {
            10, 20, 50
        };

        [ObservableProperty]
        private int _pageIndex;

        [ObservableProperty, NotifyPropertyChangedFor(nameof(MaxPageIndex))]
        private int _pageSize = PageSizeItems.First();

        [ObservableProperty, NotifyPropertyChangedFor(nameof(MaxPageIndex))]
        private int _totalCount;

        public int MaxPageIndex => TotalCount / PageSize + 1;

        private readonly ICommand _queryCommand;

        public PagingVM(ICommand command)
        {
            _queryCommand = command;
        }

        partial void OnTotalCountChanged(int totalCount)
        {
            PreviousCommand.NotifyCanExecuteChanged();
            JumpCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }

        partial void OnPageIndexChanged(int totalCount)
        {
            JumpCommand.NotifyCanExecuteChanged();
        }

        partial void OnPageSizeChanged(int totalCount)
        {
            PageIndex = 1;
            ExecuteCommand();
        }

        private void ExecuteCommand()
        {
            _queryCommand?.Execute(null);
            PreviousCommand.NotifyCanExecuteChanged();
            JumpCommand.NotifyCanExecuteChanged();
            NextCommand.NotifyCanExecuteChanged();
        }

        public void Reset()
        {
            PageIndex = 1;
        }

        [RelayCommand]
        private void First()
        {
            PageIndex = 1;
            ExecuteCommand();
        }

        [RelayCommand(CanExecute = nameof(CanPrevious))]
        private void Previous()
        {
            PageIndex -= 1;
            ExecuteCommand();
        }

        private bool CanPrevious()
        {
            return PageIndex > 1;
        }

        [RelayCommand(CanExecute = nameof(CanJump))]
        private void Jump()
        {
            ExecuteCommand();
        }

        private bool CanJump()
        {
            return PageIndex <= GetTotalPages() && PageIndex > 0;
        }

        [RelayCommand(CanExecute = nameof(CanNext))]
        private void Next()
        {
            PageIndex += 1;
            ExecuteCommand();
        }

        private bool CanNext()
        {
            return PageIndex < GetTotalPages();
        }

        [RelayCommand]
        private void Last()
        {
            PageIndex = GetTotalPages();
            ExecuteCommand();
        }

        private int GetTotalPages()
        {
            return (TotalCount / PageSize) + (TotalCount % PageSize == 0 ? 0 : 1);
        }

        public enum PagingCmd
        {
            First,
            Previous,
            Jump,
            Next,
            Last
        }
    }
}