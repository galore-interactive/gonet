using System;
using System.Text.RegularExpressions;

namespace GONet.Database
{
    public class TableQueryColumnFilter<T> where T : IDatabaseRow
    {
        public string[] ColumnNames { get; private set; }

        public object[] ComparisonValues { get; private set; }

        public ComparisonOperation[] ComparisonOperationsSeparatedWithORs { get; private set; }

        public TableColumnOperator[] AggregateColumnOperators { get; private set; }

        public TableQueryColumnFilter() { }

        public TableQueryColumnFilter(string columnName, ComparisonOperation operation, TableColumnOperator aggregateColumnOperator)
            : this(new[] { columnName }, new ComparisonOperation[] { operation }, new object[1], new TableColumnOperator[] { aggregateColumnOperator }) { }

        public TableQueryColumnFilter(string columnName, ComparisonOperation[] operationsSeparatedWithORs, TableColumnOperator[] aggregateColumnOperators)
            : this(columnName, operationsSeparatedWithORs, new object[operationsSeparatedWithORs.Length], aggregateColumnOperators) { }

        public TableQueryColumnFilter(string[] columnNames, ComparisonOperation[] operationsSeparatedWithORs, TableColumnOperator[] aggregateColumnOperators)
            : this(columnNames, operationsSeparatedWithORs, new object[operationsSeparatedWithORs.Length], aggregateColumnOperators) { }

        public TableQueryColumnFilter(string columnName, ComparisonOperation operation, object value)
            : this(new[] { columnName }, new ComparisonOperation[] { operation }, new object[] { value }, new TableColumnOperator[1]) { }

        public TableQueryColumnFilter(string columnName, ComparisonOperation[] operationsSeparatedWithORs, object[] comparisonValues)
            : this(columnName, operationsSeparatedWithORs, comparisonValues, new TableColumnOperator[comparisonValues.Length]) { }

        public TableQueryColumnFilter(string[] columnNames, ComparisonOperation[] operationsSeparatedWithORs, object[] comparisonValues)
            : this(columnNames, operationsSeparatedWithORs, comparisonValues, new TableColumnOperator[comparisonValues.Length]) { }

        private TableQueryColumnFilter(string columnName, ComparisonOperation[] operationsSeparatedWithORs, object[] comparisonValues, TableColumnOperator[] aggregateColumnOperators)
        {
            int length = operationsSeparatedWithORs.Length;
            ColumnNames = new string[length];
            for (int i = 0; i < length; ++i)
            {
                ColumnNames[i] = columnName;
            }
            ComparisonValues = comparisonValues;
            ComparisonOperationsSeparatedWithORs = operationsSeparatedWithORs;
            AggregateColumnOperators = aggregateColumnOperators;
        }

        private TableQueryColumnFilter(string[] columnNames, ComparisonOperation[] operationsSeparatedWithORs, object[] comparisonValues, TableColumnOperator[] aggregateColumnOperators)
        {
            ColumnNames = columnNames;
            ComparisonValues = comparisonValues;
            ComparisonOperationsSeparatedWithORs = operationsSeparatedWithORs;
            AggregateColumnOperators = aggregateColumnOperators;
        }
    }

    public enum ComparisonOperation : byte
    {
        /// <summary>
        /// NOTE: When this is applied to <see cref="TableQueryColumnFilter.ComparisonValues"/> of size greater than one, this comparison operator checks for equality in each item in the array until one matches at which point the entire comparison evaluates to true.
        /// </summary>
        Equal,

        /// <summary>
        /// NOTE: When this is applied to <see cref="TableQueryColumnFilter.ComparisonValues"/> of size greater than one, this comparison operator checks for equality in each item in the array until one matches at which point the entire comparison evaluates to false.
        /// </summary>
        NotEqual,

        /// <summary>
        /// NOTE: When this is applied to <see cref="TableQueryColumnFilter.ComparisonValues"/> of size greater than one, this comparison operator checks for greater than in each item in the array until one does not match at which point the entire comparison evaluates to false.
        /// </summary>
        GreaterThan,

        /// <summary>
        /// NOTE: When this is applied to <see cref="TableQueryColumnFilter.ComparisonValues"/> of size greater than one, this comparison operator checks for greater than or equal to in each item in the array until one does not match at which point the entire comparison evaluates to false.
        /// </summary>
        GreaterThanOrEqual,

        /// <summary>
        /// NOTE: When this is applied to <see cref="TableQueryColumnFilter.ComparisonValues"/> of size greater than one, this comparison operator checks for less than in each item in the array until one does not match at which point the entire comparison evaluates to false.
        /// </summary>
        LessThan,

        /// <summary>
        /// NOTE: When this is applied to <see cref="TableQueryColumnFilter.ComparisonValues"/> of size greater than one, this comparison operator checks for less than or equal to in each item in the array until one does not match at which point the entire comparison evaluates to false.
        /// </summary>
        LessThanOrEqual,

        /// <summary>
        /// This indicates something similar to a standard SQL "like" clause, but more powerful.
        /// NOTE: When this is applied to <see cref="TableQueryColumnFilter.ComparisonValues"/> of size greater than one, this comparison operator checks for a match in each item in the array until one matches at which point the entire comparison evaluates to true.
        /// WARNING: Try at your own risk as not all adapter implementations will support this.
        /// </summary>
        MatchesRegex
    }

    public enum TableColumnOperator : byte
    {
        None,
        Minimum,
        Maximum,
        Average,
        Sum,
        Total,
        Count,
    }

    public static class TableQueryColumnFilterComparisonEvaluator
    {
        public static bool Compare(object lhs, ComparisonOperation comparisonOperation, object[] rhs)
        {
            switch (comparisonOperation)
            {
                case ComparisonOperation.Equal:
                    return Equal(lhs, rhs);
                case ComparisonOperation.GreaterThan:
                    return GreaterThan(lhs, rhs);
                case ComparisonOperation.GreaterThanOrEqual:
                    return GreaterThanOrEqual(lhs, rhs);
                case ComparisonOperation.LessThan:
                    return LessThan(lhs, rhs);
                case ComparisonOperation.LessThanOrEqual:
                    return LessThanOrEqual(lhs, rhs);
                case ComparisonOperation.MatchesRegex:
                    return MatchesRegex(lhs, rhs);
                case ComparisonOperation.NotEqual:
                    return NotEqual(lhs, rhs);
                default:
                    return false;
            }
        }

        private static bool NotEqual(object lhs, object[] rhs)
        {
            int length = rhs.Length;
            for (int i = 0; i < length; ++i)
            {
                var rhsAtI = rhs[i];
                if (Equals(lhs, rhsAtI))
                {
                    return false;
                }
            }

            return true;
        }

        private static bool MatchesRegex(object lhs, object[] rhs)
        {
            int length = rhs.Length;
            for (int i = 0; i < length; ++i)
            {
                var rhsAtI = rhs[i];
                if (Regex.IsMatch(lhs.ToString(), rhsAtI.ToString()))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool LessThanOrEqual(object lhs, object[] rhs)
        {
            int length = rhs.Length;
            for (int i = 0; i < length; ++i)
            {
                var rhsAtI = rhs[i];

                var lhsComparable = lhs as IComparable;
                bool isNotMatch = lhsComparable == null || lhsComparable.CompareTo(rhsAtI) > 0;
                if (isNotMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool LessThan(object lhs, object[] rhs)
        {
            int length = rhs.Length;
            for (int i = 0; i < length; ++i)
            {
                var rhsAtI = rhs[i];

                var lhsComparable = lhs as IComparable;
                bool isNotMatch = lhsComparable == null || lhsComparable.CompareTo(rhsAtI) >= 0;
                if (isNotMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool GreaterThanOrEqual(object lhs, object[] rhs)
        {
            int length = rhs.Length;
            for (int i = 0; i < length; ++i)
            {
                var rhsAtI = rhs[i];

                var lhsComparable = lhs as IComparable;
                bool isNotMatch = lhsComparable == null || lhsComparable.CompareTo(rhsAtI) < 0;
                if (isNotMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool GreaterThan(object lhs, object[] rhs)
        {
            int length = rhs.Length;
            for (int i = 0; i < length; ++i)
            {
                var rhsAtI = rhs[i];

                var lhsComparable = lhs as IComparable;
                bool isNotMatch = lhsComparable == null || lhsComparable.CompareTo(rhsAtI) <= 0;
                if (isNotMatch)
                {
                    return false;
                }
            }

            return true;
        }

        private static bool Equal(object lhs, object[] rhs)
        {
            int length = rhs.Length;
            for (int i = 0; i < length; ++i)
            {
                var rhsAtI = rhs[i];
                if (Equals(lhs, rhsAtI))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
