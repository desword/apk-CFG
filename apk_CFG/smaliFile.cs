using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Text.RegularExpressions;

namespace apk_CFG
{
    public class smaliFile
    {
        public string ClassName;//类名称：Lcom/droider/circulate/MainActivity;
        public string StoreClassName;//smali 文件的名称  MainAcivity
        public string SuperClassName;//父类名称
        public string SourceName;  //source 位的内容
        public List<string> Annotations;//注释分析
        public List<string> Instance;//实例分析
        public List<string> Static;//静态域分析
        public List<string> Interfaces;//接口分析
        //public List<string> Direct_Method;
        //public List<string> Virt_Method;
        public List<string> All_Method;//所有的方法
        public List<method> methodCfg;//每个分析好的函数

        public string smali_path; //smali文件位置  C:\Users\Administrator\Desktop\smaliF\smali\com\example\smali\MainAcivity.smali
        public string FileContent;
        public string ClassFilePath;//输出文件夹 D:\Documents\GitHub\apk-CFG\apk_CFG\bin\Debug\smaliFile
        public List<int> method_hang;//每个方法的起始位置行数

        public smaliFile(string smapath,string ClassFilePath)
        {
            this.smali_path = smapath;
            this.ClassFilePath = ClassFilePath;
            this.StoreClassName = this.ClassName = this.SourceName = this.SuperClassName = this.FileContent = "";
            
            Annotations = new List<string>();
            Instance = new List<string>();
            Static = new List<string>();
            Interfaces = new List<string>();
            //Direct_Method = new List<string>();
           // Virt_Method = new List<string>();
            methodCfg = new List<method>();
            All_Method = new List<string>();
            method_hang = new List<int>();

            readFile();
            anaCurrentClass();
            //anaAnnotations();
            //anaStatic();
            //anaInstance();
            //anaInterface();
            //anaDirect();
            //anaVirtual();
            anaAllMethod();
            anaEachMethod();
        }

        //对每个方法创建对应的分析库
        //[警告]缺少对存在文件夹的分析        
        public void anaEachMethod()
        {
            int index = smali_path.LastIndexOf("\\", smali_path.Length - 1)+1;
            int endindex = smali_path.LastIndexOf(".", smali_path.Length - 1);
            StoreClassName = smali_path.Substring(index, endindex - index);
            //DirectoryInfo dir = new DirectoryInfo(ClassFilePath + "\\" + StoreClassName);
            //dir.Create();
            int i;
            for (i = 0; i < All_Method.Count; i++ )
                methodCfg.Add(new method(All_Method[i], ClassFilePath + @"\[" + StoreClassName + "]", this.smali_path, method_hang[i]));//单文件的存储方式
            

            /*
            foreach (string direcMethod in Direct_Method)
            {
                //methodCfg.Add(new method(direcMethod, ClassFilePath + "\\" + StoreClassName + "\\"));//多文件的存储方式
                methodCfg.Add(new method(direcMethod, ClassFilePath + @"\[" + StoreClassName +"]", this.smali_path));//单文件的存储方式
            }
            foreach (string virtuMethod in Virt_Method)
            {
                //methodCfg.Add(new method(virtuMethod, ClassFilePath + "\\" + StoreClassName + "\\"));
                methodCfg.Add(new method(virtuMethod, ClassFilePath + @"\[" + StoreClassName + "]", this.smali_path));
            }
             * */
        }

        //read smali file
        public void readFile()
        {
            StreamReader apkStreamReader = new StreamReader(this.smali_path);
            int count = 1;
            while (!apkStreamReader.EndOfStream)
            {
                string strReadLine = apkStreamReader.ReadLine(); //读取每行数据
                this.FileContent += (strReadLine + "\n");
                if (strReadLine.IndexOf(".method") != -1)//记录每个方法的起始行数，从1开始
                    method_hang.Add(count);
                count++;
            }
            // 关闭读取流文件
            apkStreamReader.Close();
        }

        //分析smali文件头部
        public void anaCurrentClass()
        {
            int index = this.FileContent.IndexOf(".class");
            int end = 0;
            if (index != -1)
            {
                end = this.FileContent.IndexOf("\n");
                index += 7;
                this.ClassName = this.FileContent.Substring(index, end - index);
                string[] classSub = ClassName.Split(' ');
                ClassName = classSub[classSub.Length-1];
            }
            end = index;
            index = this.FileContent.IndexOf(".super", index);
            if (index != -1)
            {
                end = this.FileContent.IndexOf("\n", index);
                index += 7;
                this.SuperClassName = this.FileContent.Substring(index, end - index);
            }
            end = index;
            index = this.FileContent.IndexOf(".source", index);
            if (index != -1)
            {
                end = this.FileContent.IndexOf("\n", index);
                index += 8;
                this.SourceName = this.FileContent.Substring(index, end - index);
            }
            //this.startIndex = end;
        }

        //分析annotations--  [暂时忽略方法与字段的注释]
        public void anaAnnotations()
        {
            int index = 0;
            int end = index;
            int finalend = index;
            index = this.FileContent.IndexOf("# annotations", index);
            //在# annotations内才检索
            if (index != -1)
            {
                finalend = this.FileContent.IndexOf("# ", index + 1);
                finalend = finalend != -1 ? finalend : FileContent.Length;//读到文件尾的处理
                index = this.FileContent.IndexOf(".annotation", index, finalend - index);
                //循环获取annotations
                while (index != -1)
                {
                    end = this.FileContent.IndexOf(".end annotation", index);
                    index += 12;
                    string tmp = this.FileContent.Substring(index, end - index);
                    this.Annotations.Add(tmp);
                    index = this.FileContent.IndexOf(".annotation", end, finalend - end);
                }
            }

        }

        //分析static fields
        public void anaStatic()
        {
            int index, end, finalend;
            index = this.FileContent.IndexOf("# static fields");
            if (index != -1)
            {
                finalend = this.FileContent.IndexOf("# ", index + 1);
                finalend = finalend != -1 ? finalend : FileContent.Length;
                index = this.FileContent.IndexOf(".field", index, finalend - index);
                while (index != -1)
                {
                    end = this.FileContent.IndexOf("\n", index);
                    index += 7;
                    string tmp = this.FileContent.Substring(index, end - index);
                    Static.Add(tmp);
                    index = this.FileContent.IndexOf(".field", end, finalend - end);
                }
            }
        }

        //分析 instance fields
        public void anaInstance()
        {
            int index, end, finalend;
            index = this.FileContent.IndexOf("# instance fields");
            if (index != -1)
            {
                finalend = this.FileContent.IndexOf("# ", index + 1);
                finalend = finalend != -1 ? finalend : FileContent.Length;
                index = this.FileContent.IndexOf(".field", index, finalend - index);
                //循环获取# instance fields范围内的.field
                while (index != -1)
                {
                    end = this.FileContent.IndexOf("\n", index);
                    index += 7;
                    string tmp = this.FileContent.Substring(index, end - index);
                    Instance.Add(tmp);
                    index = this.FileContent.IndexOf(".field", end, finalend - end);
                }
            }
        }

        //分析interfaces
        public void anaInterface()
        {
            int index, end, finalend;
            index = this.FileContent.IndexOf("# interfaces");
            if (index != -1)
            {
                finalend = this.FileContent.IndexOf("# ", index + 1);
                finalend = finalend != -1 ? finalend : FileContent.Length;
                index = this.FileContent.IndexOf(".implements", index, finalend - index);
                while (index != -1)
                {
                    end = this.FileContent.IndexOf("\n", index);
                    index += 12;
                    string tmp = this.FileContent.Substring(index, end - index);
                    Interfaces.Add(tmp);
                    index = this.FileContent.IndexOf(".implements", end, finalend - end);
                }
            }
        }

        //分析# direct methods
        /*
        public void anaDirect()
        {
            int index, end, finalend;
            index = this.FileContent.IndexOf("# direct methods");
            if (index != -1)
            {
                finalend = this.FileContent.IndexOf("# ", index + 1);
                finalend = finalend != -1 ? finalend : FileContent.Length;
                index = this.FileContent.IndexOf(".method", index, finalend - index);
                while (index != -1)
                {
                    end = this.FileContent.IndexOf(".end method", index);
                    index += 8;
                    string tmp = this.FileContent.Substring(index, end - index);
                    this.Direct_Method.Add(tmp);
                    index = this.FileContent.IndexOf(".method", end, finalend - end);
                }
            }
        }
        */
        //分析# virtual methods
        /*
        public void anaVirtual()
        {
            int index, end, finalend;
            index = this.FileContent.IndexOf("# virtual methods");
            if (index != -1)
            {
                finalend = this.FileContent.IndexOf("# ", index + 1);
                finalend = finalend != -1 ? finalend : FileContent.Length;
                index = this.FileContent.IndexOf(".method", index, finalend - index);
                while (index != -1)
                {
                    end = this.FileContent.IndexOf(".end method", index);
                    index += 8;
                    string tmp = this.FileContent.Substring(index, end - index);
                    this.Virt_Method.Add(tmp);
                    if (finalend < end) finalend = this.FileContent.IndexOf("# ", end);//如果匹配到的是注释符号，则继续向下面搜索
                    index = this.FileContent.IndexOf(".method", end, finalend - end);
                }
            }
        }
        */
        public void anaAllMethod()
        {
            int index, end;
            index = this.FileContent.IndexOf(".method");
            while (index != -1)
            {
                end = this.FileContent.IndexOf(".end method", index);
                index += 8;
                string tmp = this.FileContent.Substring(index, end - index);
                this.All_Method.Add(tmp);
                index = this.FileContent.IndexOf(".method", end);
            }            
        }

    }
}
