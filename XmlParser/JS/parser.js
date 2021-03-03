function xmlFile() {
    const startBracketFunc = ["[ < ]", (c) => c == '<'];
    const endBracketFunc = ["[ > ]", (c) => c == '>'];
    const letterFunc = ["[^l ]", (c) => char.IsLetter(c)];
    const whitespaceFunc = ["[ _ ]", (c) => char.IsWhiteSpace(c) || char.IsSeparator(c)];
    const symbolFunc = ["[ a ]", (c) => char.IsLetterOrDigit(c) || c == '-' || c == ':'];
    const closeTagSignFunc = ["[ / ]", (c) => c == '/'];
    const doctypeValueFunc = ["[ d ]", (c) => c != '>'];
    const nodeValueFunc = ["[ t ]", (c) => c != '<'];
    const attrValueFunc = ["[ t ]", (c) => c != '"' && c != '\''];
    const equalSignFunc = ["[ = ]", (c) => c == '='];
    const quoteSignFunc = ["[ q ]", (c) => c == '"' || c == '\''];
    const exclamationFunc = ["[ ! ]", (c) => c == '!'];
    const questionFunc = ["[ ? ]", (c) => c == '?'];

    const Action_CreateTag = 0;
    const Action_CreateValueTag = 1;
    const Action_CreateAttribute = 2;
    const Action_TagName = 3;
    const Action_TagValue = 4;
    const Action_AttrName = 5;
    const Action_AttrCreateValue = 6;
    const Action_AttrValue = 7;
    const Action_AttrCloseValue = 8;
    const Action_SelfCloseTag = 9;
    const Action_CloseTag = 10;

    function createTransition(transitionPair, ...actions) {
        return {
            name: transitionPair[0],
            value: {
                transition: transitionPair[1],
                actions: actions
            }
        }
    }

    function buildLexicalTree() {
        let beforeStartWs = createTransition(whitespaceFunc);
        let newTagStart = createTransition(startBracketFunc);
        let exclamation = createTransition(exclamationFunc);
        let version = createTransition(doctypeValueFunc);
        let question = createTransition(questionFunc);
        let startTagName = createTransition(letterFunc, Action_CreateTag, Action_TagName);
        let tagName = createTransition(symbolFunc, Action_TagName);
        let afterTagNameWs = createTransition(whitespaceFunc);
        let selfCloseSlash = createTransition(closeTagSignFunc, Action_SelfCloseTag);
        let newTagEnd = createTransition(endBracketFunc);

        let valueTagWs = createTransition(whitespaceFunc);
        let startTagValue = createTransition(nodeValueFunc, Action_CreateValueTag, Action_TagValue);
        let tagValue = createTransition(nodeValueFunc, Action_TagValue);

        let pairCloseSlash = createTransition(closeTagSignFunc, Action_CloseTag);
        let closeTagStartName = createTransition(letterFunc);
        let closeTagName = createTransition(symbolFunc);
        let afterCloseTagNameWs = createTransition(whitespaceFunc);

        let startAttrName = createTransition(letterFunc, Action_CreateAttribute, Action_AttrName);
        let attrName = createTransition(symbolFunc, Action_AttrName);
        let equal = createTransition(equalSignFunc);
        let bwEqAndQuoteWs = createTransition(whitespaceFunc);
        let quoteOpen = createTransition(quoteSignFunc, Action_AttrCreateValue);
        let attrValue = createTransition(attrValueFunc, Action_AttrValue);
        let quoteClose = createTransition(quoteSignFunc, Action_AttrCloseValue);
    }

    function processAction(action) {

    }

    function parse(xml) {
        return null;
    }

    function toString(xmlNode, emp) {
        if (xmlNode == null) return "";
        else if (xmlNode.isValueOnly) return emp + xmlNode.value + "\n";

        let xml = emp + "<" + xmlNode.name;
        xml += xmlNode.attributes.length > 0
            ? xmlNode.attributes.map(attr => attr.hasValue ? attr.name + " = \"" + attr.value + "\"" : attr.name).join(" ")
            : "";

        return xmlNode.canHaveChildren
            ? xml + ">\n" + (xmlNode.children.map(child => toString(child, emp + "   ")).reduce((childXml, next) => childXml + next)) + emp + "</" + xmlNode.Name + ">\n"
            : xml + "/>\n";
    }

    buildLexicalTree();

    const fileContent = "<test></test>";
    let xmlRoot = parse(fileContent);

    const xmlpadElement = document.getElementById("xmlpad");
    xmlpadElement.value = toString(xmlRoot, "   ");
}

xmlFile();