using CommunityToolkit.Mvvm.Messaging;
using CTCommonUI;
using CTModel;
using CTService;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NLog;
using NLog.Extensions.Logging;
using ScottPlot.Statistics;
using System.Collections.Concurrent;
using System.Configuration;
using System.Data;
using System.Diagnostics;
using System.IO;
using System.Reflection;
using System.Runtime;
using System.Runtime.InteropServices;
using System.Security.Policy;
using System.Speech.Recognition;
using System.Speech.Synthesis;
using System.Text;
using System.Threading;
using System.Threading.Channels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;
using System.Windows.Media;
using ConfigurationManager = Microsoft.Extensions.Configuration.ConfigurationManager;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace CTClient
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, IContainer
    {
        static int _executeOnce;
        static Mutex? _mutex;
        private readonly IServiceProvider _sp;

        public App()
        {
            var logger = LogManager.GetCurrentClassLogger();
            TaskScheduler.UnobservedTaskException += TaskScheduler_UnobservedTaskException;
            DispatcherUnhandledException += App_DispatcherUnhandledException;
            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException;

            logger.Info("************************application start************************");
            var svcs = new ServiceCollection();
            svcs.AddLogging(loggingBuilder =>
            {
                loggingBuilder.AddNLog();
            });
            svcs.AddSingleton<MainWindow>();

            svcs.AddKeyedSingleton(RemindSpeechWorker.REMIND_SPEECH_CHANNEL, (_, _) => Channel.CreateBounded<string>(new BoundedChannelOptions(2)
            {
                FullMode = BoundedChannelFullMode.DropOldest,
                SingleReader = true
            }));

            svcs.Configure<LocalSettings>(new ConfigurationBuilder().AddJsonFile(
#if DEBUG
                "appsettings.development.json"
#else
                "appsettings.json"
#endif
                , optional: false, reloadOnChange: false).Build());
            svcs.AddTransient<IQueryDefectService, QueryDefectService>();
            svcs.AddTransient<IThresholdService, ThresholdService>();
            svcs.AddTransient<DefectImgHelper>();
            svcs.AddSingleton<IRunServer, MqttRecieveWorker>(sp =>
            {
                var ret = new MqttRecieveWorker(sp.GetRequiredService<IOptions<LocalSettings>>(), sp.GetRequiredService<ILogger<MqttRecieveWorker>>());
                ret.NotifyMqttServer += NotifyMqttServerTopicMsg;
                return ret;
            });
            svcs.AddSingleton<IRunServer, RemindSpeechWorker>();
            svcs.AddSingleton<IRunServer, RefreshReportWorker>();
            svcs.AddTransient<IDateRangeService, DefaultShiftService>();
            _sp = svcs.BuildServiceProvider();
        }

        private void TaskScheduler_UnobservedTaskException(object? sender, UnobservedTaskExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Fatal(e.Exception);
        }

        private void CurrentDomain_UnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            LogManager.GetCurrentClassLogger().Fatal(e.ExceptionObject);
            LogManager.GetCurrentClassLogger().Error("-------------ENTER 程序异常崩溃 CallBack------------");
            var fileName = Process.GetCurrentProcess().MainModule?.FileName;
            if (Path.Exists(fileName) && Interlocked.CompareExchange(ref _executeOnce, 1, 0) == 0)
            {
                LogManager.GetCurrentClassLogger().Error("--------------------- fileName:{0} ---------------------", fileName);
                ProcessStartInfo startInfo = new ProcessStartInfo
                {
                    FileName = fileName,
                    UseShellExecute = true,
                    Arguments = "restart"
                };
                _mutex?.Dispose();
                Process.Start(startInfo);
                LogManager.GetCurrentClassLogger().Error("--------------------- 重启进程完成 ---------------------");
            }
        }

        private void App_DispatcherUnhandledException(object sender, System.Windows.Threading.DispatcherUnhandledExceptionEventArgs e)
        {
            e.Handled = true;
            LogManager.GetCurrentClassLogger().Fatal(e.Exception);
        }

        private void NotifyMqttServerTopicMsg(MqttMsgInfo msg)
        {
            new Task(() =>
            {
                if (msg is { Topic: "SteelInfo" })
                {
                    var datas = msg.Message.Split(",");
                    if (datas.Length >= 5)
                    {
                        WeakReferenceMessenger.Default.Send(new RollInfo()
                        {
                            RollNo = datas[0],
                            RemainLength = datas[1],
                            Speed = datas[2],
                            Width = datas[3],
                            Thickness = datas[4]
                        });
                    }
                }
                else if (msg is { Topic: "Remind" })
                {
                    var datas = msg.Message.Split(",");
                    var remind = !string.IsNullOrEmpty(datas[1]);
                    if (datas.Length >= 3 && int.TryParse(datas[2], out var recordNo))
                    {
                        if (remind && !_sp.GetRequiredKeyedService<Channel<string>>(RemindSpeechWorker.REMIND_SPEECH_CHANNEL).Writer.TryWrite(datas[1]))
                        {
                            LogManager.GetCurrentClassLogger().Error("remind {0} 无法写入 Channel", datas[1]);
                        }

                        var details = _sp.GetRequiredService<IQueryDefectService>().GetDefectByRecordNo(recordNo)!;

                        WeakReferenceMessenger.Default.Send(details ?? Array.Empty<DefectParam>());
                        if (details != null && details.Length > 0 && remind)
                        {
                            WeakReferenceMessenger.Default.Send(new DisplayDefectDialogEvent(details.First().DetailId, true));
                        }
                    }
                }
                else if (msg is { Topic: "RunOffsetRemind" })
                {
                    if (!_sp.GetRequiredKeyedService<Channel<string>>(RemindSpeechWorker.REMIND_SPEECH_CHANNEL).Writer.TryWrite(msg.Message))
                    {
                        LogManager.GetCurrentClassLogger().Error("remind {0} 无法写入 Channel", msg.Message);
                    }
                }
            }).Start();
        }

        public T? GetService<T>() => _sp.GetService<T>();

        protected override void OnStartup(StartupEventArgs e)
        {
            if (e.Args.Length > 0 && e.Args.Any(x => x.Equals("restart", StringComparison.OrdinalIgnoreCase)))
            {
                LogManager.GetCurrentClassLogger().Error("app restart wait 1s");
                Thread.Sleep(1000);
            }
            _mutex = new Mutex(true, "439ABA61-E007-4D92-8C8A-067930020A53", out var isCreatedNew);

            if (!isCreatedNew)
            {
                try
                {
                    LogManager.GetCurrentClassLogger().Error("------------------ 当前有运行的进程 ------------------");
                    return;
                }
                finally
                {
                    Environment.Exit(0);
                }
            }

            foreach (var server in _sp.GetServices<IRunServer>())
            {
                new Thread(async () =>
                {
                    while (true)
                    {
                        try
                        {
                            await server.Run();
                        }
                        catch (Exception ex)
                        {
                            LogManager.GetCurrentClassLogger().Error("{0} | {1}", server.GetType(), ex);
                        }
                        await Task.Delay(TimeSpan.FromSeconds(30));
                    }
                })
                {
                    Priority = ThreadPriority.Lowest,
                    IsBackground = true,
                }
                .Start();
            }
            var options = GetService<IOptions<LocalSettings>>()!.Value;
            AppSettings.DefectDefineDict = options.DefectDefineSettings.ToDictionary(x => x.DefectTypeName, x => new DefectDefineV()
            {
                DefectTypeName = x.DefectTypeName,
                DefectType = x.DefectType,
                RickLevel = x.RickLevel,
                BackColor = (Color)ColorConverter.ConvertFromString(x.BackColor)!,
                ForeColor = (Color)ColorConverter.ConvertFromString(x.ForeColor)!,
                ReportColor = (Color)ColorConverter.ConvertFromString(x.ReportColor)!,
                DefectDefineType = x.DefectDefineType,
                ThresholdMaxValue = x.ThresholdMaxValue,
                DefectDesc = x.ReportDesc
            });
            AppSettings.SpotSettingDict = options.SpotConfigs.ToDictionary(x => x.SpotName);
            base.OnStartup(e);
            _sp.GetRequiredService<MainWindow>().Show();
        }
    }
}