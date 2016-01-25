﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Crayon.ParseTree
{
	internal class DotStepStruct : Expression
	{
		public override bool CanAssignTo { get { return true; } }

		public Token DotToken { get; private set; }
		public Expression RawRoot { get; private set; }
		public string RootVar { get; private set; }
		public string FieldName { get; private set; }
		public StructDefinition StructDefinition { get; private set; }

		public DotStepStruct(Token token, StructDefinition structDef, DotStep original, Executable owner)
			: base(token, owner)
		{
			this.DotToken = original.DotToken;
			this.RawRoot = original.Root;
			this.RootVar = "v_" + ((Variable)original.Root).Name.Split('$')[1];
			this.FieldName = original.StepToken.Value;
			this.StructDefinition = structDef;
		}

		internal override Expression Resolve(Parser parser)
		{
			return this;
		}

		internal override void VariableIdAssignmentPass(Parser parser)
		{
		}

		internal override void VariableUsagePass(Parser parser)
		{
		}

		internal override Expression ResolveNames(Parser parser, Dictionary<string, Executable> lookup, string[] imports)
		{
			throw new InvalidOperationException();
		}
	}
}
