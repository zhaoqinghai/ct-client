using CommunityToolkit.Mvvm.ComponentModel;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace CTClient
{
    /// <summary>
    /// DefectReportItemControl.xaml 的交互逻辑
    /// </summary>
    public partial class DefectReportControl : UserControl
    {
        public DefectReportControl()
        {
            InitializeComponent();
        }
    }

    public class DefectReportVM : ObservableObject
    {
        public void Reset()
        {
            if (LowCrack != null)
            {
                LowCrack.Count = 0;
            }

            if (MediumCrack != null)
            {
                MediumCrack.Count = 0;
            }

            if (HighCrack != null)
            {
                HighCrack.Count = 0;
            }

            if (Crease != null)
            {
                Crease.Count = 0;
            }
        }

        public required string Title { get; set; }

        public DefectReportItem? LowCrack { get; set; }

        public DefectReportItem? MediumCrack { get; set; }

        public DefectReportItem? HighCrack { get; set; }

        public DefectReportItem? Crease { get; set; }
    }

    public partial class DefectReportItem : ObservableObject
    {
        public required string DefectName { get; init; }

        public Color BackColor { get; set; }

        public Color ForeColor { get; set; }

        [ObservableProperty]
        private int _count;

        public DefectReportItem Clone()
        {
            return new DefectReportItem
            {
                DefectName = this.DefectName,
                BackColor = this.BackColor,
                ForeColor = this.ForeColor,
                DefectDesc = this.DefectDesc,
            };
        }

        public required string DefectDesc { get; init; }
    }
}