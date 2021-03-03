using System.Collections.Generic;
using System.Linq;

using LexicalRule = XmlParser.LexicalTransition<XmlParser.TransitionDelegate, XmlParser.TransitionCallback>;
using QueueLexicalRule = XmlParser.LexicalQueueNode<XmlParser.LexicalTransition<XmlParser.TransitionDelegate, XmlParser.TransitionCallback>>;

namespace XmlParser
{
    class LexicalQueue
    {
        XmlNode xmlRoot = new XmlNode();
        QueueLexicalRule lexicalRoot = new QueueLexicalRule();

        private TransitionDelegate CreateLexicalFunc(TransitionDelegate transitionDelegate)
            => transitionDelegate;

        private Params<QueueLexicalRule> From(params QueueLexicalRule[] lexicalQueueNodes) =>
            new Params<QueueLexicalRule>(lexicalQueueNodes);

        private Params<QueueLexicalRule> To(params QueueLexicalRule[] lexicalQueueNodes) =>
            new Params<QueueLexicalRule>(lexicalQueueNodes);

        private QueueLexicalRule CreateTransition(string name, TransitionDelegate transitionDelegate, TransitionCallback transitionCallback = null) =>
            new QueueLexicalRule
            {
                Name = name,
                Value = new LexicalRule
                {
                    Transition = transitionDelegate,
                    Callback = transitionCallback ?? (() => new List<Action>())
                }
            };

        private void AddTransitionRule(Params<QueueLexicalRule> from, Params<QueueLexicalRule> to)
        {
            foreach (var nodeFrom in from.Ts)
            {
                foreach (var nodeTo in to.Ts)
                {
                    nodeFrom.Next.Add(nodeTo);
                }
            }
        }

        public LexicalQueue()
        {
            var StartBracketFunc = CreateLexicalFunc((c) => c == '<');
            var EndBracketFunc = CreateLexicalFunc((c) => c == '>');
            var LetterFunc = CreateLexicalFunc((c) => char.IsLetter(c));
            var SymbolFunc = CreateLexicalFunc((c) => char.IsLetter(c) || char.IsDigit(c) || c == '-');
            var WhitespaceFunc = CreateLexicalFunc((c) => char.IsWhiteSpace(c) || char.IsSeparator(c));
            var CloseTagSignFunc = CreateLexicalFunc((c) => c == '/');
            var DoctypeValueFunc = CreateLexicalFunc((c) => c != '>');
            var NodeValueFunc = CreateLexicalFunc((c) => c != '<');
            var AttrValueFunc = CreateLexicalFunc((c) => c != '"' && c != '\'');
            var EqualSignFunc = CreateLexicalFunc((c) => c == '=');
            var QuoteSignFunc = CreateLexicalFunc((c) => c == '"' || c == '\'');
            var ExclamationSignFunc = CreateLexicalFunc((c) => c == '!');
            var QuestionSignFunc = CreateLexicalFunc((c) => c == '?');

            var beforeStartWhitespace = CreateTransition("[ _ ]", WhitespaceFunc);
            var startTagBracket = CreateTransition("[ < ]", StartBracketFunc);
            var exclamationSign = CreateTransition("[ ! ]", ExclamationSignFunc);
            var version = CreateTransition("[ d ]", DoctypeValueFunc);
            var questionSign = CreateTransition("[ ? ]", QuestionSignFunc);
            var startTagName = CreateTransition("[^l ]", LetterFunc, () => new List<Action> { Action.CreateTag, Action.TagName });
            var tagName = CreateTransition("[ a ]", SymbolFunc, () => new List<Action> { Action.TagName });
            var afterTagNameWhitespace = CreateTransition("[ _ ]", WhitespaceFunc);
            var selfCloseSlash = CreateTransition("[ / ]", CloseTagSignFunc, () => new List<Action> { Action.SelfCloseTag });
            var endTagBracket = CreateTransition("[ > ]", EndBracketFunc);

            var valueTagWhitespace = CreateTransition("[ _ ]", WhitespaceFunc);
            var startTagValue = CreateTransition("[^t ]", NodeValueFunc, () => new List<Action> { Action.CreateValueTag, Action.TagValue });
            var tagValue = CreateTransition("[ t ]", NodeValueFunc, () => new List<Action> { Action.TagValue });

            var pairCloseSlash = CreateTransition("[ / ]", CloseTagSignFunc, () => new List<Action> { Action.CloseTag });
            var pairCloseTagStartName = CreateTransition("[^l ]", LetterFunc);
            var pairCloseTagName = CreateTransition("[ a ]", SymbolFunc);
            var afterPairCloseTagNameWhitespace = CreateTransition("[ _ ]", WhitespaceFunc);

            var startAttrName = CreateTransition("[^l ]", LetterFunc, () => new List<Action> { Action.CreateAttribute, Action.AttrName });
            var attrName = CreateTransition("[ a ]", SymbolFunc, () => new List<Action> { Action.AttrName });
            var equalSign = CreateTransition("[ = ]", EqualSignFunc);
            var bwEqAndQuoteWhitespace = CreateTransition("[ _ ]", WhitespaceFunc);
            var quoteOpenSign = CreateTransition("[ q ]", QuoteSignFunc, () => new List<Action> { Action.AttrCreateValue });
            var attrValue = CreateTransition("[ t ]", AttrValueFunc, () => new List<Action> { Action.AttrValue });
            var quoteCloseSign = CreateTransition("[ q ]", QuoteSignFunc, () => new List<Action> { Action.AttrCloseValue });

            AddTransitionRule(
                From(lexicalRoot, beforeStartWhitespace),
                To(beforeStartWhitespace, startTagBracket));

            AddTransitionRule(
                From(startTagBracket),
                To(exclamationSign, questionSign, startTagName, pairCloseSlash));

            AddTransitionRule(
                From(exclamationSign, questionSign),
                To(version));

            AddTransitionRule(
                From(version),
                To(version, endTagBracket));

            AddTransitionRule(
                From(pairCloseSlash),
                To(pairCloseTagStartName));

            AddTransitionRule(
                From(startTagName, tagName),
                To(afterTagNameWhitespace, tagName, selfCloseSlash, endTagBracket));

            AddTransitionRule(
                From(afterTagNameWhitespace),
                To(afterTagNameWhitespace, selfCloseSlash, endTagBracket, startAttrName, equalSign));

            AddTransitionRule(
                From(startAttrName, attrName),
                To(attrName, equalSign, afterTagNameWhitespace));

            AddTransitionRule(
                From(equalSign, bwEqAndQuoteWhitespace),
                To(bwEqAndQuoteWhitespace, quoteOpenSign));

            AddTransitionRule(
                From(quoteOpenSign, attrValue),
                To(quoteCloseSign, attrValue));

            AddTransitionRule(
                From(quoteCloseSign),
                To(afterTagNameWhitespace, selfCloseSlash, endTagBracket));

            AddTransitionRule(
                From(selfCloseSlash),
                To(endTagBracket));

            AddTransitionRule(
                From(endTagBracket, valueTagWhitespace),
                To(valueTagWhitespace, startTagValue, startTagBracket));

            AddTransitionRule(
                From(startTagValue, tagValue),
                To(tagValue, startTagBracket));

            AddTransitionRule(
                From(pairCloseTagStartName, pairCloseTagName),
                To(afterPairCloseTagNameWhitespace, pairCloseTagName, endTagBracket));

            AddTransitionRule(
                From(afterPairCloseTagNameWhitespace),
                To(afterPairCloseTagNameWhitespace, endTagBracket));
        }

        private char _quote;
        private XmlNode _currentXmlNode;

        private bool Process(Action action, char c)
        {
            switch (action)
            {
                case Action.CreateTag:
                    if (_currentXmlNode.IsValueOnly)
                    {
                        _currentXmlNode.Value = _currentXmlNode.Value.Trim();
                        _currentXmlNode = _currentXmlNode.Parent;
                    }

                    var xmlNodeCT = new XmlNode { Parent = _currentXmlNode };
                    _currentXmlNode.Children.Add(xmlNodeCT);
                    _currentXmlNode = xmlNodeCT;
                    return true;
                case Action.CreateValueTag:
                    var xmlNodeCVT = new XmlNode { Parent = _currentXmlNode, IsValueOnly = true, CanHaveChildren = false };
                    _currentXmlNode.Children.Add(xmlNodeCVT);
                    _currentXmlNode = xmlNodeCVT;
                    return true; 
                case Action.TagName:
                    _currentXmlNode.Name += c;
                    return true; 
                case Action.TagValue:
                    _currentXmlNode.Value += c;
                    return true; 
                case Action.CreateAttribute:
                    _currentXmlNode.Attributes.Add(new XmlAttribute());
                    return true; 
                case Action.AttrName:
                    _currentXmlNode.Attributes.Last().Name += c;
                    return true; 
                case Action.AttrCreateValue:
                    _quote = c;
                    _currentXmlNode.Attributes.Last().HasValue = true;
                    return true; 
                case Action.AttrCloseValue:
                    if (_quote != c)
                    {
                        _currentXmlNode.Attributes.Last().Value += c;
                        return false;
                    }
                    _currentXmlNode.Attributes.Last().Value = _currentXmlNode.Attributes.Last().Value.Trim();
                    return true; 
                case Action.AttrValue:
                    _currentXmlNode.Attributes.Last().Value += c;
                    return true; 
                case Action.SelfCloseTag:
                    _currentXmlNode.CanHaveChildren = false;
                    _currentXmlNode = _currentXmlNode.Parent;
                    return true; 
                case Action.CloseTag:
                    if (_currentXmlNode.IsValueOnly)
                    {
                        _currentXmlNode.Value = _currentXmlNode.Value.Trim();
                        _currentXmlNode = _currentXmlNode.Parent;
                    }
                    _currentXmlNode = _currentXmlNode.Parent;
                    return true;
                default: return false;
            }
        }

        public XmlNode Parse(string xml)
        {
            Logger.Log($"Start parsing -\n{ xml }\n");
            var currentLexicalNode = lexicalRoot;

            _currentXmlNode = xmlRoot;
            foreach (var c in xml)
            {
                var error = true;
                var parsingNodes = currentLexicalNode.Next;

                Logger.Log($"Trying to parse - { c }\n   Trying apply - { string.Join(", ", parsingNodes.Select(n => n.Name)) }");
                foreach (var parsingNode in parsingNodes)
                {
                    if (parsingNode.Value.Transition(c))
                    {
                        error = false;

                        var actions = parsingNode.Value.Callback();
                        var isSuccess = actions.TrueForAll(action => Process(action, c));

                        if (isSuccess)
                        {
                            Logger.Log($"      { parsingNode.Name } - Applied");
                            currentLexicalNode = parsingNode;
                        }
                        break;
                    }
                }

                if (error)
                {
                    Logger.Log("Parse status - Error");
                    return null;
                }
            }
            Logger.Log("Parse status - OK");
            return xmlRoot;
        }
    }
}
