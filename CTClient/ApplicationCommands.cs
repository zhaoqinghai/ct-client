using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Input;

namespace CTClient
{
    public static class ApplicationCommands
    {
        public static readonly RoutedUICommand DisplayDefectDetailCommand = new RoutedUICommand(
           "DisplayDefectDetail", "DisplayDefectDetailCommand", typeof(ApplicationCommands));

        public static readonly RoutedUICommand ShowMsgCommand = new RoutedUICommand(
           "ShowMsg", "ShowMsgCommand", typeof(ApplicationCommands));
    }
}