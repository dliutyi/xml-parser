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
    class Params<T>
    {
        public List<T> Values;
        public Params(List<T> values)
        {
            Values = values;
        }
    }

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

    delegate bool TransitionDelegate(char c);

    class LexicalTransition
    {
        public TransitionDelegate Transition { get; set; }
        public List<Action> Actions { get; set; }
    }

    class LexicalNode
    {
        public string Name { get; set; }
        public LexicalTransition Value { get; set; }
        public List<LexicalNode> Next { get; set; } = new List<LexicalNode>();
    }
}
