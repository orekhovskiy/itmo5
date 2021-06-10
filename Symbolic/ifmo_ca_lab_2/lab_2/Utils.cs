using System.Xml;
using System;
using System.Collections.Generic;

namespace ShiftCo.ITMO.CA.Lab_2
{
    class Utils
    {
        public static XmlNode Head(List<XmlNode> list)
        {
            if (list.Count > 0) return list[0];
            return null;
        }

        public static List<XmlNode> Tail(List<XmlNode> list)
        {
            if (list.Count > 0)
            {
                list.RemoveAt(0);
                return list;
            }
            return null;
        }

        public static List<XmlNode> NodeListToList (XmlNodeList nList)
        {
            var list = new List<XmlNode>();
            foreach (XmlNode node in nList)
            {
                list.Add(node);
            }
            return list;
        }
    }
}
