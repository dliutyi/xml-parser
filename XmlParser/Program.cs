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
        static string ToString(XmlNode xmlNode, string emp = "")
        {
            if (xmlNode == null) return "";
            else if (xmlNode.IsValueOnly) return $"{ emp }{ xmlNode.Value }\n";

            string xml = $"{ emp }<{ xmlNode.Name }";
            xml += xmlNode.Attributes.Count > 0 
                ? $" { string.Join(' ', xmlNode.Attributes.Select(attr => attr.HasValue ? $"{attr.Name}=\"{attr.Value}\"" : attr.Name)) }"
                : "";
            return xmlNode.CanHaveChildren
                ? $"{ xml }>\n{ xmlNode.Children.Select(child => ToString(child, emp + "   ")).Aggregate((childXml, next) => childXml + next) }{ emp }</{ xmlNode.Name }>\n"
                : $"{ xml }/>\n";
        }

        static void Main(string[] args)
        {
            var xml = File.ReadAllText(@"file.xml");

            Logger.Log("XML PARSER");
            var xmlLexicalParser = new XmlFile();
            var xmlRoot = xmlLexicalParser.Parse(xml);
            Logger.Log("");
            if (xmlRoot != null) {
                xmlRoot.Children.ForEach(x => Logger.Log(ToString(x)));
            }
        }
    }
}
