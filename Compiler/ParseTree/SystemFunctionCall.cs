﻿namespace Crayon.ParseTree
{
	internal class SystemFunctionCall : Expression
	{
		public override bool CanAssignTo { get { return false; } }
		public string Name { get; private set; }
		public Expression[] Args { get; private set; }
		public ILibraryConfig AssociatedLibrary { get; private set; }

		public SystemFunctionCall(Token token, Expression[] args, Executable owner)
			: base(token, owner)
		{
			this.Name = token.Value;
			this.Args = args;
		}

		internal override Expression Resolve(Parser parser)
		{
			if (this.Name.StartsWith("$_lib_"))
			{
				string libraryName = this.Name.Split('_')[2];
				ILibraryConfig library = parser.SystemLibraryManager.GetLibraryFromKey(libraryName);
				if (library != null)
				{
					this.AssociatedLibrary = library;
				}
			}

			if (this.Name == "$_comment" && !parser.PreserveTranslationComments)
			{
				return null;
			}

			if (this.Name == "$_has_increment")
			{
				bool hasIncrement = !parser.NullablePlatform.GetType().IsAssignableFrom(typeof(Crayon.Translator.Python.PythonPlatform));
				return new BooleanConstant(this.FirstToken, hasIncrement, this.FunctionOrClassOwner);
			}

			if (this.Name == "$_is_javascript")
			{
				bool isJavaScript = parser.NullablePlatform.GetType().IsAssignableFrom(typeof(Crayon.Translator.JavaScript.JavaScriptPlatform));
				return new BooleanConstant(this.FirstToken, isJavaScript, this.FunctionOrClassOwner);
			}

			// args have already been resolved.
			return this;
		}

		internal override Expression ResolveNames(Parser parser, System.Collections.Generic.Dictionary<string, Executable> lookup, string[] imports)
		{
			this.BatchExpressionNameResolver(parser, lookup, imports, this.Args);
			return this;
		}

		internal override void VariableUsagePass(Parser parser)
		{
			for (int i = 0; i < this.Args.Length; ++i)
			{
				this.Args[i].VariableUsagePass(parser);
			}
		}

		internal override void VariableIdAssignmentPass(Parser parser)
		{
			for (int i = 0; i < this.Args.Length; ++i)
			{
				this.Args[i].VariableIdAssignmentPass(parser);
			}
		}
	}
}
