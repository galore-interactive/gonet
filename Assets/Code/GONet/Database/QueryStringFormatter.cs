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
