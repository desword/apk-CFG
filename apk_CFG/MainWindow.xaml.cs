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
using MindFusion.Diagramming.Wpf;
using System.Xml.Linq;
using MindFusion.Diagramming.Wpf.Layout;

namespace apk_CFG
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public smaliFile sf;
        public List<method> me;

        public MainWindow()
        {
            InitializeComponent();
        }

        public bool openFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c://";
            openFileDialog.Filter = "All File|*.*|apk File|*.apk";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == true)
            {
                string fName = openFileDialog.FileName;
                //show.Text = fName;
                sf = new smaliFile(fName, "e:");
                //show.Text = sf.SourceName;
                //show.Text += sf.Static.Count;
                /*
                for( int i=0 ;i< sf.Direct_Method.Count ;i++)
                {
                    show.Text += sf.Direct_Method[i];
                }
                 * */
                show.Text = "success!!";
                me = sf.methodCfg;
                return true;
                //DrawGraph(me[3].xmlPath);
            }
            else
                return false;
            /*
            method mm = new method(show.Text.ToString());
            //show.AppendText("\n\n\n" + mm.methodContent);


            for (int i = 0; i < mm.LinkFunc.Count; i++)
            {
                //show.AppendText("\n [" + i + "]:" + mm.InstruBlock[i]);
                show.AppendText("\n link:" + mm.LinkFunc[i]);
            }
             * */
        }

        public void walkEveryXml()
        {
            if( me.Count > 0 )
                DrawGraph(me[me.Count-1].xmlPath);
        }

        public void DrawGraph(string xmlPath)
        {
            //使用之前，先全部清理掉
            diagram.ClearAll();
            var nodeMap = new Dictionary<string, DiagramNode>();
            var bounds = new Rect(30, 30, 100, 60);

            // load the graph xml
            var xml = XDocument.Load(xmlPath);
            var graph = xml.Element("Graph");

            // load node data
            var nodes = graph.Descendants("Node");
            foreach (var node in nodes)
            {
                var diagramNode = diagram.Factory.CreateShapeNode(bounds);
                nodeMap[node.Attribute("id").Value] = diagramNode;
                diagramNode.Text = node.Attribute("name").Value;
            }

            // load link data
            var links = graph.Descendants("Link");
            foreach (var link in links)
            {
                DiagramLink dl = diagram.Factory.CreateDiagramLink(
                    nodeMap[link.Attribute("origin").Value],
                    nodeMap[link.Attribute("target").Value]);
                dl.AddLabel(link.Attribute("label").Value);  //添加链接信息
            }
            
            // arrange the graph
            var layout = new LayeredLayout();
            layout.Arrange(diagram);
        }

        private void load_Click(object sender, RoutedEventArgs e)
        {
            if (!openFile())
            {
                MessageBox.Show("请选择文件！");
                return;
            }
            walkEveryXml();
            
        }
    }
}
