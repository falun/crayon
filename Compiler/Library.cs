﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace Crayon
{
    internal class Library
    {
        private string platformName;
        public string Name { get; set; }
        public string RootDirectory { get; set; }
        private HashSet<string> onlyImportableFrom = null;

        private readonly Dictionary<string, string> replacements = new Dictionary<string, string>();

        public Library(string name, string libraryManifestPath, string platformName)
        {
            this.Name = name;
            this.RootDirectory = System.IO.Path.GetDirectoryName(libraryManifestPath);
            string[] manifest = System.IO.File.ReadAllText(libraryManifestPath).Split('\n');
            Dictionary<string, string> values = new Dictionary<string, string>();
            Dictionary<string, bool> flagValues = new Dictionary<string, bool>();

            this.platformName = platformName;
            string platformPrefix = "[" + platformName + "]";

            foreach (string line in manifest)
            {
                string trimmedLine = line.Trim();
                if (trimmedLine.Length > 0 && line[0] != '#')
                {
                    string[] parts = trimmedLine.Split(':');
                    if (parts.Length >= 2)
                    {
                        string key = parts[0].Trim();
                        string value = parts[1];
                        for (int i = 2; i < parts.Length; ++i)
                        {
                            value += ":" + parts[i];
                        }

                        if (key.StartsWith("["))
                        {
                            if (key.StartsWith(platformPrefix))
                            {
                                key = key.Substring(platformPrefix.Length).Trim();
                            }
                            else
                            {
                                continue;
                            }
                        }

                        if (key == "BOOL_FLAG")
                        {
                            // TODO: parse bool flag value
                            parts = value.Split(':');
                            if (parts.Length == 2)
                            {
                                key = parts[0].Trim();
                                bool boolValue = parts[1].Trim().ToLowerInvariant() == "true";
                                flagValues[key] = boolValue;
                            }
                            else
                            {
                                throw new ParserException(null, "Library '" + name + "' has a syntax error in a boolean flag.");
                            }
                        }
                        else
                        {
                            values[key] = value;
                        }
                    }
                    else if (parts.Length == 1 && parts[0].Length != 0)
                    {
                        throw new ParserException(null, "Library '" + name + "' has a syntax error in its manifest.");
                    }
                }
            }

            foreach (string key in flagValues.Keys)
            {
                this.replacements[key] = flagValues[key] ? "true" : "false";
            }

            this.filepathsByFunctionName = new Dictionary<string, string>();
            // Build a lookup dictionary of all file names that are simple function names e.g. "foo.cry"
            // Then go through and look up all the file names that contain . prefixes with the platform name and
            // overwrite the lookup value for that entry with the more specific path.
            // myFunction.cry
            // android.myFunction.cry
            // on Python, myFunction will be included for lib_foo_myFunction(), but on Android, android.myFunction.cry will be included instead.

            string[] files = new string[0];
            if (FileUtil.DirectoryExists(this.RootDirectory + "/native"))
            {
                files = System.IO.Directory.GetFiles(System.IO.Path.Combine(this.RootDirectory, "native"));
            }
            Dictionary<string, string> moreSpecificFiles = new Dictionary<string, string>();
            foreach (string fileWithDirectory in files)
            {
                string file = System.IO.Path.GetFileName(fileWithDirectory);
                if (file.EndsWith(".cry"))
                {
                    string functionName = file.Substring(0, file.Length - ".cry".Length);
                    if (functionName.Contains('.'))
                    {
                        // Add this file to the more specific lookup, but only if it contains the current platform.
                        if (functionName.StartsWith(platformName + ".") ||
                            functionName.Contains("." + platformName + "."))
                        {
                            string[] parts = functionName.Split('.');
                            moreSpecificFiles[parts[parts.Length - 1]] = file;
                        }
                        else
                        {
                            // just let it get filtered away.
                        }
                    }
                    else
                    {
                        this.filepathsByFunctionName[functionName] = file;
                    }
                }
            }

            foreach (string functionName in moreSpecificFiles.Keys)
            {
                this.filepathsByFunctionName[functionName] = moreSpecificFiles[functionName];
            }

            if (values.ContainsKey("ONLY_ALLOW_IMPORT_FROM"))
            {
                this.onlyImportableFrom = new HashSet<string>();
                foreach (string onlyImportFrom in values["ONLY_ALLOW_IMPORT_FROM"].Split(','))
                {
                    string libraryName = onlyImportFrom.Trim();
                    this.onlyImportableFrom.Add(libraryName);
                }
            }
        }

        public bool IsAllowedImport(string currentLibrary)
        {
            // Empty list means it's open to everyone.
            if (this.onlyImportableFrom == null || this.onlyImportableFrom.Count == 0)
            {
                return true;
            }

            // Non-empty list means it must be only accessible from a specific library and not top-level user code.
            if (currentLibrary == null)
            {
                return false;
            }

            // Is the current library on the list?
            return this.onlyImportableFrom.Contains(currentLibrary);
        }

        private Dictionary<string, string> filepathsByFunctionName;

        public Dictionary<string, string> GetEmbeddedCode()
        {
            Dictionary<string, string> output = new Dictionary<string, string>() {
                { this.Name, this.ReadFile("embed.cry", true) }
            };
            string embedDir = FileUtil.JoinPath(this.RootDirectory, "embed");
            if (FileUtil.DirectoryExists(embedDir))
            {
                string[] additionalFiles = FileUtil.GetAllFilePathsRelativeToRoot(embedDir);
                foreach (string additionalFile in additionalFiles)
                {
                    string embedCode = this.ReadFile("embed/" + additionalFile, false);
                    output[this.Name + ":" + additionalFile] = embedCode;
                }
            }
            return output;
        }

        private Dictionary<string, string> supplementalFiles = null;

        public Dictionary<string, string> GetSupplementalTranslatedCode()
        {
            if (this.supplementalFiles == null)
            {
                this.supplementalFiles = new Dictionary<string, string>();
                string supplementalFilesDir = System.IO.Path.Combine(this.RootDirectory, "supplemental");
                if (System.IO.Directory.Exists(supplementalFilesDir))
                {
                    foreach (string filepath in System.IO.Directory.GetFiles(supplementalFilesDir))
                    {
                        string name = System.IO.Path.GetFileName(filepath);
                        if (name.EndsWith(".cry"))
                        {
                            string key = name.Substring(0, name.Length - ".cry".Length);
                            this.supplementalFiles[key] = this.ReadFile(System.IO.Path.Combine("supplemental", name), false);
                        }
                    }
                }
            }
            return this.supplementalFiles;
        }

        public string GetTranslationCode(string functionName)
        {
            string prefix = "lib_" + this.Name.ToLower() + "_";
            if (!functionName.StartsWith(prefix))
            {
                throw new InvalidOperationException("Cannot call library function '" + functionName + "' from the '" + this.Name + "' library.");
            }
            string shortName = functionName.Substring(prefix.Length);
            if (!this.filepathsByFunctionName.ContainsKey(shortName))
            {
                throw new NotImplementedException("The library function '" + functionName + "' is not implemented.");
            }

            return "  import inline 'LIB:" + this.Name + ":native/" + this.filepathsByFunctionName[shortName] + "';\n";
        }

        Dictionary<string, string> translations = null;

        public string TranslateNativeInvocation(Token throwToken, AbstractPlatform translator, string functionName, object[] args)
        {
            if (this.translations == null)
            {
                string methodTranslations = this.ReadFile(System.IO.Path.Combine("methods", this.platformName + ".txt"), false);
                this.translations = new Dictionary<string, string>();
                foreach (string line in methodTranslations.Split('\n'))
                {
                    string[] parts = line.Trim().Split(':');
                    if (parts.Length > 1)
                    {
                        string key = parts[0];
                        string value = parts[1];
                        for (int i = 2; i < parts.Length; ++i)
                        {
                            value += ":" + parts[i];
                        }
                        this.translations[key.Trim()] = value.Trim();
                    }
                }
            }

            if (this.translations.ContainsKey(functionName))
            {
                string output = this.translations[functionName];
                for (int i = 0; i < args.Length; ++i)
                {
                    string argAsString = translator.Translate(args[i]);
                    output = output.Replace("[ARG:" + (i + 1) + "]", argAsString);
                }
                return output;
            }

            throw new ParserException(throwToken, "No native translation provided for " + functionName);
        }

        public void ExtractResources(string platformId, Dictionary<string, string> filesToCopy, List<string> contentToEmbed)
        {
            string resourceManifest = this.ReadFile("resources/resource-manifest.txt", true).Trim();
            if (resourceManifest.Length > 0)
            {
                string mode = "inactive"; // inactive | pending | active

                foreach (string lineRaw in resourceManifest.Split('\n'))
                {
                    string[] parts = lineRaw.Trim().Split(':');
                    if (parts[0].StartsWith("#")) continue; // comment 

                    string command = parts[0].ToUpper().Trim();
                    if (command.Length > 0)
                    {
                        switch (mode)
                        {
                            case "active":
                                switch (command)
                                {
                                    case "COPYFILES":
                                        this.LibraryResourceCopyFiles(parts[1].Trim(), parts[2].Trim(), filesToCopy);
                                        break;

                                    case "EMBED":
                                        this.LibraryResourceEmbedFiles(parts[1].Trim(), contentToEmbed);
                                        break;

                                    case "END":
                                        mode = "inactive";
                                        break;
                                }
                                break;

                            case "inactive":
                                if (command == "BEGIN")
                                {
                                    mode = "pending";
                                }
                                break;

                            case "pending":
                                switch (command)
                                {
                                    case "APPLICABLE-TO":
                                        if (platformId == parts[1].Trim())
                                        {
                                            mode = "active";
                                        }
                                        break;
                                    case "END":
                                        mode = "inactive";
                                        break;
                                }
                                break;
                        }
                    }
                }
            }
        }

        private void LibraryResourceCopyFiles(string sourceDirectory, string targetPathTemplate, Dictionary<string, string> copyOutput)
        {
            foreach (string file in this.ListDirectory("resources/" + sourceDirectory))
            {
                string content = this.ReadFile("resources/" + sourceDirectory + "/" + file, false);
                string targetPath = targetPathTemplate.Replace("%FILE%", file);
                copyOutput.Add(targetPath, content);
            }
        }

        private void LibraryResourceEmbedFiles(string sourceDirectory, List<string> embedTarget)
        {
            foreach (string file in this.ListDirectory("resources/" + sourceDirectory))
            {
                embedTarget.Add(this.ReadFile("resources/" + sourceDirectory + "/" + file, false));
            }
        }

        private HashSet<string> IGNORABLE_FILES = new HashSet<string>(new string[] { ".ds_store", "thumbs.db" });
        private string[] ListDirectory(string pathRelativeToLibraryRoot)
        {
            string fullPath = FileUtil.JoinPath(this.RootDirectory, pathRelativeToLibraryRoot);
            List<string> output = new List<string>();
            if (FileUtil.DirectoryExists(fullPath))
            {
                foreach (string file in FileUtil.DirectoryListFileNames(fullPath))
                {
                    if (!IGNORABLE_FILES.Contains(file.ToLower()))
                    {
                        output.Add(file);
                    }
                }
            }
            return output.ToArray();
        }

        public string ReadFile(string pathRelativeToLibraryRoot, bool failSilently)
        {
            string fullPath = FileUtil.JoinPath(this.RootDirectory, pathRelativeToLibraryRoot);
            if (System.IO.File.Exists(fullPath))
            {
                string text = System.IO.File.ReadAllText(fullPath);

                return Constants.DoReplacements(text, this.replacements);
            }

            if (failSilently)
            {
                return "";
            }

            throw new ParserException(null, "The " + this.Name + " library does not support " + this.platformName + " projects.");
        }
    }
}
