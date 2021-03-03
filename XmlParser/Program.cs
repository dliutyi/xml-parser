using System;
using System.IO;
using System.Linq;

namespace XmlParser
{
    static class Logger
    {
        static public void Log(string str)
        {
            Console.WriteLine(str);
        }
    }

    class Program
    {
        static string ToString(XmlNode xmlNode, int spaces = 0)
        {
            if (xmlNode == null)
            {
                return "";
            }

            int tab = 3;
            string emp = new string(' ', spaces * tab);
            if (xmlNode.IsValueOnly)
            {
                return $"{ emp }{ xmlNode.Value }\n";
            }

            string xml = $"{ emp }<{ xmlNode.Name }";

            string attrs = string.Join(' ', xmlNode.Attributes.Select(attr => attr.HasValue ? $"{attr.Name}=\"{attr.Value}\"" : attr.Name));
            if (attrs.Length > 0)
            {
                xml += $" { attrs }";
            }

            if (xmlNode.CanHaveChildren)
            {
                xml += ">\n";
                foreach (var child in xmlNode.Children)
                {
                    xml += ToString(child, spaces + 1);
                }
                xml += $"{ emp }</{ xmlNode.Name }>\n";
            }
            else
            {
                xml += " />\n";
            }
            return xml;
        }

        static void Main(string[] args)
        {
            var xml = File.ReadAllText(@"file.xml");

            Logger.Log("XML PARSER");
            var xmlLexicalParser = new LexicalQueue();
            var xmlRoot = xmlLexicalParser.Parse(xml);
            Logger.Log("");
            if (xmlRoot != null) {
                xmlRoot.Children.ForEach(x => Logger.Log(ToString(x)));
            }
        }
    }
}
