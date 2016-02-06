﻿using System.Collections.Generic;

namespace Crayon.ParseTree
{
	internal class ConstStatement : Executable
	{
		public Expression Expression { get; private set; }
		public Token NameToken { get; private set; }
		public string Name { get; private set; }
		public string Namespace { get; private set; }

		public ConstStatement(Token constToken, Token nameToken, string ns, Expression expression, Executable owner)
			: base(constToken, owner)
		{
			this.Expression = expression;
			this.NameToken = nameToken;
			this.Name = nameToken.Value;
			this.Namespace = ns;
		}

		internal override IList<Executable> Resolve(Parser parser)
		{
			this.Expression = this.Expression.Resolve(parser);

			if (this.Expression is IntegerConstant ||
				this.Expression is BooleanConstant ||
				this.Expression is FloatConstant ||
				this.Expression is StringConstant)
			{
				// that's fine.
			}
			else
			{
				throw new ParserException(this.FirstToken, "Invalid valud for const. Expression must resolve to a constant at compile time.");
			}

			parser.RegisterConst(this.NameToken, this.Expression);
			return new Executable[0];
		}

		internal override void CalculateLocalIdPass(VariableIdAllocator varIds) { }

		internal override void SetLocalIdPass(VariableIdAllocator varIds) { }

		internal override Executable ResolveNames(Parser parser, Dictionary<string, Executable> lookup, string[] imports)
		{
			throw new System.InvalidOperationException();
		}

		internal override void GenerateGlobalNameIdManifest(VariableIdAllocator varIds)
		{
			throw new System.InvalidOperationException(); // should be resolved by now.
		}
	}
}