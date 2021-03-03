using System;
using System.Collections.Generic;
using System.Linq;

namespace XmlParser
{
    using LexicalPair = ValueTuple<string, TransitionDelegate>;

    delegate bool TransitionDelegate(char c);

    class LexicalTransition<T>
    {
        public TransitionDelegate Transition { get; set; }
        public List<T> Actions { get; set; }
    }

    class LexicalNode<T>
    {
        public string Name { get; set; }
        public LexicalTransition<T> Value { get; set; }
        public List<LexicalNode<T>> Next { get; set; } = new List<LexicalNode<T>>();
    }

    class LexicalParams<T>
    {
        public List<T> Values;
        public LexicalParams(List<T> values)
        {
            Values = values;
        }
    }

    class LexicalTree<T>
    {
        LexicalNode<T> _lexicalNode;

        public static LexicalPair CreateLexicalFunc(string abbr, TransitionDelegate transitionDelegate)
            => (abbr, transitionDelegate);

        public static LexicalParams<LexicalNode<T>> From(params LexicalNode<T>[] lexicalNodes) =>
            new LexicalParams<LexicalNode<T>>(lexicalNodes.ToList());

        public static LexicalParams<LexicalNode<T>> To(params LexicalNode<T>[] lexicalNodes) =>
            new LexicalParams<LexicalNode<T>>(lexicalNodes.ToList());

        public static LexicalNode<T> CreateTransition((string, TransitionDelegate) transitionFunc, params T[] actions) =>
            new LexicalNode<T>
            {
                Name = transitionFunc.Item1,
                Value = new LexicalTransition<T>
                {
                    Transition = transitionFunc.Item2,
                    Actions = actions.ToList()
                }
            };

        public LexicalTree(LexicalNode<T> lexicalNode)
        {
            _lexicalNode = lexicalNode;
        }

        public void AddRules(params (LexicalParams<LexicalNode<T>>, LexicalParams<LexicalNode<T>>)[] rules) =>
            rules.ToList().ForEach(rule => rule.Item1.Values.ForEach(from => rule.Item2.Values.ForEach(to => from.Next.Add(to))));

        public LexicalNode<T> LexicalNode { get => _lexicalNode; }
    }
}
