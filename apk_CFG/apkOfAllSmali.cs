﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;

namespace apk_CFG
{
    public class apkOfAllSmali
    {
        public string inputFilePath;
        public string outputFilePath;
        public string FName;
        public List<smaliFile> AllSmaliFile;
        public List<string> ConstomNode;//对自定义函数构造结点.[类名]->[函数名]. MethodCrossLink将加入系统函数
        public int countOfConstomNode;//自定义函数的个数
        public List<string> CrossMethodNode;//结点连接信息。 起点|终点|显示信息（systemcall, call）

        public apkOfAllSmali(string desPath)
        {
            this.inputFilePath = desPath;
            AllSmaliFile = new List<smaliFile>();
            ConstomNode = new List<string>();
            CrossMethodNode = new List<string>();

            CreatoutputFolder();
            WalkAllSmaliFile(inputFilePath + "\\smali");//直接进入smali文件夹
            CreateConstmNode();
            MethodCrossLink();
            ExportXML.exportXML(outputFilePath, FName,ConstomNode, CrossMethodNode);
        }

        //创建分析的目标文件夹
        //【缺少对已经存在文件夹的判断】
        public void CreatoutputFolder()
        {
            int NameINdex = inputFilePath.LastIndexOf("\\", inputFilePath.Length - 1);
            FName = inputFilePath.Substring(NameINdex, inputFilePath.Length - NameINdex);
            //outputFilePath = Directory.GetCurrentDirectory() + "\\" + FName;
            outputFilePath = AppDomain.CurrentDomain.BaseDirectory + "\\" + FName;
            /*
            this.storeFName = this.FName.Replace("<", "%");
            this.storeFName = this.FName.Replace(">", "%");
            this.storeFName = this.FName.Replace("/", "%");
             * */
            DirectoryInfo dir = new DirectoryInfo(outputFilePath);
            dir.Create();            
        }

        //遍历文件夹下面每个smali文件，构造smaliFile。
        public void WalkAllSmaliFile(string inputPath)
        {
            int isAndroid = inputPath.LastIndexOf("\\", inputPath.Length - 1);
            string mayBeandroid = inputPath.Substring(isAndroid, inputPath.Length - isAndroid);
            //如果存在这个文件夹，遍历其下的每个文件,并且跳过android系统文件夹
            if (System.IO.Directory.Exists(inputPath) && mayBeandroid != "\\android")    
            {
                foreach (string d in System.IO.Directory.GetFileSystemEntries(inputPath))
                {
                    int dotIndex = d.LastIndexOf(".", d.Length - 1);
                    if (System.IO.Directory.Exists(d))//如果当前的是文件夹，则递归
                        WalkAllSmaliFile(d);
                    else if (dotIndex != -1 && d.Substring(dotIndex, d.Length - dotIndex) == ".smali")//如果是smali文件，则建立smaliFile类型
                        AllSmaliFile.Add(new smaliFile(d, outputFilePath));              
                        
                    //{
                    //    ThreadStart starter = delegate { doAddsmali(d); };
                    //    new Thread(starter).Start();
                    //}
                          
                }                
            }   
        }

        private void doAddsmali(string d)
        {
            AllSmaliFile.Add(new smaliFile(d, outputFilePath));
        }

        //构造交叉函数引用的结点，结点都为用户自定义函数
        public void CreateConstmNode()
        {
            string nodeName;
            string ClaNameTmp;
            foreach (smaliFile smTmp in AllSmaliFile)
            {
                foreach (method methodTmp in smTmp.methodCfg)
                {
                    ClaNameTmp = smTmp.ClassName;
                    if (ClaNameTmp.IndexOf("\n") != -1)//移除“\n”符号
                        ClaNameTmp = ClaNameTmp.Remove(ClaNameTmp.Length - 1, 1);
                    nodeName = ClaNameTmp + "->" + methodTmp.methodName+"\n";
                    ConstomNode.Add(nodeName);
                }
            }
            countOfConstomNode = ConstomNode.Count;
        }
    
        //分析每个函数中的invoke，构造函数交叉调用图
        public void MethodCrossLink()
        {
            string invokeMethod;
            string[] isInvokeSub;
            int endId,begId=0;
            foreach (smaliFile smTmp in AllSmaliFile)
            {
                foreach (method methodTmp in smTmp.methodCfg)
                {
                    foreach (string isInvoke in methodTmp.LinkTail)
                    {
                        if (isInvoke.IndexOf("invoke-") != -1)
                        {
                            isInvokeSub = isInvoke.Split(' ');
                            invokeMethod = isInvokeSub[isInvokeSub.Length - 1];//获取调用的函数名称
                            endId = ConstomNode.IndexOf(invokeMethod);

                            if (endId < countOfConstomNode && endId >= 0)//如果是用户自定义函数
                                CrossMethodNode.Add(begId + "|" + endId + "|call");
                            else if (endId >= countOfConstomNode )//如果系统函数中有
                            {
                                CrossMethodNode.Add(begId + "|" + endId + "|systemcall");
                            }
                            else//没有存入库中的系统函数
                            {
                                ConstomNode.Add(invokeMethod);
                                endId = ConstomNode.Count - 1;
                                CrossMethodNode.Add(begId + "|" + endId + "|systemcall");
                            }
                        }
                    }
                    begId++;
                }
            }
        }
    }
}