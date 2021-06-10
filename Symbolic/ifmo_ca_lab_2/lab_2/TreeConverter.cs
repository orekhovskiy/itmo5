using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace ShiftCo.ITMO.CA.Lab_2
{
    static class TreeConverter
    {

        #region TagNames
        private const string OPERATOR = "mo";
        private const string IDENTIFIER = "mi";
        private const string NUMBER = "mn";
        private const string SUPERSCRIPT = "msup";
        #endregion

        // Преобразование выражения в дерево
        // Выражения предсавлено в формате MathML
        public static string ExpressionToTree(XmlNode doc)
        {
            // LastChild = <math>
            // Преобразуем детей <math> в список узлов
            var expr = Utils.NodeListToList(doc.ChildNodes);
            int lastSplitSign = 0;
            Node tree = null;
            // Разбиение выражения на слогаемые
            // Инста-скип вероятного знака
            for (int i = 1; i < expr.Count; i++)
            {
                if (expr[i].Name == OPERATOR && (expr[i].InnerText == "+" || expr[i].InnerText == "-"))
                {
                    // Каждое слогаемое(моночлен) переводим в узел и добавляем в дерево
                    tree = BuildMonomialSubTree(
                        expr.GetRange(lastSplitSign, i - lastSplitSign + 1), tree);
                    tree.Value = expr[i].InnerText;
                    lastSplitSign = i + 1;
                }
            }
            // так как в цикле мы не 
            tree = BuildMonomialSubTree(
                        expr.GetRange(lastSplitSign, expr.Count - lastSplitSign), tree);
            tree = tree.Left;
            return ShowTree(tree);

        }

        public static string ShowTree(Node tree)
        {
            if (tree == null) return "";
            return ShowTree(tree.Left) + tree.Value + ShowTree(tree.Right);
        }

        // Построение моночлена, как узла дерева, из списка XmlNode
        // Подразумеваем что все опереторы <mo> в моночлене - операторы умножения
        private static Node BuildMonomialSubTree(List<XmlNode> list, Node ini)
        {
            if (list == null) return ini;
            if (list.Count == 0) return ini;
            var head = Utils.Head(list);
            var tail = Utils.Tail(list);
            switch (head.Name)
            {
                case OPERATOR:
                    return BuildMonomialSubTree(tail, ini);
                case IDENTIFIER:
                    var idNode = new Node(head.InnerText, null, null);
                    return BuildMonomialSubTree(tail, MakeNode(ini, idNode, "*"));
                case NUMBER:
                    var numNode = new Node(head.InnerText, null, null);
                    return BuildMonomialSubTree(tail, MakeNode(ini, numNode, "*"));
                case SUPERSCRIPT:
                    var basis = new Node(head.FirstChild.InnerText, null, null);
                    var exp = new Node(head.LastChild.InnerText, null, null);
                    var supNode = new Node("^", basis, exp);
                    return BuildMonomialSubTree(tail, MakeNode(ini, supNode, "*"));
                default:
                    return null;
            }
        }

        // Добавляем узел(листок) в существующее дерево-моночлен
        private static Node MakeNode(Node node, Node add, string item)
        {
            switch (node)
            {
                case null:
                    node = new Node(item);
                    node.Left = add;
                    return node;
                default:
                    node.Right = add;
                    return new Node(item, node, null);
            }
        }
    }

    public class Node
    {
        public string Value;
        public Node Left, Right;

        public Node(string item)
        {
            Value = item;
            Left = null;
            Right = null;
        }
        public Node()
        {
            Value = "";
            Left = null;
            Right = null;
        }
        public Node(string item, Node Left, Node Right)
        {
            Value = item;
            this.Left = Left;
            this.Right = Right;
        }
    }
}
