using System;
using System.Xml;
using System.Xml.Linq;

namespace lab2
{
    class Program
    {
        static void Main(string[] args)
        {
            //filename = "input.xml"
            var filename = args[0];
            var expr = GetExpressionFromMathML(filename);
            XDocument xdoc = new XDocument();
            xdoc.Add(expr);
            xdoc.Save("modified.xml");

            //ExpressionToTree(Simplify(expr));
            Console.ReadKey();
        }

        private static XmlElement GetExpressionFromMathML(string filename)
        {
            var xDoc = new XmlDocument();
            xDoc.Load(filename);
            var xRoot = xDoc.DocumentElement;
            return xRoot;
        }

        private static XmlElement Simplify (XmlElement expr)
        {
            return expr;
        }

        private static void ExpressionToTree(XmlElement expr)
        {
            Console.WriteLine("chlen");
        }
    }
}