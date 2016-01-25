﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crayon.ParseTree
{
	internal class SystemFunctionReference : Expression
	{
		public override bool CanAssignTo { get { return false; } }

		public SystemFunctionReference(Token token, Executable owner)
			: base(token, owner)
		{ }

		internal override Expression Resolve(Parser parser)
		{
			throw new ParserException(this.FirstToken, "System functions cannot be passed around. They must be invoked immediately.");
		}

		internal override Expression ResolveNames(Parser parser, Dictionary<string, Executable> lookup, string[] imports)
		{
			throw new InvalidOperationException(); // created in the name resolution phase.
		}

		internal override void VariableIdAssignmentPass(Parser parser)
		{
			throw new NotImplementedException();
		}

		internal override void VariableUsagePass(Parser parser)
		{
			throw new NotImplementedException();
		}
	}
}
