function editor() {

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

    function createXmlNode() {
        return {
            name: "",
            value: "",
            parent: null,
            attributes: [],
            children: [],
            canHaveChildren: true,
            isValueOnly: false,
            originPosition: {
                value: [0, 0]
            }
        };
    }

    function createXmlAttribute() {
        return {
            name: "",
            value: "",
            hasValue: false,
            quote: '"',
            originPosition: {
                name: [0, 0],
                value: [0, 0]
            }
        };
    }

    function createTransition(transitionPair, actionHandlers = []) {
        return {
            name: transitionPair[0],
            transition: transitionPair[1],
            actions: actionHandlers,
            next: []
        }
    }

    function createTagHandler(xmlParseState, symbol, index) {
        if (xmlParseState.currentXmlNode.isValueOnly) {
            xmlParseState.currentXmlNode.value = xmlParseState.currentXmlNode.value.trim();
            xmlParseState.currentXmlNode = xmlParseState.currentXmlNode.parent;
        }

        let xmlNode = createXmlNode();
        xmlNode.parent = xmlParseState.currentXmlNode;
        xmlParseState.currentXmlNode.children.push(xmlNode);
        xmlParseState.currentXmlNode = xmlNode;
        return true;
    }

    function createValueTagHandler(xmlParseState, symbol, index) {
        let xmlNode = createXmlNode();
        xmlNode.parent = xmlParseState.currentXmlNode;
        xmlNode.isValueOnly = true;
        xmlNode.canHaveChildren = false;
        xmlNode.originPosition.value[0] = index;

        xmlParseState.currentXmlNode.children.push(xmlNode);
        xmlParseState.currentXmlNode = xmlNode;
        return true;
    }

    function tagNameHandler(xmlParseState, symbol, index) {
        xmlParseState.currentXmlNode.name += symbol;
        return true;
    }

    function tagValueHandler(xmlParseState, symbol, index) {
        xmlParseState.currentXmlNode.value += symbol;
        xmlParseState.currentXmlNode.originPosition.value[1] = index;
        return true;
    }

    function createAttributeHandler(xmlParseState, symbol, index) {
        let xmlAttr = createXmlAttribute();
        xmlAttr.originPosition.name[0] = index;
        xmlParseState.currentXmlNode.attributes.push(xmlAttr);
        return true;
    }

    function attrNameHandler(xmlParseState, symbol, index) {
        let attrs = xmlParseState.currentXmlNode.attributes;
        attrs[attrs.length - 1].name += symbol;
        attrs[attrs.length - 1].originPosition.name[1] = index;
        return true;
    }

    function attrHasValueHandler(xmlParseState, symbol, index) {
        let attrs = xmlParseState.currentXmlNode.attributes;
        attrs[attrs.length - 1].hasValue = true;
        attrs[attrs.length - 1].originPosition.name[1] = index;
        return true;
    }

    function attrCreateValueHandler(xmlParseState, symbol, index) {
        let attrs = xmlParseState.currentXmlNode.attributes;
        attrs[attrs.length - 1].quote = symbol;
        attrs[attrs.length - 1].hasValue = true;
        attrs[attrs.length - 1].originPosition.value[0] = index;
        return true;
    }

    function attrValueHandler(xmlParseState, symbol, index) {
        let attrs = xmlParseState.currentXmlNode.attributes;
        attrs[attrs.length - 1].value += symbol;
        attrs[attrs.length - 1].originPosition.value[1] = index;
        return true;
    }

    function attrCloseValueHandler(xmlParseState, symbol, index) {
        let attrs = xmlParseState.currentXmlNode.attributes;
        attrs[attrs.length - 1].originPosition.value[1] = index;
        if (attrs[attrs.length - 1].quote != symbol) {
            attrs[attrs.length - 1].value += symbol;
            return false;
        }
        attrs[attrs.length - 1].value = attrs[attrs.length - 1].value.trim();
        return true;
    }

    function selfCloseTagHandler(xmlParseState, symbol, index) {
        xmlParseState.currentXmlNode.canHaveChildren = false;
        xmlParseState.currentXmlNode = xmlParseState.currentXmlNode.parent;
        return true;
    }

    function closeTagHandler(xmlParseState, symbol, index) {
        if (xmlParseState.currentXmlNode.isValueOnly) {
            xmlParseState.currentXmlNode.value = xmlParseState.currentXmlNode.value.trim();
            xmlParseState.currentXmlNode = xmlParseState.currentXmlNode.parent;
        }
        xmlParseState.currentXmlNode = xmlParseState.currentXmlNode.parent;
        return true;
    }

    function from(...params) {
        return params;
    }

    function to(...params) {
        return params;
    }

    function actions(...params) {
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
        let startTagName   = createTransition(letterFunc      , actions(createTagHandler, tagNameHandler));
        let tagName        = createTransition(symbolFunc      , actions(tagNameHandler));
        let afterTagNameWs = createTransition(whitespaceFunc  );
        let selfCloseSlash = createTransition(closeTagSignFunc, actions(selfCloseTagHandler));
        let newTagEnd      = createTransition(endBracketFunc  );

        let valueTagWs    = createTransition(whitespaceFunc);
        let startTagValue = createTransition(nodeValueFunc , actions(createValueTagHandler, tagValueHandler));
        let tagValue      = createTransition(nodeValueFunc , actions(tagValueHandler));

        let pairCloseSlash      = createTransition(closeTagSignFunc, actions(closeTagHandler));
        let closeTagStartName   = createTransition(letterFunc      );
        let closeTagName        = createTransition(symbolFunc      );
        let afterCloseTagNameWs = createTransition(whitespaceFunc  );

        let startAttrName  = createTransition(letterFunc    , actions(createAttributeHandler, attrNameHandler));
        let attrName       = createTransition(symbolFunc    , actions(attrNameHandler));
        let equal          = createTransition(equalSignFunc , actions(attrHasValueHandler));
        let bwEqAndQuoteWs = createTransition(whitespaceFunc);
        let quoteOpen      = createTransition(quoteSignFunc , actions(attrCreateValueHandler));
        let attrValue      = createTransition(attrValueFunc , actions(attrValueHandler));
        let quoteClose     = createTransition(quoteSignFunc , actions(attrCloseValueHandler));

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
               to(attrName, equal, afterTagNameWs, selfCloseSlash, newTagEnd)],
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

        return newLexicalRoot;
    }

    function parse(xmlLexicalRoot, xml) {
        let xmlParseState = {};
        let xmlGlobalRoot = createXmlNode();

        //console.log("Start parsing -\n" + xml + "\n");
        currentLexicalNode = xmlLexicalRoot;
        xmlParseState.currentXmlNode = xmlGlobalRoot;
        [...xml].every((symbol, index) => {
            let parsingNodes = currentLexicalNode.next;
            //console.log("Trying to parse - " + symbol + "\n   Trying apply - " + parsingNodes.map(n => n.name).join(", "));

            xmlParseState.isValid = !parsingNodes.every(parsingNode => {
                if (parsingNode.transition(symbol)) {
                    xmlParseState.isActionSuccess = parsingNode.actions.every(action => action(xmlParseState, symbol, index));
                    if (xmlParseState.isActionSuccess) {
                        //console.log("      " + parsingNode.name + " - Applied");
                        currentLexicalNode = parsingNode;
                    }
                    return false;
                }
                return true;
            });
            return xmlParseState.isValid;
        });
        return { valid: xmlParseState.isValid, root: xmlGlobalRoot };
    }

    function getRootName(xmlNode) {
        return xmlNode.name;
    }

    function getNodeByName(xmlNode, nodeName) {
        if (xmlNode == null) return null;
        else if (xmlNode.name == nodeName) {
            return xmlNode;
        }
        for (let i = 0; i < xmlNode.children.length; ++i) {
            const node = getNodeByName(xmlNode.children[i], nodeName);
            if (node) {
                return node;
            }
        }
        return null;
    }

    function appendNode(xmlNode, nodeName, node) {
        let foundNode = getNodeByName(xmlNode, nodeName);
        if (foundNode) {
            foundNode.children.push(node);
        }
    }

    // replace this method with that returns all attrs with the same nodeName and attrName
    function getAttr(xmlNode, nodeName, attrName) {
        let node = getNodeByName(xmlNode, nodeName);
        if (node == null) return [null, null];
        let attr = node.attributes.find(attr => attr.name.toLowerCase() == attrName.toLowerCase());
        return attr !== undefined ? [node, attr] : [node, null];
    }

    function removeNodeById(xmlNode, nodeName, id) {
        let attr = getAttr(xmlNode, nodeName, "id");
        if (attr[1].value.toLowerCase() == id.toLowerCase()) {
            let parent = attr[0].parent;
            parent.children = parent.children.filter(child => child != attr[0]);
        }
    }

    function updateAttribute(xmlNode, nodeName, attrName, attrValue) {
        let attr = getAttr(xmlNode, nodeName, attrName)[1];
        if (attr != null) {
            attr.value = attrValue;
        }
    }

    function getAttrValue(xmlNode, nodeName, attrName) {
        const attr = getAttr(xmlNode, nodeName, attrName)[1];
        return attr != null ? attr.value.toLowerCase() : null;
    }

    function doesIdExist(xmlNode, nodeName, id) {
        const value = getAttrValue(xmlNode, nodeName, "id");
        return (value != null && value == id.toLowerCase());
    }

    const cursor = () => "<span id='cursor'></span>";
    const tagValue = (name) => "<span class='value'>" + name + "</span>";
    const attrName = (name) => "<span class='attr-name'>" + name + "</span>";
    const attrValue = (name) => "<span class='attr-value'>" + name + "</span>";

    function beautify(xmlNode, emp = "") {
        if (xmlNode == null) return "";
        else if (xmlNode.isValueOnly) return emp + xmlNode.value + "\n";

        let xml = emp + "<" + xmlNode.name;
        xml += xmlNode.attributes.length > 0
            ? " " + xmlNode.attributes.map(attr => attr.hasValue ? attr.name + "=" + attr.quote + attr.value + attr.quote : attr.name).join(" ")
            : "";

        return xmlNode.canHaveChildren
            ? xml + ">\n" + (xmlNode.children.map(child => beautify(child, emp + "   ")).reduce(((childXml, next) => childXml + next), "")) + emp + "</" + xmlNode.name + ">\n"
            : xml + " />\n";
    }

    function traverse(xmlNode) {
        if (xmlNode == null) return [];
        else if (xmlNode.isValueOnly)
            return [{ start: xmlNode.originPosition.value[0], end: xmlNode.originPosition.value[1] + 1, func: tagValue }];

        let syntaxPoints =
            xmlNode.attributes.reduce(((attrs, attr) => {
                const name = { start: attr.originPosition.name[0], end: attr.originPosition.name[1] + 1, func: attrName };
                return attrs.concat(attr.hasValue
                    ? [name, { start: attr.originPosition.value[0], end: attr.originPosition.value[1], func: attrValue }]
                    : [name]);
            }), []);

        return xmlNode.canHaveChildren
            ? xmlNode.children.reduce((points, next) => points.concat(traverse(next)), syntaxPoints)
            : syntaxPoints;
    }

    function highlightSyntax(xmlNode, xmlContent, cursorPosition) {
        const replaceOpenTag = (symbol) => symbol == '<' ? "&lt;" : symbol;
        const placeCursor = (i, cp) => i == cp ? cursor() : ""; 

        let pointsIndex = 0;
        const syntaxPoints = traverse(xmlNode);
        return [...xmlContent].reduce(((state, symbol, i) => {
            if (pointsIndex < syntaxPoints.length && syntaxPoints[pointsIndex].start <= i && syntaxPoints[pointsIndex].end > i) {
                state.sub += placeCursor(i, cursorPosition) + replaceOpenTag(symbol);
            }
            else {
                if (pointsIndex < syntaxPoints.length && syntaxPoints[pointsIndex].end == i) {
                    state.highlightedContent += syntaxPoints[pointsIndex++].func(state.sub);
                    state.sub = "";
                }
                state.highlightedContent += placeCursor(i, cursorPosition) + replaceOpenTag(symbol);
            }
            return state;
        }), { highlightedContent: "", sub: "" }).highlightedContent;
    }

    function reparse(xmlpadElement, lexicalTree, state, ev) {
        console.log("reparse is activated");
        const getCursorPosition = (inputType) => {
            const selection = window.getSelection();
            if (selection.rangeCount == 0) return 0; 

            const range = selection.getRangeAt(0);
            let preCaretRange = range.cloneRange();

            preCaretRange.selectNodeContents(xmlpadElement);
            preCaretRange.setEnd(range.endContainer, range.endOffset);
            return preCaretRange.toString().length + (inputType == "insertParagraph");
        }

        const setCursorPosition = () => {
            const cursorElement = document.getElementById("cursor");

            let range = document.createRange();
            range.setStart(cursorElement, 0);
            range.setEnd(cursorElement, 0);
            range.collapse(true);

            let selection = window.getSelection();
            selection.removeAllRanges();
            selection.addRange(range);
        }

        const cursorPosition = getCursorPosition(ev ? ev.inputType : "");
        const result = parse(lexicalTree, xmlpadElement.innerText);

        state.parseResult = {
            root: result.valid ? result.root.children[0] : null,
            valid: result.valid
        };

        xmlpadElement.className = state.parseResult.valid ? "ok" : "fail";
        xmlpadElement.innerHTML = highlightSyntax(state.parseResult.root, xmlpadElement.innerText, cursorPosition);
        setCursorPosition();
    }

    function rebeautifier(xmlNode, lexicalTree) {
        const beautified = beautify(xmlNode);
        const beautifiedParseResult = parse(lexicalTree, beautified);

        state.parseResult = {
            root: beautifiedParseResult.valid ? beautifiedParseResult.root.children[0] : null,
            valid: beautifiedParseResult.valid
        };
        return highlightSyntax(state.parseResult.root, beautified, 0);
    }

    function setupEditor(xmlpadElement, lexicalTree, state, defaultText) {
        const parseResult = parse(lexicalTree, defaultText);
        if (parseResult.valid) {
            xmlpadElement.innerHTML = rebeautifier(parseResult.root.children[0], lexicalTree);
        }
        else {
            xmlpadElement.innerText = defaultText;
            xmlpadElement.className = "fail";
        }

        xmlpadElement.addEventListener("input", (ev) => reparse(xmlpadElement, lexicalTree, state, ev));
    }

    document.addEventListener('keydown', function (event) {
        if (event.target.id != "xmlpad") {
            return;
        }

        var code = event.keyCode || event.which;
        if (code === 9) { 
            event.preventDefault();
            document.execCommand('insertHTML', false, '   ');
        }
    });
    const fileContent = `
<PortOrder>
   <FI PmPlacementBrokerIK="1" PmPlacementCptyIK="2" ContingencyDescription="My Contingency" ContingencyID="1" ContingencyCount="1" ID="NEWID-20210305141116273-1-000" Acct="ACC 010" PortName="P ID 005" PorIK="8005" Side="1" OrderType="5" TimeInForce="1" SettlDt="2025-04-18" PortGrp="Group E" PorGrpIK="7005" DimPortMasterID="DEFAULT" PortfolioMasterIK="1" DimDeskID="DEFAULT 1" Txt="FI instrument BOND." InitiatedBy="4" SubsType="0" PortCcy="USD" Ccy="USD" ValueCcy="USD" CustodyIK="1" Custody="BARC LON" CustodyAcct="Barclays London" Custodian="ABCD123456" AutoAllocate="true">
      <Instrmt DimSecID="002819AB6" SecIK="1367087" Src="4" />
      <Hdr SID="SCD" TID="FNT" SSub="SCD Trader 014" MsgID="NEWID-20210305141116273-1-000" Snt="2013-07-02T15:31:16" PosRsnd="False" />
      <Estimates Value="1500000" Consideration="1600000" />
      <OrdQty Qty="1000000" />
      <TraderInfo PrefTrad="mark" />
      <Pty R="11" ID="QA Team" />
      <MatchingRules NettingRuleIK="2" BrokerNettingRuleIK="3" />
   </FI>
</PortOrder>
`;

    //<FreeCode ID="pORDERFC1" Value="ROUTEMOMGA" />

    const lexicalTree = buildLexicalTree();

    const rootElement = document.getElementById("root");
    const tagElement = document.getElementById("tag");
    const qtyElement = document.getElementById("qty");
    const toogleElement = document.getElementById("toogle");
    const xmlpadElement = document.getElementById("xmlpad");

    let state = {
        parseResult: null
    };

    setupEditor(xmlpadElement, lexicalTree, state, fileContent);

    rootElement.value = getRootName(state.parseResult.root);

    const node = getNodeByName(state.parseResult.root, "PortOrder");
    tagElement.value = node.children[0].name;

    let qty = getAttrValue(state.parseResult.root, "OrdQty", "Qty");
    qtyElement.value = qty;

    let momgaAttrID = createXmlAttribute();
    momgaAttrID.name = "ID";
    momgaAttrID.value = "pORDERFC1";
    momgaAttrID.hasValue = true;

    let momgaAttrValue = createXmlAttribute();
    momgaAttrValue.name = "Value";
    momgaAttrValue.value = "ROUTEMOMGA";
    momgaAttrValue.hasValue = true;

    let momgaFreeCode = createXmlNode();
    momgaFreeCode.name = "FreeCode";
    momgaFreeCode.canHaveChildren = false;
    momgaFreeCode.attributes.push(momgaAttrID, momgaAttrValue);

    let toogleState = !doesIdExist(state.parseResult.root, momgaFreeCode.name, momgaAttrID.value);
    toogleElement.className = toogleState ? "on" : "off";

    toogleElement.addEventListener("click", () => {
        toogleState = !toogleState;
        toogleElement.className = toogleState ? "on" : "off";

        if (!toogleState) {
            appendNode(state.parseResult.root, node.children[0].name, momgaFreeCode);
        }
        else {
            removeNodeById(state.parseResult.root, "FreeCode", "pORDERFC1");
        }

        xmlpadElement.innerHTML = rebeautifier(state.parseResult.root, lexicalTree);
    });

    qtyElement.addEventListener("input", () => {
        updateAttribute(state.parseResult.root, "OrdQty", "Qty", qtyElement.value);
        xmlpadElement.innerHTML = rebeautifier(state.parseResult.root, lexicalTree);
    });
}

editor();