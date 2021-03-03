using System;
using System.Collections.Generic;
using System.Text;

namespace XmlParser
{
    enum Action
    {
        CreateTag,
        CreateValueTag,
        TagName,
        TagValue,
        CreateAttribute,
        AttrName,
        AttrCreateValue,
        AttrValue,
        AttrCloseValue,
        SelfCloseTag,
        CloseTag,
        None
    };
    class Params<T>
    {
        public T[] Ts;
        public Params(T[] ts)
        {
            Ts = ts;
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
        public TransitionCallback Callback { get; set; }
    }

    class LexicalQueueNode<T>
    {
        public string Name { get; set; }
        public T Value { get; set; }
        public List<LexicalQueueNode<T>> Next { get; set; } = new List<LexicalQueueNode<T>>();
    }
}
