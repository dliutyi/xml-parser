using System.Collections.Generic;

namespace XmlParser
{
    enum Action
    {
        CreateTag,
        CreateValueTag,
        CreateAttribute,
        TagName,
        TagValue,
        AttrName,
        AttrCreateValue,
        AttrValue,
        AttrCloseValue,
        SelfCloseTag,
        CloseTag
    };

    class XmlAttribute
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public bool HasValue { get; set; } = false;
    }

    class XmlNode
    {
        public string Name { get; set; }
        public string Value { get; set; }
        public XmlNode Parent { get; set; }
        public List<XmlAttribute> Attributes { get; set; } = new List<XmlAttribute>();
        public List<XmlNode> Children { get; set; } = new List<XmlNode>();
        public bool CanHaveChildren { get; set; } = true;
        public bool IsValueOnly { get; set; } = false;
    }

    class XmlParseState
    {
        public XmlNode CurrentXmlNode { get; set; }
        public Quote Quote { get; set; }
        public bool IsActionSuccess { get; set; }
        public bool IsValid { get; set; }
    }
}
