using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Microsoft.Win32;
using System.IO;

namespace apk_CFG
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void load_Click(object sender, RoutedEventArgs e)
        {
            /*
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c://";
            openFileDialog.Filter = "apk File|*.apk|All File|*.*";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == true)
            {
                string fName = openFileDialog.FileName;
                //show.Text = fName;
                smaliFile sf = new smaliFile(fName);
                //show.Text = sf.SourceName;
                //show.Text += sf.Static.Count;
                for( int i=0 ;i< sf.Direct_Method.Count ;i++)
                {
                    show.Text += sf.Direct_Method[i];
                }
            }  
            */

            method mm = new method(show.Text.ToString());
            //show.AppendText("\n\n\n" + mm.methodContent);


            for (int i = 0; i < mm.LinkFunc.Count; i++)
            {
                //show.AppendText("\n [" + i + "]:" + mm.InstruBlock[i]);
                show.AppendText("\n link:" + mm.LinkFunc[i]);
            }
           
            
        }
    }
}
