using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Socket_Server
{
    /// <summary>
    /// Interaction logic for MaxCustomerWindow.xaml
    /// </summary>
    public partial class MaxCustomerWindow : Window
    {
        public MaxCustomerWindow()
        {
            InitializeComponent();
        }

        public int maxCustomer;

        private void Continue_Click(object sender, RoutedEventArgs e)
        {
            string s = MaxCustomer_TextBox.Text;

            if (s == "")
            {
                MessageBox.Show("Number input is empty");
            }
            else if (!Regex.IsMatch(s, @"^\d+$"))
            {
                MessageBox.Show("Number input is not correct");
            }
            else
            {
                maxCustomer = int.Parse(s);

                if (maxCustomer < 2) { MessageBox.Show("Min number is 2"); }
                else { this.DialogResult = true; this.Close(); }
            }
        }
    }
}
