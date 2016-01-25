﻿namespace Crayon.ParseTree
{
	internal class BooleanNot : Expression
	{
		public override bool CanAssignTo { get { return false; } }

		public Expression Root { get; private set; }

		public BooleanNot(Token bang, Expression root, Executable owner)
			: base(bang, owner)
		{
			this.Root = root;
		}

		internal override Expression Resolve(Parser parser)
		{
			this.Root = this.Root.Resolve(parser);

			if (this.Root is BooleanConstant)
			{
				return new BooleanConstant(this.FirstToken, !((BooleanConstant)this.Root).Value, this.FunctionOrClassOwner);
			}

			return this;
		}

		internal override void VariableUsagePass(Parser parser)
		{
			this.Root.VariableUsagePass(parser);
		}

		internal override void VariableIdAssignmentPass(Parser parser)
		{
			this.Root.VariableIdAssignmentPass(parser);
		}

		internal override Expression ResolveNames(Parser parser, System.Collections.Generic.Dictionary<string, Executable> lookup, string[] imports)
		{
			this.Root = this.Root.ResolveNames(parser, lookup, imports);
			return this;
		}
	}
}
