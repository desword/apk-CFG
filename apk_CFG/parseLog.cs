using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Xml.Linq;

namespace apk_CFG
{
    public class parseLog
    {
        public string LogPath;
        public string outputPath;
        public string outLogPath;//D:\Documents\GitHub\apk-CFG\apk_CFG\bin\Debug\smali\log
        public string MethodXmlPath;//D:\Documents\GitHub\apk-CFG\apk_CFG\bin\Debug\smali\[MainActivity]FOR()V.xml

        public List<string> CodeContent;//记录分析好的path编码
        public string parsedCode;//分析好的path路径   0->2->3->4
        public string id_exit;
        public int count;//文件名替补计数
        public XDocument xml;
        //public List<string> id_looptail;//loop尾的编号序列

        //logPath 待分析的文件路径:C:\Users\Administrator\Desktop\log.txt
        //outputPath 输出的分析好的xml路径D:\Documents\GitHub\apk-CFG\apk_CFG\bin\Debug\smali
        public parseLog(string logPath,string outputPath)
        {
            this.LogPath = logPath;
            this.outputPath = outputPath;
            this.CodeContent = new List<string>();
            this.parsedCode = "";
            this.count = 0;

            createLogFolder(this.outputPath);
            readLog(this.LogPath);
            getMethodPath();
            parseTheLog(this.MethodXmlPath);
            SaveXmlLog(this.outLogPath);
        }

        //将获取好的log信息存入xml文件中
        public void SaveXmlLog(string logPath)
        {
            //获取存储的文件名
            //xx_xx_xx
            string dateTime = "log" + DateTime.Now.Hour + "_" + DateTime.Now.Minute + "_" + DateTime.Now.Second;
            int beg,end;
            beg = this.MethodXmlPath.LastIndexOf("\\", this.MethodXmlPath.Length-1)+1;
            end = this.MethodXmlPath.LastIndexOf(".",this.MethodXmlPath.Length-1);
            string ClassMethod = this.MethodXmlPath.Substring(beg, end - beg);//[MainActivity]FOR()V
            string fileName = dateTime + ClassMethod + ".xml";
            if (fileName.Length > 120)
            {
                fileName = "~" + this.count + ".xml";
                this.count++;
            }
            //添加log信息到xml中
            this.xml = XDocument.Load(this.MethodXmlPath);
            XElement graph = this.xml.Element("Graph");
            var links = graph.Descendants("Link");
            string[] CodeSub = this.parsedCode.Split('|');
            string current = CodeSub[0], next = CodeSub[1];
            foreach (var link in links)//初始化log标志位
            {
                link.SetAttributeValue("log", 0);
            }
            int i;
            for (i = 1; i < CodeSub.Length; i++)
            {
                foreach (var link in links)
                {
                    if (link.Attribute("origin").Value.Equals(CodeSub[i - 1])
                        && link.Attribute("target").Value.Equals(CodeSub[i]))
                    {     
                        //单独设置log属性，标记为1，表示经过的路径 o(s1)(s2)(s3)表示经过的步骤                   
                        link.SetAttributeValue("log",link.Attribute("log").Value+"(s" + i + ")");
                    }                    
                }
            }
            this.xml.Save(logPath + "\\" + fileName);
        }

        //获取当前待分析序列的函数文件位置
        public void getMethodPath()
        {
            string methodName = "";
            foreach (string con in this.CodeContent)
                if (con.IndexOf("beg") != -1)
                {
                    methodName = con.Remove(0,3);
                    break;
                }
            this.MethodXmlPath = outputPath + "\\" + methodName + ".xml";
        }

        //读取xml文件开始根据编码进行分析
        //[暂时对一个函数内部，没有调用的情况进行分析]
        public void parseTheLog(string methodPath)
        {
            this.xml = XDocument.Load(methodPath);
            XElement graph = xml.Element("Graph");
            var links = graph.Descendants("Link");
            int numOfvirLink = parseTheLog_initInfo(links);
            var ret = graph.Descendants("Source");
            foreach (var rr in ret)
                this.id_exit = rr.Attribute("retNo").Value;//获取结束点的id

            foreach (string con in this.CodeContent)
            {
                if (con.IndexOf("beg") != -1) continue;//如果是开始标志，跳过
                if (con.IndexOf("end") != -1)
                {
                    string contmp = con.Remove(con.Length - 3, 3);
                    this.parsedCode += (parseTheLog_getcurrentSequence(graph, contmp, numOfvirLink));
                    break;//如果是结束标志，跳出
                } 
                this.parsedCode += ( parseTheLog_getcurrentSequence(graph, con, numOfvirLink) + "|") ;
            }
            //xml = null;
        }

        //loop尾的判断
        //[loop尾问题]：  1、代码块小，2、跳过去的是true label， 或者本身是if-else， label
        //[从下跳转到上面的时候，下一段代码才是if-else的情况]iterator
        //[普遍的问题：]if-else条件判断的内容是一个函数或者多个函数，那么直接的代码块将不是if-else
        public int isLoopTail(XElement link)
        {
            int ori, tar;
            ori = Int32.Parse(link.Attribute("origin").Value);//当前位置的代码号
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
                if (ll.Attribute("origin").Value.Equals(tar.ToString()))
                {
                    tarLabel = ll.Attribute("label").Value;
                    midTar = ll.Attribute("target").Value;
                }

            foreach (var ll in links)//目标块的间接下一个label
                if (ll.Attribute("origin").Value.Equals(midTar))
                    midTarLabel = ll.Attribute("label").Value;

            if (isObigT //代码块大于目标块
                && (tarLabel.Equals("True") || tarLabel.Equals("False")//目标块是ifelse判断
                || (tarLabel.Equals("OnReturn") && midTarLabel.Equals("True") || midTarLabel.Equals("False"))))//目标块间接判断
                return 1;
            if (isObigT //代码块大于目标块
                && (oarLabel.Equals("True") || oarLabel.Equals("False")))//当前块是ifelse判断
                return 2;
            return 0;//不是loop尾
        }

        //获取virtulink的个数
        public int parseTheLog_initInfo(IEnumerable<XElement> links)
        { 
            int count=0;
            foreach( var link in links)
            {
                if (isLoopTail(link) ==1 || isLoopTail(link) ==2)
                {
                    count++;
                    //this.id_looptail.Add(link.Attribute("origin").Value);//获取loop尾的id
                }                    
            }
            return count * 2;
                
        }

        //获取当前编码的path序列
        //[潜在问题，并不是获得相邻边里面最大的那个边//已修正，获得的都是最大的那个边]
        public string parseTheLog_getcurrentSequence(XElement graph, string pathCode, int numOfvirLink)
        {
            int maxEdgeValue = -1;
            string currentNodeId = "0";//从0开始
            string nextNodeId = "0";
            string Sequence = "0";//path序列
            int pathCodeInt = Int32.Parse(pathCode);

            int i;            
            //虚边就是抢开头或者结尾。 正常边要避开jmp边
            var links = graph.Descendants("Link");
            while (true)
            {
                //正常边判断
                foreach (var link in links)
                {
                    if ( isLoopTail(link)==0 && link.Attribute("origin").Value.Equals(currentNodeId)
                        && Int32.Parse(link.Attribute("wei").Value) > maxEdgeValue
                        && Int32.Parse(link.Attribute("wei").Value) <= pathCodeInt)
                    {
                        maxEdgeValue = Int32.Parse(link.Attribute("wei").Value);
                        nextNodeId = link.Attribute("target").Value;
                    }
                }

                for (i = 0; i < numOfvirLink; i++)
                {
                    var virs = graph.Descendants("virtuLink" + i);
                    foreach (var vir in virs)
                    {
                        if (vir.Attribute("origin").Value.Equals("0")
                              && Int32.Parse(vir.Attribute("wei").Value) > maxEdgeValue
                              && Int32.Parse(vir.Attribute("wei").Value) <= pathCodeInt)//如果虚边是可采纳的边，则从loop头开始出发
                        {
                            maxEdgeValue = Int32.Parse(vir.Attribute("wei").Value);
                            currentNodeId = nextNodeId = Sequence = vir.Attribute("target").Value;

                        }
                        else if (vir.Attribute("target").Value.Equals(this.id_exit)
                               && Int32.Parse(vir.Attribute("wei").Value) > maxEdgeValue
                               && Int32.Parse(vir.Attribute("wei").Value) <= pathCodeInt)//如果当前结点是loop尾，且去往exit的边刚好可以减去pathcode
                        {
                            return Sequence;//返回loop结点
                        }
                    }
                }
                pathCodeInt -= maxEdgeValue;
                maxEdgeValue = -1;
                if (nextNodeId.Equals(this.id_exit))//如果下一个结点是exit，则直接返回
                    return Sequence + "|" + nextNodeId;
                if (currentNodeId != nextNodeId)
                    Sequence += ("|" + nextNodeId);
                currentNodeId = nextNodeId;//移动到下一个结点
            }      

        }

        //创建log分析文件夹
        public void createLogFolder(string output)
        {
            this.outLogPath = output + "\\log";
            DirectoryInfo dir = new DirectoryInfo(this.outLogPath);
            if( !dir.Exists )
                dir.Create();
        }

        //读取文件
        public void readLog(string log_path)
        {
            StreamReader apkStreamReader = new StreamReader(log_path);
            while (!apkStreamReader.EndOfStream)
            {
                string strReadLine = apkStreamReader.ReadLine(); //读取每行数据
                string[] lineSub = strReadLine.Split(' ');
                this.CodeContent.Add(lineSub[lineSub.Length-1]);//将最后的数字等数据输入
            }
            // 关闭读取流文件
            apkStreamReader.Close();
        }

        ~parseLog()
        { }
    }
}
