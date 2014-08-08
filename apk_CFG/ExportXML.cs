using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;

namespace apk_CFG
{
    public class ExportXML
    {
        public static void exportXML(string outputPath,string FileName,List<string> Node,List<string>  LinkNode)
        {
            XmlDocument xml = new XmlDocument();

            xml.AppendChild(xml.CreateXmlDeclaration("1.0", "UTF-8", null));
            XmlElement graph = xml.CreateElement("Graph");
            XmlElement nodes = xml.CreateElement("Nodes");
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
                nodes.AppendChild(node);
            }
            xml.AppendChild(graph);
            graph.AppendChild(nodes);
            if (LinkNode.Count != 1)//如果不止一个代码块
            {
                XmlElement links = xml.CreateElement("Links");
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
                    links.AppendChild(link);
                }
                graph.AppendChild(links);
            }
            xml.Save(outputPath + FileName + ".xml");
        }
    }
}
