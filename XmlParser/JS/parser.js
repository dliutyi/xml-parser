function parser() {
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
        var beforeStartWs = createTransition(whitespaceFunc);
    }

    function parse() {

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

    const fileXml = "<test></test>";
    let xmlRoot = null;

    const xmlpadElement = document.getElementById("xmlpad");
    xmlpadElement.value = toString(xmlRoot, "   ");
}

parser();