﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace apk_CFG
{
    public class ExportXML
    {
        public static int count = 1;

        //添加插桩辅助信息的xml
        //SourceSmaliandBE:  sourcePath|beg|end|retNo|nodeNUm
        //Locals: content|hang
        //Node:   name|[hang]-- 可选的
        public static void exportXML(string outputPath, string FileName, List<string> Node, List<string> LinkNode, string SourceSmaliandBE, string Locals)
        {
            XmlDocument xml = new XmlDocument();

            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "UTF-8", null));
            XmlElement graph = xml.CreateElement("Graph");
            //XmlElement nodes = xml.CreateElement("Nodes");

            //--添加sourcepath信息
            int i;
            XmlElement source;
            XmlAttribute path, beg, end,retno,nodeNum;
            String[] sourceSub = SourceSmaliandBE.Split('|');
            for (i = 0; i < 5 - sourceSub.Length; i++)//处理分组的个数小于5的情况
            {
                SourceSmaliandBE += ("|-1");
            }
            sourceSub = SourceSmaliandBE.Split('|');
            source = xml.CreateElement("Source");
            path = xml.CreateAttribute("path");
            path.Value = sourceSub[0];
            beg = xml.CreateAttribute("beg");
            beg.Value = sourceSub[1];
            
            end = xml.CreateAttribute("end");//因此end域的形式为 end|
            end.Value = sourceSub[2];
            retno = xml.CreateAttribute("retNo");//添加return结点的下标
            retno.Value = sourceSub[3];
            nodeNum = xml.CreateAttribute("noNum");//结点的个数
            nodeNum.Value = sourceSub[4];
            /*
            for (i = 3; i < sourceSub.Length; i++ )
                end.Value += ("|" + sourceSub[i]);
            */
            source.Attributes.Append(path);
            source.Attributes.Append(beg);
            source.Attributes.Append(end);
            source.Attributes.Append(retno);
            source.Attributes.Append(nodeNum);
            graph.AppendChild(source);

            //---添加locals信息
            XmlElement loca;
            XmlAttribute number, hanglca;
            if (Locals == null) Locals = "-1|-1|-1";//对absrtact方法的处理
            String[] Localsub = Locals.Split('|');
            loca = xml.CreateElement("locals");
            number = xml.CreateAttribute("num");
            number.Value = Localsub[0];
            hanglca = xml.CreateAttribute("hang");
            hanglca.Value = Localsub[1];
            loca.Attributes.Append(number);
            loca.Attributes.Append(hanglca);
            graph.AppendChild(loca);

            //---添加node信息
            XmlElement node;
            XmlAttribute id, name,hang;
            String[] nodesub;
            for (i = 0; i < Node.Count; i++)
            {
                node = xml.CreateElement("Node");
                id = xml.CreateAttribute("id");
                id.Value = i + "";
                name = xml.CreateAttribute("name");
                nodesub = Node[i].Split('|');
                name.Value = nodesub[0];
                if (nodesub.Length > 1)//如果有行数，就把对应的行数添加进去
                {
                    hang = xml.CreateAttribute("hang");
                    hang.Value = nodesub[1];
                    node.Attributes.Append(hang);
                }
                node.Attributes.Append(id);
                node.Attributes.Append(name);
                graph.AppendChild(node);
                //nodes.AppendChild(node);
            }            
            //graph.AppendChild(nodes);

            //---添加edge信息
            if (LinkNode.Count != 1)//如果不止一个代码块
            {
                //XmlElement links = xml.CreateElement("Links");
                XmlElement link;
                XmlAttribute origin, target, label;
                for (i = 0; i < LinkNode.Count; i++)
                {
                    string[] llkk = LinkNode[i].Split('|');
                    link = xml.CreateElement("Link");
                    origin = xml.CreateAttribute("origin");
                    origin.Value = llkk[0];
                    target = xml.CreateAttribute("target");
                    target.Value = llkk[1];
                    label = xml.CreateAttribute("label");
                    label.Value = llkk[2];
                    link.Attributes.Append(origin);
                    link.Attributes.Append(target);
                    link.Attributes.Append(label);
                    //links.AppendChild(link);
                    graph.AppendChild(link);
                }
                //graph.AppendChild(links);
            }
            xml.AppendChild(graph);
            if (FileName.Length > 120)
            {
                FileName = "~" + count;
                count++;
            }
            xml.Save(outputPath + FileName + ".xml");
        }

        //纯cfg分析xml
        /*
        public static void exportXML(string outputPath,string FileName,List<string> Node,List<string>  LinkNode)
        {
            XmlDocument xml = new XmlDocument();

            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "UTF-8", null));
            XmlElement graph = xml.CreateElement("Graph");
            //XmlElement nodes = xml.CreateElement("Nodes");
            XmlElement node;
            XmlAttribute id, name;
            int i;
            for (i = 0; i < Node.Count; i++)
            {
                node = xml.CreateElement("Node");
                id = xml.CreateAttribute("id");
                id.Value = i + "";
                name = xml.CreateAttribute("name");
                name.Value = Node[i];
                node.Attributes.Append(id);
                node.Attributes.Append(name);
                graph.AppendChild(node);
                //nodes.AppendChild(node);
            }
            xml.AppendChild(graph);
            //graph.AppendChild(nodes);
            if (LinkNode.Count != 1)//如果不止一个代码块
            {
                //XmlElement links = xml.CreateElement("Links");
                XmlElement link;
                XmlAttribute origin, target, label;
                for (i = 0; i < LinkNode.Count; i++)
                {
                    string[] llkk = LinkNode[i].Split('|');
                    link = xml.CreateElement("Link");
                    origin = xml.CreateAttribute("origin");
                    origin.Value = llkk[0];
                    target = xml.CreateAttribute("target");
                    target.Value = llkk[1];
                    label = xml.CreateAttribute("label");
                    label.Value = llkk[2];
                    link.Attributes.Append(origin);
                    link.Attributes.Append(target);
                    link.Attributes.Append(label);
                    //links.AppendChild(link);
                    graph.AppendChild(link);
                }
                //graph.AppendChild(links);
            }
            if (FileName.Length > 120)
            {
                FileName = "~" + count;
                count++;
            }                
            xml.Save(outputPath + FileName + ".xml");
        }
        */
        
    }
}
