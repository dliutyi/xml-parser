using System;
using System.Linq;

namespace XmlParser
{
    using LexicalPair = ValueTuple<string, TransitionDelegate>;

    class LexicalTree
    {
        LexicalNode _lexicalNode;

        public static LexicalPair CreateLexicalFunc(string abbr, TransitionDelegate transitionDelegate)
            => (abbr, transitionDelegate);

        public static Params<LexicalNode> From(params LexicalNode[] lexicalNodes) =>
            new Params<LexicalNode>(lexicalNodes.ToList());

        public static Params<LexicalNode> To(params LexicalNode[] lexicalNodes) =>
            new Params<LexicalNode>(lexicalNodes.ToList());

        public static LexicalNode CreateTransition((string, TransitionDelegate) transitionFunc, params Action[] actions) =>
            new LexicalNode
            {
                Name = transitionFunc.Item1,
                Value = new LexicalTransition
                {
                    Transition = transitionFunc.Item2,
                    Actions = actions.ToList()
                }
            };

        public LexicalTree(LexicalNode lexicalNode)
        {
            _lexicalNode = lexicalNode;
        }

        public void AddRules(params (Params<LexicalNode>, Params<LexicalNode>)[] rules) =>
            rules.ToList().ForEach(rule => rule.Item1.Values.ForEach(from => rule.Item2.Values.ForEach(to => from.Next.Add(to))));

        public LexicalNode LexicalNode { get => _lexicalNode; }
    }
}
