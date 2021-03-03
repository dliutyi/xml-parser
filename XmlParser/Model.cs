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
    delegate List<Action> TransitionCallback();

    class LexicalTransition<T, V>
    {
        public TransitionDelegate Transition { get; set; }
        public List<Action> Actions { get; set; }
    }

    class LexicalQueueNode<T>
    {
        public string Name { get; set; }
        public T Value { get; set; }
        public List<LexicalQueueNode<T>> Next { get; set; } = new List<LexicalQueueNode<T>>();
    }
}
