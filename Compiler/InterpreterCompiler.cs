﻿using System.Collections.Generic;
using System.Linq;
using Crayon.ParseTree;

namespace Crayon
{
    internal class InterpreterCompiler
    {
        // Order in which the files are compiled.
        private static readonly string[] FILES = new string[] {

            // These 3 must go first
            "Structs.cry",
            "Constants.cry",
            "Globals.cry",

            // These are just piles of functions so they can be compiled in any order.
            "BinaryOpsUtil.cry",
            "ByteCodeLoader.cry",
            "Interpreter.cry",
            "MetadataInitializer.cry",
            "PrimitiveMethods.cry",
            "ResourceManager.cry",
            "Runner.cry",
            "TypesUtil.cry",
            "ValueUtil.cry",
        };

        private AbstractPlatform platform;
        private Parser interpreterParser;

        public InterpreterCompiler(AbstractPlatform platform, SystemLibraryManager sysLibMan)
        {
            this.platform = platform;
            this.interpreterParser = new Parser(platform, null, sysLibMan);
        }

        public Dictionary<string, Executable[]> Compile()
        {
            Dictionary<string, Executable[]> output = new Dictionary<string, Executable[]>();

            Dictionary<string, string> replacements = this.BuildReplacementsDictionary();

            Dictionary<string, string> filesById = new Dictionary<string, string>();
            Dictionary<string, string> libraryProducedFiles = this.interpreterParser.SystemLibraryManager.GetSupplementalTranslationFiles();
            List<string> orderedFileIds = new List<string>();

            foreach (string file in FILES)
            {
                string fileId = file.Split('.')[0];
                string code = Util.ReadInterpreterFileInternally(file);
                filesById[fileId] = code;
                orderedFileIds.Add(fileId);
            }

            foreach (string fileId in libraryProducedFiles.Keys)
            {
                filesById[fileId] = libraryProducedFiles[fileId];
                orderedFileIds.Add(fileId);
            }

            foreach (string fileId in orderedFileIds)
            {
                string code = Constants.DoReplacements(filesById[fileId], replacements);

                Executable[] lines = this.interpreterParser.ParseInterpreterCode(fileId, code);
                if (lines.Length > 0)
                {
                    output[fileId] = lines.ToArray();
                }
            }

            string switchLookupCode = this.interpreterParser.GetSwitchLookupCode().Trim();
            if (switchLookupCode.Length > 0)
            {
                output["SwitchLookups"] = this.interpreterParser.ParseInterpreterCode("SwitchLookups.cry", switchLookupCode);
            }

            return output;
        }

        public Dictionary<string, string> BuildReplacementsDictionary()
        {
            Dictionary<string, string> replacements = new Dictionary<string, string>();
            replacements.Add("PLATFORM_SUPPORTS_LIST_CLEAR", this.platform.SupportsListClear ? "true" : "false");
            replacements.Add("STRONGLY_TYPED", this.platform.IsStronglyTyped ? "true" : "false");
            replacements.Add("IS_ARRAY_SAME_AS_LIST", this.platform.IsArraySameAsList ? "true" : "false");
            replacements.Add("IS_BYTECODE_LOADED_DIRECTLY", this.platform.IsByteCodeLoadedDirectly ? "true" : "false");
            replacements.Add("PLATFORM_SHORT_ID", this.platform.PlatformShortId);
            replacements.Add("LIBRARY_FUNCTION_BIG_SWITCH_STATEMENT", this.platform.LibraryBigSwitchStatement);
            replacements.Add("IS_PHP", (this.platform is Crayon.Translator.Php.PhpPlatform) ? "true" : "false");
            replacements.Add("IS_CHAR_A_NUMBER", this.platform.IsCharANumber ? "true" : "false");
            replacements.Add("INT_IS_FLOOR", this.platform.IntIsFloor ? "true" : "false");
            replacements.Add("IS_THREAD_BLOCKING_ALLOWED", this.platform.IsThreadBlockingAllowed ? "true" : "false");
            replacements.Add("IS_JAVASCRIPT", this.platform.LanguageId == LanguageId.JAVASCRIPT ? "true" : "false");
            replacements.Add("IS_ANDROID", this.platform.PlatformShortId == "game-csharp-android" ? "true" : "false");
            return replacements;
        }

        public StructDefinition[] GetStructDefinitions()
        {
            return this.interpreterParser.GetStructDefinitions();
        }
    }
}
