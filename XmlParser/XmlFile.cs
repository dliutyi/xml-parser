using System.Linq;

using LexicalRule = XmlParser.LexicalTransition<XmlParser.TransitionDelegate, XmlParser.TransitionCallback>;
using QueueLexicalRule = XmlParser.LexicalQueueNode<XmlParser.LexicalTransition<XmlParser.TransitionDelegate, XmlParser.TransitionCallback>>;

namespace XmlParser
{
    class LexicalQueue
    {
        XmlNode xmlRoot = new XmlNode();
        QueueLexicalRule lexicalRoot = new QueueLexicalRule();

        private (string, TransitionDelegate) CreateLexicalFunc(string abbr, TransitionDelegate transitionDelegate)
            => (abbr, transitionDelegate);

        private Params<QueueLexicalRule> From(params QueueLexicalRule[] lexicalQueueNodes) =>
            new Params<QueueLexicalRule>(lexicalQueueNodes.ToList());

        private Params<QueueLexicalRule> To(params QueueLexicalRule[] lexicalQueueNodes) =>
            new Params<QueueLexicalRule>(lexicalQueueNodes.ToList());

        private QueueLexicalRule CreateTransition((string, TransitionDelegate) transitionFunc, params Action[] actions) =>
            new QueueLexicalRule
            {
                Name = transitionFunc.Item1,
                Value = new LexicalRule
                {
                    Transition = transitionFunc.Item2,
                    Actions = actions.ToList()
                }
            };

        public void AddRules(params (Params<QueueLexicalRule>, Params<QueueLexicalRule>)[] rules) =>
            rules.ToList().ForEach(rule => rule.Item1.Values.ForEach(from => rule.Item2.Values.ForEach(to => from.Next.Add(to))));

        public LexicalQueue()
        {
            var StartBracketFunc = CreateLexicalFunc("[ < ]", (c) => c == '<');
            var EndBracketFunc   = CreateLexicalFunc("[ > ]", (c) => c == '>');
            var LetterFunc       = CreateLexicalFunc("[^l ]", (c) => char.IsLetter(c));
            var SymbolFunc       = CreateLexicalFunc("[ a ]", (c) => char.IsLetterOrDigit(c) || c == '-' || c == ':');
            var WhitespaceFunc   = CreateLexicalFunc("[ _ ]", (c) => char.IsWhiteSpace(c)    || char.IsSeparator(c) );
            var CloseTagSignFunc = CreateLexicalFunc("[ / ]", (c) => c == '/');
            var DoctypeValueFunc = CreateLexicalFunc("[ d ]", (c) => c != '>');
            var NodeValueFunc    = CreateLexicalFunc("[ t ]", (c) => c != '<');
            var AttrValueFunc    = CreateLexicalFunc("[ t ]", (c) => c != '"' && c != '\'');
            var EqualSignFunc    = CreateLexicalFunc("[ = ]", (c) => c == '=');
            var QuoteSignFunc    = CreateLexicalFunc("[ q ]", (c) => c == '"' || c == '\'');
            var ExclamationFunc  = CreateLexicalFunc("[ ! ]", (c) => c == '!');
            var QuestionFunc     = CreateLexicalFunc("[ ? ]", (c) => c == '?');

            var beforeStartWs  = CreateTransition(WhitespaceFunc  );
            var newTagStart    = CreateTransition(StartBracketFunc);
            var exclamation    = CreateTransition(ExclamationFunc );
            var version        = CreateTransition(DoctypeValueFunc);
            var question       = CreateTransition(QuestionFunc    );
            var startTagName   = CreateTransition(LetterFunc, Action.CreateTag, Action.TagName);
            var tagName        = CreateTransition(SymbolFunc, Action.TagName);
            var afterTagNameWs = CreateTransition(WhitespaceFunc);
            var selfCloseSlash = CreateTransition(CloseTagSignFunc, Action.SelfCloseTag);
            var newTagEnd      = CreateTransition(EndBracketFunc);

            var valueTagWs    = CreateTransition(WhitespaceFunc);
            var startTagValue = CreateTransition(NodeValueFunc, Action.CreateValueTag, Action.TagValue);
            var tagValue      = CreateTransition(NodeValueFunc, Action.TagValue);

            var pairCloseSlash      = CreateTransition(CloseTagSignFunc, Action.CloseTag);
            var closeTagStartName   = CreateTransition(LetterFunc);
            var closeTagName        = CreateTransition(SymbolFunc);
            var afterCloseTagNameWs = CreateTransition(WhitespaceFunc);

            var startAttrName  = CreateTransition(LetterFunc, Action.CreateAttribute, Action.AttrName );
            var attrName       = CreateTransition(SymbolFunc, Action.AttrName);
            var equal          = CreateTransition(EqualSignFunc );
            var bwEqAndQuoteWs = CreateTransition(WhitespaceFunc);
            var quoteOpen      = CreateTransition(QuoteSignFunc, Action.AttrCreateValue);
            var attrValue      = CreateTransition(AttrValueFunc, Action.AttrValue      );
            var quoteClose     = CreateTransition(QuoteSignFunc, Action.AttrCloseValue );

            AddRules(
                 (From(lexicalRoot, beforeStartWs),
                    To(beforeStartWs, newTagStart)),
                 (From(newTagStart),
                    To(exclamation, question, startTagName, pairCloseSlash)),

                 /*  [ <? ]  ||  [ <! ]  */
                 (From(exclamation, question),
                    To(version)),
                 (From(version),
                    To(version, newTagEnd)),

                 /*  [ <tag ]  */
                 (From(startTagName, tagName),
                    To(afterTagNameWs, tagName, selfCloseSlash, newTagEnd)),
                 (From(afterTagNameWs),
                    To(afterTagNameWs, selfCloseSlash, newTagEnd, startAttrName, equal)),

                 /*  [ attrName="attrValue" ]  ||  [ attrName ]  */
                 (From(startAttrName, attrName),
                    To(attrName, equal, afterTagNameWs)),
                 (From(equal, bwEqAndQuoteWs),
                    To(bwEqAndQuoteWs, quoteOpen)),
                 (From(quoteOpen, attrValue),
                    To(quoteClose, attrValue)),
                 (From(quoteClose),
                    To(afterTagNameWs, selfCloseSlash, newTagEnd)),

                 /*  [ /> ]  ||  [ > ]  */
                 (From(selfCloseSlash),
                    To(newTagEnd)),
                 (From(newTagEnd, valueTagWs),
                    To(valueTagWs, startTagValue, newTagStart)),

                 (From(startTagValue, tagValue),
                    To(tagValue, newTagStart)),

                 /*  [ </tag> ]  */
                 (From(pairCloseSlash),
                    To(closeTagStartName)),
                 (From(closeTagStartName, closeTagName),
                    To(afterCloseTagNameWs, closeTagName, newTagEnd)),
                 (From(afterCloseTagNameWs),
                    To(afterCloseTagNameWs, newTagEnd))
            );
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
                        var isSuccess = parsingNode.Value.Actions.TrueForAll(action => Process(action, c));

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
