using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CTClient
{
    public static class AppSettings
    {
        public static Dictionary<string, SpotConfig> SpotSettingDict { get; set; } = null!;

        public static Dictionary<string, DefectDefineV> DefectDefineDict { get; set; } = null!;

        public const string MqttThresholdTopic = "ThresholdSetting";

        public static HashSet<string> DrawDefectTypeSet = new HashSet<string>()
        {
            "轻微边裂",
            "中等边裂",
            "严重边裂",
            "折印"
        };

        private static BitmapImage? _logo;
        public static BitmapImage Logo => _logo ??= GetLogo();

        private static BitmapImage GetLogo()
        {
            if (_logo == null)
            {
                var image = new BitmapImage(new Uri("/Assets/Logo.png", UriKind.RelativeOrAbsolute));
                return image;
            }
            return _logo;
        }
    }

    public class DefectDefineV
    {
        public required string DefectTypeName { get; set; }

        public Color ForeColor { get; set; }

        public Color BackColor { get; set; }

        public Color ReportColor { get; set; }

        public float ThresholdMaxValue { get; set; }
        public required string DefectDesc { get; set; }

        public DefectDefineType DefectDefineType { get; set; }

        public int DefectType { get; set; }

        public int? RickLevel { get; set; }
    }
}