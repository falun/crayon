﻿using System.Collections.Generic;
using System.Linq;

namespace Crayon.ParseTree
{
	internal class FunctionCall : Expression
	{
		public override bool CanAssignTo { get { return false; } }

		public Expression Root { get; private set; }
		public Token ParenToken { get; private set; }
		public Expression[] Args { get; private set; }

		public FunctionCall(Expression root, Token parenToken, IList<Expression> args, Executable owner)
			: base(root.FirstToken, owner)
		{
			this.Root = root;
			this.ParenToken = parenToken;
			this.Args = args.ToArray();
		}

		internal override Expression Resolve(Parser parser)
		{
			// TODO: isset(var) insertion goes here

			for (int i = 0; i < this.Args.Length; ++i)
			{
				this.Args[i] = this.Args[i].Resolve(parser);
			}

			if (this.Root is Variable)
			{
				string varName = ((Variable)this.Root).Name;

				if (parser.IsTranslateMode && varName.StartsWith("$"))
				{
					return new SystemFunctionCall(this.Root.FirstToken, this.Args, this.FunctionOrClassOwner).Resolve(parser);
				}

				if (parser.GetClass(varName) != null)
				{
					throw new ParserException(this.ParenToken, "Cannot invoke a class like a function. To construct a new class, the \"new\" keyword must be used.");
				}
			}

			if (this.Root is DotStep && ((DotStep)this.Root).Root is BaseKeyword)
			{

			}

			this.Root = this.Root.Resolve(parser);

			return this;
		}

		internal override void SetLocalIdPass(VariableIdAllocator varIds)
		{
			this.Root.SetLocalIdPass(varIds);
			for (int i = 0; i < this.Args.Length; ++i)
			{
				this.Args[i].SetLocalIdPass(varIds);
			}
		}

		internal override Expression ResolveNames(Parser parser, Dictionary<string, Executable> lookup, string[] imports)
		{
			this.Root = this.Root.ResolveNames(parser, lookup, imports);
			this.BatchExpressionNameResolver(parser, lookup, imports, this.Args);

			if (this.Root is LibraryFunctionReference)
			{
				return new LibraryFunctionCall(
					this.FirstToken,
					((LibraryFunctionReference)this.Root).Name,
					this.Args,
					this.FunctionOrClassOwner);
			}

			if (this.Root is SystemFunctionReference)
			{
				return new SystemFunctionCall(this.FirstToken, this.Args, this.FunctionOrClassOwner);
			}

			if (this.Root is DotStep ||
				this.Root is Variable ||
				this.Root is FieldReference ||
				this.Root is FunctionReference ||
				this.Root is BracketIndex ||
				this.Root is BaseMethodReference)
			{
				return this;
			}

			if (this.Root is IConstantValue)
			{
				if (this.Args.Length == 1 && this.Args[0] is BinaryOpChain)
				{
					throw new ParserException(this.ParenToken, "Constants cannot be invoked like functions. Although it sort of looks like you're missing an op here.");
				}
				throw new ParserException(this.ParenToken, "Constants cannot be invoked like functions.");
			}

			if (this.Root is ClassReference)
			{
				throw new ParserException(this.Root.FirstToken, "Classes cannot be invoked like a function. If you meant to instantiate a new instance, use the 'new' keyword.");
			}

			throw new ParserException(this.ParenToken, "This cannot be invoked like a function.");
		}
	}
}