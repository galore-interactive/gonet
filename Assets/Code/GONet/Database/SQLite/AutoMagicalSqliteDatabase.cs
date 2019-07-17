using Community.CsharpSqlite.SQLiteClient;
using GONet.Utils;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using UnityEngine;

namespace GONet.Database.Sqlite
{
    /// <summary>
    /// Sqlite and MessagePack persistence option with CRUD operations.
    /// A very easy to use Sqlite persistence layer that requires 0 (zero)
    /// configuration to start using.  Well, unless of course you consider telling
    /// it where the database file is to be configuration (even so, another feature is 
    /// a database file is created if not already present)...let's say there is no
    /// schema config stuffs required (e.g. CREATE TABLE...SQL mumbo jumbo).  Users do
    /// not even need to know SQL to use this easy API.
    /// By default, any <see cref="IDatabaseRow"/> objects saved here will be saved
    /// into a table with two columns (uid PRIMARY KEY INTEGER & mpb BLOB).
    /// If more columns are needed for querying, mark the class properties with the <see cref="FilterableByAttribute"/>
    /// and all the rest is automagical.
    /// The names of the tables will always match the <see cref="Type.Name"/> value of the item(s)
    /// saved into it.
    /// Also, if an item is saved before the table exists, the table is created at that time.
    /// </summary>
    public class AutoMagicalSqliteDatabase : IDatabase
    {
        public const string CONNECTION_STRING_FILE_URI_PREFIX = "file://";
        private const int BYTE_BUFFER_CHUNK_SIZE = 2 * 1024;

        private readonly Dictionary<Thread, ArrayPool<byte>> byteBufferPoolMap = new Dictionary<Thread, ArrayPool<byte>>(2);

        public const long DefaultNoUID = 0;

        private bool wasNoUIDSetExternally = false;
        private long noUID = DefaultNoUID;
        public long NoUID
        {
            get { return noUID; }
            set
            {
                if (wasNoUIDSetExternally)
                {
                    throw new Exception("NoUID already set to: " + noUID);
                }

                noUID = value;
                wasNoUIDSetExternally = true;
            }
        }

        List<Type> typesAlreadyCreatedInDatabase = new List<Type>();

        #region consts

        public const string SQLITE_TYPE_TEXT = "TEXT";
        public const string SQLITE_TYPE_REAL = "REAL";
        public const string SQLITE_TYPE_INTEGER = "INTEGER";
        public const string SQLITE_TYPE_BLOB = "BLOB";
        public const string SQLITE_TYPE_NULL = "NULL";

        private const string SPACE = " ";
        private const string COMMA_SPACE = "," + SPACE;
        private const string AMPERSAND = "@";
        private const string EQUALS_AMPERSAND = " = " + AMPERSAND;

        private const string MESSAGE_PACK_BYTES_PARAMETER_NAME = "@mpb";
        private const string UID_PARAMETER_NAME = "@uid";

        private const string UID_PROPERTY_NAME = "UID";

        private const string INSERT_INTO_FORMAT = "INSERT INTO {0} (mpb{1}) VALUES (" + MESSAGE_PACK_BYTES_PARAMETER_NAME + "{2});";
        private const string SELECT_ALL_COLUMNS_FROM_FORMAT = "SELECT uid, mpb FROM {0}";
        private const string SELECT_ALL_COLUMNS_FROM_WHERE_UID_FORMAT = SELECT_ALL_COLUMNS_FROM_FORMAT + " WHERE uid = " + UID_PARAMETER_NAME;
        private const string SELECT_UID_WHERE_MESSAGE_PACK_BYTES_EQUALS_FORMAT = "SELECT uid FROM {0} WHERE mpb = " + MESSAGE_PACK_BYTES_PARAMETER_NAME;
        private const string UPDATE_FORMAT = "UPDATE {0} SET mpb = " + MESSAGE_PACK_BYTES_PARAMETER_NAME + "{1} WHERE uid = @uid;";
        private const string CREATE_TABLE_FORMAT = "CREATE TABLE IF NOT EXISTS {0} ( uid integer primary key, mpb blob{1});";
        private const string CREATE_INDEX_FORMAT = "CREATE INDEX IF NOT EXISTS {0}_{1}_idx ON {0} ({1});";
        private const string DELETE_FROM_FORMAT = "DELETE FROM {0} WHERE UID = "+UID_PARAMETER_NAME;

        #endregion

        private static ConcurrentDictionary<Type, IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>>> queryablePropertiesDataMap =
            new ConcurrentDictionary<Type, IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>>>();

        /// <summary>
        /// A valid Sqlite 3 connection string.  This must be set prior to performing CRUD operations!
        /// Example: "Data Source=relativePath/save.db"
        /// </summary>
        public string ConnectionString { get; set; }

        #region IPersistor

        public bool Delete(IDatabaseRow item)
        {
            if(item==null || item.UID ==NoUID)
            {
                return false;
            }

            try
            {
                while(currentThread != null)
                {
                    Thread.Sleep(1);
                }
                currentThread = Thread.CurrentThread;
                using (SqliteConnection connection = new SqliteConnection(ConnectionString))
                {
                    OpenConnection(connection);
                    using (SqliteCommand command = (SqliteCommand)connection.CreateCommand())
                    {
                        Type tableType = item.GetType();
                        string tableName = tableType.Name;
                        command.CommandText = string.Format(DELETE_FROM_FORMAT, tableName);
                        command.Parameters.Add(UID_PARAMETER_NAME, DbType.Int64).Value = item.UID;

                        command.ExecuteNonQuery();

                    }
                    connection.Close();
                }
            }
            finally
            {
                currentThread = null;
            }
            return true;
        }

        /// <summary>
        /// Hey dog this shit hasn't been tested so just a heads up
        /// </summary>
        /// <returns></returns>
        public int Delete<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow
        {
            Debug.Assert(queryFilters != null);
            int deletionCount = 0;
            try
            {
                while (currentThread != null)
                {
                    Thread.Sleep(1);
                }
                currentThread = Thread.CurrentThread;

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    OpenConnection(connection);
                    using (SqliteCommand command = (SqliteCommand)connection.CreateCommand())
                    {
                        Type tableType = typeof(T);
                        string tableName = tableType.Name;

                        if (queryFilters != null)
                        {
                            int filterCount = queryFilters.Length;
                            for (int iFilter = 0; iFilter < filterCount; ++iFilter)
                            {
                                TableQueryColumnFilter<T> columnFilter = queryFilters[iFilter];
                                int comparisonCount = columnFilter.ComparisonValues.Length;
                                for (int iComparison = 0; iComparison < comparisonCount; ++iComparison)
                                {
                                    const string AMP = "@";
                                    const string UNDIE = "_";
                                    if (columnFilter.AggregateColumnOperators[iComparison] == TableColumnOperator.None)
                                    {
                                        command.Parameters.Add(string.Concat(AMP, columnFilter.ColumnNames[iComparison], iFilter, UNDIE, iComparison),
                                                           Mono.Data.Sqlite.SqliteConvert.TypeToDbType(columnFilter.ComparisonValues.GetType())).Value =
                                            columnFilter.ComparisonValues[iComparison];
                                    }
                                }
                            }
                        }

                        //Since the parameters are the same between the two statements, just need to change the commandtext to say delete instead of select.
                        command.CommandText = SQLiteQueryFormatter.Instance.CreateDeleteStatement(queryFilters);
                        deletionCount = command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch (SqliteException ex)
            {
                GONetLog.Error(ex.Message);
                deletionCount = -1;
                // for now assume it is table does not exist, which we quietly hide....look at it as acceptable
            }
            finally
            {
                currentThread = null;
            }


            return deletionCount;
        }

        public bool Delete<T>(long uid) where T : IDatabaseRow
        {
            if(uid == NoUID)
            {
                return false;
            }
            try
            {
                while (currentThread != null)
                {
                    Thread.Sleep(1);
                }
                currentThread = Thread.CurrentThread;

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    OpenConnection(connection);
                    using (SqliteCommand command = (SqliteCommand)connection.CreateCommand())
                    {
                        Type tableType = typeof(T);
                        string tableName = tableType.Name;

                        command.CommandText = string.Format(DELETE_FROM_FORMAT, tableName);
                        command.Parameters.Add(UID_PARAMETER_NAME, DbType.Int64).Value = uid;

                        command.ExecuteNonQuery();
                    }
                    connection.Close();
                }
            }
            catch(SqliteException ex)
            {
                GONetLog.Error(ex.Message);
                currentThread = null;
                return false;
            }
            finally
            {
                currentThread = null;
            }
            return true;
        }

        public T Read<T>(long uid) where T : IDatabaseRow
        {
            T item = default(T);

            if (uid == NoUID)
            {
                return item;
            }

            try
            {
                while (currentThread != null)
                {
                    Thread.Sleep(1);
                }
                currentThread = Thread.CurrentThread;

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    OpenConnection(connection);
                    using (SqliteCommand command = (SqliteCommand)connection.CreateCommand())
                    {
                        Type tableType = typeof(T);
                        string tableName = tableType.Name;

                        command.CommandText = string.Format(SELECT_ALL_COLUMNS_FROM_WHERE_UID_FORMAT, tableName);
                        command.Parameters.Add(UID_PARAMETER_NAME, DbType.Int64).Value = uid;

                        using (var reader = command.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                item = MapObjectFromReader<T>(reader);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqliteException)
            {
                // for now assume it is table does not exist, which we quietly hide....look at it as acceptable
            }
            finally
            {
                currentThread = null;
            }

            return item;
        }

        public List<T> ReadAll<T>() where T : IDatabaseRow
        {
            return ReadList<T>();
        }

        public T ReadFirstOrDefault<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow
        {
            List<T> list = ReadList(queryFilters);
            if (list == null)
            {
                return default(T);
            }
            else
            {
                return list.FirstOrDefault();
            }
        }

        volatile Thread currentThread;

        public void AppendListWithPostWhereClause<T>(List<T> appendTo, string postWhereClause, params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow
        {
            try
            {
                while (currentThread != null)
                {
                    Thread.Sleep(1);
                }
                currentThread = Thread.CurrentThread;

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    OpenConnection(connection);
                    using (SqliteCommand command = (SqliteCommand)connection.CreateCommand())
                    {
                        Type tableType = typeof(T);
                        string tableName = tableType.Name;

                        command.CommandText = SQLiteQueryFormatter.Instance.CreateSelectStatement(queryFilters);

                        if (!string.IsNullOrWhiteSpace(postWhereClause))
                        {
                            command.CommandText += postWhereClause;
                        }

                        if (queryFilters != null)
                        {
                            int filterCount = queryFilters.Length;
                            for (int iFilter = 0; iFilter < filterCount; ++iFilter)
                            {
                                TableQueryColumnFilter<T> columnFilter = queryFilters[iFilter];
                                int comparisonCount = columnFilter.ComparisonValues.Length;
                                for (int iComparison = 0; iComparison < comparisonCount; ++iComparison)
                                {
                                    const string AMP = "@";
                                    const string UNDIE = "_";
                                    if (columnFilter.AggregateColumnOperators[iComparison] == TableColumnOperator.None)
                                    {
                                        command.Parameters.Add(string.Concat(AMP, columnFilter.ColumnNames[iComparison], iFilter, UNDIE, iComparison),
                                                           Mono.Data.Sqlite.SqliteConvert.TypeToDbType(columnFilter.ComparisonValues.GetType())).Value =
                                            columnFilter.ComparisonValues[iComparison];
                                    }
                                }
                            }
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                T resultRow = MapObjectFromReader<T>(reader);
                                appendTo.Add(resultRow);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqliteException)
            {
                // for now assume it is table does not exist, which we quietly hide....look at it as acceptable
            }
            finally
            {
                currentThread = null;
            }
        }

        public List<T> ReadListWithPostWhereClause<T>(string postWhereClause, params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow
        {
            List<T> list = new List<T>();

            AppendListWithPostWhereClause(list, postWhereClause, queryFilters);

            return list;
        }

        public List<T> ReadList<T>(params TableQueryColumnFilter<T>[] queryFilters) where T : IDatabaseRow
        {
            return ReadListWithPostWhereClause((string)null, queryFilters);
        }

        public bool Save<T>(T item, bool shouldBypassUIDUpdate = false) where T : IDatabaseRow
        {
            if (item == null)
            {
                return false;
            }

            try
            {
                while (currentThread != null)
                {
                    Thread.Sleep(1);
                }
                currentThread = Thread.CurrentThread;

                using (SqliteConnection connection = new SqliteConnection(ConnectionString))
                {
                    OpenConnection(connection);
                    using (SqliteCommand command = (SqliteCommand)connection.CreateCommand())
                    {
                        Type tableType = item.GetType();
                        string tableName = tableType.Name;

                        if (!typesAlreadyCreatedInDatabase.Contains(tableType))
                        {
                            // add the table
                            command.CommandText = string.Format(CREATE_TABLE_FORMAT, tableName, GetCreateTableColumnsClause(tableType));
                            command.ExecuteNonQuery();

                            // add any indexes registered in properties marked with [Queryable(true)]
                            foreach (string createIndexSql in GetCreateIndexStatements(tableType, tableName))
                            {
                                command.CommandText = createIndexSql;
                                command.ExecuteNonQuery();
                            }

                            typesAlreadyCreatedInDatabase.Add(tableType);
                        }

                        bool isSaveAnInsert = item.UID == NoUID; // no UID means need to insert, while if it has UID, it is an update
                        if (isSaveAnInsert)
                        {
                            command.CommandText = string.Format(INSERT_INTO_FORMAT, tableName, GetInsertIntoOrSelectColumnNameClause(tableType), GetInsertIntoValuesClause(tableType, item, command.Parameters));
                        }
                        else
                        {
                            command.CommandText = string.Format(UPDATE_FORMAT, tableName, GetUpdateColumnNameEqualsValueClause(tableType, item, command.Parameters));
                            command.Parameters.Add(UID_PARAMETER_NAME, DbType.Int64).Value = item.UID;
                        }

                        byte[] objectBytes = SerializationUtils.SerializeToBytes(item);
                        command.Parameters.Add(MESSAGE_PACK_BYTES_PARAMETER_NAME, DbType.Binary).Value = objectBytes;

                        command.ExecuteNonQuery(); // does insert or update

                        if (isSaveAnInsert && !shouldBypassUIDUpdate)
                        {
                            command.CommandText = string.Format(SELECT_UID_WHERE_MESSAGE_PACK_BYTES_EQUALS_FORMAT, tableName); // TODO look at this logic, multiple rows might have the same byte[]........which would cause this to break...but one not so pleasant way to solve this is to save one more time after insert happens so that the MessagePack byte[] stored in the database will include the proper UID value, which would fix this issue!!!

                            using (SqliteDataReader reader = command.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    item.UID = reader.GetInt64(0); // this makes sure the return value item has the new UID assigned by database during the insert

                                    // TODO based on the TODO in parent if statement here, we could do another Save(item) call here to have the MessagePack byte[] saved in database reflect the new UID
                                }
                                else
                                {
                                    throw new Exception("just inserted new row, but could not read it back to get the UID assigned to it");
                                }
                            }
                        }
                    }
                    connection.Close();
                }
            }
            finally
            {
                currentThread = null;
            }

            return true;
        }

        /// <summary>
        /// Saves all <paramref name="items"/> in a single transaction (for higher performance/speed).
        /// 
        /// IMPORTANT: Unlike <see cref="Save(IDatabaseRow)"/>, this method does not leave all elements in <paramref name="items"/> with <see cref="IDatabaseRow.UID"/> set when saving anew.
        ///            Therefore, a subsequent call to <see cref="Save(IDatabaseRow)"/> for an element inside <paramref name="items"/> will yield undesirable behavior, in that a new row will be inserted instead an update.  TODO work this issue out!
        /// </summary>
        /// <param name="limit">maximum number of items in <paramref name="items"/> to save, if not supplied, all are saved</param>
        public bool Save<T>(IEnumerable<T> items, int limit = int.MaxValue, bool shouldBypassUIDUpdate = true) where T : IDatabaseRow
        {
            if (items == null)
            {
                return false;
            }

            try
            {
                while (currentThread != null)
                {
                    Thread.Sleep(1);
                }
                currentThread = Thread.CurrentThread;

                using (SqliteConnection connection = new SqliteConnection(ConnectionString))
                {
                    OpenConnection(connection);
                    using (SqliteCommand command = (SqliteCommand)connection.CreateCommand())
                    {
                        Type tableType = typeof(T);
                        string tableName = tableType.Name;

                        if (!typesAlreadyCreatedInDatabase.Contains(tableType))
                        {
                            // add the table
                            command.CommandText = string.Format(CREATE_TABLE_FORMAT, tableName, GetCreateTableColumnsClause(tableType));
                            command.ExecuteNonQuery();

                            // add any indexes registered in properties marked with [Queryable(true)]
                            foreach (string createIndexSql in GetCreateIndexStatements(tableType, tableName))
                            {
                                command.CommandText = createIndexSql;
                                command.ExecuteNonQuery();
                            }

                            typesAlreadyCreatedInDatabase.Add(tableType);
                        }

                        using (SqliteTransaction transaction = (SqliteTransaction)connection.BeginTransaction())
                        {
                            int i = 0;
                            foreach (T item in items)
                            {
                                command.Parameters.Clear(); // since this is re-used, need to clear any parameters from previous items

                                bool isSaveAnInsert = item.UID == NoUID; // no UID means need to insert, while if it has UID, it is an update
                                if (isSaveAnInsert)
                                {
                                    command.CommandText = string.Format(INSERT_INTO_FORMAT, tableName, GetInsertIntoOrSelectColumnNameClause(tableType), GetInsertIntoValuesClause(tableType, item, command.Parameters));
                                }
                                else
                                {
                                    command.CommandText = string.Format(UPDATE_FORMAT, tableName, GetUpdateColumnNameEqualsValueClause(tableType, item, command.Parameters));
                                    command.Parameters.Add(UID_PARAMETER_NAME, DbType.Int64).Value = item.UID;
                                }

                                byte[] objectBytes = SerializationUtils.SerializeToBytes(item);
                                command.Parameters.Add(MESSAGE_PACK_BYTES_PARAMETER_NAME, DbType.Binary).Value = objectBytes;

                                command.ExecuteNonQuery(); // does insert or update

                                if (++i >= limit)
                                {
                                    break;
                                }
                            }

                            transaction.Commit();

                            if (!shouldBypassUIDUpdate)
                            {
                                // TODO FIXME read all the UIDs for these things somehow
                            }
                        }
                    }
                    connection.Close();
                }
            }
            finally
            {
                currentThread = null;
            }

            return true;
        }

        #endregion

        /// <summary>
        /// For those who know SQL and don't want to deal with <see cref="ReadList{T}(TableQueryColumnFilter{T}[])"/> and its
        /// requirement to use <see cref="TableQueryColumnFilter{T}"/>s, then this is the method to use.
        /// </summary>
        /// <typeparam name="T">identifies which table from which to select the return list</typeparam>
        /// <param name="whereClauseAndBeyond">
        /// SQL text to include "where" and everything/anything after it that you want.
        /// WARNING: Your SQL text is not validated to be good/valid prior to executing the query so it is up to you to make it right!
        /// </param>
        /// <returns></returns>
        public List<T> ReadListSQLDirect<T>(string whereClauseAndBeyond) where T : IDatabaseRow
        {
            List<T> list = new List<T>();

            AppendListSQLDirect<T>(list, whereClauseAndBeyond);

            return list;
        }

        public void AppendListSQLDirect<T>(List<T> appendTo, string whereClauseAndBeyond, params object[] clauseArguments) where T : IDatabaseRow
        {
            try
            {
                while (currentThread != null)
                {
                    Thread.Sleep(1);
                }
                currentThread = Thread.CurrentThread;

                using (var connection = new SqliteConnection(ConnectionString))
                {
                    OpenConnection(connection);
                    using (SqliteCommand command = (SqliteCommand)connection.CreateCommand())
                    {
                        Type tableType = typeof(T);
                        string tableName = tableType.Name;

                        TableQueryColumnFilter<T>[] useThisToGetTableTypeAndExcludeWhereClause = null;
                        command.CommandText = SQLiteQueryFormatter.Instance.CreateSelectStatement(useThisToGetTableTypeAndExcludeWhereClause);

                        if (!string.IsNullOrWhiteSpace(whereClauseAndBeyond))
                        {
                            const string SPACE = " ";
                            command.CommandText += string.Concat(SPACE, whereClauseAndBeyond);
                        }

                        if (clauseArguments != null)
                        {
                            const char SPACE = ' ';
                            string[] parameterNames = command.CommandText.Split(SPACE).Where(x => x.StartsWith(AMPERSAND)).ToArray();
                            if (parameterNames == null || clauseArguments.Length != parameterNames.Length)
                            {
                                const string GYST = "Get your shtuff together and pass in the correct @parameter names in your SQL and make sure your clause arguments list/count matches.";
                                throw new ArgumentException(GYST);
                            }

                            int length = clauseArguments.Length;
                            for (int i = 0; i < length; ++i)
                            {
                                object arg = clauseArguments[i];
                                command.Parameters.Add(parameterNames[i], Mono.Data.Sqlite.SqliteConvert.TypeToDbType(arg.GetType())).Value = arg;
                            }
                        }

                        using (var reader = command.ExecuteReader())
                        {
                            while (reader.Read())
                            {
                                T resultRow = MapObjectFromReader<T>(reader);
                                appendTo.Add(resultRow);
                            }
                        }
                    }
                    connection.Close();
                }
            }
            catch (SqliteException)
            {
                // for now assume it is table does not exist, which we quietly hide....look at it as acceptable
            }
            finally
            {
                currentThread = null;
            }
        }

        internal static string GetInsertIntoOrSelectColumnNameClause(Type tableType)
        {
            StringBuilder clauseBuilder = new StringBuilder(200);
            IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>> queryablePropertyData = GetQueryablePropertiesData(tableType);

            foreach (var queryableProperty in queryablePropertyData)
            {
                PropertyInfo property = queryableProperty.Key;
                string propertyName = property.Name;
                clauseBuilder.Append(COMMA_SPACE).Append(propertyName);
            }

            return clauseBuilder.ToString();
        }

        private static void OpenConnection(SqliteConnection connection)
        {
            { // ensure directory/path exists and if not create it or else connection open will fail
                const string EQUALS = "=";
                int equalsIndex = connection.ConnectionString.IndexOf(EQUALS);
                string pathToDbFile = connection.ConnectionString.Substring(equalsIndex + 1).Replace(CONNECTION_STRING_FILE_URI_PREFIX, string.Empty).Trim();
                if (!Path.IsPathRooted(pathToDbFile)) // if not, assuming relative path to current directory
                {
                    pathToDbFile = Path.Combine(Directory.GetCurrentDirectory(), pathToDbFile);
                }

                string directoryPathOnly = Path.GetDirectoryName(pathToDbFile);
                if (!Directory.Exists(directoryPathOnly))
                {
                    Directory.CreateDirectory(directoryPathOnly);
                }
            }

            connection.Open();
        }

        /// <summary>
        /// POST: Also adds to <paramref name="sQLiteParameterCollection"/>
        /// </summary>
        /// <param name="tableType"></param>
        /// <param name="object"></param>
        /// <param name="sQLiteParameterCollection"></param>
        /// <returns>example: ", @name, @building"</returns>
        private object GetInsertIntoValuesClause(Type tableType, IDatabaseRow item, SqliteParameterCollection sQLiteParameterCollection)
        {
            StringBuilder clauseBuilder = new StringBuilder(200);
            IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>> queryablePropertyData = GetQueryablePropertiesData(tableType);

            foreach (var queryableProperty in queryablePropertyData)
            {
                PropertyInfo property = queryableProperty.Key;
                string propertyName = property.Name;
                clauseBuilder.Append(COMMA_SPACE).Append(AMPERSAND).Append(propertyName);

                sQLiteParameterCollection.Add(AMPERSAND + propertyName, Mono.Data.Sqlite.SqliteConvert.TypeToDbType(property.PropertyType)).Value = property.GetValue(item, null); // TODO replace reflection GetValue with FastDynamicProperty or spring impl
            }

            return clauseBuilder.ToString();
        }

        private IEnumerable<string> GetCreateIndexStatements(Type tableType, string tableName)
        {
            IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>> queryablePropertyData = GetQueryablePropertiesData(tableType);
            List<string> createIndexStatements = new List<string>(queryablePropertyData.Count());

            foreach (var queryableProperty in queryablePropertyData)
            {
                if (queryableProperty.Value.CreateIndex)
                {
                    createIndexStatements.Add(string.Format(CREATE_INDEX_FORMAT, tableName, queryableProperty.Key.Name));
                }
            }

            return createIndexStatements;
        }

        private string GetCreateTableColumnsClause(Type tableType)
        {
            StringBuilder clauseBuilder = new StringBuilder(200);
            IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>> queryablePropertyData = GetQueryablePropertiesData(tableType);

            foreach (var queryableProperty in queryablePropertyData)
            {
                PropertyInfo property = queryableProperty.Key;
                clauseBuilder.Append(COMMA_SPACE).Append(property.Name).Append(SPACE).Append(GetSqliteDataTypeString(property.PropertyType));
            }

            return clauseBuilder.ToString();
        }

        private string GetSqliteDataTypeString(Type type)
        {
            DbType dbType = Mono.Data.Sqlite.SqliteConvert.TypeToDbType(type);
            switch (dbType)
            {
                case DbType.String:
                    return SQLITE_TYPE_TEXT;

                case DbType.Single:
                case DbType.Double:
                    return SQLITE_TYPE_REAL;

                case DbType.Boolean:
                case DbType.Date:
                case DbType.DateTime:
                case DbType.DateTime2:
                    return SQLITE_TYPE_INTEGER;

                case DbType.Int16:
                case DbType.Int32:
                case DbType.Int64:
                case DbType.UInt16:
                case DbType.UInt32:
                case DbType.UInt64:
                    return SQLITE_TYPE_INTEGER;

                case DbType.Binary:
                    return SQLITE_TYPE_BLOB;

                default:
                    return SQLITE_TYPE_NULL;
            }
        }

        /// <summary>
        /// POST: Also adds to <paramref name="sQLiteParameterCollection"/>
        /// </summary>
        /// <param name="tableType"></param>
        /// <param name="object"></param>
        /// <param name="sQLiteParameterCollection">this gets updated</param>
        /// <returns>example: "name = @name, buildingNumber = @buildingNumber"</returns>
        private string GetUpdateColumnNameEqualsValueClause(Type tableType, IDatabaseRow item, SqliteParameterCollection sQLiteParameterCollection)
        {
            StringBuilder clauseBuilder = new StringBuilder(200);
            IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>> queryablePropertyData = GetQueryablePropertiesData(tableType);

            foreach (var queryableProperty in queryablePropertyData)
            {
                PropertyInfo property = queryableProperty.Key;
                string propertyName = property.Name;
                clauseBuilder.Append(COMMA_SPACE).Append(propertyName).Append(EQUALS_AMPERSAND).Append(propertyName);

                sQLiteParameterCollection.Add(AMPERSAND + propertyName, Mono.Data.Sqlite.SqliteConvert.TypeToDbType(property.PropertyType)).Value = property.GetValue(item, null); // TODO replace reflection GetValue with FastDynamicProperty or spring impl
            }

            return clauseBuilder.ToString();
        }

        private static IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>> GetQueryablePropertiesData(Type type)
        {
            IEnumerable<KeyValuePair<PropertyInfo, FilterableByAttribute>> queryablePropertiesData = null;
            if (!queryablePropertiesDataMap.TryGetValue(type, out queryablePropertiesData))
            {
                List<KeyValuePair<PropertyInfo, FilterableByAttribute>> list = new List<KeyValuePair<PropertyInfo, FilterableByAttribute>>();

                foreach (var property in type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic).OrderBy(prop => prop.Name))
                {
                    if (UID_PROPERTY_NAME.Equals(property.Name))
                    {
                        // UID_PROPERTY_NAME is automatically treated as Queryable, no need to readd and cause problems of duplication in sql statements
                        continue;
                    }

                    object[] attributes = property.GetCustomAttributes(typeof(FilterableByAttribute), true);
                    if (attributes != null && attributes.Length > 0)
                    {
                        list.Add(new KeyValuePair<PropertyInfo, FilterableByAttribute>(property, (FilterableByAttribute)attributes[0]));
                    }
                }

                queryablePropertiesData = list;

                queryablePropertiesDataMap[type] = queryablePropertiesData;
            }
            return queryablePropertiesData;
        }

        private byte[] GetBytes(SqliteDataReader reader, int columnIndex)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                ArrayPool<byte> byteBufferPool;
                if (!byteBufferPoolMap.TryGetValue(Thread.CurrentThread, out byteBufferPool))
                {
                    byteBufferPool = new ArrayPool<byte>(10, 2, BYTE_BUFFER_CHUNK_SIZE, BYTE_BUFFER_CHUNK_SIZE);
                    byteBufferPoolMap[Thread.CurrentThread] = byteBufferPool;
                }

                byte[] buffer = byteBufferPool.Borrow(BYTE_BUFFER_CHUNK_SIZE);
                long bytesRead;
                long fieldOffset = 0;

                while ((bytesRead = reader.GetBytes(columnIndex, fieldOffset, buffer, 0, BYTE_BUFFER_CHUNK_SIZE)) > 0)
                {
                    stream.Write(buffer, 0, (int)bytesRead);
                    fieldOffset += bytesRead;
                }

                byteBufferPool.Return(buffer);
                return stream.ToArray();
            }
        }

        private T MapObjectFromReader<T>(SqliteDataReader reader) where T : IDatabaseRow
        {
            T item;

            byte[] mpb = GetBytes(reader, 1);
            item = SerializationUtils.DeserializeFromBytes<T>(mpb);
            item.UID = (long)reader.GetInt64(0); // this is set after deserialization on purpose to doubley be sure the uid is correct with the primary key in the database's eye

            return item;
        }
    }
}
