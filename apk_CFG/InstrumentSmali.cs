﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;
using System.Xml;

namespace apk_CFG
{
    public class InstrumentSmali
    {
        //[问题]：仅凭跳转是jmp， 或者跳转的代码块小于当前的代码块无法准确的判断是否是loop
        //解决： 两个条件，代码块小于当前；  跳转过去的结点是ifelse结点， 或者本身是if-else， label
        public string cfgPath;
        public List<string> smaliFileContent;//smali文件的内容,  index对应行号[不知是否有未解决的牵扯的问题]
        //public string smaliFileContent;
        XDocument xml;//xml内容记录
        public List<string> reverNodes;//逆序分析的结点  
        public List<string> virtuLink;//为for循环添加的虚边:  original|target|wei
        public bool isFirstIns;
        public string SmaliNameTojudge;

        //---四个变量名
        public string vPathId;       //计数变量
        public string vIncreMent;    //增量中间量
        public string vMethodTag;    //方法头和尾的字符串量
        public string vStrBuilder;   //SB 连接字符串的中间量

        //---xml文件有用信息
        public string SmaliClassSourcePath;//smali文件的源路径:  C:\Users\Administrator\Desktop\smaliDF\smali\com\example\smali\MainActivity.smali
        public string PreSmaliClassSourcePath;//前面一个smali文件的路径
        public int method_beg;//方法的头与尾的行号
        public int method_end;
        public int locals_num;//locals 的个数以及行号
        public int locals_hang;
        public int return_no;// return的node索引
        public int Node_num;//代码块，结点的个数
        public string SmaliClassName;//文件名 即类名:MainActivity
        public string MethodName;//正在运行的方法名: FOR()V
        

        //CfgFolderPath:  D:\Documents\GitHub\apk-CFG\apk_CFG\bin\Debug\smaliDF
        public InstrumentSmali(string CfgFolderPath)
        {
            this.cfgPath = CfgFolderPath;
            this.smaliFileContent = new List<string>();//每个文件，每个类。初次对一个项目进行插桩时，初始化
            this.isFirstIns = true;//第一次载入文件进行分析

            ergodicEveryXml(this.cfgPath);
            this.xml = null;
        }

        //遍历每个xml文件，并且分析
        public void ergodicEveryXml(string CFGpath)
        {
            if (System.IO.Directory.Exists(CFGpath))//xml,cfg分析结果文件
            {
                foreach (string d in System.IO.Directory.GetFileSystemEntries(CFGpath))
                {
                    this.reverNodes = new List<string>();//初始化逆向分析结点库--每个方法
                    this.virtuLink = new List<string>();//每个方法
                    //this.smaliFileContent = new List<string>();//每个文件，每个类

                    extraXmlInfo(d);//提取xml信息
                    //如果没有相关信息，则直接返回
                    if (this.return_no == -1 || this.method_beg == -1 || this.method_end == -1
                        || this.locals_hang == -1 || this.locals_num == -1) break;
                    if (this.isFirstIns)//如果是第一次载入，则确定当前的类名为初始类名
                    {
                        this.SmaliNameTojudge = this.SmaliClassName;
                        justInstrument_loadsmali();//每个文件，每个类. 载入文件内容
                        this.isFirstIns = false;
                    }
                    else if (!this.SmaliNameTojudge.Equals(this.SmaliClassName))//如果下一个要分析的类与当前类不一样
                    {
                        SaveSamli(this.PreSmaliClassSourcePath);//存储前面处理好的smali文件
                        this.PreSmaliClassSourcePath = this.SmaliClassSourcePath;//前一个文件的路径设置为当前路径
                        this.SmaliNameTojudge = this.SmaliClassName;//记录新的待分析的类
                        this.smaliFileContent = new List<string>();//申请新的内容
                        justInstrument_loadsmali();//载入新的内容
                    }

                    reverseNode();//将结点逆向排序
                    dealWithLoop();//处理循环，添加head and tail边
                    calcNodeandEdgeValue();//计算每条边以及点的weight
                    
                    justInstrument();//代码插桩
                    SaveXml(d);
                }
                SaveSamli(this.SmaliClassSourcePath);//将最后一个smali文件存储
            }
        }

        //存储smali的instument文件
        //smaliPath: C:\Users\Administrator\Desktop\smaliDF\smali\com\example\smali\MainActivity.smali
        public void SaveSamli(string smaliPath)
        {
            //写入smali
            using (StreamWriter file = new StreamWriter(smaliPath))
            {
                foreach (string line in this.smaliFileContent)
                    file.WriteLine(line);
            } 
        }

        //存储xml文件
        //xmlPath: D:\Documents\GitHub\apk-CFG\apk_CFG\bin\Debug\smaliDF\[MainActivity]FOR()V.xml 
        public void SaveXml(string xmlPath)
        {
            XElement graph = this.xml.Element("Graph");
            string[] virSub;
            int i;
            for (i = 0; i < this.virtuLink.Count; i++)
            {
                graph.SetElementValue("virtuLink"+i, 0);
                var virlinks = graph.Descendants("virtuLink"+i);
                foreach (var virlink in virlinks)//加入添加的dummy边
                {
                    virSub = this.virtuLink[i].Split('|');
                    virlink.SetAttributeValue("origin", virSub[0]);
                    virlink.SetAttributeValue("target", virSub[1]);
                    virlink.SetAttributeValue("wei", virSub[2]);
                }
            }                
            //写入xml
            this.xml.Save(xmlPath);
        }

 
        //loop尾的判断
        //[loop尾问题]：  1、代码块小，2、跳过去的是true label， 或者本身是if-else， label
        //[从下跳转到上面的时候，下一段代码才是if-else的情况]iterator
        //[普遍的问题：]if-else条件判断的内容是一个函数或者多个函数，那么直接的代码块将不是if-else
        public int isLoopTail(XElement link)
        {
            int ori, tar;
            ori = Int32.Parse( link.Attribute("origin").Value);//当前位置的代码号
            tar = Int32.Parse(link.Attribute("target").Value);//目标位置的代码号
            string tarLabel = "";//目标位置的label
            string oarLabel = link.Attribute("label").Value;//当前位置的label
            XElement graph = this.xml.Element("Graph");
            var links = graph.Descendants("Link");
            bool isObigT = false;
            foreach (var ll in links)//判断出发结点是否可能是loop尾结点
            {
                if (ll.Attribute("origin").Value.Equals(ori.ToString())
                    && Int32.Parse(ll.Attribute("origin").Value) > Int32.Parse(ll.Attribute("target").Value))
                {
                    isObigT = true;
                    break;
                }            
            }
            string midTar = "";
            string midTarLabel = "";
            foreach (var ll in links)//目标块的直接label
                if(ll.Attribute("origin").Value.Equals(tar.ToString()))
                {
                    tarLabel = ll.Attribute("label").Value;
                    midTar = ll.Attribute("target").Value;
                }

            foreach (var ll in links)//目标块的间接下一个label
                if (ll.Attribute("origin").Value.Equals(midTar))
                    midTarLabel = ll.Attribute("label").Value;

            if (isObigT //代码块大于目标块
                && (tarLabel.Equals("True") || tarLabel.Equals("False")//目标块是ifelse判断
                || (tarLabel.Equals("OnReturn") && midTarLabel.Equals("True") || midTarLabel.Equals("False") )))//目标块间接判断
                return 1;
            if (isObigT //代码块大于目标块
                && (oarLabel.Equals("True") || oarLabel.Equals("False")))//当前块是ifelse判断
                return 2;
            return 0;//不是loop尾
        }



        //归约出结点分析序列
        //应对exit 和循环结点分离的情况，一个if把两个代码块分离开了
        //加入结点之前，判断加入的点是否有额外的[没有加入rever序列]的分支，
        //一旦有分支，则立刻抛弃所有等待加入的点，寻找一个[没有加入]过的jmp，ori加入。
        //[loop尾问题]：  1、代码块小，2、跳过去的是true label， 或者本身是if-else， label
        //[考虑移除代码部分]
        public void reverseNode()
        {            
            //XDocument xtmp = toXDocument(this.xml);
            XElement graph = this.xml.Element("Graph");
            var links = graph.Descendants("Link");
            this.reverNodes.Add(this.return_no+"");//加入exit结点
            List<int> tmpRe;//暂时存储符合条件的点            
            
            string searchNumer = this.return_no+"";
            int poiIndex = 1;//当前扫描的结点位置
            int ori, tar;//------用于判断loop尾
            while (this.reverNodes.Count < this.Node_num-1 )//一直加入到只剩下entry结点
            {
                tmpRe = new List<int>();
                foreach (var link in links)//加入所有当前结点的前继结点, 且不是loop尾的结点
                {
                    if (link.Attribute("target").Value.Equals(searchNumer) && isLoopTail(link)!=1 )
                        tmpRe.Add(Int32.Parse(link.Attribute("origin").Value));  
                    //对于非loop的jmp的处理. 如果jmp指向的是exit
                    //[考虑移除]----------------
                    //else if (link.Attribute("target").Value.Equals(this.return_no + ""))
                     //   tmpRe.Add(Int32.Parse(link.Attribute("origin").Value));  
                }
                if (tmpRe.Count == 0) break;//如果没有前继结点，说明没有此图中没有连线
                tmpRe.Sort( (x,y) => y-x );//从大到小排列
                bool isFindloop = false;
                //int ori, tar;
                foreach (int x in tmpRe)
                {
                    //寻找当前点是否有未加入rever序列的出度点
                    foreach (var link in links)
                    {
                        //寻找待加入的点的出度点不在reve序列中的情况
                        if (link.Attribute("origin").Value.Equals(x + "") 
                            && this.reverNodes.IndexOf(link.Attribute("target").Value) == -1)
                        {
                            foreach (var link2 in links)
                            {
                                ori = Int32.Parse(link2.Attribute("origin").Value);
                                tar = Int32.Parse(link2.Attribute("target").Value);
                                //寻找loop尾，且未在reve序列中的结点
                                if (isLoopTail(link2) == 1 || isLoopTail(link2) == 2 && this.reverNodes.IndexOf(link2.Attribute("origin").Value) == -1)                                   
                                {
                                    this.reverNodes.Add(link2.Attribute("origin").Value);                                    
                                    break;//找到了可用的loop尾，就加入并跳出标记
                                }
                            }
                            isFindloop = true;//即使没有找到可用的loop，尾，也标记，然后跳出
                        }
                        if (isFindloop == true) break;//如果有未加入reve序列的出度点的点跳出
                    }
                    if (isFindloop == true) break;
                    if (this.reverNodes.IndexOf(x + "")==-1 && x!=0)//当前结点没有添加，  且不是entry结点
                        this.reverNodes.Add(x + "");
                }
                searchNumer = this.reverNodes[poiIndex++]; 
            }
            this.reverNodes.Add(0 + "");
                
        }

        //分析是否有for循环，有则进行加边，去边处理
        //直接添加链接head和tail的边即可，   去边直接检索jmp的label即可
        //[目前把有jmp标记的都认为是有for循环--有问题--正在解决]
        //[加入isLoopTail判断函数]
        public void dealWithLoop()
        {
            //XDocument xtmp = toXDocument(this.xml);
            XElement graph = this.xml.Element("Graph");
            var links = graph.Descendants("Link");
            foreach (var link in links)
            {
                if ( isLoopTail(link)==1 || isLoopTail(link)==2 )//判断当前是loop尾
                {
                    this.virtuLink.Add(link.Attribute("origin").Value + "|" + this.return_no);//loop尾到exit的虚边
                    this.virtuLink.Add("0|" + link.Attribute("target").Value);//entry到loop头的虚边
                }
            }
        }
              

        //按照结点序列，依次分析结点的值，以及边的值
        //并且直接将边的值依次赋值到新的xml对应的link中
        //dummy边的值先暂时存储在 virtulink中
        //[加入loop判断]
        public void calcNodeandEdgeValue()
        {
            //XDocument xtmp = toXDocument(this.xml);
            XElement graph = this.xml.Element("Graph");
            var links = graph.Descendants("Link");

            int i,j;
            string[] elesub;//dummy边的数组
            int[] NodeWeight = new int[this.Node_num];//每个结点的weight[是否初始化为0]
            NodeWeight[this.return_no] = 1;//exit结点的weight为1

            for(i=1 ; i<this.reverNodes.Count ; i++)
            {
                foreach (var link in links)//遍历每条非[jmp] loop backedge   的 原始边
                {
                    if (link.Attribute("origin").Value.Equals(this.reverNodes[i])
                          && isLoopTail(link)==0 )//寻找当前结点的出度边，且不是backedge边
                    {
                        link.SetAttributeValue("wei", NodeWeight[Int32.Parse(this.reverNodes[i])]);
                        NodeWeight[Int32.Parse(this.reverNodes[i])] += NodeWeight[Int32.Parse(link.Attribute("target").Value)];//更新结点weight
                        //XAttribute weis = new XAttribute();
                        //wei = this.xml.cre
                    }
                }
                for (j = 0; j < this.virtuLink.Count;j++ )//遍历每条新增的 dummy边
                {
                    elesub = this.virtuLink[j].Split('|');
                    if (elesub[0].Equals(this.reverNodes[i]))
                    {
                        this.virtuLink[j] += ("|" + NodeWeight[Int32.Parse(this.reverNodes[i])]);//为dummy边赋值
                        NodeWeight[Int32.Parse(this.reverNodes[i])] += NodeWeight[Int32.Parse(elesub[1])];
                    }
                }
            }
        }

        //对具有权值>0的边进行插桩操作
        //[loop尾的判断]
        public void justInstrument()
        {
            
            justInstrument_methodHead();
            justInstrument_methodTail();

            XElement graph = this.xml.Element("Graph");
            var links = graph.Descendants("Link");

            foreach (var link in links)
            {
                if (isLoopTail(link) == 1 || isLoopTail(link) == 2)//处理loop尾的jmp边
                {
                    justInstrument_loop(link);
                }
                else if(link.Attribute("label").Value.Equals("False"))//处理if边，
                {
                    justInstrument_inc(link);
                }
            }
        }

        //载入源smali文件
        public void justInstrument_loadsmali()
        { 
            StreamReader apkStreamReader = new StreamReader(this.SmaliClassSourcePath);
            while (!apkStreamReader.EndOfStream)
            {
                string strReadLine = apkStreamReader.ReadLine(); //读取每行数据
                this.smaliFileContent.Add(strReadLine);//index对应行号，内容就是对应行的内容
            }
            // 关闭读取流文件
            apkStreamReader.Close();
        }

        //构造方法头的instrument
        //包括修改.locals , 初始化pathId，  声明[类名][方法]的开始
        public void justInstrument_methodHead()
        { 
            //修改locals
            int loNum = this.locals_num + 4 ;//多四个局部变量。 
            this.smaliFileContent[this.locals_hang] = ".locals " + loNum + "\n";
            //初始化pathId记录数器
            this.smaliFileContent[this.method_beg] += ("\nconst/4 "+ this.vPathId + ", 0x0\n");
            //声明[类名][方法]的开始.  --beg[类名]方法名--
            this.smaliFileContent[this.method_beg] +=
                ("const-string " + this.vMethodTag + ", \"beg[" + this.SmaliClassName + "]" + this.MethodName + "\"\n"
                +"invoke-static {" + this.vMethodTag + ", " + this.vMethodTag + @"}, Landroid/util/Log;->i(Ljava/lang/String;Ljava/lang/String;)I"+"\n");
        }

        //构造方法尾的instrument
        //输出对应的计数值，以及end结束符号. 插入的输出，在return的前面
        public void justInstrument_methodTail()
        {
            string endTmp = this.smaliFileContent[this.method_end];//暂时存储return
            this.smaliFileContent[this.method_end] =
                "\nnew-instance "+ this.vStrBuilder +@", Ljava/lang/StringBuilder;" + "\n"
                + "invoke-static {" + this.vPathId + @"}, Ljava/lang/String;->valueOf(I)Ljava/lang/String;"+"\n"
                + "move-result-object "+ this.vPathId +"\n"
                + "invoke-direct {" +this.vStrBuilder +", "+ this.vPathId + @"}, Ljava/lang/StringBuilder;-><init>(Ljava/lang/String;)V" + "\n"
                + "const-string "+ this.vMethodTag +", \"end\"\n"
                + "invoke-virtual {"+ this.vStrBuilder +", "+ this.vMethodTag + @"}, Ljava/lang/StringBuilder;->append(Ljava/lang/String;)Ljava/lang/StringBuilder;"+"\n"
                + "move-result-object "+ this.vStrBuilder +"\n"
                + "invoke-virtual {"+ this.vStrBuilder + @"}, Ljava/lang/StringBuilder;->toString()Ljava/lang/String;"+"\n"
                + "move-result-object "+ this.vStrBuilder +"\n"
                + "invoke-static {"+this.vMethodTag +", " + this.vStrBuilder + @"}, Landroid/util/Log;->i(Ljava/lang/String;Ljava/lang/String;)I"+"\n";
            this.smaliFileContent[this.method_end] += endTmp;
        }

        //构造loop尾的instrument
        // r += 指向尾部的wei;  output(r) ; r = 头指向loop头的边;
        public void justInstrument_loop(XElement link)
        {
            XElement graph = this.xml.Element("Graph");
            var links = graph.Descendants("Node");
            string loopHeadId = link.Attribute("target").Value;
            string loopTailId = link.Attribute("origin").Value;
            int gotoHang=0;
            string gotoTmp= "";
            foreach( var ll in links)//搜索loop尾行的位置
            {
                if (ll.Attribute("id").Value.Equals(loopTailId))
                {
                    gotoHang = Int32.Parse(ll.Attribute("hang").Value);
                    gotoTmp = this.smaliFileContent[gotoHang];//暂时存储loop尾行的内容
                    break;
                }
            }
            //获取loop头和loop尾的inc
            string headIncre = "",tailIncre = "";
            string[] virSub;
            foreach (string vir in this.virtuLink)
            { 
                virSub = vir.Split('|');
                if (virSub[0].Equals(loopTailId))//如果当前边是 tail->exit
                    tailIncre = virSub[2];
                else if(virSub[1].Equals(loopHeadId))//如果当前边是 entry->head
                    headIncre = virSub[2];
            }
            this.smaliFileContent[gotoHang] = "";//首先归零loop尾行的数据
            //1、如果tailinc不是0，就进行加值. 在goto之前添加:  vPathId += tailIncre
            if (!tailIncre.Equals("0"))
            {
                this.smaliFileContent[gotoHang] =
                      "\nconst/16 "+ this.vIncreMent +", "+tailIncre + "\n" 
                    + "add-int "+ this.vPathId +", "+ this.vPathId +", "+ this.vIncreMent +"\n";
            }
            //2、输出一次循环的计数值
            this.smaliFileContent[gotoHang] +=
                ("\ninvoke-static {"+ this.vPathId +"}, Ljava/lang/String;->valueOf(I)Ljava/lang/String;\n"
                  +  "move-result-object "+ this.vIncreMent + "\n"
                  +  "invoke-static {"+ this.vIncreMent +", "+ this.vIncreMent +"}, Landroid/util/Log;->i(Ljava/lang/String;Ljava/lang/String;)I\n");
                //("\ninvoke-static {" + this.vPathId + ", " + this.vPathId + @"}, Landroid/util/Log;->i(Ljava/lang/String;Ljava/lang/String;)I" + "\n");
            //3、给vPathId赋予新的headinc的值
            this.smaliFileContent[gotoHang] +=
                  ( "const/16 "+ this.vPathId +", "+ headIncre +"\n"  );
            //4、赋予loop尾内容
            this.smaliFileContent[gotoHang] += gotoTmp;
        }

        //构造一般增量的instrument
        //获取origin， 找到行号， 如果是if， 在后面插入inc； 
        //[可能存在，关键字没有对应行号的情况。 一般是没有的]
        public void justInstrument_inc(XElement link)
        {
            XElement graph = this.xml.Element("Graph");
            var links = graph.Descendants("Node");
            string ori = link.Attribute("origin").Value;
            int oriHang=0;
            string increm = link.Attribute("wei").Value;//需要增加的值
            foreach (var ll in links)//寻找origin对应的行号
                if (ll.Attribute("id").Value.Equals(ori))
                {
                    oriHang = Int32.Parse(ll.Attribute("hang").Value);
                    break;
                }
            this.smaliFileContent[oriHang] += 
                    ( "\nconst/16 "+ this.vIncreMent +", "+ increm +"\n" +
                      "add-int "+ this.vPathId +", "+ this.vPathId +", "+ this.vIncreMent +"\n" );
        }

        //提取xml文件的source, local
        public void extraXmlInfo(string CFGxmlpath)
        {
            //this.xml = new XmlDocument();
            //this.xml.Load(CFGxmlpath);
            //XDocument xtmp = toXDocument(this.xml);
            this.xml = XDocument.Load(CFGxmlpath);
            XElement graph = xml.Element("Graph");
            IEnumerable<XElement>  sources= graph.Descendants("Source");
            foreach (XElement ss in sources)
            {
                this.SmaliClassSourcePath = ss.Attribute("path").Value;
                this.method_beg = Int32.Parse(ss.Attribute("beg").Value);
                this.method_end = Int32.Parse(ss.Attribute("end").Value);
                this.return_no = Int32.Parse(ss.Attribute("retNo").Value);
                this.Node_num = Int32.Parse(ss.Attribute("noNum").Value);
            }
            //如果是第一次载入，存储当前的路径
            if( this.isFirstIns )
                this.PreSmaliClassSourcePath = this.SmaliClassSourcePath;
            IEnumerable<XElement> loca = graph.Descendants("locals");
            foreach (XElement ll in loca)
            {
                this.locals_hang = Int32.Parse(ll.Attribute("hang").Value);
                this.locals_num = Int32.Parse(ll.Attribute("num").Value);
            }
            //构造四个变量的名称
            int numTmp = this.locals_num;
            this.vPathId = "v" + numTmp;
            numTmp++;
            this.vIncreMent = "v" + numTmp;
            numTmp++;
            this.vMethodTag = "v" + numTmp;
            numTmp++;
            this.vStrBuilder = "v" + numTmp;
            //提取类名，方法名
            int index,endindex;
            index = this.SmaliClassSourcePath.LastIndexOf("\\", this.SmaliClassSourcePath.Length)+1;
            endindex = this.SmaliClassSourcePath.LastIndexOf(".",this.SmaliClassSourcePath.Length);
            this.SmaliClassName = this.SmaliClassSourcePath.Substring(index, endindex-index);
            IEnumerable<XElement> nodes = graph.Descendants("Node");
            foreach(var node in nodes)
            {
                if(node.Attribute("id").Value.Equals("0"))
                {
                    string block = node.Attribute("name").Value;
                    endindex = block.IndexOf("\n");
                    string[] blockSub = block.Substring(0,endindex).Split(' ');
                    this.MethodName = blockSub[blockSub.Length-1];
                }
            }
        }

        public XmlDocument toXmlDocument(XDocument x)
        {
            var xmlDocu = new XmlDocument();
            using (var xmlreader = x.CreateReader())
                xmlDocu.Load(xmlreader);
            return xmlDocu;
        }

        public XDocument toXDocument(XmlDocument xml)
        {
            using (var nodeReader = new XmlNodeReader(xml))
            {
                nodeReader.MoveToContent();
                return XDocument.Load(nodeReader);            
            }
        }




        ~InstrumentSmali()
        { }
    }
}
