using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;


namespace ShiftCo.ITMO.CA.Lab_2
{
    class TermsSimplifier
    {
        #region TagNames
        private const string OPERATOR = "mo";
        private const string IDENTIFIER = "mi";
        private const string NUMBER = "mn";
        private const string SUPERSCRIPT = "msup";
        #endregion 

        // Метод приведения слогаемых в форме MathML
        public static XDocument AddLikeTerms(XmlNode doc)
        {
            // Конвертирование выражения из MathML в пользовательскую структуру данных
            var nodes = Utils.NodeListToList(doc.ChildNodes);
            var xMonomials = NodeListToXMonomials(nodes);
            var monomials = MapMonomials(xMonomials);

            // Приведение слогаемых
            for (int i = 0; i < monomials.Count; i++)
            {
                int j = i + 1;
                while (j < monomials.Count)
                {
                    if (VariablesAreEqual(monomials[i].Variables, monomials[j].Variables))
                    {
                        monomials[i].Coefficient += monomials[j].Coefficient;
                        monomials.RemoveAt(j);
                    }
                    else j++;
                }
            }
            // Возвращаем значение, сконвертировав его обратно в MathML
            return MapToXDocument(monomials);
        }

        // Конвертирование выражения из списка узлов в список одночленов(список списков узлов)
        private static List<List<XmlNode>> NodeListToXMonomials(List<XmlNode> nodes)
        {
            var monomials = new List<List<XmlNode>>();
            int lastSplitSign = 0;
            // Инста-скип возможного первого знака
            for (int i = 1; i < nodes.Count; i++)
            {
                if (nodes[i].Name == OPERATOR &&
                   (nodes[i].InnerText == "+" || nodes[i].InnerText == "-"))
                {
                    monomials.Add(nodes.GetRange(lastSplitSign, i - lastSplitSign));
                    lastSplitSign = i;
                }
            }
            monomials.Add(nodes.GetRange(lastSplitSign, nodes.Count - lastSplitSign));
            return monomials;
        }

        // Конвертирование выражения из списка одночленов в пользовательскую структуру
        private static List<Monomial> MapMonomials(List<List<XmlNode>> xMonomials)
        {
            var monomials = new List<Monomial>();
            foreach (List<XmlNode> xMonomial in xMonomials)
            {
                var monomial = new Monomial();
                foreach (XmlNode node in xMonomial)
                {
                    switch (node.Name)
                    {
                        case OPERATOR:
                            if (node.InnerText == "-") monomial.Coefficient *= -1;
                            break;
                        case IDENTIFIER:
                            monomial.Variables.Add(node.InnerText);
                            break;
                        case NUMBER:
                            monomial.Coefficient *= Convert.ToInt32(node.InnerText);
                            break;
                    }
                }
                monomial.Variables.Sort();
                monomials.Add(monomial);
            }
            return monomials;
        }

        // Метод сравнения переменных двух одночленов
        private static bool VariablesAreEqual(List<string> var1, List<string> var2)
        {
            if (var1.Count != var2.Count) return false;
            for (int i = 0; i < var1.Count; i++)
            {
                if (!var1[i].Equals(var2[i])) return false;
            }
            return true;
        }

        // Конвертирование выражения из пользовательской структуы данных в MathML
        private static XDocument MapToXDocument(List<Monomial> monomials)
        {
            var xDoc = new XDocument();
            var root = new XElement("math");

            foreach (Monomial monomial in monomials)
                if (monomial.Coefficient != 0)
                {
                    string sign;
                    if (monomial.Coefficient < 0) sign = "-"; else sign = "+";
                    root.Add(new XElement(OPERATOR, sign));
                    if (Math.Abs(monomial.Coefficient) != 1)
                        root.Add(new XElement(NUMBER, Math.Abs(monomial.Coefficient).ToString()));
                    int i = 0;
                    while (i < monomial.Variables.Count)
                    {
                        if (i + 1 == monomial.Variables.Count || monomial.Variables[i] != monomial.Variables[i + 1])
                        {
                            root.Add(new XElement(IDENTIFIER, monomial.Variables[i]));
                            i++;
                        }
                        else
                        {
                            int c = 0;
                            while (i + c < monomial.Variables.Count && monomial.Variables[i] == monomial.Variables[i + c]) c++;
                            var msup = new XElement(SUPERSCRIPT);
                            var mi = new XElement(IDENTIFIER, monomial.Variables[i]);
                            var mn = new XElement(NUMBER, c.ToString());
                            msup.Add(mi);
                            msup.Add(mn);
                            root.Add(msup);
                            i += c;
                        }
                    }
                }
            xDoc.Add(root);
            return xDoc;
        }
    }

    // Пользовательский тип данных 
    // Одночлен имеет коэффициент и переменные
    public class Monomial
    {
        public int Coefficient;
        public List<string> Variables;
        public Monomial()
        {
            Coefficient = 1;
            Variables = new List<string>();
        }
    }
}