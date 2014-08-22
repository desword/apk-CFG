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
using System.Windows.Threading;

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
        public string smaliPath;
        //整个apk，反编译后smali文件夹解决方案
        public string destPath;
        public apkOfAllSmali allSmali = null;
        public string outputPath;//最终分析结果目录  D:\Documents\GitHub\apk-CFG\apk_CFG\bin\Debug\smaliDF
        public string outFileName;//建立的文件夹名称  smaliDF
        public List<SmaliMeta> listViewContent;
        //分析xml文件
        public string xmlPath;
        //线程
        private Thread updateListView = null;
        private Thread updatePgr = null;
        private Thread updateSelect = null;//选择项目处理的线程
        private bool isOk = false;//是否处理完成的标记

        public MainWindow()
        {
            InitializeComponent();
            diagram.Behavior = Behavior.Modify;//设置不能新建结点
            overview1.Document = diagram;//设置overview的指向
            overview1.FitAll = true;//设置overview显示全部diagram
            outputPath = AppDomain.CurrentDomain.BaseDirectory + outFileName;//获取程序启动的路径
            listViewContent = new List<SmaliMeta>();
        }
       
        //读入单个的smali文件，并分析
        private void load_Click(object sender, RoutedEventArgs e)
        {
            smaliPath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c://";
            openFileDialog.Filter = "All File|*.*|smali File|*.smali";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == true)
                smaliPath =  openFileDialog.FileName;

            if (smaliPath == "")
            {
                MessageBox.Show("请选择文件！");
                return;
            }
            listView1.Items.Clear();
            listViewContent.Clear();
            int extendindex = smaliPath.LastIndexOf(".", smaliPath.Length - 1);
            int FolderNameINdex = smaliPath.LastIndexOf("\\", smaliPath.Length - 1) + 1;
            outFileName = smaliPath.Substring(FolderNameINdex, extendindex - FolderNameINdex);
            if (!Directory.Exists(outputPath + outFileName))//如果不存在单个的smali分析文件，则分析
                new smaliFile(smaliPath, outputPath);
            walkEveryXml(outputPath + outFileName);
        }

        //读入apk反编译的整个项目文件并分析
        private void openFileFold_Click(object sender, RoutedEventArgs e)
        {
            destPath = "";
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
                destPath = ofd.FileName;
            }
            if (destPath == "")
            {
                MessageBox.Show("请选择文件夹！！");
                return;
            }
            listView1.Items.Clear();
            listViewContent.Clear();

            int FolderNameINdex = destPath.LastIndexOf("\\", destPath.Length - 1) + 1;
            //判断是否是文件夹
            if (Directory.Exists(destPath))
            {
                outFileName = destPath.Substring(FolderNameINdex, destPath.Length - FolderNameINdex);
                if (!Directory.Exists(outputPath + outFileName))//如果不存在文件夹，则分析整个文件夹
                    anaSmaliFolder();
                walkEveryXml(outputPath + outFileName);
                btn_instrument.IsEnabled = true;
            }
            //ergodicFile();
            //anaSmaliFolder();            
        }
        
        //读入已经分析好的xml文件，解读
        private void load_xml_Click(object sender, RoutedEventArgs e)
        {
            xmlPath = "";
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.InitialDirectory = "c://";
            openFileDialog.Filter = "All File|*.*|xml File|*.xml";
            openFileDialog.RestoreDirectory = true;
            openFileDialog.FilterIndex = 1;
            if (openFileDialog.ShowDialog() == true)
                xmlPath = openFileDialog.FileName;
            if (xmlPath == "")
            {
                MessageBox.Show("请选择文件！");
                return;
            }
            listView1.Items.Clear();
            listViewContent.Clear();
            int extendindex = xmlPath.LastIndexOf(".", xmlPath.Length - 1);
            int FolderNameINdex = xmlPath.LastIndexOf("\\", xmlPath.Length - 1) + 1;
            outFileName = xmlPath.Substring(FolderNameINdex, extendindex - FolderNameINdex);
            readSinglexml(outFileName, xmlPath);//直接分析xml文件
        }

        //文件拖拽的监听事件，并且分别分析
        private void drag_enter(object sender, DragEventArgs e)
        {
            destPath = ((System.Array)e.Data.GetData(DataFormats.FileDrop)).GetValue(0).ToString();
            if (destPath == "")
            {
                MessageBox.Show("发生了一些错误！");
                return;
            }
            listView1.Items.Clear();
            listViewContent.Clear();
            int extendindex = destPath.LastIndexOf(".", destPath.Length - 1);
            int FolderNameINdex = destPath.LastIndexOf("\\", destPath.Length - 1) + 1;
            //判断是否是文件夹
            if (Directory.Exists(destPath))
            {
                outFileName = destPath.Substring(FolderNameINdex, destPath.Length - FolderNameINdex);
                if (!Directory.Exists(outputPath + outFileName))//如果不存在文件夹，则分析整个文件夹
                    anaSmaliFolder();
                walkEveryXml(outputPath + outFileName);
                btn_instrument.IsEnabled = true;
            }
            else if (extendindex != -1 && destPath.Substring(extendindex, destPath.Length - extendindex) == ".smali")
            {
                outFileName = destPath.Substring(FolderNameINdex, extendindex - FolderNameINdex);
                if (!Directory.Exists(outputPath + outFileName))//如果不存在单个的smali分析文件，则分析
                    new smaliFile(destPath, outputPath);
                listView1.Items.Clear();
                listViewContent.Clear();
                walkEveryXml(outputPath + outFileName);
            }
            else if (extendindex != -1 && destPath.Substring(extendindex, destPath.Length - extendindex) == ".xml")
            {
                listView1.Items.Clear();
                listViewContent.Clear();
                outFileName = destPath.Substring(FolderNameINdex, extendindex - FolderNameINdex);
                readSinglexml(outFileName, destPath);//直接分析xml文件
            }
            //对log文件的处理
            else if (extendindex != -1 && destPath.Substring(extendindex, destPath.Length - extendindex) == ".txt")
            {
                parseLog log = new parseLog(destPath, outputPath + outFileName);
                walkEveryXml(log.outLogPath);

            }
            else
            {
                MessageBox.Show("请选择.smali格式文件、.xml格式文件或者apk反编译后的文件夹！");
            }
        }

        //遍历文件夹下所有的xml文件，将条目按方法添加到listview中
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
                    else if (d.Substring(d.LastIndexOf(".", d.Length - 1), d.Length - d.LastIndexOf(".", d.Length - 1)) == ".xml")   //如果是xml文件，则加入到listview中
                    {                        
                        FNindex = d.IndexOf(outFileName);
                        FNindex = FNindex + outFileName.Length + 1;
                        getFileName = d.Substring(FNindex, d.Length - FNindex);                        
                        
                        SmaliMeta smTmp = new SmaliMeta(getFileName, d);
                        listViewContent.Insert(0, smTmp);
                        listView1.Items.Insert(0, smTmp.showName);
                    }
                }
            }  
        }

        //读入单个的xml文件，并进行解读
        private void readSinglexml(string showName, string path)
        {
            SmaliMeta smTmp = new SmaliMeta(showName, path);
            listViewContent.Insert(0, smTmp);
            listView1.Items.Insert(0, smTmp.showName);
            DrawGraph(path,false);
        }

        //根据xml绘制CFG
        public void DrawGraph(object xmlPaths,bool isLog)
        {
            //使用之前，先全部清理掉
            string xmlPath = xmlPaths as string;
            string retu_id="";

            diagram.ClearAll();
            diagram.LinkHeadShape = ArrowHeads.PointerArrow;//设置连线箭头的类型
            GlassEffect effect = new GlassEffect();
            effect.Type = GlassEffectType.Type4;//设置结点的玻璃效果
            effect.GlowColor = Colors.Black;
            diagram.NodeEffects.Add(effect);

            var nodeMap = new Dictionary<string, DiagramNode>();
            var bounds = new Rect(30, 30, 10, 2);

            // load the graph xml
            var xml = XDocument.Load(xmlPath);
            var graph = xml.Element("Graph");

            // load node data
            var sours = graph.Descendants("Source");
            foreach (var sour in sours)//获取exit的结点
                retu_id = sour.Attribute("retNo").Value;

            var nodes = graph.Descendants("Node");
            foreach (var node in nodes)
            {
                var diagramNode = diagram.Factory.CreateShapeNode(bounds);

                nodeMap[node.Attribute("id").Value] = diagramNode;
                diagramNode.Text = node.Attribute("name").Value;
                //--调整结点大小以显示全部内容，必须放在设置了内容值之后
                diagramNode.ResizeToFitText(FitSize.KeepRatio);
                diagramNode.TextAlignment = TextAlignment.Left;
            }

            //设置特殊结点的颜色
            ShapeNode s2 = (ShapeNode)nodeMap["0"];//起点位置是绿色
            s2.Brush = Brushes.LightGreen;
            s2 = (ShapeNode)nodeMap[retu_id];//终止位置是红色
            s2.Brush = Brushes.Red;

            // load link data
            Style linkStyle = new Style();
            linkStyle.Setters.Add(new Setter(DiagramLink.BrushProperty, Brushes.Red));//log信息的颜色标记

            var links = graph.Descendants("Link");
            foreach (var link in links)
            {
                DiagramLink dl = diagram.Factory.CreateDiagramLink(
                    nodeMap[link.Attribute("origin").Value],
                    nodeMap[link.Attribute("target").Value]);
                
                if( link.Attribute("label").Value.Equals("True"))//为ifelse 标记形状
                {
                    ShapeNode s = (ShapeNode)nodeMap[link.Attribute("origin").Value];
                    s.Shape = Shapes.Decision;
                    //s.Brush = Brushes.RoyalBlue;
                    s.TextAlignment = TextAlignment.Center;
                }
                
                //----------log采集的信息显示                
                if (isLog && !link.Attribute("log").Value.Equals("0"))
                {
                    dl.Style = linkStyle;                    
                    string logShow = link.Attribute("log").Value.Remove(0, 1);
                    dl.AddLabel(link.Attribute("label").Value + "--"+logShow); //显示运行的步骤信息
                    dl.IntermediateShape = ArrowHeads.PointerArrow;
                }
                else
                    dl.AddLabel(link.Attribute("label").Value);  //添加链接信息
                    

                //diagram.DiagramLinkStyle = linkStyle;
                //Brush a = new Brush();
                //a.
                //dl.HeadPen.Brush = Brush;
            }
            
            // arrange the graph
            var layout = new MindFusion.Diagramming.Wpf.Layout.DecisionLayout();
            //layout.IgnoreNodeSize = false;//使得结点不会覆盖显示 
            layout.StartNode = nodeMap["0"];
            layout.Arrange(diagram);//自动布局结点

        }
              
        //#rigion for analysis smali folders
        //分析smali文件夹
        private void anaSmaliFolder()
        {
            show.Visibility = Visibility.Visible;
            //show.Text = "正在分析中...";
            updateListView = new Thread(ergodicFile);
            updateListView.Start();
            updatePgr = new Thread(doWork);
            updatePgr.Start();
        }
        //多线程，遍历所有smali文件
        private void ergodicFile()
        {
            allSmali = new apkOfAllSmali(destPath);
            outFileName = allSmali.FName;

            listView1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                new Action<string>(walkEveryXml), outputPath + outFileName);
            isOk = true;
            updateListView.Abort();
            
        }
        //多线程，更新进度条
        private void doWork()
        {
            int count = 0;  
            while(true)
            {
                show.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                    new Action<int>(chgShow), count);
                progressBar1.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                    new Action<int>(pgr), count);
                
                count++;
                Thread.Sleep(10);                              
            }            
        }
        protected void pgr(int count)
        {
            if (count == 0)//第一次载入，则显示进度条
            {
                progressBar1.Visibility = Visibility.Visible;
                progressBar1.Value = 0;
            }                
            progressBar1.Value++;
            if (isOk == true)//当处理完毕，就隐藏进度条
            {
                progressBar1.Visibility = Visibility.Hidden;
                updatePgr.Abort();
                isOk = false;
            }                
            if (progressBar1.Value == progressBar1.Maximum)//如果进度条达到最大值
                progressBar1.Value = 0;
        }
        //更新显示信息
        private void chgShow(int count)
        {
            string[] showtxt = { "正在分析中.", "正在分析中..", "正在分析中..." };
            if( count == 0)//第一次载入，显示文本
                show.Visibility = Visibility.Visible;
            if (isOk == false)
            {
                show.Text = showtxt[count % 3];
            }
            if (isOk == true)
                show.Visibility = Visibility.Hidden;
        }

        //listview变更的监听事件
        private void lv_selectionChanged(object sender, SelectionChangedEventArgs e)
        {            
            if (listView1.SelectedIndex != -1)
            {
                isOk = false;
                updatePgr = new Thread(doWork);
                updatePgr.Start();
                updateSelect = new Thread(new ParameterizedThreadStart(threadDraw));
                updateSelect.Start(listViewContent[listView1.SelectedIndex].smaliPath);//绘制CFG的线程              

            }                
        }
        private void threadDraw(object path)
        {
            string pp = path as string;
            bool islogs = false;
            int index = pp.LastIndexOf("\\", pp.Length - 1) + 1;
            if (pp.Substring(index, 3).Equals("log")) islogs = true;//判断选择是否经过处理的log
            this.diagram.Dispatcher.BeginInvoke(DispatcherPriority.SystemIdle,
                   new Action<object, bool>(DrawGraph), path as string, islogs);
            isOk = true;
            //updatePgr.Abort();            
        }

        private void windowClosed(object sender, EventArgs e)
        {
            if (updateListView.ThreadState == ThreadState.Running)
                updateListView.Abort();
            if (updatePgr.ThreadState == ThreadState.Running)
                updatePgr.Abort();
            if (updateSelect.ThreadState == ThreadState.Running)
                updateSelect.Abort();
        }

        //执行对项目的log语句注入
        private void btn_instrument_Click(object sender, RoutedEventArgs e)
        {
            if (this.outputPath != "")
            {
                isOk = false;
                updatePgr = new Thread(doWork);
                updatePgr.Start();
                Thread instr = new Thread(Instrument_thread);
                instr.Start();                
            }
        }
        private void Instrument_thread()
        {
            new InstrumentSmali(this.outputPath + this.outFileName);
            isOk = true;
            MessageBox.Show("完成对apk项目的插桩，可以进行重编译获取运行的记录信息了！");
        }

        private void zoomIn_Click(object sender, RoutedEventArgs e)
        {
            diagram.ZoomFactor = Math.Min(1000, diagram.ZoomFactor + 10);
        }

        private void zoomOut_Click(object sender, RoutedEventArgs e)
        {
            diagram.ZoomFactor = Math.Max(20, diagram.ZoomFactor - 10);
        }

    }
}
