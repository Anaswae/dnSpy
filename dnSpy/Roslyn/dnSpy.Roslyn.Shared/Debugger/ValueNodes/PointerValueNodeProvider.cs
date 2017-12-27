﻿/*
    Copyright (C) 2014-2017 de4dot@gmail.com

    This file is part of dnSpy

    dnSpy is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    dnSpy is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with dnSpy.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using dnSpy.Contracts.Debugger.DotNet.Evaluation;
using dnSpy.Contracts.Debugger.DotNet.Evaluation.ValueNodes;
using dnSpy.Contracts.Debugger.DotNet.Text;
using dnSpy.Contracts.Debugger.Evaluation;

namespace dnSpy.Roslyn.Shared.Debugger.ValueNodes {
	sealed class PointerValueNodeProvider : DbgDotNetValueNodeProvider {
		public override DbgDotNetText Name => DbgDotNetText.Empty;
		public override string Expression { get; }
		public override string ImageName => PredefinedDbgValueNodeImageNames.Pointer;
		public override bool? HasChildren => true;

		readonly DbgDotNetValueNodeProviderFactory valueNodeProviderFactory;
		readonly DbgDotNetValue value;
		DbgDotNetValue derefValue;
		bool initialized;

		public PointerValueNodeProvider(DbgDotNetValueNodeProviderFactory valueNodeProviderFactory, string expression, DbgDotNetValue value) {
			Debug.Assert(IsSupported(value));
			this.valueNodeProviderFactory = valueNodeProviderFactory;
			this.value = value;
			Expression = expression;
		}

		public static bool IsSupported(DbgDotNetValue value) =>
			value.Type.IsPointer && !value.IsNull && value.Type.GetElementType() != value.Type.AppDomain.System_Void;

		public override ulong GetChildCount(DbgEvaluationInfo evalInfo) {
			if (!initialized) {
				initialized = true;
				derefValue = value.LoadIndirect();
				Debug.Assert(derefValue != null);
			}
			return derefValue != null ? 1UL : 0;
		}

		public override DbgDotNetValueNode[] GetChildren(LanguageValueNodeFactory valueNodeFactory, DbgEvaluationInfo evalInfo, ulong index, int count, DbgValueNodeEvaluationOptions options) {
			if (derefValue == null)
				return Array.Empty<DbgDotNetValueNode>();
			var derefExpr = valueNodeProviderFactory.GetDereferenceExpression(Expression);
			ref readonly var derefName = ref valueNodeProviderFactory.GetDereferencedName();
			var nodeInfo = new DbgDotNetValueNodeInfo(derefValue, derefExpr);
			var res = valueNodeProviderFactory.Create(evalInfo, true, derefValue.Type, nodeInfo, options);
			DbgDotNetValueNode valueNode;
			if (res.ErrorMessage != null)
				valueNode = valueNodeFactory.CreateError(evalInfo, DbgDotNetText.Empty, res.ErrorMessage, derefExpr, false);
			else
				valueNode = valueNodeFactory.Create(res.Provider, derefName, nodeInfo, derefExpr, PredefinedDbgValueNodeImageNames.DereferencedPointer, false, false, value.Type.GetElementType(), derefValue.Type, null, default, null);
			return new[] { valueNode };
		}

		public override void Dispose() => derefValue?.Dispose();
	}
}
