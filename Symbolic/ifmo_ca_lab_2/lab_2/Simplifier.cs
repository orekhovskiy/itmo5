using System;
using System.Xml;
using System.Xml.Linq;
using System.Collections.Generic;

namespace ShiftCo.ITMO.CA.Lab_2
{
    class Simplifier
    {
        #region Поля
        XmlDocument xDoc;
        XmlNode xRoot;

        // Эталонные узлы для последующего копирования
        XmlNode RefNodeMult, RefNodeFenc, RefNodeMinusOne, RefNodePlus;
        #endregion

        public XmlDocument SimplifyThis(ref XmlDocument xDoc)
        {
            this.xDoc = xDoc;

            // Получение корневого элемента
            xRoot = xDoc.DocumentElement;

            // Инициализация эталонных узлов
            InstanciateReferenceNodes();

            // Избавление от степеней
            while (true)
            {
                XmlNode ExpNode = FindAnyExp();
                if (ExpNode == null)
                {
                    break;
                }
                SimplifyExponent(ExpNode);
            }

            // Перемножение скобок по дистрибутивному свойству с предварительным представлением знаков минус, предшествующих 
            // скобкам, в виде множителя отрицательной единицы
            while (true)
            {
                XmlNode PrecMinusNode = FindPrecMinusNode();
                if (PrecMinusNode == null)
                {
                    break;
                }
                RepresentMinusNode(PrecMinusNode);
            }
            while (true)
            {
                XmlNode FencedMultNode = FindFencedMult();
                if (FencedMultNode == null)
                {
                    break;
                }
                SimplifyDistributive(FencedMultNode);
            }

            // Опущение оставшихся незначащих скобок
            RemoveParentheses();

            // Приведение подобных слагаемых
            xDoc = AddAlikeTerms();

            return xDoc;
        }

        private XmlNode FindInsigPlusNode()
        {
            // Получение всех элементов с тегом "mo", т.е. тегов со знаком плюс
            XmlNodeList InsigPlusNodes = xDoc.GetElementsByTagName("mo");
            foreach (XmlNode InsigPlusNode in InsigPlusNodes)
            {
                // Возврат данного элемента, если он кодирует знак плюс, а следующий за ним элемент — минус
                if (InsigPlusNode.InnerText == "+" && InsigPlusNode.NextSibling.InnerText == "-")
                {
                    return InsigPlusNode;
                }
            }
            return null;
        }

        private XmlNode FindPrecMinusNode()
        {
            // Получение всех элементов с тегом "mo", т.е. тегов со знаком минус
            XmlNodeList PrecMinusNodes = xDoc.GetElementsByTagName("mo");
            foreach (XmlNode PrecMinusNode in PrecMinusNodes)
            {
                // Возврат данного элемента, если он кодирует знак минус, а следующий за ним элемент — скобка
                if (PrecMinusNode.InnerText == "-" && PrecMinusNode.NextSibling.LocalName == "mfenced")
                {
                    return PrecMinusNode;
                }
            }
            return null;
        }

        private void RepresentMinusNode(XmlNode PrecMinusNode)
        {
            XmlNode ParentNode = PrecMinusNode.ParentNode;
            ParentNode.InsertBefore(RefNodePlus.Clone(), PrecMinusNode);
            ParentNode.InsertAfter(RefNodeMult.Clone(), PrecMinusNode);
            ParentNode.ReplaceChild(RefNodeMinusOne.Clone(), PrecMinusNode);
        }

        private void SimplifyDistributive(XmlNode FencedMultNode)
        {
            // Составление нового тега (скобок), содержащего все сочетания субоперандов левого операнда с правым, разделенных знаками + и -
            XmlNode Substitutor = RefNodeFenc.Clone();
            // Получение псевдо-операндов умножения
            XmlNode LPseudoOp = FencedMultNode.PreviousSibling;
            XmlNode RPseudoOp = FencedMultNode.NextSibling;
            // Получение граничных узлов контейнеров субоперандов левого и правого операндов, т.е. областей, содержащих субоперанды
            List<XmlNode> LOpSubOpsContBounds = GetOpSubOpsContBounds(LPseudoOp, true);
            List<XmlNode> ROpSubOpsContBounds = GetOpSubOpsContBounds(RPseudoOp, false);
            // Получение субоперандов, участвующих в сочетаниях, из контейнеров субоперандов левого и правого операндов
            List<List<XmlNode>> LOpSubOps = GetOpSubOps(LOpSubOpsContBounds);
            List<List<XmlNode>> ROpSubOps = GetOpSubOps(ROpSubOpsContBounds);
            // Произведение субоперандов одного операнда на субоперанды другого
            List<List<XmlNode>> MultResult = MultiplySubOps(LOpSubOps, ROpSubOps);
            // Наполнение субститутора узлами — результатом произведения субоперандов
            PopulateSubstitutor(ref Substitutor, MultResult);
            // Поиск самого верхнего узла левого операнда и самого нижнего узла правого операнда для последующего удаления
            List<XmlNode> DeletionBounds = GetDeletionBounds(LPseudoOp, RPseudoOp);
            // Получение узла, родительского по отношению к области операции умножения
            XmlNode ParentNode = FencedMultNode.ParentNode;
            // Вставка субститутора перед левым операндом умножения
            ParentNode.InsertBefore(Substitutor, DeletionBounds[0]);
            // Удаления обоих операндов и узла операции
            RemoveMultOperAndOps(ParentNode, DeletionBounds);
        }

        private void RemoveMultOperAndOps(XmlNode ParentNode, List<XmlNode> DeletionBounds)
        {
            XmlNode NodeIter = DeletionBounds[0];
            while (NodeIter != DeletionBounds[1])
            {
                XmlNode swap = NodeIter.NextSibling;
                ParentNode.RemoveChild(NodeIter);
                NodeIter = swap;
            }
            ParentNode.RemoveChild(NodeIter);
        }

        private void PopulateSubstitutor(ref XmlNode Substitutor, List<List<XmlNode>> SubOps)
        {
            foreach (List<XmlNode> SubOp in SubOps)
            {
                foreach (XmlNode Node in SubOp)
                {
                    Substitutor.FirstChild.AppendChild(Node.Clone());
                }
            }
        }

        private List<XmlNode> GetDeletionBounds(XmlNode LPseudoOp, XmlNode RPseudoOp)
        {
            List<XmlNode> DeletionBounds = new List<XmlNode>();
            // Заполнение границ значениями по умолчанию
            DeletionBounds.Add(LPseudoOp);
            DeletionBounds.Add(RPseudoOp);
            // Нахождение других значений в случае, если тип узла — не скобка
            if (LPseudoOp.LocalName != "mfenced")
            {
                while (MoveNodeIter(DeletionBounds[0], true) != null)
                {
                    if (MoveNodeIter(DeletionBounds[0], true).InnerText == "+" ||
                        MoveNodeIter(DeletionBounds[0], true).InnerText == "-")
                    {
                        break;
                    }
                    DeletionBounds[0] = MoveNodeIter(DeletionBounds[0], true);
                }
            }
            if (RPseudoOp.LocalName != "mfenced")
            {
                while (MoveNodeIter(DeletionBounds[1], false) != null)
                {
                    if (MoveNodeIter(DeletionBounds[1], false).InnerText == "+" ||
                        MoveNodeIter(DeletionBounds[1], false).InnerText == "-")
                    {
                        break;
                    }
                    DeletionBounds[1] = MoveNodeIter(DeletionBounds[1], false);
                }
            }
            return DeletionBounds;
        }

        private List<List<XmlNode>> MultiplySubOps(List<List<XmlNode>> LOpSubOps, List<List<XmlNode>> ROpSubOps)
        {
            List<List<XmlNode>> Result = new List<List<XmlNode>>();
            Result.Add(new List<XmlNode>());
            int currSubOp = 0;
            // Перемножение каждого субоперанда левого операнда с каждым субоперандом првого операнда
            foreach (List<XmlNode> LSubOp in LOpSubOps)
            {
                foreach (List<XmlNode> RSubOp in ROpSubOps)
                {
                    // Вставка узла знака, если таковой необходим
                    XmlNode SignNode = GetNeededSignNode(LSubOp[0], RSubOp[0]);
                    if (SignNode != null)
                    {
                        Result[currSubOp].Add(SignNode);
                    }
                    // Слияние субоперандов, разделенных узлами оператора умножения
                    foreach (XmlNode Node in LSubOp)
                    {
                        if (Node.InnerText == "+" || Node.InnerText == "-")
                        {
                            continue;
                        }
                        Result[currSubOp].Add(Node.Clone());
                    }
                    Result[currSubOp].Add(RefNodeMult.Clone());
                    foreach (XmlNode Node in RSubOp)
                    {
                        if (Node.InnerText == "+" || Node.InnerText == "-")
                        {
                            continue;
                        }
                        Result[currSubOp].Add(Node.Clone());
                    }
                    currSubOp++;
                    Result.Add(new List<XmlNode>());
                }
            }
            return Result;
        }

        private XmlNode GetNeededSignNode(XmlNode LSignNode, XmlNode RSignNode)
        {
            // Сбор знаков
            int lSign, rSign;
            lSign = rSign = 0;
            switch (LSignNode.InnerText)
            {
                case "+":
                    lSign = 1;
                    break;
                case "-":
                    lSign = -1;
                    break;
            }
            switch (RSignNode.InnerText)
            {
                case "+":
                    rSign = 1;
                    break;
                case "-":
                    rSign = -1;
                    break;
            }
            // Возврат значения в зависимости от конфигурации знаков
            Tuple<int, int> signConfig = new Tuple<int, int>(lSign, rSign);
            switch (signConfig)
            {
                case var tuple when tuple.Item1 == -1 && tuple.Item2 == -1:
                    return LSignNode.Clone();
                case var tuple when tuple.Item1 == -1:
                    return LSignNode.Clone();
                case var tuple when tuple.Item2 == -1:
                    return RSignNode.Clone();
                case var tuple when tuple.Item1 == 1:
                    return LSignNode.Clone();
                case var tuple when tuple.Item2 == 1:
                    return RSignNode.Clone();
                default:
                    return null;
            }
        }

        private List<List<XmlNode>> GetOpSubOps(List<XmlNode> OpSubOpsContBounds)
        {
            List<List<XmlNode>> OpSubOps = new List<List<XmlNode>>();
            OpSubOps.Add(new List<XmlNode>());
            int currSubOp = 0;
            XmlNode NodeIter = OpSubOpsContBounds[0];
            OpSubOps[currSubOp].Add(NodeIter.Clone());
            // Итерирование по узлам до нижней границы контейнера
            while (NodeIter != OpSubOpsContBounds[1])
            {
                NodeIter = MoveNodeIter(NodeIter, false);
                if (NodeIter.LocalName == "mo" && (NodeIter.InnerText == "+" || NodeIter.InnerText == "-"))
                {
                    OpSubOps.Add(new List<XmlNode>());
                    currSubOp++;
                }
                OpSubOps[currSubOp].Add(NodeIter.Clone());
            }
            return OpSubOps;
        }

        private List<XmlNode> GetOpSubOpsContBounds(XmlNode PseudoOp, bool isLeftOp)
        {
            XmlNode FixedBound, MovingBound;
            switch (PseudoOp.LocalName)
            {
                // Если операнд является скобкой, т.е. составным операндом
                case "mfenced":
                    FixedBound = MovingBound = PseudoOp.FirstChild.FirstChild;
                    while (MoveNodeIter(MovingBound, false) != null)
                    {
                        MovingBound = MoveNodeIter(MovingBound, false);
                    }
                    return new List<XmlNode>() { FixedBound, MovingBound };
                // Если операнд является элементарным операндом
                default:
                    FixedBound = MovingBound = PseudoOp;
                    while (MoveNodeIter(MovingBound, isLeftOp) != null)
                    {
                        if (MoveNodeIter(MovingBound, isLeftOp).InnerText == "+" ||
                            MoveNodeIter(MovingBound, isLeftOp).InnerText == "-")
                        {
                            break;
                        }
                        MovingBound = MoveNodeIter(MovingBound, isLeftOp);
                    }
                    // Возврат сначала верхней границы, затем — нижней
                    switch (isLeftOp)
                    {
                        case true:
                            return new List<XmlNode>() { MovingBound, FixedBound };
                        default:
                            return new List<XmlNode>() { FixedBound, MovingBound };
                    }
            }
        }

        private XmlNode MoveNodeIter(XmlNode NodeIter, bool toMoveUp)
        {
            switch (toMoveUp)
            {
                case true:
                    return NodeIter.PreviousSibling;
                default:
                    return NodeIter.NextSibling;
            }
        }

        private XmlNode FindFencedMult()
        {
            // Получение всех элементов с тегом "mo", т.е. тегов операции умножения
            XmlNodeList OperNodes = xDoc.GetElementsByTagName("mo");
            foreach (XmlNode OperNode in OperNodes)
            {
                // Возврат данного тега, если он кодирует операцию умножения и имеет хотя бы одним из своих операндов скобку
                if (OperNode.InnerText == "*")
                {
                    if (OperNode.PreviousSibling.LocalName == "mfenced" || OperNode.NextSibling.LocalName == "mfenced")
                    {
                        return OperNode;
                    }
                }
            }
            return null;
        }

        private void SimplifyExponent(XmlNode mlSup)
        {
            // Получение узлов основания и показателя степени
            XmlNode mlSupBase = mlSup.FirstChild;
            XmlNode mlSupExp = mlSup.LastChild;
            // Извлечение численного значения показателя степени
            int exponent = Convert.ToInt32(mlSupExp.InnerXml);
            // Удаление узла показателя степени
            mlSup.RemoveChild(mlSupExp);
            // Вставка узла операции умножения и узла основания (exponent - 1) раз
            for (int i = 0; i < exponent - 1; i++)
            {
                XmlNode mlSupBaseClone = mlSupBase.Clone();
                XmlNode refNodeMultInst = RefNodeMult.Clone();
                mlSup.InsertAfter(refNodeMultInst, mlSupBase);
                mlSup.InsertAfter(mlSupBaseClone, refNodeMultInst);
            }
            // Копирование внутренних узлов узла возведения в степень mlSup и вставка их перед самим узлом
            XmlNodeList mlSupChildren = mlSup.ChildNodes;
            foreach (XmlNode mlSupChild in mlSupChildren)
            {
                XmlNode mlSupChildClone = mlSupChild.Clone();
                mlSup.ParentNode.InsertBefore(mlSupChildClone, mlSup);
            }
            // Удаление узла возведения в степень
            mlSup.ParentNode.RemoveChild(mlSup);
        }

        private XmlNode FindAnyExp()
        {
            // Получение всех элементов с тегом "msups", т.е. тегов операции возведения в степень
            XmlNodeList ExpNodes = xDoc.GetElementsByTagName("msup");
            // Возврат первого элемента списка
            if (ExpNodes != null)
            {
                return ExpNodes[0];
            }
            return null;
        }

        private void InstanciateReferenceNodes()
        {
            // Узел операции умножения
            RefNodeMult = xDoc.CreateNode(XmlNodeType.Element, "mo", "");
            RefNodeMult.InnerText = "*";

            // Узел скобок
            RefNodeFenc = xDoc.CreateNode(XmlNodeType.Element, "mfenced", "");
            XmlNode refNodeRow = xDoc.CreateNode(XmlNodeType.Element, "mrow", "");
            RefNodeFenc.AppendChild(refNodeRow);

            // Узел числа один
            XmlNode UtilNodeMinus = xDoc.CreateNode(XmlNodeType.Element, "mo", "");
            UtilNodeMinus.InnerText = "-";
            XmlNode UtilNodeOne = xDoc.CreateNode(XmlNodeType.Element, "mn", "");
            UtilNodeOne.InnerText = "1";
            RefNodeMinusOne = RefNodeFenc.Clone();
            RefNodeMinusOne.FirstChild.AppendChild(UtilNodeMinus);
            RefNodeMinusOne.FirstChild.AppendChild(UtilNodeOne);

            // Узел операции сложения
            RefNodePlus = xDoc.CreateNode(XmlNodeType.Element, "mo", "");
            RefNodePlus.InnerText = "+";
        }

        // Законтаченные методы
        private XmlDocument AddAlikeTerms()
        {
            XDocument LinqDoc = TermsSimplifier.AddLikeTerms(xRoot);
            xDoc = new XmlDocument();
            xDoc.LoadXml(LinqDoc.ToString());
            xRoot = xDoc.DocumentElement;
            if (xRoot.FirstChild.InnerText == "+")
            {
                xRoot.RemoveChild(xRoot.FirstChild);
            }
            return xDoc;
        }

        private void RemoveParentheses()
        {
            foreach (XmlNode Node in xRoot)
            {
                if (Node.LocalName == "mfenced")
                {
                    var InsNodes = Utils.NodeListToList(Node.FirstChild.ChildNodes);
                    foreach (XmlNode InsNode in InsNodes)
                    {
                        xRoot.InsertBefore(InsNode, Node);
                    }
                    xRoot.RemoveChild(Node);
                    RemoveParentheses();
                }
            }

            // Опущение знаков плюс, предшествующих знакам минус. Возможно происходит при опущении скобок
            while (true)
            {
                XmlNode InsigPlusNode = FindInsigPlusNode();
                if (InsigPlusNode == null)
                {
                    break;
                }
                InsigPlusNode.ParentNode.RemoveChild(InsigPlusNode);
            }
        }
    }
}
