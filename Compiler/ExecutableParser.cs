﻿using System.Collections.Generic;
using System.Linq;
using Crayon.ParseTree;

namespace Crayon
{
    internal static class ExecutableParser
    {
        private static readonly HashSet<string> ASSIGNMENT_OPS = new HashSet<string>("= += -= *= /= %= |= &= ^= <<= >>=".Split(' '));

        public static Executable Parse(Parser parser, TokenStream tokens, bool simpleOnly, bool semicolonPresent, bool isRoot, Executable owner)
        {
            string value = tokens.PeekValue();
            
            if (!simpleOnly)
            {
                Token staticToken = null;
                Token finalToken = null;
                while (value == "static" || value == "final")
                {
                    if (value == "static" && staticToken == null)
                    {
                        staticToken = tokens.Pop();
                        value = tokens.PeekValue();
                    }
                    if (value == "final" && finalToken == null)
                    {
                        finalToken = tokens.Pop();
                        value = tokens.PeekValue();
                    }
                }

                if (staticToken != null || finalToken != null)
                {
                    if (value != "class")
                    {
                        if (staticToken != null)
                        {
                            throw new ParserException(staticToken, "Only classes, methods, and fields may be marked as static");
                        }
                        else
                        {
                            throw new ParserException(finalToken, "Only classes may be marked as final.");
                        }
                    }

                    if (staticToken != null && finalToken != null)
                    {
                        throw new ParserException(staticToken, "Classes cannot be both static and final.");
                    }
                }

                if (!isRoot && (value == "function" || value == "class"))
                {
                    throw new ParserException(tokens.Peek(), (value == "function" ? "Function" : "Class") + " definition cannot be nested in another construct.");
                }

                if (parser.IsTranslateMode && value == "struct")
                {
                    if (!isRoot)
                    {
                        throw new ParserException(tokens.Peek(), "structs cannot be nested into any other construct.");
                    }

                    return ParseStruct(tokens, owner);
                }

                if (value == "import")
                {
                    Token importToken = tokens.PopExpected("import");

                    bool inline = parser.IsTranslateMode && tokens.PopIfPresent("inline");

                    if (inline)
                    {
                        Token fileToken = tokens.Pop();
                        char c = fileToken.Value[0];
                        if (c != '\'' && c != '"') throw new ParserException(fileToken, "Inline imports are supposed to be strings.");
                        tokens.PopExpected(";");
                        string inlineImportFileName = fileToken.Value.Substring(1, fileToken.Value.Length - 2);
                        string inlineImportFileContents;
                        if (inlineImportFileName.StartsWith("LIB:"))
                        {
                            string[] parts = inlineImportFileName.Split(':');
                            string libraryName = parts[1];
                            string filename = FileUtil.JoinPath(parts[2].Split('/'));
                            Library library = parser.SystemLibraryManager.GetLibraryFromKey(libraryName.ToLower());
                            inlineImportFileContents = library.ReadFile(filename, false);
                        }
                        else
                        {
                            inlineImportFileContents = Util.ReadInterpreterFileInternally(inlineImportFileName);
                        }
                        // TODO: Anti-pattern alert. Clean this up.
                        if (inlineImportFileContents.Contains("%%%"))
                        {
                            Dictionary<string, string> replacements = parser.NullablePlatform.InterpreterCompiler.BuildReplacementsDictionary();
                            inlineImportFileContents = Constants.DoReplacements(inlineImportFileContents, replacements);
                        }
                        Token[] inlineTokens = Tokenizer.Tokenize(inlineImportFileName, inlineImportFileContents, 0, true);

                        tokens.InsertTokens(inlineTokens);

                        return ExecutableParser.Parse(parser, tokens, simpleOnly, semicolonPresent, isRoot, owner); // start exectuable parser anew.
                    }

                    if (!isRoot)
                    {
                        throw new ParserException(tokens.Peek(), "Imports can only be made from the root of a file and cannot be nested inside other constructs.");
                    }

                    List<string> importPathBuilder = new List<string>();
                    while (!tokens.PopIfPresent(";"))
                    {
                        if (importPathBuilder.Count > 0)
                        {
                            tokens.PopExpected(".");
                        }

                        Token pathToken = tokens.Pop();
                        Parser.VerifyIdentifier(pathToken);
                        importPathBuilder.Add(pathToken.Value);
                    }
                    string importPath = string.Join(".", importPathBuilder);

                    return new ImportStatement(importToken, importPath);
                }

                if (value == "enum")
                {
                    if (!isRoot)
                    {
                        throw new ParserException(tokens.Peek(), "Enums can only be defined from the root of a file and cannot be nested inside functions/loops/etc.");
                    }

                    return ParseEnumDefinition(parser, tokens, owner);
                }

                if (value == "namespace")
                {
                    if (!isRoot)
                    {
                        throw new ParserException(tokens.Peek(), "Namespace declarations cannot be nested in other constructs.");
                    }
                }

                switch (value)
                {
                    case "namespace": return ParseNamespace(parser, tokens, owner);
                    case "function": return ParseFunction(parser, tokens, owner);
                    case "class": return ParseClassDefinition(parser, tokens, owner, staticToken, finalToken);
                    case "enum": return ParseEnumDefinition(parser, tokens, owner);
                    case "for": return ParseFor(parser, tokens, owner);
                    case "while": return ParseWhile(parser, tokens, owner);
                    case "do": return ParseDoWhile(parser, tokens, owner);
                    case "switch": return ParseSwitch(parser, tokens, owner);
                    case "if": return ParseIf(parser, tokens, owner);
                    case "try": return ParseTry(parser, tokens, owner);
                    case "return": return ParseReturn(tokens, owner);
                    case "break": return ParseBreak(tokens, owner);
                    case "continue": return ParseContinue(tokens, owner);
                    case "const": return ParseConst(parser, tokens, owner);
                    case "constructor": return ParseConstructor(parser, tokens, owner);
                    default: break;
                }
            }

            Expression expr = ExpressionParser.Parse(tokens, owner);
            value = tokens.PeekValue();
            if (ASSIGNMENT_OPS.Contains(value))
            {
                Token assignment = tokens.Pop();
                Expression assignmentValue = ExpressionParser.Parse(tokens, owner);
                if (semicolonPresent) tokens.PopExpected(";");
                return new Assignment(expr, assignment, assignment.Value, assignmentValue, owner);
            }

            if (semicolonPresent)
            {
                tokens.PopExpected(";");
            }

            return new ExpressionAsExecutable(expr, owner);
        }

        private static Executable ParseConstructor(Parser parser, TokenStream tokens, Executable owner)
        {
            Token constructorToken = tokens.PopExpected("constructor");
            tokens.PopExpected("(");
            List<Token> argNames = new List<Token>();
            List<Expression> argValues = new List<Expression>();
            bool optionalArgFound = false;
            while (!tokens.PopIfPresent(")"))
            {
                if (argNames.Count > 0)
                {
                    tokens.PopExpected(",");
                }

                Token argName = tokens.Pop();
                Parser.VerifyIdentifier(argName);
                Expression defaultValue = null;
                if (tokens.PopIfPresent("="))
                {
                    defaultValue = ExpressionParser.Parse(tokens, owner);
                    optionalArgFound = true;
                }
                else if (optionalArgFound)
                {
                    throw new ParserException(argName, "All optional arguments must come at the end of the argument list.");
                }

                argNames.Add(argName);
                argValues.Add(defaultValue);
            }

            List<Expression> baseArgs = new List<Expression>();
            Token baseToken = null;
            if (tokens.PopIfPresent(":"))
            {
                baseToken = tokens.PopExpected("base");
                tokens.PopExpected("(");
                while (!tokens.PopIfPresent(")"))
                {
                    if (baseArgs.Count > 0)
                    {
                        tokens.PopExpected(",");
                    }

                    baseArgs.Add(ExpressionParser.Parse(tokens, owner));
                }
            }

            IList<Executable> code = Parser.ParseBlock(parser, tokens, true, owner);

            return new ConstructorDefinition(constructorToken, argNames, argValues, baseArgs, code, baseToken, owner);
        }

        private static Executable ParseConst(Parser parser, TokenStream tokens, Executable owner)
        {
            Token constToken = tokens.PopExpected("const");
            Token nameToken = tokens.Pop();
            Parser.VerifyIdentifier(nameToken);
            tokens.PopExpected("=");
            Expression expression = ExpressionParser.Parse(tokens, owner);
            tokens.PopExpected(";");

            return new ConstStatement(constToken, nameToken, parser.CurrentNamespace, expression, owner);
        }

        private static Executable ParseEnumDefinition(Parser parser, TokenStream tokens, Executable owner)
        {
            Token enumToken = tokens.PopExpected("enum");
            Token nameToken = tokens.Pop();
            Parser.VerifyIdentifier(nameToken);
            string name = nameToken.Value;
            tokens.PopExpected("{");
            bool nextForbidden = false;
            List<Token> items = new List<Token>();
            List<Expression> values = new List<Expression>();
            while (!tokens.PopIfPresent("}"))
            {
                if (nextForbidden) tokens.PopExpected("}"); // crash

                Token enumItem = tokens.Pop();
                Parser.VerifyIdentifier(enumItem);
                if (tokens.PopIfPresent("="))
                {
                    values.Add(ExpressionParser.Parse(tokens, owner));
                }
                else
                {
                    values.Add(null);
                }
                nextForbidden = !tokens.PopIfPresent(",");
                items.Add(enumItem);
            }

            return new EnumDefinition(enumToken, nameToken, parser.CurrentNamespace, items, values, owner);
        }

        private static Executable ParseClassDefinition(Parser parser, TokenStream tokens, Executable owner, Token staticToken, Token finalToken)
        {
            Token classToken = tokens.PopExpected("class");
            Token classNameToken = tokens.Pop();
            Parser.VerifyIdentifier(classNameToken);
            List<Token> baseClassTokens = new List<Token>();
            List<string> baseClassStrings = new List<string>();
            if (tokens.PopIfPresent(":"))
            {
                if (baseClassTokens.Count > 0)
                {
                    tokens.PopExpected(",");
                }

                Token baseClassToken = tokens.Pop();
                string baseClassName = baseClassToken.Value;

                Parser.VerifyIdentifier(baseClassToken);
                while (tokens.PopIfPresent("."))
                {
                    Token baseClassTokenNext = tokens.Pop();
                    Parser.VerifyIdentifier(baseClassTokenNext);
                    baseClassName += "." + baseClassTokenNext.Value;
                }

                baseClassTokens.Add(baseClassToken);
                baseClassStrings.Add(baseClassName);
            }

            ClassDefinition cd = new ClassDefinition(
                classToken,
                classNameToken,
                baseClassTokens,
                baseClassStrings,
                parser.CurrentNamespace,
                owner,
                staticToken,
                finalToken);

            tokens.PopExpected("{");
            List<FunctionDefinition> methods = new List<FunctionDefinition>();
            List<FieldDeclaration> fields = new List<FieldDeclaration>();
            ConstructorDefinition constructorDef = null;
            ConstructorDefinition staticConstructorDef = null;

            while (!tokens.PopIfPresent("}"))
            {
                Dictionary<string, List<Annotation>> annotations = null; 

                while (tokens.IsNext("@"))
                {
                    annotations = annotations ?? new Dictionary<string, List<Annotation>>();
                    Annotation annotation = AnnotationParser.ParseAnnotation(tokens);
                    if (!annotations.ContainsKey(annotation.Type))
                    {
                        annotations[annotation.Type] = new List<Annotation>();
                    }

                    annotations[annotation.Type].Add(annotation);
                }

                if (tokens.IsNext("function") || tokens.AreNext("static", "function"))
                {
                    methods.Add((FunctionDefinition)ExecutableParser.ParseFunction(parser, tokens, cd));
                }
                else if (tokens.IsNext("constructor"))
                {
                    if (constructorDef != null)
                    {
                        throw new ParserException(tokens.Pop(), "Multiple constructors are not allowed. Use optional arguments.");
                    }

                    constructorDef = (ConstructorDefinition)ExecutableParser.ParseConstructor(parser, tokens, cd);

                    if (annotations != null && annotations.ContainsKey("private"))
                    {
                        constructorDef.PrivateAnnotation = annotations["private"][0];
                        annotations["private"].RemoveAt(0);
                    }
                }
                else if (tokens.AreNext("static", "constructor"))
                {
                    tokens.Pop(); // static token
                    if (staticConstructorDef != null)
                    {
                        throw new ParserException(tokens.Pop(), "Multiple static constructors are not allowed.");
                    }

                    staticConstructorDef = (ConstructorDefinition)ExecutableParser.ParseConstructor(parser, tokens, cd);
                }
                else if (tokens.IsNext("field") || tokens.AreNext("static", "field"))
                {
                    fields.Add(ExecutableParser.ParseField(tokens, cd));
                }
                else
                {
                    tokens.PopExpected("}");
                }

                if (annotations != null)
                {
                    foreach (List<Annotation> annotationsOfType in annotations.Values)
                    {
                        if (annotationsOfType.Count > 0)
                        {
                            throw new ParserException(annotationsOfType[0].FirstToken, "Unused or extra annotation.");
                        }
                    }
                }
            }

            cd.Methods = methods.ToArray();
            cd.Constructor = constructorDef;
            cd.StaticConstructor = staticConstructorDef;
            cd.Fields = fields.ToArray();

            return cd;
        }

        private static FieldDeclaration ParseField(TokenStream tokens, ClassDefinition owner)
        {
            bool isStatic = tokens.PopIfPresent("static");
            Token fieldToken = tokens.PopExpected("field");
            Token nameToken = tokens.Pop();
            Parser.VerifyIdentifier(nameToken);
            FieldDeclaration fd = new FieldDeclaration(fieldToken, nameToken, owner, isStatic);
            if (tokens.PopIfPresent("="))
            {
                fd.DefaultValue = ExpressionParser.Parse(tokens, owner);
            }
            tokens.PopExpected(";");
            return fd;
        }

        private static Executable ParseStruct(TokenStream tokens, Executable owner)
        {
            Token structToken = tokens.PopExpected("struct");
            Token structNameToken = tokens.Pop();
            Parser.VerifyIdentifier(structNameToken);

            tokens.PopExpected("{");

            List<Token> fieldTokens = new List<Token>();
            List<Annotation> typeAnnotations = new List<Annotation>();
            bool nextForbidden = false;
            while (!tokens.PopIfPresent("}"))
            {
                if (nextForbidden) tokens.PopExpected("}"); // crash

                Annotation annotation = tokens.IsNext("@") ? AnnotationParser.ParseAnnotation(tokens) : null;

                Token fieldToken = tokens.Pop();
                Parser.VerifyIdentifier(fieldToken);
                nextForbidden = !tokens.PopIfPresent(",");
                fieldTokens.Add(fieldToken);
                typeAnnotations.Add(annotation);
            }

            return new StructDefinition(structToken, structNameToken, fieldTokens, typeAnnotations, owner);
        }

        private static Executable ParseNamespace(Parser parser, TokenStream tokens, Executable owner)
        {
            Token namespaceToken = tokens.PopExpected("namespace");
            Token first = tokens.Pop();
            Parser.VerifyIdentifier(first);
            List<Token> namespacePieces = new List<Token>() { first };
            while (tokens.PopIfPresent("."))
            {
                Token nsToken = tokens.Pop();
                Parser.VerifyIdentifier(nsToken);
                namespacePieces.Add(nsToken);
            }

            string name = string.Join(".", namespacePieces.Select<Token, string>(t => t.Value));
            parser.PushNamespacePrefix(name);

            Namespace namespaceInstance = new Namespace(namespaceToken, name, owner);

            tokens.PopExpected("{");
            List<Executable> namespaceMembers = new List<Executable>();
            while (!tokens.PopIfPresent("}"))
            {
                Executable executable = ExecutableParser.Parse(parser, tokens, false, false, true, namespaceInstance);
                if (executable is FunctionDefinition ||
                    executable is ClassDefinition ||
                    executable is EnumDefinition ||
                    executable is ConstStatement ||
                    executable is Namespace)
                {
                    namespaceMembers.Add(executable);
                }
                else
                {
                    throw new ParserException(executable.FirstToken, "Only function, class, and nested namespace declarations may exist as direct members of a namespace.");
                }
            }

            namespaceInstance.Code = namespaceMembers.ToArray();

            parser.PopNamespacePrefix();

            return namespaceInstance;
        }

        private static Executable ParseFunction(Parser parser, TokenStream tokens, Executable nullableOwner)
        {
            bool isStatic =
                nullableOwner != null &&
                nullableOwner is ClassDefinition &&
                tokens.PopIfPresent("static");

            Token functionToken = tokens.PopExpected("function");

            List<Annotation> functionAnnotations = new List<Annotation>();

            while (tokens.IsNext("@"))
            {
                functionAnnotations.Add(AnnotationParser.ParseAnnotation(tokens));
            }

            Token functionNameToken = tokens.Pop();
            Parser.VerifyIdentifier(functionNameToken);

            FunctionDefinition fd = new FunctionDefinition(functionToken, nullableOwner, isStatic, functionNameToken, functionAnnotations, parser.CurrentNamespace);

            tokens.PopExpected("(");
            List<Token> argNames = new List<Token>();
            List<Expression> defaultValues = new List<Expression>();
            List<Annotation> argAnnotations = new List<Annotation>();
            bool optionalArgFound = false;
            while (!tokens.PopIfPresent(")"))
            {
                if (argNames.Count > 0) tokens.PopExpected(",");

                Annotation annotation = tokens.IsNext("@") ? AnnotationParser.ParseAnnotation(tokens) : null;
                Token argName = tokens.Pop();
                Expression defaultValue = null;
                Parser.VerifyIdentifier(argName);
                if (tokens.PopIfPresent("="))
                {
                    optionalArgFound = true;
                    defaultValue = ExpressionParser.Parse(tokens, fd);
                }
                else if (optionalArgFound)
                {
                    throw new ParserException(argName, "All optional arguments must come at the end of the argument list.");
                }
                argAnnotations.Add(annotation);
                argNames.Add(argName);
                defaultValues.Add(defaultValue);
            }

            IList<Executable> code = Parser.ParseBlock(parser, tokens, true, fd);

            fd.ArgNames = argNames.ToArray();
            fd.DefaultValues = defaultValues.ToArray();
            fd.ArgAnnotations = argAnnotations.ToArray();
            fd.Code = code.ToArray();

            return fd;
        }

        private static Executable ParseFor(Parser parser, TokenStream tokens, Executable owner)
        {
            Token forToken = tokens.PopExpected("for");
            tokens.PopExpected("(");

            if (Parser.IsValidIdentifier(tokens.PeekValue()) && tokens.PeekValue(1) == ":")
            {
                Token iteratorToken = tokens.Pop();
                if (Parser.IsReservedKeyword(iteratorToken.Value))
                {
                    throw new ParserException(iteratorToken, "Cannot use this name for an iterator.");
                }
                tokens.PopExpected(":");
                Expression iterationExpression = ExpressionParser.Parse(tokens, owner);
                tokens.PopExpected(")");
                IList<Executable> body = Parser.ParseBlock(parser, tokens, false, owner);

                return new ForEachLoop(forToken, iteratorToken, iterationExpression, body, owner);
            }
            else
            {
                List<Executable> init = new List<Executable>();
                while (!tokens.PopIfPresent(";"))
                {
                    if (init.Count > 0) tokens.PopExpected(",");
                    init.Add(Parse(parser, tokens, true, false, false, owner));
                }
                Expression condition = null;
                if (!tokens.PopIfPresent(";"))
                {
                    condition = ExpressionParser.Parse(tokens, owner);
                    tokens.PopExpected(";");
                }
                List<Executable> step = new List<Executable>();
                while (!tokens.PopIfPresent(")"))
                {
                    if (step.Count > 0) tokens.PopExpected(",");
                    step.Add(Parse(parser, tokens, true, false, false, owner));
                }

                IList<Executable> body = Parser.ParseBlock(parser, tokens, false, owner);

                return new ForLoop(forToken, init, condition, step, body, owner);
            }
        }

        private static Executable ParseWhile(Parser parser, TokenStream tokens, Executable owner)
        {
            Token whileToken = tokens.PopExpected("while");
            tokens.PopExpected("(");
            Expression condition = ExpressionParser.Parse(tokens, owner);
            tokens.PopExpected(")");
            IList<Executable> body = Parser.ParseBlock(parser, tokens, false, owner);
            return new WhileLoop(whileToken, condition, body, owner);
        }

        private static Executable ParseDoWhile(Parser parser, TokenStream tokens, Executable owner)
        {
            Token doToken = tokens.PopExpected("do");
            IList<Executable> body = Parser.ParseBlock(parser, tokens, true, owner);
            tokens.PopExpected("while");
            tokens.PopExpected("(");
            Expression condition = ExpressionParser.Parse(tokens, owner);
            tokens.PopExpected(")");
            tokens.PopExpected(";");
            return new DoWhileLoop(doToken, body, condition, owner);
        }

        private static Executable ParseSwitch(Parser parser, TokenStream tokens, Executable owner)
        {
            Token switchToken = tokens.PopExpected("switch");

            Expression explicitMax = null;
            Token explicitMaxToken = null;
            if (tokens.IsNext("{"))
            {
                explicitMaxToken = tokens.Pop();
                explicitMax = ExpressionParser.Parse(tokens, owner);
                tokens.PopExpected("}");
            }

            tokens.PopExpected("(");
            Expression condition = ExpressionParser.Parse(tokens, owner);
            tokens.PopExpected(")");
            tokens.PopExpected("{");
            List<List<Expression>> cases = new List<List<Expression>>();
            List<Token> firstTokens = new List<Token>();
            List<List<Executable>> code = new List<List<Executable>>();
            char state = '?'; // ? - first, O - code, A - case
            bool defaultEncountered = false;
            while (!tokens.PopIfPresent("}"))
            {
                if (tokens.IsNext("case"))
                {
                    if (defaultEncountered)
                    {
                        throw new ParserException(tokens.Peek(), "default condition in a switch statement must be the last condition.");
                    }

                    Token caseToken = tokens.PopExpected("case");
                    if (state != 'A')
                    {
                        cases.Add(new List<Expression>());
                        firstTokens.Add(caseToken);
                        code.Add(null);
                        state = 'A';
                    }
                    cases[cases.Count - 1].Add(ExpressionParser.Parse(tokens, owner));
                    tokens.PopExpected(":");
                }
                else if (tokens.IsNext("default"))
                {
                    Token defaultToken = tokens.PopExpected("default");
                    if (state != 'A')
                    {
                        cases.Add(new List<Expression>());
                        firstTokens.Add(defaultToken);
                        code.Add(null);
                        state = 'A';
                    }
                    cases[cases.Count - 1].Add(null);
                    tokens.PopExpected(":");
                    defaultEncountered = true;
                }
                else
                {
                    if (state != 'O')
                    {
                        cases.Add(null);
                        firstTokens.Add(null);
                        code.Add(new List<Executable>());
                        state = 'O';
                    }
                    code[code.Count - 1].Add(ExecutableParser.Parse(parser, tokens, false, true, false, owner));
                }
            }

            return new SwitchStatement(switchToken, condition, firstTokens, cases, code, explicitMax, explicitMaxToken, owner);
        }

        private static Executable ParseIf(Parser parser, TokenStream tokens, Executable owner)
        {
            Token ifToken = tokens.PopExpected("if");
            tokens.PopExpected("(");
            Expression condition = ExpressionParser.Parse(tokens, owner);
            tokens.PopExpected(")");
            IList<Executable> body = Parser.ParseBlock(parser, tokens, false, owner);
            IList<Executable> elseBody;
            if (tokens.PopIfPresent("else"))
            {
                elseBody = Parser.ParseBlock(parser, tokens, false, owner);
            }
            else
            {
                elseBody = new Executable[0];
            }
            return new IfStatement(ifToken, condition, body, elseBody, owner);
        }

        private static Executable ParseTry(Parser parser, TokenStream tokens, Executable owner)
        {
            Token tryToken = tokens.PopExpected("try");
            IList<Executable> tryBlock = Parser.ParseBlock(parser, tokens, true, owner);
            Token catchToken = null;
            IList<Executable> catchBlock = null;
            Token exceptionToken = null;
            Token finallyToken = null;
            IList<Executable> finallyBlock = null;

            if (tokens.IsNext("catch"))
            {
                catchToken = tokens.Pop();
                if (tokens.PopIfPresent("("))
                {
                    exceptionToken = tokens.Pop();
                    char firstChar = exceptionToken.Value[0];
                    if (firstChar != '_' &&
                        !(firstChar >= 'a' && firstChar <= 'z') &&
                        !(firstChar >= 'A' && firstChar <= 'Z'))
                    {
                        throw new ParserException(exceptionToken, "Invalid name for variable.");
                    }
                    tokens.PopExpected(")");
                }

                catchBlock = Parser.ParseBlock(parser, tokens, true, owner);
            }

            if (tokens.IsNext("finally"))
            {
                finallyToken = tokens.Pop();
                finallyBlock = Parser.ParseBlock(parser, tokens, true, owner);
            }

            return new TryStatement(tryToken, tryBlock, catchToken, exceptionToken, catchBlock, finallyToken, finallyBlock, owner);
        }

        private static Executable ParseBreak(TokenStream tokens, Executable owner)
        {
            Token breakToken = tokens.PopExpected("break");
            tokens.PopExpected(";");
            return new BreakStatement(breakToken, owner);
        }

        private static Executable ParseContinue(TokenStream tokens, Executable owner)
        {
            Token continueToken = tokens.PopExpected("continue");
            tokens.PopExpected(";");
            return new ContinueStatement(continueToken, owner);
        }

        private static Executable ParseReturn(TokenStream tokens, Executable owner)
        {
            Token returnToken = tokens.PopExpected("return");
            Expression expr = null;
            if (!tokens.PopIfPresent(";"))
            {
                expr = ExpressionParser.Parse(tokens, owner);
                tokens.PopExpected(";");
            }

            return new ReturnStatement(returnToken, expr, owner);
        }
    }
}
