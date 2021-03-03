using System;
using System.Linq;

namespace XmlParser
{
    using LexicalPair = ValueTuple<string, TransitionDelegate>;
    using XmlLexicalTree = LexicalTree<Action>;
    using XmlLexicalNode = LexicalNode<Action>;

    enum Quote { Double, Single }

    class XmlFile
    {
        XmlNode _globalRoot;
        XmlParseState _xmlParseState;

        XmlLexicalTree _lexicalTree;

        public XmlFile()
        {
            BuildLexicalTree();
        }

        private bool Process(Action action, char c)
        {
            switch (action)
            {
                case Action.CreateTag:
                    if (_xmlParseState.CurrentXmlNode.IsValueOnly)
                    {
                        _xmlParseState.CurrentXmlNode.Value = _xmlParseState.CurrentXmlNode.Value.Trim();
                        _xmlParseState.CurrentXmlNode = _xmlParseState.CurrentXmlNode.Parent;
                    }

                    var xmlNodeCT = new XmlNode { Parent = _xmlParseState.CurrentXmlNode };
                    _xmlParseState.CurrentXmlNode.Children.Add(xmlNodeCT);
                    _xmlParseState.CurrentXmlNode = xmlNodeCT;
                    return true;
                case Action.CreateValueTag:
                    var xmlNodeCVT = new XmlNode { Parent = _xmlParseState.CurrentXmlNode, IsValueOnly = true, CanHaveChildren = false };
                    _xmlParseState.CurrentXmlNode.Children.Add(xmlNodeCVT);
                    _xmlParseState.CurrentXmlNode = xmlNodeCVT;
                    return true; 
                case Action.TagName:
                    _xmlParseState.CurrentXmlNode.Name += c;
                    return true; 
                case Action.TagValue:
                    _xmlParseState.CurrentXmlNode.Value += c;
                    return true; 
                case Action.CreateAttribute:
                    _xmlParseState.CurrentXmlNode.Attributes.Add(new XmlAttribute());
                    return true; 
                case Action.AttrName:
                    _xmlParseState.CurrentXmlNode.Attributes.Last().Name += c;
                    return true; 
                case Action.AttrCreateValue:
                    _xmlParseState.Quote = ParseQuote(c);
                    _xmlParseState.CurrentXmlNode.Attributes.Last().HasValue = true;
                    return true; 
                case Action.AttrCloseValue:
                    if (_xmlParseState.Quote != ParseQuote(c))
                    {
                        _xmlParseState.CurrentXmlNode.Attributes.Last().Value += c;
                        return false;
                    }
                    _xmlParseState.CurrentXmlNode.Attributes.Last().Value = _xmlParseState.CurrentXmlNode.Attributes.Last().Value.Trim();
                    return true; 
                case Action.AttrValue:
                    _xmlParseState.CurrentXmlNode.Attributes.Last().Value += c;
                    return true; 
                case Action.SelfCloseTag:
                    _xmlParseState.CurrentXmlNode.CanHaveChildren = false;
                    _xmlParseState.CurrentXmlNode = _xmlParseState.CurrentXmlNode.Parent;
                    return true; 
                case Action.CloseTag:
                    if (_xmlParseState.CurrentXmlNode.IsValueOnly)
                    {
                        _xmlParseState.CurrentXmlNode.Value = _xmlParseState.CurrentXmlNode.Value.Trim();
                        _xmlParseState.CurrentXmlNode = _xmlParseState.CurrentXmlNode.Parent;
                    }
                    _xmlParseState.CurrentXmlNode = _xmlParseState.CurrentXmlNode.Parent;
                    return true;
                default: return false;
            }
        }

        public XmlNode Parse(string xml)
        {
            Logger.Log($"Start parsing -\n{ xml }\n");
            var currentLexicalNode = _lexicalTree.LexicalNode;

            _xmlParseState.CurrentXmlNode = new XmlNode();
            foreach (var c in xml)
            {
                _xmlParseState.IsValid = true;
                var parsingNodes = currentLexicalNode.Next;

                Logger.Log($"Trying to parse - { c }\n   Trying apply - { string.Join(", ", parsingNodes.Select(n => n.Name)) }");
                foreach (var parsingNode in parsingNodes)
                {
                    if (parsingNode.Value.Transition(c))
                    {
                        _xmlParseState.IsValid = false;
                        _xmlParseState.IsActionSuccess = parsingNode.Value.Actions.TrueForAll(action => Process(action, c));

                        if (_xmlParseState.IsActionSuccess)
                        {
                            Logger.Log($"      { parsingNode.Name } - Applied");
                            currentLexicalNode = parsingNode;
                        }
                        break;
                    }
                }

                if (_xmlParseState.IsValid)
                {
                    Logger.Log("Parse status - Error");
                    return null;
                }
            }
            Logger.Log("Parse status - OK");

            _globalRoot = _xmlParseState.CurrentXmlNode;
            return _globalRoot;
        }

        private Quote ParseQuote(char c) => c == '"' ? Quote.Double : Quote.Single;

        private void BuildLexicalTree()
        {
            var beforeStartWs  = XmlLexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc  );
            var newTagStart    = XmlLexicalTree.CreateTransition(XmlFormatter.StartBracketFunc);
            var exclamation    = XmlLexicalTree.CreateTransition(XmlFormatter.ExclamationFunc );
            var version        = XmlLexicalTree.CreateTransition(XmlFormatter.DoctypeValueFunc);
            var question       = XmlLexicalTree.CreateTransition(XmlFormatter.QuestionFunc    );
            var startTagName   = XmlLexicalTree.CreateTransition(XmlFormatter.LetterFunc, Action.CreateTag, Action.TagName);
            var tagName        = XmlLexicalTree.CreateTransition(XmlFormatter.SymbolFunc, Action.TagName);
            var afterTagNameWs = XmlLexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc);
            var selfCloseSlash = XmlLexicalTree.CreateTransition(XmlFormatter.CloseTagSignFunc, Action.SelfCloseTag);
            var newTagEnd      = XmlLexicalTree.CreateTransition(XmlFormatter.EndBracketFunc);

            var valueTagWs    = XmlLexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc);
            var startTagValue = XmlLexicalTree.CreateTransition(XmlFormatter.NodeValueFunc, Action.CreateValueTag, Action.TagValue);
            var tagValue      = XmlLexicalTree.CreateTransition(XmlFormatter.NodeValueFunc, Action.TagValue);

            var pairCloseSlash      = XmlLexicalTree.CreateTransition(XmlFormatter.CloseTagSignFunc, Action.CloseTag);
            var closeTagStartName   = XmlLexicalTree.CreateTransition(XmlFormatter.LetterFunc);
            var closeTagName        = XmlLexicalTree.CreateTransition(XmlFormatter.SymbolFunc);
            var afterCloseTagNameWs = XmlLexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc);

            var startAttrName  = XmlLexicalTree.CreateTransition(XmlFormatter.LetterFunc, Action.CreateAttribute, Action.AttrName);
            var attrName       = XmlLexicalTree.CreateTransition(XmlFormatter.SymbolFunc, Action.AttrName);
            var equal          = XmlLexicalTree.CreateTransition(XmlFormatter.EqualSignFunc );
            var bwEqAndQuoteWs = XmlLexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc);
            var quoteOpen      = XmlLexicalTree.CreateTransition(XmlFormatter.QuoteSignFunc, Action.AttrCreateValue);
            var attrValue      = XmlLexicalTree.CreateTransition(XmlFormatter.AttrValueFunc, Action.AttrValue);
            var quoteClose     = XmlLexicalTree.CreateTransition(XmlFormatter.QuoteSignFunc, Action.AttrCloseValue);

            var newLexicalRoot = new XmlLexicalNode();
            _lexicalTree = new XmlLexicalTree(newLexicalRoot);

            _lexicalTree.AddRules(
                 (XmlLexicalTree.From(newLexicalRoot, beforeStartWs),
                    XmlLexicalTree.To(beforeStartWs, newTagStart)),
                 (XmlLexicalTree.From(newTagStart),
                    XmlLexicalTree.To(exclamation, question, startTagName, pairCloseSlash)),

                 /*  [ <? ]  ||  [ <! ]  */
                 (XmlLexicalTree.From(exclamation, question),
                    XmlLexicalTree.To(version)),
                 (XmlLexicalTree.From(version),
                    XmlLexicalTree.To(version, newTagEnd)),

                 /*  [ <tag ]  */
                 (XmlLexicalTree.From(startTagName, tagName),
                    XmlLexicalTree.To(afterTagNameWs, tagName, selfCloseSlash, newTagEnd)),
                 (XmlLexicalTree.From(afterTagNameWs),
                    XmlLexicalTree.To(afterTagNameWs, selfCloseSlash, newTagEnd, startAttrName, equal)),

                 /*  [ attrName="attrValue" ]  ||  [ attrName ]  */
                 (XmlLexicalTree.From(startAttrName, attrName),
                    XmlLexicalTree.To(attrName, equal, afterTagNameWs)),
                 (XmlLexicalTree.From(equal, bwEqAndQuoteWs),
                    XmlLexicalTree.To(bwEqAndQuoteWs, quoteOpen)),
                 (XmlLexicalTree.From(quoteOpen, attrValue),
                    XmlLexicalTree.To(quoteClose, attrValue)),
                 (XmlLexicalTree.From(quoteClose),
                    XmlLexicalTree.To(afterTagNameWs, selfCloseSlash, newTagEnd)),

                 /*  [ /> ]  ||  [ > ]  */
                 (XmlLexicalTree.From(selfCloseSlash),
                    XmlLexicalTree.To(newTagEnd)),
                 (XmlLexicalTree.From(newTagEnd, valueTagWs),
                    XmlLexicalTree.To(valueTagWs, startTagValue, newTagStart)),

                 (XmlLexicalTree.From(startTagValue, tagValue),
                    XmlLexicalTree.To(tagValue, newTagStart)),

                 /*  [ </tag> ]  */
                 (XmlLexicalTree.From(pairCloseSlash),
                    XmlLexicalTree.To(closeTagStartName)),
                 (XmlLexicalTree.From(closeTagStartName, closeTagName),
                    XmlLexicalTree.To(afterCloseTagNameWs, closeTagName, newTagEnd)),
                 (XmlLexicalTree.From(afterCloseTagNameWs),
                    XmlLexicalTree.To(afterCloseTagNameWs, newTagEnd))
            );
        }
    }

    static class XmlFormatter
    {
        public static LexicalPair StartBracketFunc = XmlLexicalTree.CreateLexicalFunc("[ < ]", (c) => c == '<');
        public static LexicalPair EndBracketFunc   = XmlLexicalTree.CreateLexicalFunc("[ > ]", (c) => c == '>');
        public static LexicalPair LetterFunc       = XmlLexicalTree.CreateLexicalFunc("[^l ]", (c) => char.IsLetter(c));
        public static LexicalPair WhitespaceFunc   = XmlLexicalTree.CreateLexicalFunc("[ _ ]", (c) => char.IsWhiteSpace(c) || char.IsSeparator(c));
        public static LexicalPair SymbolFunc       = XmlLexicalTree.CreateLexicalFunc("[ a ]", (c) => char.IsLetterOrDigit(c) || c == '-' || c == ':');
        public static LexicalPair CloseTagSignFunc = XmlLexicalTree.CreateLexicalFunc("[ / ]", (c) => c == '/');
        public static LexicalPair DoctypeValueFunc = XmlLexicalTree.CreateLexicalFunc("[ d ]", (c) => c != '>');
        public static LexicalPair NodeValueFunc    = XmlLexicalTree.CreateLexicalFunc("[ t ]", (c) => c != '<');
        public static LexicalPair AttrValueFunc    = XmlLexicalTree.CreateLexicalFunc("[ t ]", (c) => c != '"' && c != '\'');
        public static LexicalPair EqualSignFunc    = XmlLexicalTree.CreateLexicalFunc("[ = ]", (c) => c == '=');
        public static LexicalPair QuoteSignFunc    = XmlLexicalTree.CreateLexicalFunc("[ q ]", (c) => c == '"' || c == '\'');
        public static LexicalPair ExclamationFunc  = XmlLexicalTree.CreateLexicalFunc("[ ! ]", (c) => c == '!');
        public static LexicalPair QuestionFunc     = XmlLexicalTree.CreateLexicalFunc("[ ? ]", (c) => c == '?');
    }
}
