using System;
using System.Text;
using System.Xml;
using System.IO;
using TidyNet;

namespace ImportContentFromRss
{
    static class Utilities
    {
        public const String XhtmlNamespace = "http://www.w3.org/1999/xhtml";
        public static String ConvertHtmlToXhtml(String source)
        {
            MemoryStream input = new MemoryStream(Encoding.UTF8.GetBytes(source));
            MemoryStream output = new MemoryStream();

            TidyMessageCollection tmc = new TidyMessageCollection();
            Tidy tidy = new Tidy();
            
            tidy.Options.DocType = DocType.Omit;
            tidy.Options.DropFontTags = true;
            tidy.Options.LogicalEmphasis = true;
            tidy.Options.Xhtml = true;
            tidy.Options.XmlOut = true;
            tidy.Options.MakeClean = true;
            tidy.Options.TidyMark = false;
            tidy.Options.NumEntities = true;
            

            tidy.Parse(input, output, tmc);

            XmlDocument x = new XmlDocument();
            XmlDocument xhtml = new XmlDocument();
            xhtml.LoadXml("<body />");
            XmlNode xhtmlBody = xhtml.SelectSingleNode("/body");

            x.LoadXml(Encoding.UTF8.GetString(output.ToArray()));
            XmlAttribute ns = x.CreateAttribute("xmlns");
            ns.Value = XhtmlNamespace;
            XmlNode body = x.SelectSingleNode("/html/body");
            foreach (XmlNode node in body.ChildNodes)
            {
                if (node.NodeType == XmlNodeType.Element)
                    node.Attributes.Append(ns);

                xhtmlBody.AppendChild(xhtml.ImportNode(node, true));
            }
            return xhtmlBody.InnerXml;
        }
        public static string ConvertToXmlDate(DateTime dateTime)
        {
            return dateTime.Equals(DateTime.MinValue) ? DateTime.Now.ToString("yyyy-MM-ddTHH:mm:ss") : dateTime.ToString("yyyy-MM-ddTHH:mm:ss");
        }
        public static void Log(string message)
        {
            Console.WriteLine(string.Format("[{0}] {1}", DateTime.Now.ToString("HH:mm:ss.fff"), message));
        }
    }
}
