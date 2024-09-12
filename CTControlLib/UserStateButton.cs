using System;
using System.Collections.Generic;
using System.Linq;
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

namespace CTControlLib
{
    public class UserStateButton : ToggleButton
    {
        static UserStateButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UserStateButton), new FrameworkPropertyMetadata(typeof(UserStateButton)));
        }

        public object OnContent
        {
            get { return (object)GetValue(OnContentProperty); }
            set { SetValue(OnContentProperty, value); }
        }

        public static readonly DependencyProperty OnContentProperty =
            DependencyProperty.Register("OnContent", typeof(object), typeof(UserStateButton));

        public object OffContent
        {
            get { return (object)GetValue(OffContentProperty); }
            set { SetValue(OffContentProperty, value); }
        }

        public static readonly DependencyProperty OffContentProperty =
            DependencyProperty.Register("OffContent", typeof(object), typeof(UserStateButton));
    }
}