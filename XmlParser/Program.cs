using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using LexicalRule = XmlParser.LexicalTransition<XmlParser.TransitionDelegate, XmlParser.TransitionCallback>;
using QueueLexicalRule = XmlParser.LexicalQueueNode<XmlParser.LexicalTransition<XmlParser.TransitionDelegate, XmlParser.TransitionCallback>>;

namespace XmlParser
{
    static class Logger
    {
        static public void Log(string str)
        {
            Console.WriteLine(str);
        }
    }

    class Params<T>
    {
        public T[] Ts;
        public Params(T[] ts)
        {
            Ts = ts;
        }
    }
    class LexicalQueue
    {
        XmlNode xmlRoot = new XmlNode();
        QueueLexicalRule lexicalRoot = new QueueLexicalRule();

        private TransitionDelegate CreateLexicalFunc(TransitionDelegate transitionDelegate)
            => transitionDelegate;

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

        private void CopyTransitionRule(QueueLexicalRule to, QueueLexicalRule from)
        {
            to.Next = from.Next;
        }

        private Params<QueueLexicalRule> From(params QueueLexicalRule[] lexicalQueueNodes)
        {
            return new Params<QueueLexicalRule>(lexicalQueueNodes);
        }
        private Params<QueueLexicalRule> To(params QueueLexicalRule[] lexicalQueueNodes)
        {
            return new Params<QueueLexicalRule>(lexicalQueueNodes);
        }

        public LexicalQueue()
        {
            var StartBracketFunc    = CreateLexicalFunc((c) => c == '<' );
            var EndBracketFunc      = CreateLexicalFunc((c) => c == '>' );
            var LetterFunc          = CreateLexicalFunc((c) => char.IsLetter(c));
            var SymbolFunc          = CreateLexicalFunc((c) => char.IsLetter(c)     || char.IsDigit(c) || c == '-');
            var WhitespaceFunc      = CreateLexicalFunc((c) => char.IsWhiteSpace(c) || char.IsSeparator(c));
            var CloseTagSignFunc    = CreateLexicalFunc((c) => c == '/' );
            var DoctypeValueFunc    = CreateLexicalFunc((c) => c != '>' );
            var NodeValueFunc       = CreateLexicalFunc((c) => c != '<' );
            var AttrValueFunc       = CreateLexicalFunc((c) => c != '"' && c != '\'');
            var EqualSignFunc       = CreateLexicalFunc((c) => c == '=' );
            var QuoteSignFunc       = CreateLexicalFunc((c) => c == '"' || c == '\'' );
            var ExclamationSignFunc = CreateLexicalFunc((c) => c == '!' );
            var QuestionSignFunc    = CreateLexicalFunc((c) => c == '?' );

            var beforeStartWhitespace   = CreateTransition("[ _ ]", WhitespaceFunc     );
            var startTagBracket         = CreateTransition("[ < ]", StartBracketFunc   );
            var exclamationSign         = CreateTransition("[ ! ]", ExclamationSignFunc);
            var version                 = CreateTransition("[ d ]", DoctypeValueFunc   );
            var questionSign            = CreateTransition("[ ? ]", QuestionSignFunc   );
            var startTagName            = CreateTransition("[^l ]", LetterFunc, () => new List<Action> { Action.CreateTag, Action.TagName });
            var tagName                 = CreateTransition("[ a ]", SymbolFunc, () => new List<Action> { Action.TagName });
            var afterTagNameWhitespace  = CreateTransition("[ _ ]", WhitespaceFunc);
            var selfCloseSlash          = CreateTransition("[ / ]", CloseTagSignFunc, () => new List<Action> { Action.SelfCloseTag });
            var endTagBracket           = CreateTransition("[ > ]", EndBracketFunc);

            var valueTagWhitespace = CreateTransition("[ _ ]", WhitespaceFunc);
            var startTagValue      = CreateTransition("[^t ]", NodeValueFunc, () => new List<Action> { Action.CreateValueTag, Action.TagValue });
            var tagValue           = CreateTransition("[ t ]", NodeValueFunc, () => new List<Action> { Action.TagValue });

            var pairCloseSlash                  = CreateTransition("[ / ]", CloseTagSignFunc, () => new List<Action> { Action.CloseTag });
            var pairCloseTagStartName           = CreateTransition("[^l ]", LetterFunc    );
            var pairCloseTagName                = CreateTransition("[ a ]", SymbolFunc    );
            var afterPairCloseTagNameWhitespace = CreateTransition("[ _ ]", WhitespaceFunc);

            var startAttrName          = CreateTransition("[^l ]", LetterFunc, () => new List<Action> { Action.CreateAttribute, Action.AttrName });
            var attrName               = CreateTransition("[ a ]", SymbolFunc, () => new List<Action> { Action.AttrName });
            var equalSign              = CreateTransition("[ = ]", EqualSignFunc );
            var bwEqAndQuoteWhitespace = CreateTransition("[ _ ]", WhitespaceFunc);
            var quoteOpenSign          = CreateTransition("[ q ]", QuoteSignFunc, () => new List<Action> { Action.AttrCreateValue });
            var attrValue              = CreateTransition("[ t ]", AttrValueFunc, () => new List<Action> { Action.AttrValue });
            var quoteCloseSign         = CreateTransition("[ q ]", QuoteSignFunc, () => new List<Action> { Action.AttrCloseValue });

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

        public XmlNode Parse(string xml)
        {
            Logger.Log($"Start parsing -\n{ xml }\n");
            var quote = ' ';
            var currentXmlNode = xmlRoot;
            var currentLexicalNode = lexicalRoot;
            foreach (var c in xml)
            {
                var error = true;
                var parsingNodes = currentLexicalNode.Next;
             
                Logger.Log($"Trying to parse - { c }\n   Trying apply - { string.Join(", ", parsingNodes.Select(n => n.Name)) }");
                foreach (var parsingNode in parsingNodes)
                {
                    if (parsingNode.Value.Transition(c))
                    {
                        var actions = parsingNode.Value.Callback();
                        var isSuccess = true;
                        foreach (var action in actions)
                        {
                            switch (action)
                            {
                                case Action.CreateTag:
                                    if (currentXmlNode.IsValueOnly)
                                    {
                                        currentXmlNode.Value = currentXmlNode.Value.Trim();
                                        currentXmlNode = currentXmlNode.Parent;
                                    }

                                    var xmlNodeCT = new XmlNode { Parent = currentXmlNode };
                                    currentXmlNode.Children.Add(xmlNodeCT);
                                    currentXmlNode = xmlNodeCT;
                                    break;
                                case Action.CreateValueTag:
                                    var xmlNodeCVT = new XmlNode { Parent = currentXmlNode, IsValueOnly = true, CanHaveChildren = false };
                                    currentXmlNode.Children.Add(xmlNodeCVT);
                                    currentXmlNode = xmlNodeCVT;
                                    break;
                                case Action.TagName:
                                    currentXmlNode.Name += c;
                                    break;
                                case Action.TagValue:
                                    currentXmlNode.Value += c;
                                    break;
                                case Action.CreateAttribute:
                                    currentXmlNode.Attributes.Add(new XmlAttribute());
                                    break;
                                case Action.AttrName:
                                    currentXmlNode.Attributes.Last().Name += c;
                                    break;
                                case Action.AttrCreateValue:
                                    quote = c;
                                    currentXmlNode.Attributes.Last().HasValue = true;
                                    break;
                                case Action.AttrCloseValue:
                                    if (quote != c)
                                    {
                                        currentXmlNode.Attributes.Last().Value += c;
                                        isSuccess = false;
                                        break;
                                    }
                                    currentXmlNode.Attributes.Last().Value = currentXmlNode.Attributes.Last().Value.Trim();
                                    break;
                                case Action.AttrValue:
                                    currentXmlNode.Attributes.Last().Value += c;
                                    break;
                                case Action.SelfCloseTag:
                                    currentXmlNode.CanHaveChildren = false;
                                    currentXmlNode = currentXmlNode.Parent;
                                    break;
                                case Action.CloseTag:
                                    if (currentXmlNode.IsValueOnly)
                                    {
                                        currentXmlNode.Value = currentXmlNode.Value.Trim();
                                        currentXmlNode = currentXmlNode.Parent;
                                    }
                                    currentXmlNode = currentXmlNode.Parent;
                                    break;
                            }
                        }

                        Logger.Log($"      { parsingNode.Name } - Applied");

                        error = false;
                        if (isSuccess)
                        {
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
    class Program
    {
        static string ToString(XmlNode xmlNode, int spaces = 0)
        {
            if (xmlNode == null)
            {
                return "";
            }

            int tab = 3;
            string emp = new string(' ', spaces * tab);
            if (xmlNode.IsValueOnly)
            {
                return $"{ emp }{ xmlNode.Value }\n";
            }

            string xml = $"{ emp }<{ xmlNode.Name }";

            string attrs = string.Join(' ', xmlNode.Attributes.Select(attr => attr.HasValue ? $"{attr.Name}=\"{attr.Value}\"" : attr.Name));
            if (attrs.Length > 0)
            {
                xml += $" { attrs }";
            }

            if (xmlNode.CanHaveChildren)
            {
                xml += ">\n";
                foreach (var child in xmlNode.Children)
                {
                    xml += ToString(child, spaces + 1);
                }
                xml += $"{ emp }</{ xmlNode.Name }>\n";
            }
            else
            {
                xml += " />\n";
            }
            return xml;
        }

        static void Main(string[] args)
        {
            var xml = File.ReadAllText(@"file.xml");

            Logger.Log("XML PARSER");
            var xmlLexicalParser = new LexicalQueue();
            var xmlRoot = xmlLexicalParser.Parse(xml);
            Logger.Log("");
            if (xmlRoot != null) {
                xmlRoot.Children.ForEach(x => Logger.Log(ToString(x)));
            }
        }
    }
}
