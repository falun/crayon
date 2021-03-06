﻿using System.Collections.Generic;
using Crayon.ParseTree;

namespace Crayon
{
    internal class SystemLibraryManager
    {
        private Dictionary<string, Library> importedLibraries = new Dictionary<string, Library>();
        private Dictionary<string, Library> librariesByKey = new Dictionary<string, Library>();

        private Dictionary<string, string> functionNameToLibraryName = new Dictionary<string, string>();

        private Dictionary<string, int> libFunctionIds = new Dictionary<string, int>();
        private List<string> orderedListOfFunctionNames = new List<string>();

        public static bool IsValidLibrary(string name)
        {
            return systemLibraryPathsByName.ContainsKey(name);
        }

        public string GetLibrarySwitchStatement(AbstractPlatform platform)
        {
            List<string> output = new List<string>();
            foreach (string name in this.orderedListOfFunctionNames)
            {
                output.Add("case " + this.libFunctionIds[name] + ":\n");
                output.Add("$_comment('" + name + "');");
                output.Add(this.importedLibraries[this.functionNameToLibraryName[name]].GetTranslationCode(name));
                output.Add("\nbreak;\n");
            }
            if (this.orderedListOfFunctionNames.Count == 0)
            {
                output.Add("case 0: break;");
            }
            return string.Join("\n", output);
        }

        public Library GetLibraryFromKey(string key)
        {
            Library output;
            return this.librariesByKey.TryGetValue(key, out output) ? output : null;
        }

        public int GetIdForFunction(string name, string library)
        {
            if (this.libFunctionIds.ContainsKey(name))
            {
                return this.libFunctionIds[name];
            }

            this.functionNameToLibraryName[name] = library;
            this.orderedListOfFunctionNames.Add(name);
            int id = this.orderedListOfFunctionNames.Count;
            this.libFunctionIds[name] = id;
            return id;
        }

        public Dictionary<string, string> GetEmbeddedCode(string libraryName)
        {
            return this.importedLibraries[libraryName].GetEmbeddedCode();
        }

        public Dictionary<string, string> GetSupplementalTranslationFiles()
        {
            Dictionary<string, string> output = new Dictionary<string, string>();
            foreach (Library library in this.importedLibraries.Values)
            {
                Dictionary<string, string> files = library.GetSupplementalTranslatedCode();
                foreach (string key in files.Keys)
                {
                    output[key] = files[key];
                }
            }
            return output;
        }

        private static Dictionary<string, string> systemLibraryPathsByName = null;

        private string GetSystemLibraryPath(string name)
        {
            if (systemLibraryPathsByName == null)
            {
                systemLibraryPathsByName = new Dictionary<string, string>();

                string crayonHome = System.Environment.GetEnvironmentVariable("CRAYON_HOME");

#if RELEASE
                if (crayonHome == null)
                {
                    throw new System.InvalidOperationException("Please set the CRAYON_HOME environment variable to the location of the directory containing both 'crayon.exe' and the 'lib' directory.");
                }
#endif

                List<string> directoriesToCheck = new List<string>();

                if (crayonHome != null)
                {
                    string crayonHomeLibraries = System.IO.Path.Combine(crayonHome, "libs");
                    directoriesToCheck.AddRange(System.IO.Directory.GetDirectories(crayonHomeLibraries));
                }

#if DEBUG
                // Presumably running from source. Walk up to the root directory and find the Libraries directory.
                // From there use the list of folders.
                string currentDirectory = System.IO.Path.GetFullPath(".");
                while (!string.IsNullOrEmpty(currentDirectory))
                {
                    string path = System.IO.Path.Combine(currentDirectory, "Libraries");
                    if (System.IO.Directory.Exists(path))
                    {
                        directoriesToCheck.AddRange(System.IO.Directory.GetDirectories(path));
                        break;
                    }
                    currentDirectory = System.IO.Path.GetDirectoryName(currentDirectory);
                }
#endif
                foreach (string dir in directoriesToCheck)
                {
                    string libraryName = System.IO.Path.GetFileName(dir);
                    string manifestPath = System.IO.Path.Combine(dir, "manifest.txt");
                    if (System.IO.File.Exists(manifestPath))
                    {
                        systemLibraryPathsByName[libraryName] = manifestPath;
                    }
                }
            }

            string fullpath;
            return systemLibraryPathsByName.TryGetValue(name, out fullpath)
                ? fullpath
                : null;
        }

        private readonly Dictionary<string, Library> alreadyImported = new Dictionary<string, Library>();
        private static readonly Executable[] EMPTY_EXECUTABLE = new Executable[0];

        public Executable[] ImportLibrary(Parser parser, Token throwToken, string name)
        {
            name = name.Split('.')[0];
            Library library = alreadyImported.ContainsKey(name) ? alreadyImported[name] : null;
            Executable[] embedCode = EMPTY_EXECUTABLE;
            if (library == null)
            {
                string libraryManifestPath = this.GetSystemLibraryPath(name);

                if (libraryManifestPath == null)
                {
                    // TODO: show suggestions of similarly named libraries
                    throw new ParserException(throwToken, "Library not found.");
                }

                library = new Library(name, libraryManifestPath, parser.BuildContext.Platform);

                alreadyImported.Add(name, library);

                library.ExtractResources(parser.BuildContext.Platform, this.filesToCopy, this.contentToEmbed);

                this.importedLibraries[name] = library;
                this.librariesByKey[name.ToLowerInvariant()] = library;

                string oldSystemLibrary = parser.CurrentSystemLibrary;
                parser.CurrentSystemLibrary = name;

                List<Executable> output = new List<Executable>();
                Dictionary<string, string> embeddedCode = library.GetEmbeddedCode();
                foreach (string embeddedFile in embeddedCode.Keys)
                {
                    string fakeName = "[" + embeddedFile + "]";
                    string code = embeddedCode[embeddedFile];
                    output.AddRange(parser.ParseInterpretedCode(fakeName, code, name));
                }

                parser.CurrentSystemLibrary = oldSystemLibrary;
                embedCode = output.ToArray();
            }

            // Even if already imported, still must check to see if this import is allowed here.
            if (!library.IsAllowedImport(parser.CurrentSystemLibrary))
            {
                throw new ParserException(throwToken, "This library cannot be imported from here.");
            }

            return embedCode;
        }

        private Dictionary<string, string> filesToCopy = new Dictionary<string, string>();
        private List<string> contentToEmbed = new List<string>();

        public Dictionary<string, string> CopiedFiles
        {
            get
            {
                return new Dictionary<string, string>(this.filesToCopy);
            }
        }

        public string EmbeddedContent
        {
            get
            {
                // TODO: check the content itself to see if there's a \r\n or \n and then use the correct one.
                return string.Join("\n", contentToEmbed);
            }
        }
    }
}
