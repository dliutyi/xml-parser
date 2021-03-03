function xmlFile() {

    const separators = [
        0x0000, 0x000A, 0x000C, 0x000D,
        0x0020, 0x00A0, 0x1680, 0x180E,
        0x2000, 0x2001, 0x2002, 0x2003,
        0x2004, 0x2005, 0x2006, 0x2007,
        0x2008, 0x2009, 0x200A, 0x2028,
        0x2029, 0x202F, 0x205F, 0x3000
    ];

    const emptyFunc        = ["", null];
    const startBracketFunc = ["[ < ]", (c) => c == '<'];
    const endBracketFunc   = ["[ > ]", (c) => c == '>'];
    const letterFunc       = ["[^l ]", (c) => c.toLowerCase() != c.toUpperCase()];
    const whitespaceFunc   = ["[ _ ]", (c) => c == ' ' || separators.includes(c.charCodeAt(0))];
    const symbolFunc       = ["[ a ]", (c) => c.toLowerCase() != c.toUpperCase() || (c > "0" && c <= "9") || c == '-' || c == ':'];
    const closeTagSignFunc = ["[ / ]", (c) => c == '/'];
    const doctypeValueFunc = ["[ d ]", (c) => c != '>'];
    const nodeValueFunc    = ["[ t ]", (c) => c != '<'];
    const attrValueFunc    = ["[ t ]", (c) => c != '"' && c != '\''];
    const equalSignFunc    = ["[ = ]", (c) => c == '='];
    const quoteSignFunc    = ["[ q ]", (c) => c == '"' || c == '\''];
    const exclamationFunc  = ["[ ! ]", (c) => c == '!'];
    const questionFunc     = ["[ ? ]", (c) => c == '?'];

    const Action_CreateTag       = 0;
    const Action_CreateValueTag  = 1;
    const Action_CreateAttribute = 2;
    const Action_TagName         = 3;
    const Action_TagValue        = 4;
    const Action_AttrName        = 5;
    const Action_AttrCreateValue = 6;
    const Action_AttrValue       = 7;
    const Action_AttrCloseValue  = 8;
    const Action_SelfCloseTag    = 9;
    const Action_CloseTag        = 10;

    let xmlParseState = {};
    let xmlGlobalRoot = null;
    let newLexicalRoot = null;

    function log(text) {
        console.log(text);
    }

    function createTransition(transitionPair, ...actions) {
        return {
            name: transitionPair[0],
            value: {
                transition: transitionPair[1],
                actions: actions
            },
            next: []
        }
    }

    function from(...params) {
        return params;
    }

    function to(...params) {
        return params;
    }

    function addRules(...rules) {
        rules.forEach(rule => rule[0].forEach(from => rule[1].forEach(to => from.next.push(to))));
    }

    function buildLexicalTree() {
        let beforeStartWs  = createTransition(whitespaceFunc  );
        let newTagStart    = createTransition(startBracketFunc);
        let exclamation    = createTransition(exclamationFunc );
        let version        = createTransition(doctypeValueFunc);
        let question       = createTransition(questionFunc    );
        let startTagName   = createTransition(letterFunc      , Action_CreateTag, Action_TagName);
        let tagName        = createTransition(symbolFunc      , Action_TagName);
        let afterTagNameWs = createTransition(whitespaceFunc  );
        let selfCloseSlash = createTransition(closeTagSignFunc, Action_SelfCloseTag);
        let newTagEnd      = createTransition(endBracketFunc  );

        let valueTagWs    = createTransition(whitespaceFunc);
        let startTagValue = createTransition(nodeValueFunc , Action_CreateValueTag, Action_TagValue);
        let tagValue      = createTransition(nodeValueFunc , Action_TagValue);

        let pairCloseSlash      = createTransition(closeTagSignFunc, Action_CloseTag);
        let closeTagStartName   = createTransition(letterFunc      );
        let closeTagName        = createTransition(symbolFunc      );
        let afterCloseTagNameWs = createTransition(whitespaceFunc  );

        let startAttrName  = createTransition(letterFunc    , Action_CreateAttribute, Action_AttrName);
        let attrName       = createTransition(symbolFunc    , Action_AttrName);
        let equal          = createTransition(equalSignFunc );
        let bwEqAndQuoteWs = createTransition(whitespaceFunc);
        let quoteOpen      = createTransition(quoteSignFunc , Action_AttrCreateValue);
        let attrValue      = createTransition(attrValueFunc , Action_AttrValue);
        let quoteClose     = createTransition(quoteSignFunc , Action_AttrCloseValue);

        newLexicalRoot = createTransition(emptyFunc);
        addRules(
            [from(newLexicalRoot, beforeStartWs),
               to(beforeStartWs, newTagStart)],
            [from(newTagStart),
               to(exclamation, question, startTagName, pairCloseSlash)],

            /*  [ <? ]  ||  [ <! ]  */
            [from(exclamation, question),
               to(version)],
            [from(version),
               to(version, newTagEnd)],

            /*  [ <tag ]  */
            [from(startTagName, tagName),
               to(afterTagNameWs, tagName, selfCloseSlash, newTagEnd)],
            [from(afterTagNameWs),
               to(afterTagNameWs, selfCloseSlash, newTagEnd, startAttrName, equal)],

            /*  [ attrName="attrValue" ]  ||  [ attrName ]  */
            [from(startAttrName, attrName),
               to(attrName, equal, afterTagNameWs)],
            [from(equal, bwEqAndQuoteWs),
               to(bwEqAndQuoteWs, quoteOpen)],
            [from(quoteOpen, attrValue),
               to(quoteClose, attrValue)],
            [from(quoteClose),
               to(afterTagNameWs, selfCloseSlash, newTagEnd)],

            /*  [ /> ]  ||  [ > ]  */
            [from(selfCloseSlash),
               to(newTagEnd)],
            [from(newTagEnd, valueTagWs),
               to(valueTagWs, startTagValue, newTagStart)],

            [from(startTagValue, tagValue),
               to(tagValue, newTagStart)],

            /*  [ </tag> ]  */
            [from(pairCloseSlash),
               to(closeTagStartName)],
            [from(closeTagStartName, closeTagName),
               to(afterCloseTagNameWs, closeTagName, newTagEnd)],
            [from(afterCloseTagNameWs),
               to(afterCloseTagNameWs, newTagEnd)]
        );
    }

    function createXmlNode() {
        return {
            name: "",
            value: "",
            parent: null,
            attributes: [],
            children: [],
            canHaveChildren: true,
            isValueOnly: false
        };
    }

    function createXmlAttribute() {
        return {
            name: "",
            value: "",
            hasValue: false
        };
    }

    function process(action, c) {
        switch (action) {
            case Action_CreateTag:
                if (xmlParseState.currentXmlNode.isValueOnly) {
                    xmlParseState.currentXmlNode.value = xmlParseState.currentXmlNode.value.trim();
                    xmlParseState.currentXmlNode = xmlParseState.currentXmlNode.parent;
                }

                let xmlNodeCT = createXmlNode();
                xmlNodeCT.parent = xmlParseState.currentXmlNode;

                xmlParseState.currentXmlNode.children.push(xmlNodeCT);
                xmlParseState.currentXmlNode = xmlNodeCT;
                return true;
            case Action_CreateValueTag:
                var xmlNodeCVT = createXmlNode();
                xmlNodeCVT.parent = xmlParseState.currentXmlNode;
                xmlNodeCVT.isValueOnly = true;
                xmlNodeCVT.canHaveChildren = false

                xmlParseState.currentXmlNode.children.push(xmlNodeCVT);
                xmlParseState.currentXmlNode = xmlNodeCVT;
                return true;
            case Action_TagName:
                xmlParseState.currentXmlNode.name += c;
                return true;
            case Action_TagValue:
                xmlParseState.currentXmlNode.value += c;
                return true;
            case Action_CreateAttribute:
                xmlParseState.currentXmlNode.attributes.push(createXmlAttribute());
                return true;
            case Action_AttrName:
                let attrsAN = xmlParseState.currentXmlNode.attributes;
                attrsAN[attrsAN.length - 1].name += c;
                return true;
            case Action_AttrCreateValue:
                xmlParseState.quote = c;

                let attrsACV = xmlParseState.currentXmlNode.attributes;
                attrsACV[attrsACV.length - 1].hasValue = true;
                return true;
            case Action_AttrCloseValue:
                let attrsACLV = xmlParseState.currentXmlNode.attributes;
                if (xmlParseState.quote != c) {
                    attrsACLV[attrsACLV.length - 1].value += c;
                    return false;
                }
                attrsACLV[attrsACLV.length - 1].value = attrsACLV[attrsACLV.length - 1].value.trim();
                return true;
            case Action_AttrValue:
                let attrsAV = xmlParseState.currentXmlNode.attributes;
                attrsAV[attrsAV.length - 1].value += c;
                return true;
            case Action_SelfCloseTag:
                xmlParseState.currentXmlNode.canHaveChildren = false;
                xmlParseState.currentXmlNode = xmlParseState.currentXmlNode.parent;
                return true;
            case Action_CloseTag:
                if (xmlParseState.currentXmlNode.isValueOnly) {
                    xmlParseState.currentXmlNode.value = xmlParseState.currentXmlNode.value.trim();
                    xmlParseState.currentXmlNode = xmlParseState.currentXmlNode.parent;
                }
                xmlParseState.currentXmlNode = xmlParseState.currentXmlNode.parent;
                return true;
            default: return false;
        }
    }

    function parse(xml) {
        log("Start parsing -\n" + xml + "\n");
        currentLexicalNode = newLexicalRoot;

        xmlParseState.currentXmlNode = createXmlNode();
        [...xml].every(c => {
            let parsingNodes = currentLexicalNode.next;
            log("Trying to parse - " + c + "\n   Trying apply - " + parsingNodes.map(n => n.name).join(", "));

            xmlParseState.isValid = !parsingNodes.every(parsingNode => {
                if (parsingNode.value.transition(c)) {
                    xmlParseState.isActionSuccess = parsingNode.value.actions.every(action => process(action, c));
                    if (xmlParseState.isActionSuccess) {
                        log("      " + parsingNode.name + " - Applied");
                        currentLexicalNode = parsingNode;
                    }
                    return false;
                }
                return true;
            });

            return xmlParseState.isValid;
        });

        if (!xmlParseState.isValid) {
            log("Parse status - Error");
            return null;
        }

        log("Parse status - OK");
        xmlGlobalRoot = xmlParseState.currentXmlNode;
        return xmlGlobalRoot;
    }

    function toString(xmlNode, emp = "") {
        if (xmlNode == null) return "";
        else if (xmlNode.isValueOnly) return emp + xmlNode.value + "\n";

        let xml = emp + "<" + xmlNode.name;
        xml += xmlNode.attributes.length > 0
            ? " " + xmlNode.attributes.map(attr => attr.hasValue ? attr.name + "=\"" + attr.value + "\"" : attr.name).join(" ")
            : "";

        return xmlNode.canHaveChildren
            ? xml + ">\n" + (xmlNode.children.map(child => toString(child, emp + "   ")).reduce(((childXml, next) => childXml + next), "")) + emp + "</" + xmlNode.name + ">\n"
            : xml + "/>\n";
    }

    buildLexicalTree();

    const fileContent = `
<?xml version="1.0" encoding="utf-8" ?> 
<database msg="'upload'" response='"success"'>
    <cities>
        <city>
            Kyiv
            <translation>
                <russian>Киев</russian>
            </translation>
        </city>
        <city>Mykolaiv</city>
    </cities>
    <people>
        <person firstname="Dmytro" lastname="Liutyi" />
        <person firstname="Julia" lastname="Liuta" />
    </people>
</database>
`;
    let xmlRoot = parse(fileContent);

    const xmlpadElement = document.getElementById("xmlpad");
    if (xmlRoot != null) {
        xmlpadElement.value = xmlRoot.children.reduce(((acc, child) => acc + toString(child)), "");
    }
}

xmlFile();