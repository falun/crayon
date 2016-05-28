﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crayon.Translator.CSharp
{
	class CSharpXamarinAndroidPlatform : CSharpPlatform
    {
        public override string GeneratedFilesFolder { get { return "%PROJECT_ID%/Assets/GeneratedFiles"; } }

        public CSharpXamarinAndroidPlatform() : base(new CSharpXamarinAndroidSystemFunctionTranslator(), 
			new CSharpXamarinAndroidOpenGlTranslator())
		{

		}

		public override string PlatformShortId { get { return "csharp-android"; } }

		public override void AddPlatformSpecificSystemLibraries(HashSet<string> systemLibraries)
		{
			// Nope
		}

		public override void ApplyPlatformSpecificReplacements(Dictionary<string, string> replacements)
		{
			replacements["PROJECT_FILE_EXTRAS"] = string.Join("\n",
				@"<ProjectTypeGuid>{EFBA0AD7-5A72-4C68-AF49-83D382785DCF};{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}</ProjectTypeGuid>",
				@"    <OutputType>Library</OutputType>",
				@"    <AndroidApplication>true</AndroidApplication>",
				@"    <AndroidResgenFile>Resources\Resource.Designer.cs</AndroidResgenFile>",
				@"    <AndroidUseLatestPlatformSdk>True</AndroidUseLatestPlatformSdk>",
				@"    <AndroidManifest>Properties\AndroidManifest.xml</AndroidManifest>",
				@"    <TargetFrameworkVersion>v6.0</TargetFrameworkVersion>");
		}

        public override void ApplyPlatformSpecificOverrides(string projectId, Dictionary<string, FileOutput> files)
        {
            // Hack
            foreach (string key in files.Keys)
            {
                if (key.StartsWith(projectId + "/GeneratedFiles/"))
                {
                    FileOutput file = files[key];
                    string newKey = projectId + "/Assets" + key.Substring(projectId.Length);
                    files.Remove(key);
                    files.Add(newKey, file);
                }
            }
        }

        public override void PlatformSpecificFiles(string projectId, List<string> compileTargets, Dictionary<string, FileOutput> files, Dictionary<string, string> replacements, SpriteSheetBuilder spriteSheet)
        {
            files[projectId + ".sln"] = new FileOutput()
            {
                Type = FileOutputType.Text,
                TextContent = Constants.DoReplacements(
                    Util.ReadFileInternally("Translator/CSharp/Project/XamarinAndroid/SolutionFile.sln.txt"),
                    replacements)
            };
            
            List<string> additionalAndroidAssets = new List<string>();
            foreach (string spriteSheetImage in spriteSheet.FinalPaths)
            {
                // TODO: need a better system of putting things in predefined destinations, rather than hacking it between states
                // in this fashion.
                string path = spriteSheetImage.Substring("%PROJECT_ID%".Length + 1).Replace('/', '\\');
                additionalAndroidAssets.Add("    <AndroidAsset Include=\"" + path + "\" />\r\n");
            }
            replacements["ADDITIONAL_ANDROID_ASSETS"] = string.Join("\n", additionalAndroidAssets);

            files[projectId + "/" + projectId + ".csproj"] = new FileOutput()
			{
				Type = FileOutputType.Text,
				TextContent = Constants.DoReplacements(
					Util.ReadFileInternally("Translator/CSharp/Project/XamarinAndroid/ProjectFile.csproj.txt"),
					replacements)
			};
            
            files[projectId + "/Resources/drawable/Icon.png"] = new FileOutput()
			{
				Type = FileOutputType.Binary,
				BinaryContent = Util.ReadBytesInternally("Translator/CSharp/Project/XamarinAndroid/Icon.png"),
			};

			// TODO: if not really used, can this be removed from the project?
			files[projectId + "/Resources/layout/Main.axml"] = new FileOutput()
			{
				Type = FileOutputType.Text,
				TextContent = Constants.DoReplacements(
					Util.ReadFileInternally("Translator/CSharp/Project/XamarinAndroid/Main.axml.txt"),
					replacements),
			};

			files[projectId + "/Resources/values/strings.xml"] = new FileOutput()
			{
				Type = FileOutputType.Text,
				TextContent = Constants.DoReplacements(
					Util.ReadFileInternally("Translator/CSharp/Project/XamarinAndroid/Strings.xml.txt"),
					replacements),
			};

            files[projectId + "/Resources/Resource.Designer.cs"] = new FileOutput()
            {
                Type = FileOutputType.Text,
                TextContent = Constants.DoReplacements(
                    Util.ReadFileInternally("Translator/CSharp/Project/XamarinAndroid/ResourceDesigner.txt"),
                    replacements),
            };

            files[projectId + "/Assets/ByteCode.txt"] = new FileOutput()
            {
                Type = FileOutputType.Text,
                TextContent = this.Context.ByteCodeString
            };

            foreach (string filename in new string[] {
                "AssemblyInfo",
                "CsxaGlRenderer",
                "CsxaTranslationHelper",
                "GlView1",
                "MainActivity",
                "ResourceReader",
            })
            {
                compileTargets.Add(filename + ".cs");
                string target = projectId + "/" + (filename == "AssemblyInfo" ? "Properties/" : "") + filename + ".cs";
                files[target] = new FileOutput()
                {
                    Type = FileOutputType.Text,
                    TextContent = Constants.DoReplacements(
                        Util.ReadFileInternally("Translator/CSharp/Project/XamarinAndroid/" + filename + ".txt"),
                        replacements)
                };
            }
		}
	}
}