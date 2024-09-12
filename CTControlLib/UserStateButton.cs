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
    public class UserStateButton : Button
    {
        static UserStateButton()
        {
            DefaultStyleKeyProperty.OverrideMetadata(typeof(UserStateButton), new FrameworkPropertyMetadata(typeof(UserStateButton)));
        }

        protected override void OnClick()
        {
            if (State >= StateMax)
            {
                State = 0;
            }
            else
            {
                State += 1;
            }
            base.OnClick();
        }

        public int State
        {
            get { return (int)GetValue(StateProperty); }
            set { SetValue(StateProperty, value); }
        }

        public static readonly DependencyProperty StateProperty =
            DependencyProperty.Register("State", typeof(int), typeof(UserStateButton), new PropertyMetadata(0));

        public int StateMax
        {
            get { return (int)GetValue(StateMaxProperty); }
            set { SetValue(StateMaxProperty, value); }
        }

        public static readonly DependencyProperty StateMaxProperty =
            DependencyProperty.Register("StateMax", typeof(int), typeof(UserStateButton), new PropertyMetadata(0));
    }
}