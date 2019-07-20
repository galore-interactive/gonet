/* Copyright (C) Shaun Curtis Sheppard - All Rights Reserved
 * Unauthorized copying of this file, via any medium is strictly prohibited
 * Proprietary and confidential
 * Written by Shaun Sheppard <shasheppard@gmail.com>, June 2019
 *
 * Authorized use is explicitly limited to the following:	
 * -The ability to view and reference source code without changing it
 * -The ability to enhance debugging with source code access
 * -The ability to distribute products based on original sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on original source code, whereas this license must be included if source code provided in said products
 * -The ability to modify source code for local use only
 * -The ability to distribute products based on modified sources for non-commercial purposes, whereas this license must be included if source code provided in said products
 * -The ability to commercialize products built on modified source code, whereas this license must be included if source code provided in said products
 */

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace GONet.Database
{
    public abstract class QueryStringFormatter
    {

        static readonly Dictionary<ComparisonOperation, string> defaultComparisonOperationStringMap = new Dictionary<ComparisonOperation, string>()
        {
            { ComparisonOperation.Equal, " == " },
            { ComparisonOperation.GreaterThan, " > " },
            { ComparisonOperation.GreaterThanOrEqual, " >= " },
            { ComparisonOperation.LessThan, " < " },
            { ComparisonOperation.LessThanOrEqual, " <= " },
            { ComparisonOperation.NotEqual, " != " },
        };

        static readonly Dictionary<TableColumnOperator, string> defaultTableColumnOperatorStringFormatMap = new Dictionary<TableColumnOperator, string>()
        {
            { TableColumnOperator.Average, "avg({0})" },
            { TableColumnOperator.Count, "count({0})" },
            { TableColumnOperator.Maximum, "max({0})" },
            { TableColumnOperator.Minimum, "min({0})" },
            { TableColumnOperator.Sum, "sum({0})" },
            { TableColumnOperator.Total, "total({0})" },
        };

        public virtual Dictionary<ComparisonOperation, string> ComparisonOperationStringMap { get { return defaultComparisonOperationStringMap; } }

        public virtual Dictionary<TableColumnOperator, string> TableColumnOperatorStringFormatMap { get { return defaultTableColumnOperatorStringFormatMap; } }

        public abstract string CreateSelectStatement<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow;

        public abstract string CreateSelectCountStatement<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow;

        public abstract string CreateDeleteStatement<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow;

        protected virtual string GetSymbol(ComparisonOperation operation)
        {
            switch (operation)
            {
                case ComparisonOperation.Equal:
                    return "==";

                case ComparisonOperation.GreaterThan:
                    return ">";

                case ComparisonOperation.LessThan:
                    return "<";

                case ComparisonOperation.NotEqual:
                    return "!=";

                case ComparisonOperation.LessThanOrEqual:
                    return "<=";

                case ComparisonOperation.GreaterThanOrEqual:
                    return ">=";

                default:
                    throw new Exception("Operation not coded.");
            }
        }

    }
}
