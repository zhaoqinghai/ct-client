using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using CTCommonUI;
using CTService;
using MahApps.Metro.Controls;
using MahApps.Metro.Controls.Dialogs;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Serialization;
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

namespace CTClient
{
    /// <summary>
    /// SettingView.xaml 的交互逻辑
    /// </summary>
    [ObservableObject]
    public partial class SettingView : UserControl
    {
        private readonly IThresholdService _thresholdService;
        private readonly ILogger<HistoryView> _logger;
        private readonly MqttServerSettings _settings;

        public SettingView()
        {
            _thresholdService = (App.Current as IContainer)!.GetService<IThresholdService>()!;
            _logger = (App.Current as IContainer)!.GetService<ILogger<HistoryView>>()!;
            _settings = (App.Current as IContainer)!.GetService<IOptions<LocalSettings>>()!.Value.MqttServerSettings;
            var thresholdSettings = AppSettings.DefectDefineDict.Where(x => x.Value.DefectDefineType.HasFlag(DefectDefineType.Threshold)).ToDictionary(x => x.Value.DefectTypeName, x => x.Value);
            Task.Factory.StartNew(() =>
            {
                return _thresholdService.GetDefectDefineConfigs();
            }).ContinueWith(t =>
            {
                if (t.IsFaulted || t.IsCanceled)
                {
                    _logger.LogError("查询阈值表数据失败：{0}", t.Exception);
                }
                else
                {
                    foreach (var kvp in t.Result)
                    {
                        if (thresholdSettings.TryGetValue(kvp.Name, out var value) && float.TryParse(kvp.MaxValue, out var maxValue))
                        {
                            value.ThresholdMaxValue = maxValue;
                        }
                    }
                }
                App.Current.Dispatcher.Invoke(() =>
                {
                    {
                        if (thresholdSettings.TryGetValue("轻微边裂", out var dd))
                        {
                            LowCrack = Transfer(dd);
                        }
                    }
                    {
                        if (thresholdSettings.TryGetValue("中等边裂", out var dd))
                        {
                            MediumCrack = Transfer(dd);
                        }
                    }
                    {
                        if (thresholdSettings.TryGetValue("严重边裂", out var dd))
                        {
                            HighCrack = Transfer(dd);
                        }
                    }
                    {
                        if (thresholdSettings.TryGetValue("折印", out var dd))
                        {
                            Crease = Transfer(dd);
                        }
                    }
                    {
                        if (thresholdSettings.TryGetValue("跑偏", out var dd))
                        {
                            OffTrack = Transfer(dd);
                        }
                    }
                });
            });
            InitializeComponent();
        }

        [ObservableProperty]
        private DefectDefineVM? _lowCrack;

        [ObservableProperty]
        private DefectDefineVM? _mediumCrack;

        [ObservableProperty]
        private DefectDefineVM? _highCrack;

        [ObservableProperty]
        private DefectDefineVM? _crease;

        [ObservableProperty]
        private DefectDefineVM? _offTrack;

        private DefectDefineVM Transfer(DefectDefineV defectDefineV)
        {
            return new DefectDefineVM()
            {
                DefectTypeName = defectDefineV.DefectTypeName,
                ForeColor = defectDefineV.ForeColor,
                BackColor = defectDefineV.BackColor,
                MaxValue = defectDefineV.ThresholdMaxValue,
            };
        }

        [RelayCommand]
        private async Task Commit()
        {
            try
            {
                if (HasValidationError(this))
                {
                    ApplicationCommands.ShowMsgCommand?.Execute(new ShowMsgInfo()
                    {
                        Error = true,
                        Message = "存在异常数据请检查"
                    }, null);
                    return;
                }

                var items = new List<DefectDefineVM?>() {
                    LowCrack,
                    MediumCrack,
                    HighCrack,
                    Crease,
                    OffTrack
                };
                var details = JsonSerializer.Serialize(items, new JsonSerializerOptions
                {
                    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
                });
                var configs = items.Where(x => x != null).Select(x => new CTModel.DefectDefineConfig
                {
                    Name = x!.DefectTypeName,
                    MaxValue = x.MaxValue.ToString()
                }).ToList();
                await Task.Factory.StartNew(async () =>
                {
                    _logger.LogInformation("update threshold {0}", details);
                    _thresholdService.UpdateConfigs(configs);

                    var mqttFactory = new MqttFactory();

                    using var mqttClient = mqttFactory.CreateMqttClient();
                    var mqttClientOptions = new MqttClientOptionsBuilder().WithTcpServer(_settings.ServerIp, _settings.ServerPort).Build();
                    await mqttClient.ConnectAsync(mqttClientOptions);
                    await mqttClient.PublishAsync(new MqttApplicationMessageBuilder().WithTopic(AppSettings.MqttThresholdTopic).WithPayload(details).Build());
                });
                ApplicationCommands.ShowMsgCommand?.Execute((ShowMsgInfo)"提交成功", null);
            }
            catch (Exception ex)
            {
                _logger.LogError("更新阈值异常:{0}", ex);
            }
        }

        public static bool HasValidationError(DependencyObject parent)
        {
            if (parent == null)
                return false;

            if (Validation.GetHasError(parent))
                return true;

            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
            {
                var child = VisualTreeHelper.GetChild(parent, i);
                if (HasValidationError(child))
                    return true;
            }

            return false;
        }
    }

    public partial class DefectDefineVM : ObservableObject
    {
        [JsonIgnore]
        public string DefectTypeName { get; set; } = null!;

        [JsonIgnore]
        public Color ForeColor { get; set; }

        [JsonIgnore]
        public Color BackColor { get; set; }

        [ObservableProperty]
        [property: JsonIgnore]
        private float _maxValue;

        public string ConfigurationName => DefectTypeName;

        public string ConfigurationMax => MaxValue.ToString();
    }

    public class ShowMsgInfo
    {
        public bool Error { get; set; }

        public required string Message { get; set; }

        public static implicit operator ShowMsgInfo(string msg)
        {
            return new ShowMsgInfo()
            {
                Error = false,
                Message = msg
            };
        }
    }
}