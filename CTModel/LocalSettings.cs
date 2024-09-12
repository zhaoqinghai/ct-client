using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTClient
{
    public class LocalSettings
    {
        public string WinTitle { get; set; } = string.Empty;

        public ConnectionStrings ConnectionStrings { get; set; } = null!;

        public List<SpotConfig> SpotConfigs { get; set; } = null!;

        public DefectSettings DefectSettings { get; set; } = null!;

        public DefectDefineSettings DefectDefineSettings { get; set; } = null!;

        public MqttServerSettings MqttServerSettings { get; set; } = new MqttServerSettings()
        {
            ServerIp = string.Empty
        };
    }

    public class SpotConfig
    {
        public string SpotName { get; set; } = string.Empty;

        public string LeftLable { get; set; } = string.Empty;

        public string RightLable { get; set; } = string.Empty;

        public string Keyword { get; set; } = string.Empty;

        public int NewestDefectColCount { get; set; } = 4;

        public int NewestDefectRowCount { get; set; } = 1;

        public int CurrentDayDefectCount { get; set; } = 20;

        public int CurrentRollDefectCount { get; set; } = 20;

        public int CurrentDefectColCount { get; set; } = 6;

        public double CameraPxSize { get; set; } = .2;
    }

    public class ConnectionStrings
    {
        public string DefectDb { get; set; } = string.Empty;
    }

    public class DefectSettings
    {
        public string SmallImageSaveFolder { get; set; } = "D:/HistoryImages";

        public string GlobalDefectIgnoreSql { get; set; } = null!;
    }

    public class MqttServerSettings
    {
        public string ServerIp { get; set; } = string.Empty;

        public int ServerPort { get; set; }

        public string[] Topics { get; set; } = new string[0];
    }

    public class DefectDefineSettings : List<DefectDefine>
    {
    }

    public class DefectDefine
    {
        public string DefectTypeName { get; set; } = string.Empty;

        public string ForeColor { get; set; } = string.Empty;

        public string BackColor { get; set; } = string.Empty;

        public string ReportColor { get; set; } = string.Empty;

        public string ReportDesc { get; set; } = string.Empty;

        public float ThresholdMaxValue { get; set; }

        public DefectDefineType DefectDefineType { get; set; }

        public int DefectType { get; set; }

        public int? RickLevel { get; set; }
    }

    [Flags]
    public enum DefectDefineType
    {
        None = 0,

        Search = 1,

        Threshold = 1 << 1
    }
}