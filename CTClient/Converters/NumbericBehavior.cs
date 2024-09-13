using MahApps.Metro.Controls;
using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace CTClient.Converters
{
    public class NumericUpDownBehavior : Behavior<NumericUpDown>
    {
        const string PART_TextBox = "PART_TextBox";

        private string _oldText = string.Empty;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.Loaded += AssociatedObject_Loaded;
        }

        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            var tbx = AssociatedObject.Template.FindName(PART_TextBox, AssociatedObject) as TextBox;
            if (tbx != null)
            {
                _oldText = tbx.Text;
                tbx.TextChanged -= Tbx_TextChanged;
                DataObject.RemovePastingHandler(tbx, this.OnValueTextBoxPaste);
                tbx.TextChanged += Tbx_TextChanged;
                DataObject.AddPastingHandler(tbx, this.OnValueTextBoxPaste);
            }
        }

        private void Tbx_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (sender is TextBox tbx)
            {
                if (!IsNumericInput(tbx.Text))
                {
                    if (tbx.Text == _oldText)
                    {
                        AssociatedObject.Value = 0;
                    }
                    else
                    {
                        tbx.Text = _oldText;
                    }
                    e.Handled = true;
                }
                else
                {
                    _oldText = tbx.Text;
                }
            }
        }

        private void OnValueTextBoxPaste(object sender, DataObjectPastingEventArgs e)
        {
            if (sender is TextBox tbx)
            {
                if (!IsNumericInput(tbx.Text))
                {
                    if (tbx.Text == _oldText)
                    {
                        AssociatedObject.Value = 0;
                    }
                    else
                    {
                        tbx.Text = _oldText;
                    }
                    e.Handled = true;
                }
                else
                {
                    _oldText = tbx.Text;
                }
            }
        }

        protected override void OnDetaching()
        {
            AssociatedObject.Loaded -= AssociatedObject_Loaded;

            base.OnDetaching();
        }

        private bool IsNumericInput(string input)
        {
            // 允许输入的为数字
            return double.TryParse(input, out _);
        }
    }
}