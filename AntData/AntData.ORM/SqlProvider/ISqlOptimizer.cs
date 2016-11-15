﻿using System;

namespace AntData.ORM.SqlProvider
{
	using SqlQuery;

	public interface ISqlOptimizer
	{
		SelectQuery    Finalize         (SelectQuery selectQuery);
		ISqlExpression ConvertExpression(ISqlExpression expression);
		ISqlPredicate  ConvertPredicate (SelectQuery selectQuery, ISqlPredicate  predicate);
	}
}
