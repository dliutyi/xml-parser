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

    function log(text) {
        //console.log(text);
    }

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
                name: [0, 0],
                value: [0, 0]
            }
        };
    }

    function createXmlAttribute() {
        return {
            name: "",
            value: "",
            hasValue: false,
            quote: ' ',
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
        xmlNode.originPosition.name[0] = index;

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
        xmlParseState.currentXmlNode.originPosition.name[1] = index;
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

        log("Start parsing -\n" + xml + "\n");
        currentLexicalNode = xmlLexicalRoot;
        xmlParseState.currentXmlNode = xmlGlobalRoot;
        [...xml].every((symbol, index) => {
            let parsingNodes = currentLexicalNode.next;
            log("Trying to parse - " + symbol + "\n   Trying apply - " + parsingNodes.map(n => n.name).join(", "));

            xmlParseState.isValid = !parsingNodes.every(parsingNode => {
                if (parsingNode.transition(symbol)) {
                    xmlParseState.isActionSuccess = parsingNode.actions.every(action => action(xmlParseState, symbol, index));
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
        }

        log("Parse status - OK");
        return { valid: xmlParseState.isValid, root: xmlGlobalRoot };
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

    function highlightSyntax(xmlNode, xmlContent, cursorPosition) {
        const tryPushCursor = (start, end) => {
            if (start >= cursorPosition && cursorPosition < end) {
                xmlContent = xmlContent.slice(0, cursorPosition) + cursor() + xmlContent.slice(cursorPosition);
            }
        };

        if (xmlNode == null) return "";
        else if (xmlNode.isValueOnly) {
            const startIndex = xmlNode.originPosition.value[0];
            const endIndex = xmlNode.originPosition.value[1] + 1;

            return xmlContent.slice(0, startIndex) + tagValue(xmlContent.slice(startIndex, endIndex)) + xmlContent.slice(endIndex);
        }
        else if (xmlNode.canHaveChildren) {
            const index = xmlNode.originPosition.name[1];
            xmlContent = xmlContent.slice(0, index - 1) + "&lt;" + xmlContent.slice(index);

            for (let i = xmlNode.children.length - 1; i >= 0; --i) {
                xmlContent = highlightSyntax(xmlNode.children[i], xmlContent);
            }
        }

        const attrs = xmlNode.attributes;
        for (let i = attrs.length - 1; i >= 0; --i) {
            if (attrs[i].hasValue) {
                const startValueIndex = attrs[i].originPosition.value[0];
                const endValueIndex = attrs[i].originPosition.value[1] + 1;
                xmlContent = xmlContent.slice(0, startValueIndex) + attrValue(xmlContent.slice(startValueIndex, endValueIndex)) + xmlContent.slice(endValueIndex);
            }

            const startNameIndex = attrs[i].originPosition.name[0];
            const endNameIndex = attrs[i].originPosition.name[1] + 1;
            xmlContent = xmlContent.slice(0, startNameIndex) + attrName(xmlContent.slice(startNameIndex, endNameIndex)) + xmlContent.slice(endNameIndex);
        }

        const index = xmlNode.originPosition.name[0];
        xmlContent = xmlContent.slice(0, index - 1) + "&lt;" + xmlContent.slice(index);

        return xmlContent;
    }

    function reverseTraverse(xmlNode) {
        if (xmlNode == null) return "";

        if (xmlNode.isValueOnly) {
            return " [" + xmlNode.originPosition.value[0] + ", " + xmlNode.originPosition.value[1] + "]" + xmlNode.value + "\n";
        }

        let xmlContent = "";
        if (xmlNode.canHaveChildren) {
            xmlContent += " [" + xmlNode.originPosition.name[1] + "]" + xmlNode.name + "\n";
            for (let i = xmlNode.children.length - 1; i >= 0; --i) {
                xmlContent += reverseTraverse(xmlNode.children[i]);
            }
        }

        const attrs = xmlNode.attributes;
        for (let i = attrs.length - 1; i >= 0; --i) {
            xmlContent += " [" + attrs[i].originPosition.value[0] + ", " + attrs[i].originPosition.value[1] + "]" + attrs[i].value + "=" + "[" + attrs[i].originPosition.name[0] + ", " + attrs[i].originPosition.name[1] + "]"  + attrs[i].name;
        }

        return xmlContent + " [" + xmlNode.originPosition.name[0] + "]" + xmlNode.name + "\n";
    }

    //function beautify(xmlNode, emp = "") {
    //    if (xmlNode == null) return "";
    //    else if (xmlNode.isValueOnly) return emp + tagValue(xmlNode.value) + "\n";

    //    let xml = emp + "&lt;" + xmlNode.name;
    //    xml += xmlNode.attributes.length > 0
    //        ? " " + xmlNode.attributes.map(attr => attr.hasValue ? attrName(attr.name + "=") + attrValue(attr.quote + attr.value + attr.quote) : attrName(attr.name)).join(" ")
    //        : "";

    //    return xmlNode.canHaveChildren
    //        ? xml + ">\n" + (xmlNode.children.map(child => beautify(child, emp + "   ")).reduce(((childXml, next) => childXml + next), "")) + emp + "&lt;/" + xmlNode.name + ">\n"
    //        : xml + "/>\n";
    //}

    function setupEditor(defaultText) {
        const xmlpadElement = document.getElementById("xmlpad");

        const lexicalTree = buildLexicalTree();
        const parseResult = parse(lexicalTree, defaultText);
        if (parseResult.valid) {
            const beautified = beautify(parseResult.root.children[0]);
            console.log(beautified);

            const beautifiedParseResult = parse(lexicalTree, beautified);
            xmlpadElement.innerHTML = highlightSyntax(beautifiedParseResult.root.children[0], beautified, 0);

            console.log(reverseTraverse(beautifiedParseResult.root.children[0]));
        }
        else {
            xmlpadElement.innerText = defaultText; 
            xmlpadElement.className = "fail";
        }

        const reparse = () => {
            const getCursorPosition = () => {
                const range = window.getSelection().getRangeAt(0);
                let preCaretRange = range.cloneRange();

                preCaretRange.selectNodeContents(xmlpadElement);
                preCaretRange.setEnd(range.endContainer, range.endOffset);
                return preCaretRange.toString().length;
            }

            const setCursorPosition = (node, pos) => {
                const cursorElement = document.getElementById("cursor");
                if (cursorElement) {
                    let range = document.createRange();
                    range.setStart(cursorElement, 0);
                    range.setEnd(cursorElement, 0);
                    range.collapse(true);

                    let selection = window.getSelection();
                    selection.removeAllRanges();
                    selection.addRange(range);
                }
            }

            const cursorPosition = getCursorPosition();
            console.log("Cursor position is " + cursorPosition);

            const parseResult = parse(lexicalTree, xmlpadElement.innerText);
            if (parseResult.valid) {
                xmlpadElement.className = "ok";
            }
            else {
                xmlpadElement.className = "fail";
            }

            let xmlContent = highlightSyntax(parseResult.root.children[0], xmlpadElement.innerText, cursorPosition);
            xmlpadElement.innerHTML = xmlContent;

            const getCursorNode = (cp) => {
                let treeWalker = document.createTreeWalker(
                    xmlpadElement,
                    NodeFilter.SHOW_TEXT,
                    { acceptNode: function (node) { return NodeFilter.FILTER_ACCEPT; } },
                    false
                );

                let position = 0;
                let currentNode = treeWalker.nextNode();
                while (currentNode) {
                    position += currentNode.length;

                    if (cp < position) {
                        console.log(position);

                        let textElement = document.createElement("span");
                        let textNode1 = document.createTextNode(currentNode.textContent.slice(0, position - cp));
                        let cursorElement = document.createElement("span");
                        cursorElement.id = "cursor";
                        let textNode2 = document.createTextNode(currentNode.textContent.slice(position - cp));
                        textElement.appendChild(textNode1);
                        textElement.appendChild(cursorElement);
                        textElement.appendChild(textNode2);

                        currentNode.replaceWith(textElement);
                        return;
                    }
                    currentNode = treeWalker.nextNode();
                }

                return [xmlpadElement, 0];
            };

            getCursorNode(cursorPosition);
            //console.log(cursorNode);
            setCursorPosition();
        };

        xmlpadElement.addEventListener("input", reparse);
    }

    const fileContent = `
<database msg="'upload'" response='"success"'>
    <cities>
        <city>
            Kyiv
            <translation a>
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

    setupEditor(fileContent);
}

editor();