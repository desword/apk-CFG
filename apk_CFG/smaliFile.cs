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
        public string SuperClassName;
        public string SourceName;
        public List<string> Annotations;
        public List<string> Instance;
        public List<string> Static;
        public List<string> Interfaces;
        public List<string> Direct_Method;
        public List<string> Virt_Method;
        public List<method> methodCfg;//每个分析好的函数

        public string smali_path;
        public string FileContent;
        public string ClassFilePath;

        public smaliFile(string smapath,string ClassFilePath)
        {
            this.smali_path = smapath;
            this.ClassFilePath = ClassFilePath;
            this.ClassName = this.SourceName = this.SuperClassName = this.FileContent = "";
            
            Annotations = new List<string>();
            Instance = new List<string>();
            Static = new List<string>();
            Interfaces = new List<string>();
            Direct_Method = new List<string>();
            Virt_Method = new List<string>();
            methodCfg = new List<method>();

            readFile();
            anaCurrentClass();
            anaAnnotations();
            anaStatic();
            anaInstance();
            anaInterface();
            anaDirect();
            anaVirtual();
            anaEachMethod();
        }

        //对每个方法创建对应的分析库
        //[警告]缺少对存在文件夹的分析
        public void anaEachMethod()
        {
            int index = smali_path.LastIndexOf("\\", smali_path.Length - 1);
            int endindex = smali_path.LastIndexOf(".", smali_path.Length - 1) + 1;
            string ClassName = smali_path.Substring(index, endindex - index);
            DirectoryInfo dir = new DirectoryInfo(ClassFilePath + "\\" + ClassName);
            dir.Create();
            foreach (string direcMethod in Direct_Method)
            {
                methodCfg.Add(new method(direcMethod, ClassFilePath + "\\" + ClassName+"\\"));
            }
            foreach (string virtuMethod in Virt_Method)
            {
                methodCfg.Add(new method(virtuMethod, ClassFilePath + "\\" + ClassName+"\\"));
            }
        }

        //read smali file
        public void readFile()
        {
            StreamReader apkStreamReader = new StreamReader(this.smali_path);
            while (!apkStreamReader.EndOfStream)
            {
                string strReadLine = apkStreamReader.ReadLine(); //读取每行数据
                this.FileContent += (strReadLine + "\n");
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

        //分析# virtual methods
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
                    index = this.FileContent.IndexOf(".method", end, finalend - end);
                }
            }
        }
    }
}
