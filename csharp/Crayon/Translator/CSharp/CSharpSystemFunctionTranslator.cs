﻿using System;
using System.Collections.Generic;
using System.Linq;
using Crayon.ParseTree;
using System.Text;

namespace Crayon.Translator.CSharp
{
	class CSharpSystemFunctionTranslator : AbstractSystemFunctionTranslator
	{
		public CSharpSystemFunctionTranslator() : base() { }

		protected override void TranslateArcCos(List<string> output, Expression value)
		{
			output.Add("Math.Acos(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateArcSin(List<string> output, Expression value)
		{
			output.Add("Math.Asin(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateArcTan(List<string> output, Expression dy, Expression dx)
		{
			output.Add("Math.Atan2(");
			this.Translator.TranslateExpression(output, dy);
			output.Add(", ");
			this.Translator.TranslateExpression(output, dx);
			output.Add(")");
		}

		protected override void TranslateArrayGet(List<string> output, Expression list, Expression index)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add("[");
			this.Translator.TranslateExpression(output, index);
			output.Add("]");
		}

		protected override void TranslateArrayLength(List<string> output, Expression list)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".Length");
		}

		protected override void TranslateArraySet(List<string> output, Expression list, Expression index, Expression value)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add("[");
			this.Translator.TranslateExpression(output, index);
			output.Add(this.Shorten("] = "));
			this.Translator.TranslateExpression(output, value);
		}

		protected override void TranslateBeginFrame(List<string> output)
		{
			// Nope
		}

		protected override void TranslateBlitImage(List<string> output, Expression image, Expression x, Expression y)
		{
			output.Add("GameWindow.Instance.BlitImage((Image)");
			this.Translator.TranslateExpression(output, image);
			output.Add(", ");
			this.Translator.TranslateExpression(output, x);
			output.Add(", ");
			this.Translator.TranslateExpression(output, y);
			output.Add(")");
		}

		protected override void TranslateCast(List<string> output, StringConstant typeValue, Expression expression)
		{
			CSharpPlatform platform = (CSharpPlatform)this.Platform;
			string typeString = platform.GetTypeStringFromAnnotation(typeValue.FirstToken, ((StringConstant)typeValue).Value);

			output.Add("(");
			output.Add(typeString);
			output.Add(")");
			this.Translator.TranslateExpression(output, expression);
		}

		protected override void TranslateCastToList(List<string> output, Expression enumerableThing)
		{
			output.Add("new List<Value>(");
			this.Translator.TranslateExpression(output, enumerableThing);
			output.Add(")");
		}

		protected override void TranslateCharToString(List<string> output, Expression charValue)
		{
			output.Add("\"\" + ");
			this.Translator.TranslateExpression(output, charValue);
		}

		protected override void TranslateComment(List<string> output, StringConstant commentValue)
		{
#if DEBUG
			output.Add("// " + commentValue.Value);
#endif
		}

		protected override void TranslateConvertListToArray(List<string> output, StringConstant type, Expression list)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".ToArray()");
		}

		protected override void TranslateCos(List<string> output, Expression value)
		{
			output.Add("Math.Cos(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateCurrentTimeSeconds(List<string> output)
		{
			output.Add("DateTime.Now.Ticks / 10000000.0");
		}

		protected override void TranslateDictionaryContains(List<string> output, Expression dictionary, Expression key)
		{
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".ContainsKey(");
			this.Translator.TranslateExpression(output, key);
			output.Add(")");
		}

		protected override void TranslateDictionaryGetGuaranteed(List<string> output, Expression dictionary, Expression key)
		{
			this.Translator.TranslateExpression(output, dictionary);
			output.Add("[");
			this.Translator.TranslateExpression(output, key);
			output.Add("]");
		}

		protected override void TranslateDictionaryGetKeys(List<string> output, Expression dictionary)
		{
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".Keys.ToArray()");
		}

		protected override void TranslateDictionaryGetValues(List<string> output, Expression dictionary)
		{
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".Values");
		}

		protected override void TranslateDictionaryRemove(List<string> output, Expression dictionary, Expression key)
		{
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".Remove(");
			this.Translator.TranslateExpression(output, key);
			output.Add(")");
		}

		protected override void TranslateDictionarySet(List<string> output, Expression dict, Expression key, Expression value)
		{
			this.Translator.TranslateExpression(output, dict);
			output.Add("[");
			this.Translator.TranslateExpression(output, key);
			output.Add("] = ");
			this.Translator.TranslateExpression(output, value);
		}

		protected override void TranslateDictionarySize(List<string> output, Expression dictionary)
		{
			this.Translator.TranslateExpression(output, dictionary);
			output.Add(".Count");
		}

		protected override void TranslateDotEquals(List<string> output, Expression root, Expression compareTo)
		{
			this.Translator.TranslateExpression(output, root);
			output.Add(".Equals(");
			this.Translator.TranslateExpression(output, compareTo);
			output.Add(")");
		}

		protected override void TranslateDownloadImage(List<string> output, Expression key, Expression path)
		{
			output.Add("Image.LoadImage(");
			this.Translator.TranslateExpression(output, key);
			output.Add(", ");
			this.Translator.TranslateExpression(output, path);
			output.Add(")");
		}

		protected override void TranslateDrawEllipse(List<string> output, Expression left, Expression top, Expression width, Expression height, Expression red, Expression green, Expression blue, Expression alpha)
		{
			output.Add("GameWindow.Instance.DrawEllipse(");
			this.Translator.TranslateExpression(output, left);
			output.Add(", ");
			this.Translator.TranslateExpression(output, top);
			output.Add(", ");
			this.Translator.TranslateExpression(output, width);
			output.Add(", ");
			this.Translator.TranslateExpression(output, height);
			output.Add(", ");
			this.Translator.TranslateExpression(output, red);
			output.Add(", ");
			this.Translator.TranslateExpression(output, green);
			output.Add(", ");
			this.Translator.TranslateExpression(output, blue);
			output.Add(", ");
			this.Translator.TranslateExpression(output, alpha);
			output.Add(")");
		}

		protected override void TranslateDrawLine(List<string> output, Expression ax, Expression ay, Expression bx, Expression by, Expression lineWidth, Expression red, Expression green, Expression blue, Expression alpha)
		{
			output.Add("GameWindow.Instance.DrawLine(");
			this.Translator.TranslateExpression(output, ax);
			output.Add(", ");
			this.Translator.TranslateExpression(output, ay);
			output.Add(", ");
			this.Translator.TranslateExpression(output, bx);
			output.Add(", ");
			this.Translator.TranslateExpression(output, by);
			output.Add(", ");
			this.Translator.TranslateExpression(output, lineWidth);
			output.Add(", ");
			this.Translator.TranslateExpression(output, red);
			output.Add(", ");
			this.Translator.TranslateExpression(output, green);
			output.Add(", ");
			this.Translator.TranslateExpression(output, blue);
			output.Add(", ");
			this.Translator.TranslateExpression(output, alpha);
			output.Add(")");
		}

		protected override void TranslateDrawRectangle(List<string> output, Expression left, Expression top, Expression width, Expression height, Expression red, Expression green, Expression blue, Expression alpha)
		{
			output.Add("GameWindow.Instance.DrawRectangle(");
			this.Translator.TranslateExpression(output, left);
			output.Add(", ");
			this.Translator.TranslateExpression(output, top);
			output.Add(", ");
			this.Translator.TranslateExpression(output, width);
			output.Add(", ");
			this.Translator.TranslateExpression(output, height);
			output.Add(", ");
			this.Translator.TranslateExpression(output, red);
			output.Add(", ");
			this.Translator.TranslateExpression(output, green);
			output.Add(", ");
			this.Translator.TranslateExpression(output, blue);
			output.Add(", ");
			this.Translator.TranslateExpression(output, alpha);
			output.Add(")");
		}

		protected override void TranslateExponent(List<string> output, Expression baseNum, Expression powerNum)
		{
			output.Add("System.Math.Pow(");
			this.Translator.TranslateExpression(output, baseNum);
			output.Add(", ");
			this.Translator.TranslateExpression(output, powerNum);
			output.Add(")");
		}

		protected override void TranslateFillScreen(List<string> output, Expression red, Expression green, Expression blue)
		{
			output.Add("GameWindow.Instance.FillScreen(");
			this.Translator.TranslateExpression(output, red);
			output.Add(", ");
			this.Translator.TranslateExpression(output, green);
			output.Add(", ");
			this.Translator.TranslateExpression(output, blue);
			output.Add(")");
		}

		protected override void TranslateFlipImage(List<string> output, Expression image, Expression flipX, Expression flipY)
		{
			output.Add("Image.FlipImage(");
			this.Translator.TranslateExpression(output, image);
			output.Add(", ");
			this.Translator.TranslateExpression(output, flipX);
			output.Add(", ");
			this.Translator.TranslateExpression(output, flipY);
			output.Add(")");
		}

		protected override void TranslateForceParens(List<string> output, Expression expression)
		{
			output.Add("(");
			this.Translator.TranslateExpression(output, expression);
			output.Add(")");
		}

		protected override void TranslateGetEventsRawList(List<string> output)
		{
			output.Add("GameWindow.Instance.GetEvents()");
		}

		protected override void TranslateGetImage(List<string> output, Expression imageKey)
		{
			output.Add("Image.GetImageByKey(");
			this.Translator.TranslateExpression(output, imageKey);
			output.Add(")");
		}

		protected override void TranslateGetImageHeight(List<string> output, Expression image)
		{
			output.Add("((Image)");
			this.Translator.TranslateExpression(output, image);
			output.Add(").Height");
		}

		protected override void TranslateGetImageWidth(List<string> output, Expression image)
		{
			output.Add("((Image)");
			this.Translator.TranslateExpression(output, image);
			output.Add(").Width");
		}

		protected override void TranslateGetProgramData(List<string> output)
		{
			output.Add("TranslationHelper.ProgramData");
		}

		protected override void TranslateGetRawByteCodeString(List<string> output, string theString)
		{
			output.Add("ResourceReader.ReadByteCodeFile()");
		}

		protected override void TranslateInitializeGameWithFps(List<string> output, Expression fps)
		{
			output.Add("GameWindow.FPS = ");
			this.Translator.TranslateExpression(output, fps);
		}

		protected override void TranslateInitializeScreen(List<string> output, Expression gameWidth, Expression gameHeight, Expression screenWidth, Expression screenHeight)
		{
			output.Add("GameWindow.InitializeScreen(");
			this.Translator.TranslateExpression(output, gameWidth);
			output.Add(", ");
			this.Translator.TranslateExpression(output, gameHeight);
			if (!(screenWidth is NullConstant))
			{
				output.Add(", ");
				this.Translator.TranslateExpression(output, screenWidth);
				output.Add(", ");
				this.Translator.TranslateExpression(output, screenHeight);
			}
			output.Add(")");
		}

		protected override void TranslateInt(List<string> output, Expression value)
		{
			output.Add("((int)");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateIsImageLoaded(List<string> output, Expression key)
		{
			throw new Exception("This should have been optimized out.");
		}

		protected override void TranslateIsValidInteger(List<string> output, Expression number)
		{
			output.Add("int.TryParse(");
			this.Translator.TranslateExpression(output, number);
			output.Add(", out v_int1)"); // meh
		}

		protected override void TranslateListClear(List<string> output, Expression list)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".Clear()");
		}

		protected override void TranslateListConcat(List<string> output, Expression listA, Expression listB)
		{
			output.Add("TranslationHelper.ValueListConcat(");
			this.Translator.TranslateExpression(output, listA);
			output.Add(", ");
			this.Translator.TranslateExpression(output, listB);
			output.Add(")");
		}

		protected override void TranslateListGet(List<string> output, Expression list, Expression index)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add("[");
			this.Translator.TranslateExpression(output, index);
			output.Add("]");
		}

		protected override void TranslateListInsert(List<string> output, Expression list, Expression index, Expression value)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".Insert(");
			this.Translator.TranslateExpression(output, index);
			output.Add(", ");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateListJoin(List<string> output, Expression list, Expression sep)
		{
			output.Add("string.Join(");
			this.Translator.TranslateExpression(output, sep);
			output.Add(", ");
			this.Translator.TranslateExpression(output, list);
			output.Add(")");
		}

		protected override void TranslateListJoinChars(List<string> output, Expression list)
		{
			output.Add("string.Join(\"\", ");
			this.Translator.TranslateExpression(output, list);
			output.Add(")");
		}

		protected override void TranslateListLastIndex(List<string> output, Expression list)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".Count - 1");
		}

		protected override void TranslateListLength(List<string> output, Expression list)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".Count");
		}

		protected override void TranslateListPop(List<string> output, Expression list)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".RemoveAt(");
			this.Translator.TranslateExpression(output, list);
			output.Add(".Count - 1)");
		}

		protected override void TranslateListPush(List<string> output, Expression list, Expression value)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".Add(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateListRemoveAt(List<string> output, Expression list, Expression index)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".RemoveAt(");
			this.Translator.TranslateExpression(output, index);
			output.Add(")");
		}

		protected override void TranslateListReverseInPlace(List<string> output, Expression list)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add(".Reverse()");
		}

		protected override void TranslateListSet(List<string> output, Expression list, Expression index, Expression value)
		{
			this.Translator.TranslateExpression(output, list);
			output.Add("[");
			this.Translator.TranslateExpression(output, index);
			output.Add("] = ");
			this.Translator.TranslateExpression(output, value);
		}

		protected override void TranslateListShuffleInPlace(List<string> output, Expression list)
		{
			output.Add("TranslationHelper.ShuffleInPlace(");
			this.Translator.TranslateExpression(output, list);
			output.Add(")");
		}

		protected override void TranslateMultiplyList(List<string> output, Expression list, Expression num)
		{
			output.Add("TranslationHelper.MultiplyList(");
			this.Translator.TranslateExpression(output, list);
			output.Add(", ");
			this.Translator.TranslateExpression(output, num);
			output.Add(")");
		}

		protected override void TranslateNewArray(List<string> output, StringConstant type, Expression size)
		{
			CSharpPlatform platform = (CSharpPlatform)this.Platform;
			string csharpType = platform.GetTypeStringFromAnnotation(type.FirstToken, type.Value);
			output.Add("new ");
			// Delightful hack...
			int padding = 0;
			while (csharpType.EndsWith("[]"))
			{
				padding++;
				csharpType = csharpType.Substring(0, csharpType.Length - 2);
			}
			output.Add(csharpType);
			output.Add("[");
			this.Translator.TranslateExpression(output, size);
			output.Add("]");
			while (padding-- > 0)
			{
				output.Add("[]");
			}
		}

		protected override void TranslateNewDictionary(List<string> output, StringConstant keyType, StringConstant valueType)
		{
			CSharpPlatform platform = (CSharpPlatform)this.Platform;
			string csharpKeyType = platform.GetTypeStringFromAnnotation(keyType.FirstToken, keyType.Value);
			string csharpValueType = platform.GetTypeStringFromAnnotation(valueType.FirstToken, valueType.Value);
			output.Add("new Dictionary<");
			output.Add(csharpKeyType);
			output.Add(", ");
			output.Add(csharpValueType);
			output.Add(">()");
		}

		protected override void TranslateNewList(List<string> output, StringConstant type)
		{
			CSharpPlatform platform = (CSharpPlatform)this.Platform;
			string csharpType = platform.GetTypeStringFromAnnotation(type.FirstToken, ((StringConstant)type).Value);
			output.Add("new List<");
			output.Add(csharpType);
			output.Add(">()");
		}

		protected override void TranslateNewListOfSize(List<string> output, StringConstant type, Expression length)
		{
			CSharpPlatform platform = (CSharpPlatform)this.Platform;
			output.Add("TranslationHelper.NewListOfSize<");
			output.Add(platform.GetTypeStringFromAnnotation(type.FirstToken, type.Value));
			output.Add(">(");
			this.Translator.TranslateExpression(output, length);
			output.Add(")");
		}

		protected override void TranslateNewStack(List<string> output, StringConstant type)
		{
			CSharpPlatform platform = (CSharpPlatform)this.Platform;
			string csharpType = platform.GetTypeStringFromAnnotation(type.FirstToken, ((StringConstant)type).Value);
			output.Add("new CrStack<");
			output.Add(csharpType);
			output.Add(">()");
		}

		protected override void TranslateParseInt(List<string> output, Expression rawString)
		{
			output.Add("int.Parse(");
			this.Translator.TranslateExpression(output, rawString);
			output.Add(")");
		}

		protected override void TranslateParseJson(List<string> output, Expression rawString)
		{
			output.Add("JsonParser.ParseJsonIntoValue(");
			this.Translator.TranslateExpression(output, rawString);
			output.Add(")");
		}

		protected override void TranslatePauseForFrame(List<string> output)
		{
			// Nope
		}

		protected override void TranslatePrint(List<string> output, Expression message)
		{
			output.Add("System.Console.WriteLine(");
			this.Translator.TranslateExpression(output, message);
			output.Add(")");
		}

		protected override void TranslateRandomFloat(List<string> output)
		{
			output.Add("TranslationHelper.GetRandomNumber()");
		}

		protected override void TranslateRegisterTicker(List<string> output)
		{
			// Nope
		}

		protected override void TranslateRegisterTimeout(List<string> output)
		{
			// Nope
		}

		protected override void TranslateResourceReadText(List<string> output, Expression path)
		{
			// TODO: this
			output.Add("ResourceReader.ReadTextResource(");
			this.Translator.TranslateExpression(output, path);
			output.Add(")");
		}

		protected override void TranslateSetProgramData(List<string> output, Expression programData)
		{
			output.Add("TranslationHelper.ProgramData = ");
			this.Translator.TranslateExpression(output, programData);
		}

		protected override void TranslateSetTitle(List<string> output, Expression title)
		{
			output.Add("GameWindow.Instance.SetTitle(");
			this.Translator.TranslateExpression(output, title);
			output.Add(")");
		}

		protected override void TranslateSin(List<string> output, Expression value)
		{
			output.Add("Math.Sin(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateStackGet(List<string> output, Expression stack, Expression index)
		{
			this.Translator.TranslateExpression(output, stack);
			output.Add(".Items[");
			this.Translator.TranslateExpression(output, index);
			output.Add("]");
		}

		protected override void TranslateStackLength(List<string> output, Expression stack)
		{
			this.Translator.TranslateExpression(output, stack);
			output.Add(".Size");
		}

		protected override void TranslateStackPop(List<string> output, Expression stack)
		{
			this.Translator.TranslateExpression(output, stack);
			output.Add(".Pop()");
		}

		protected override void TranslateStackPush(List<string> output, Expression stack, Expression value)
		{
			this.Translator.TranslateExpression(output, stack);
			output.Add(".Push(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateStackSet(List<string> output, Expression stack, Expression index, Expression value)
		{
			this.Translator.TranslateExpression(output, stack);
			output.Add(".Items[");
			this.Translator.TranslateExpression(output, index);
			output.Add("] = ");
			this.Translator.TranslateExpression(output, value);
		}

		protected override void TranslateStringAsChar(List<string> output, StringConstant stringConstant)
		{
			char c = stringConstant.Value[0];
			string value;
			switch (c)
			{
				case '\'': value = "'\\''"; break;
				case '\\': value = "'\\\\'"; break;
				case '\n': value = "'\\n'"; break;
				case '\r': value = "'\\r'"; break;
				case '\0': value = "'\\0'"; break;
				case '\t': value = "'\\t'"; break;
				default: value = "'" + c + "'"; break;
			}
			output.Add(value);
		}

		protected override void TranslateStringCast(List<string> output, Expression thing, bool strongCast)
		{
			if (strongCast)
			{
				output.Add("(");
				this.Translator.TranslateExpression(output, thing);
				output.Add(").ToString()");
			}
			else
			{
				this.Translator.TranslateExpression(output, thing);
			}
		}

		protected override void TranslateStringCharAt(List<string> output, Expression stringValue, Expression index)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add("[");
			this.Translator.TranslateExpression(output, index);
			output.Add("]");
		}

		protected override void TranslateStringContains(List<string> output, Expression haystack, Expression needle)
		{
			this.Translator.TranslateExpression(output, haystack);
			output.Add(".Contains(");
			this.Translator.TranslateExpression(output, needle);
			output.Add(")");
		}

		protected override void TranslateStringEndsWith(List<string> output, Expression stringExpr, Expression findMe)
		{
			this.Translator.TranslateExpression(output, stringExpr);
			output.Add(".EndsWith(");
			this.Translator.TranslateExpression(output, findMe);
			output.Add(")");
		}

		protected override void TranslateStringFromCode(List<string> output, Expression characterCode)
		{
			output.Add("(\"\" + (char)");
			this.Translator.TranslateExpression(output, characterCode);
			output.Add(")");
		}

		protected override void TranslateStringIndexOf(List<string> output, Expression haystack, Expression needle)
		{
			this.Translator.TranslateExpression(output, haystack);
			output.Add(".IndexOf(");
			this.Translator.TranslateExpression(output, needle);
			output.Add(")");
		}

		protected override void TranslateStringLength(List<string> output, Expression stringValue)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".Length");
		}

		protected override void TranslateStringLower(List<string> output, Expression stringValue)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".ToLowerInvariant()");
		}

		protected override void TranslateStringParseFloat(List<string> output, Expression stringValue)
		{
			output.Add("double.Parse(");
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(")");
		}

		protected override void TranslateStringParseInt(List<string> output, Expression value)
		{
			output.Add("int.Parse(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateStringReplace(List<string> output, Expression stringValue, Expression findMe, Expression replaceWith)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".Replace(");
			this.Translator.TranslateExpression(output, findMe);
			output.Add(", ");
			this.Translator.TranslateExpression(output, replaceWith);
			output.Add(")");
		}

		protected override void TranslateStringReverse(List<string> output, Expression stringValue)
		{
			output.Add("TranslationHelper.StringReverse(");
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(")");
		}

		protected override void TranslateStringSplit(List<string> output, Expression stringExpr, Expression sep)
		{
			output.Add("TranslationHelper.StringSplit(");
			this.Translator.TranslateExpression(output, stringExpr);
			output.Add(", ");
			this.Translator.TranslateExpression(output, sep);
			output.Add(")");
		}

		protected override void TranslateStringStartsWith(List<string> output, Expression stringExpr, Expression findMe)
		{
			this.Translator.TranslateExpression(output, stringExpr);
			output.Add(".StartsWith(");
			this.Translator.TranslateExpression(output, findMe);
			output.Add(")");
		}

		protected override void TranslateStringTrim(List<string> output, Expression stringValue)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".Trim()");
		}

		protected override void TranslateStringUpper(List<string> output, Expression stringValue)
		{
			this.Translator.TranslateExpression(output, stringValue);
			output.Add(".ToUpperInvariant()");
		}

		protected override void TranslateTan(List<string> output, Expression value)
		{
			output.Add("Math.Tan(");
			this.Translator.TranslateExpression(output, value);
			output.Add(")");
		}

		protected override void TranslateUnregisterTicker(List<string> output)
		{
			// Nope
		}

		protected override void TranslateUnsafeFloatDivision(List<string> output, Expression numerator, Expression denominator)
		{
			this.Translator.TranslateExpression(output, numerator);
			output.Add(" / ");
			this.Translator.TranslateExpression(output, denominator);
		}

		protected override void TranslateUnsafeIntegerDivision(List<string> output, Expression numerator, Expression denominator)
		{
			this.Translator.TranslateExpression(output, numerator);
			output.Add(" / ");
			this.Translator.TranslateExpression(output, denominator);
		}
	}
}
