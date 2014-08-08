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
using System.Threading;

namespace apk_CFG
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        public class SmaliMeta
        {
            public string showName;
            public string smaliPath;
            public SmaliMeta(string SN, string SP)
            {
                showName = SN;
                smaliPath = SP;
            }
        }
        //单独的smali文件解析方案
        public smaliFile sf;
        public List<method> me;        
        public string smaliPath;
        //整个apk，反编译后smali文件夹解决方案
        public string destPath;
        public apkOfAllSmali allSmali;
        public string outputPath;//最终分析结果目录
        public string outFileName;//建立的文件夹名称
        public List<SmaliMeta> listViewContent;

        //线程
        private Thread updateListView = null;
        private Thread updatePgr = null;
        private bool isOk = false;
        private delegate void UpdatePBarUI();

        public MainWindow()
        {
            InitializeComponent();

            listViewContent = new List<SmaliMeta>();
        }

        public string openFile()
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c://";
            openFileDialog.Filter = "All File|*.*|apk File|*.apk";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == true)
            {
                return openFileDialog.FileName;
                //show.Text = fName;
                //sf = new smaliFile(fName, "e:");
                //show.Text = sf.SourceName;
                //show.Text += sf.Static.Count;
                /*
                for( int i=0 ;i< sf.Direct_Method.Count ;i++)
                {
                    show.Text += sf.Direct_Method[i];
                }
                 * */
                
                //me = sf.methodCfg;
                //show.Text = me[me.Count - 1].methodName;
                //return true;
                //DrawGraph(me[3].xmlPath);
            }            
                return "";
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
        public string openFolder()
        {
            OpenFileDialog ofd = new OpenFileDialog();
            ofd.Filter = "folders|*.sado;fjsdal;fj";
            ofd.FileName = "\r";
            ofd.CheckFileExists = false;
            ofd.CheckPathExists = true;
            ofd.ValidateNames = false;
            ofd.Title = "请到目标文件夹下，点击空白部分选择目标文件夹,切勿直接单击文件夹";
            if (ofd.ShowDialog() == true)
            {
                ofd.FileName = ofd.FileName.TrimEnd('\r');
                int lastSeparatorIndex = ofd.FileName.LastIndexOf('\\');
                ofd.FileName = ofd.FileName.Remove(lastSeparatorIndex);
                return ofd.FileName;
            }
            return "";
        }
        private void load_Click(object sender, RoutedEventArgs e)
        {
            smaliPath = openFile();
            if (smaliPath == "")
            {
                MessageBox.Show("请选择文件！");
                return;
            }
            sf = new smaliFile(smaliPath, "e:\\");
            me = sf.methodCfg;

        }
        
        //遍历每个smali文件，并且把对应的路径添加到listview中
        public void walkEveryXml(string inputPath)
        {
            string getFileName;
            int FNindex;
            //如果存在这个文件夹，遍历其下的每个文件
            if (System.IO.Directory.Exists(inputPath) )
            {
                foreach (string d in System.IO.Directory.GetFileSystemEntries(inputPath))
                {
                    if (System.IO.Directory.Exists(d))//如果当前的是文件夹，则递归
                        walkEveryXml(d);
                    else//如果是xml文件，则加入到listview中
                    {
                        FNindex = d.IndexOf(outFileName);
                        FNindex = FNindex + outFileName.Length + 1;
                        getFileName = d.Substring(FNindex, d.Length - FNindex);
                        SmaliMeta smTmp = new SmaliMeta(getFileName, d);
                        //listView1.Items.Add(smTmp);
                        listViewContent.Insert(0, smTmp);
                        listView1.Items.Insert(0, smTmp.showName);
                    }
                }
            }  
        }

        public void DrawGraph(string xmlPath)
        {
            //使用之前，先全部清理掉
            diagram.ClearAll();
            var nodeMap = new Dictionary<string, DiagramNode>();
            var bounds = new Rect(30, 30, 300, 50);

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
          
        private void openFileFold_Click(object sender, RoutedEventArgs e)
        {
            destPath = openFolder();
            if (destPath == "")
            {
                MessageBox.Show("请选择文件夹！！");
                return;
            }

            listView1.Items.Clear();
            listViewContent.Clear();
            //ergodicFile();
            
            updateListView = new Thread(ergodicFile);
            if (updateListView.ThreadState == ThreadState.Running)
                updateListView.Abort();
            updateListView.Start();

            /*
            updatePgr = new Thread(doWork);
            if (updatePgr.ThreadState == ThreadState.Running)
                updatePgr.Abort();
            updatePgr.Start();*/
            //DrawGraph(outputPath + "\\Circulate.xml");
        }

        //多线程，更新进度条
        private void doWork()
        {
            UpdatePBarUI up = new UpdatePBarUI(pgr);
            Dispatcher.Invoke(up);
        }
        protected void pgr()
        {
            int count = 0;
            progressBar1.Visibility = Visibility.Visible;
            progressBar1.Value = 0;
            while (isOk == false)
            {
                if (allSmali.AllSmaliFile.Count > count)
                {
                    count = allSmali.AllSmaliFile.Count;
                    progressBar1.Value++;
                }
            }
            progressBar1.Visibility = Visibility.Hidden;
        }

        //多线程，遍历所有smali文件
        private void ergodicFile()
        {
            allSmali = new apkOfAllSmali(destPath);

            outputPath = allSmali.outputFilePath;
            outFileName = allSmali.FName;
            isOk = true;
            walkEveryXml(outputPath);
        }

        private void lv_selectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (listView1.SelectedIndex != -1)
                DrawGraph(listViewContent[listView1.SelectedIndex].smaliPath);
        }
    }
}
