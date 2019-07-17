using System;
using System.Text;

namespace GONet.Database.Sqlite
{
    public class SQLiteQueryFormatter : QueryStringFormatter
    {
        public static readonly SQLiteQueryFormatter Instance = new SQLiteQueryFormatter();

        const string FROM = " from ";
        const string SELECT = "select uid, mpb";
        const string WHERE = " where";
        const string SPACE = " ";
        const string AMP = "@";
        const string UNDIE = "_";
        const string OR = "or";
        const string AND = "and";
        const string LEFT_PAREN = "(";
        const string RIGHT_PAREN = ")";
        const string SELECT_AGGREGATE_COLUMN_OPERATOR_FORMAT = "select {0} from {1}";
        private const string DELETE = "DELETE";
        private const string SELECT_COUNT = "select COUNT()";

        SQLiteQueryFormatter() { }

        public override string CreateSelectStatement<T>(params TableQueryColumnFilter<T>[] queryFilters)
        {
            StringBuilder builder = new StringBuilder();
            Type tableType = typeof(T);
            builder.Append(SELECT);

            builder.Append(FROM).Append(tableType.Name);
            CreateWhereClause(queryFilters, builder, tableType);

            return builder.ToString();
        }

        public override string CreateSelectCountStatement<T>(params TableQueryColumnFilter<T>[] queryFilters)
        {
            StringBuilder builder = new StringBuilder();
            Type tableType = typeof(T);
            builder.Append(SELECT_COUNT);

            builder.Append(FROM).Append(tableType.Name);
            CreateWhereClause(queryFilters, builder, tableType);

            return builder.ToString();
        }

        public override string CreateDeleteStatement<T>(params TableQueryColumnFilter<T>[] queryFilters)
        {
            StringBuilder builder = new StringBuilder();
            Type tableType = typeof(T);
            builder.Append(DELETE)
                .Append(FROM).Append(tableType.Name);

            CreateWhereClause(queryFilters, builder, tableType);

            return builder.ToString();
        }

        private void CreateWhereClause<T>(TableQueryColumnFilter<T>[] queryFilters, StringBuilder builder, Type tableType) where T : IDatabaseRow
        {
            if (queryFilters != null && queryFilters.Length > 0)
            {
                builder.Append(WHERE);

                int filterCount = queryFilters.Length;
                for (int iFilter = 0; iFilter < filterCount; ++iFilter)
                {
                    TableQueryColumnFilter<T> columnFilter = queryFilters[iFilter];
                    int comparisonCount = columnFilter.ComparisonValues.Length;
                    if (comparisonCount > 1)
                    {
                        builder.Append(LEFT_PAREN);
                    }
                    for (int iComparison = 0; iComparison < comparisonCount; ++iComparison)
                    {
                        string columnName = columnFilter.ColumnNames[iComparison];
                        builder.Append(SPACE)
                            .Append(columnName)
                            .Append(ComparisonOperationStringMap[columnFilter.ComparisonOperationsSeparatedWithORs[iComparison]]);

                        TableColumnOperator tableColumnOperator = columnFilter.AggregateColumnOperators[iComparison];
                        if (tableColumnOperator == TableColumnOperator.None)
                        {
                            string bob = string.Concat(AMP, columnName, iFilter, UNDIE, iComparison);// this three things together refer to an argument passed into the "prepared statement" (i.e., placeholder value to go here!)
                            builder.Append(bob);
                        }
                        else
                        {
                            string bob = string.Format(base.TableColumnOperatorStringFormatMap[tableColumnOperator], columnName);
                            string aggregateSelector = string.Format(SELECT_AGGREGATE_COLUMN_OPERATOR_FORMAT, bob, tableType.Name);
                            builder.Append(LEFT_PAREN).Append(aggregateSelector).Append(RIGHT_PAREN);
                        }

                        builder.Append(SPACE);

                        if (iComparison < comparisonCount - 1)
                        {
                            builder.Append(OR);
                        }
                    }
                    if (comparisonCount > 1)
                    {
                        builder.Append(RIGHT_PAREN);
                    }

                    if (iFilter < filterCount - 1)
                    {
                        builder.Append(AND);
                    }
                }
            }
        }
    }
}
