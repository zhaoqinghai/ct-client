using MahApps.Metro.Controls;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CTControlLib
{
    public class CTDateTimePicker : DateTimePicker
    {
        protected override string GetValueForTextBox()
        {
            return SelectedDateTime?.ToString("yyyy-MM-dd HH:mm:ss") ?? string.Empty;
        }
    }
}