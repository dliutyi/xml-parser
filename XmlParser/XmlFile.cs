using System;
using System.Linq;

namespace XmlParser
{
    using LexicalPair = ValueTuple<string, TransitionDelegate>;
    enum Quote { Double, Single }
    class XmlFile
    {
        XmlNode _xmlRoot;
        XmlNode _currentXmlNode;
        LexicalTree _lexicalTree;

        Quote _quote;

        public XmlFile()
        {
            BuildLexicalTree();
        }

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
                    _quote = ParseQuote(c);
                    _currentXmlNode.Attributes.Last().HasValue = true;
                    return true; 
                case Action.AttrCloseValue:
                    if (_quote != ParseQuote(c))
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
            var currentLexicalNode = _lexicalTree.LexicalNode;

            _currentXmlNode = new XmlNode();
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

            _xmlRoot = _currentXmlNode;
            return _xmlRoot;
        }

        private Quote ParseQuote(char c) => c == '"' ? Quote.Double : Quote.Single;

        private void BuildLexicalTree()
        {
            var beforeStartWs  = LexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc  );
            var newTagStart    = LexicalTree.CreateTransition(XmlFormatter.StartBracketFunc);
            var exclamation    = LexicalTree.CreateTransition(XmlFormatter.ExclamationFunc );
            var version        = LexicalTree.CreateTransition(XmlFormatter.DoctypeValueFunc);
            var question       = LexicalTree.CreateTransition(XmlFormatter.QuestionFunc    );
            var startTagName   = LexicalTree.CreateTransition(XmlFormatter.LetterFunc, Action.CreateTag, Action.TagName);
            var tagName        = LexicalTree.CreateTransition(XmlFormatter.SymbolFunc, Action.TagName);
            var afterTagNameWs = LexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc);
            var selfCloseSlash = LexicalTree.CreateTransition(XmlFormatter.CloseTagSignFunc, Action.SelfCloseTag);
            var newTagEnd      = LexicalTree.CreateTransition(XmlFormatter.EndBracketFunc);

            var valueTagWs    = LexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc);
            var startTagValue = LexicalTree.CreateTransition(XmlFormatter.NodeValueFunc, Action.CreateValueTag, Action.TagValue);
            var tagValue      = LexicalTree.CreateTransition(XmlFormatter.NodeValueFunc, Action.TagValue);

            var pairCloseSlash      = LexicalTree.CreateTransition(XmlFormatter.CloseTagSignFunc, Action.CloseTag);
            var closeTagStartName   = LexicalTree.CreateTransition(XmlFormatter.LetterFunc);
            var closeTagName        = LexicalTree.CreateTransition(XmlFormatter.SymbolFunc);
            var afterCloseTagNameWs = LexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc);

            var startAttrName  = LexicalTree.CreateTransition(XmlFormatter.LetterFunc, Action.CreateAttribute, Action.AttrName);
            var attrName       = LexicalTree.CreateTransition(XmlFormatter.SymbolFunc, Action.AttrName);
            var equal          = LexicalTree.CreateTransition(XmlFormatter.EqualSignFunc );
            var bwEqAndQuoteWs = LexicalTree.CreateTransition(XmlFormatter.WhitespaceFunc);
            var quoteOpen      = LexicalTree.CreateTransition(XmlFormatter.QuoteSignFunc, Action.AttrCreateValue);
            var attrValue      = LexicalTree.CreateTransition(XmlFormatter.AttrValueFunc, Action.AttrValue);
            var quoteClose     = LexicalTree.CreateTransition(XmlFormatter.QuoteSignFunc, Action.AttrCloseValue);

            LexicalNode lexicalRoot = new LexicalNode();
            _lexicalTree = new LexicalTree(lexicalRoot);

            _lexicalTree.AddRules(
                 (LexicalTree.From(lexicalRoot, beforeStartWs),
                    LexicalTree.To(beforeStartWs, newTagStart)),
                 (LexicalTree.From(newTagStart),
                    LexicalTree.To(exclamation, question, startTagName, pairCloseSlash)),

                 /*  [ <? ]  ||  [ <! ]  */
                 (LexicalTree.From(exclamation, question),
                    LexicalTree.To(version)),
                 (LexicalTree.From(version),
                    LexicalTree.To(version, newTagEnd)),

                 /*  [ <tag ]  */
                 (LexicalTree.From(startTagName, tagName),
                    LexicalTree.To(afterTagNameWs, tagName, selfCloseSlash, newTagEnd)),
                 (LexicalTree.From(afterTagNameWs),
                    LexicalTree.To(afterTagNameWs, selfCloseSlash, newTagEnd, startAttrName, equal)),

                 /*  [ attrName="attrValue" ]  ||  [ attrName ]  */
                 (LexicalTree.From(startAttrName, attrName),
                    LexicalTree.To(attrName, equal, afterTagNameWs)),
                 (LexicalTree.From(equal, bwEqAndQuoteWs),
                    LexicalTree.To(bwEqAndQuoteWs, quoteOpen)),
                 (LexicalTree.From(quoteOpen, attrValue),
                    LexicalTree.To(quoteClose, attrValue)),
                 (LexicalTree.From(quoteClose),
                    LexicalTree.To(afterTagNameWs, selfCloseSlash, newTagEnd)),

                 /*  [ /> ]  ||  [ > ]  */
                 (LexicalTree.From(selfCloseSlash),
                    LexicalTree.To(newTagEnd)),
                 (LexicalTree.From(newTagEnd, valueTagWs),
                    LexicalTree.To(valueTagWs, startTagValue, newTagStart)),

                 (LexicalTree.From(startTagValue, tagValue),
                    LexicalTree.To(tagValue, newTagStart)),

                 /*  [ </tag> ]  */
                 (LexicalTree.From(pairCloseSlash),
                    LexicalTree.To(closeTagStartName)),
                 (LexicalTree.From(closeTagStartName, closeTagName),
                    LexicalTree.To(afterCloseTagNameWs, closeTagName, newTagEnd)),
                 (LexicalTree.From(afterCloseTagNameWs),
                    LexicalTree.To(afterCloseTagNameWs, newTagEnd))
            );
        }
    }
    static class XmlFormatter
    {
        public static LexicalPair StartBracketFunc = LexicalTree.CreateLexicalFunc("[ < ]", (c) => c == '<');
        public static LexicalPair EndBracketFunc   = LexicalTree.CreateLexicalFunc("[ > ]", (c) => c == '>');
        public static LexicalPair LetterFunc       = LexicalTree.CreateLexicalFunc("[^l ]", (c) => char.IsLetter(c));
        public static LexicalPair WhitespaceFunc   = LexicalTree.CreateLexicalFunc("[ _ ]", (c) => char.IsWhiteSpace(c) || char.IsSeparator(c));
        public static LexicalPair SymbolFunc       = LexicalTree.CreateLexicalFunc("[ a ]", (c) => char.IsLetterOrDigit(c) || c == '-' || c == ':');
        public static LexicalPair CloseTagSignFunc = LexicalTree.CreateLexicalFunc("[ / ]", (c) => c == '/');
        public static LexicalPair DoctypeValueFunc = LexicalTree.CreateLexicalFunc("[ d ]", (c) => c != '>');
        public static LexicalPair NodeValueFunc    = LexicalTree.CreateLexicalFunc("[ t ]", (c) => c != '<');
        public static LexicalPair AttrValueFunc    = LexicalTree.CreateLexicalFunc("[ t ]", (c) => c != '"' && c != '\'');
        public static LexicalPair EqualSignFunc    = LexicalTree.CreateLexicalFunc("[ = ]", (c) => c == '=');
        public static LexicalPair QuoteSignFunc    = LexicalTree.CreateLexicalFunc("[ q ]", (c) => c == '"' || c == '\'');
        public static LexicalPair ExclamationFunc  = LexicalTree.CreateLexicalFunc("[ ! ]", (c) => c == '!');
        public static LexicalPair QuestionFunc     = LexicalTree.CreateLexicalFunc("[ ? ]", (c) => c == '?');
    }
}
