using Database.Initialization;
using Microsoft.Data.Sqlite;
using StackExchange.Profiling.Storage;
using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Data.SqlClient;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MiniProfiler.Initialization
{
    public static class MiniProfilerInitializer
    {
        public static List<(string Schema, string TableName)> Tables => new List<(string Schema, string TableName)>() {
            ("dbo", "MiniProfilerClientTimings"),
            ("dbo", "MiniProfilers"),
            ("dbo", "MiniProfilerTimings")
        };

        #region Ensure Db and Tables Created
        public static async Task<bool> EnsureDbAndTablesCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    return await EnsureDbAndTablesCreatedAsync(connection, cancellationToken);
                }
            }
            else
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    return await EnsureDbAndTablesCreatedAsync(connection, cancellationToken);
                }
            }
        }

        public static async Task<bool> EnsureDbAndTablesCreatedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
        {
            if (existingConnection is SqliteConnection)
            {
                await EnsureDbCreatedAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                var storage = new SqliteStorage("");
                var sqlScripts = storage.TableCreationScripts;
                bool created = false;

                var opened = false;
                if (existingConnection.State != System.Data.ConnectionState.Open)
                {
                    await existingConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    opened = true;
                }

                try
                {
                    using (SqliteTransaction transaction = ((SqliteConnection)existingConnection).BeginTransaction())
                    {
                        foreach (var commandSql in sqlScripts.Where(sqlScript => !persistedTables.Any(table => sqlScript.Contains(table.TableName))))
                        {
                            using (var command = new SqliteCommand(commandSql, ((SqliteConnection)existingConnection), transaction))
                            {
                                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                            }
                            created = true;
                        }

                        transaction.Commit();
                    }
                }
                finally
                {
                    if (opened && existingConnection.State == System.Data.ConnectionState.Open)
                    {
                        existingConnection.Close();
                    }
                }

                return created;
            }
            else if (existingConnection is Microsoft.Data.SqlClient.SqlConnection || existingConnection is System.Data.SqlClient.SqlConnection)
            {
                var created = await EnsureDbCreatedAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                var storage = new SqlServerStorage("");
                var sqlScripts = storage.TableCreationScripts;

                //Initialize Schema
                var opened = false;
                if (existingConnection.State != System.Data.ConnectionState.Open)
                {
                    await existingConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                    opened = true;
                }

                try
                {
                    if (existingConnection is Microsoft.Data.SqlClient.SqlConnection sqlConnection)
                    {
                        using (Microsoft.Data.SqlClient.SqlTransaction transaction = sqlConnection.BeginTransaction())
                        {
                            foreach (var commandSql in sqlScripts.Where(sqlScript => !persistedTables.Any(table => sqlScript.Contains(table.TableName))))
                            {
                                using (var command = new Microsoft.Data.SqlClient.SqlCommand(commandSql, sqlConnection, transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                                created = true;
                            }

                            transaction.Commit();
                        }
                    }
                    else if (existingConnection is System.Data.SqlClient.SqlConnection systemSqlConnection)
                    {
                        using (System.Data.SqlClient.SqlTransaction transaction = systemSqlConnection.BeginTransaction())
                        {
                            foreach (var commandSql in sqlScripts.Where(sqlScript => !persistedTables.Any(table => sqlScript.Contains(table.TableName))))
                            {
                                using (var command = new System.Data.SqlClient.SqlCommand(commandSql, systemSqlConnection, transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                                created = true;
                            }

                            transaction.Commit();
                        }
                    }
                }
                finally
                {
                    if (opened && existingConnection.State == System.Data.ConnectionState.Open)
                    {
                        existingConnection.Close();
                    }
                }

                return created;
            }
            else
            {
                throw new Exception("Unsupported Connection");
            }
        }
        #endregion

        #region Ensure Db Created
        public static Task<bool> EnsureDbCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureCreatedAsync(connectionString, cancellationToken);
        }

        public static Task<bool> EnsureDbCreatedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureCreatedAsync(existingConnection, cancellationToken);
        }
        #endregion

        #region Ensure Tables Deleted
        public static async Task EnsureTablesDeletedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            var commands = new List<String>();
            if (string.IsNullOrEmpty(connectionString))
            {
            
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                using (var connection = new SqliteConnection(connectionString))
                {
                    await EnsureTablesDeletedAsync(connection, cancellationToken);
                }
            }
            else
            {
                using (var connection = new Microsoft.Data.SqlClient.SqlConnection(connectionString))
                {
                    await EnsureTablesDeletedAsync(connection, cancellationToken);
                }
            }
        }

        public static async Task<bool> EnsureTablesDeletedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
        {
            var commands = new List<String>();
            if (existingConnection is SqliteConnection)
            {
                bool dbExists = await DbInitializer.ExistsAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                    var opened = false;
                    if (existingConnection.State != System.Data.ConnectionState.Open)
                    {
                        await existingConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        opened = true;
                    }

                    var deleteTables = Tables.Where(x => persistedTables.Any(p => (p.TableName == x.TableName || p.TableName == $"{x.Schema}.{x.TableName}") && (p.Schema == x.Schema || string.IsNullOrEmpty(p.Schema))));

                    //Drop tables
                    foreach (var tableName in deleteTables)
                    {
                        var commandSql = $"DROP TABLE [{tableName.TableName}];";
                        commands.Add(commandSql);
                    }

                    bool deleted = false;
                    try
                    {
                        using (SqliteTransaction transaction = ((SqliteConnection)existingConnection).BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                deleted = true;
                                using (var command = new SqliteCommand(commandSql, ((SqliteConnection)existingConnection), transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            transaction.Commit();
                        }
                    }
                    finally
                    {
                        if(opened && existingConnection.State == System.Data.ConnectionState.Open)
                        {
                            existingConnection.Close();
                        }
                    }

                    return deleted;
                }
                else
                {
                    return await EnsureDbDestroyedAsync(existingConnection, cancellationToken).ConfigureAwait(false);
                }
            }
            else if (existingConnection is Microsoft.Data.SqlClient.SqlConnection || existingConnection is System.Data.SqlClient.SqlConnection)
            {
                bool dbExists = await DbInitializer.ExistsAsync(existingConnection, cancellationToken);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TablesAsync(existingConnection, cancellationToken).ConfigureAwait(false);

                    var opened = false;
                    if (existingConnection.State != System.Data.ConnectionState.Open)
                    {
                        await existingConnection.OpenAsync(cancellationToken).ConfigureAwait(false);
                        opened = true;
                    }

                    var deleteTables = Tables.Where(x => persistedTables.Any(p => p.TableName == x.TableName && p.Schema == x.Schema));

                    //Drop tables
                    foreach (var tableName in deleteTables)
                    {
                        var commandSql = $"DROP TABLE [{tableName.Schema}].[{tableName.TableName}]";

                        commands.Add(commandSql);
                    }

                    bool deleted = false;
                    try
                    {
                        if (existingConnection is Microsoft.Data.SqlClient.SqlConnection sqlConnection)
                        {
                            using (Microsoft.Data.SqlClient.SqlTransaction transaction = sqlConnection.BeginTransaction())
                            {
                                foreach (var commandSql in commands)
                                {
                                    deleted = true;
                                    using (var command = new Microsoft.Data.SqlClient.SqlCommand(commandSql, sqlConnection, transaction))
                                    {
                                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                    }
                                }

                                transaction.Commit();
                            }
                        }
                        else if (existingConnection is System.Data.SqlClient.SqlConnection systemSqlConnection)
                        {
                            using (System.Data.SqlClient.SqlTransaction transaction = systemSqlConnection.BeginTransaction())
                            {
                                foreach (var commandSql in commands)
                                {
                                    deleted = true;
                                    using (var command = new System.Data.SqlClient.SqlCommand(commandSql, systemSqlConnection, transaction))
                                    {
                                        await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                    }
                                }

                                transaction.Commit();
                            }
                        }
                    }
                    finally
                    {
                        if (opened && existingConnection.State == System.Data.ConnectionState.Open)
                        {
                            existingConnection.Close();
                        }
                    }

                    return deleted;
                }
                else
                {
                   return await EnsureDbDestroyedAsync(existingConnection, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                throw new Exception("Unsupported Connection");
            }
        }
        #endregion

        #region Ensure Db Destroyed
        public static Task<bool> EnsureDbDestroyedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken);
        }

        public static Task<bool> EnsureDbDestroyedAsync(DbConnection existingConnection, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureDestroyedAsync(existingConnection, cancellationToken);
        }
        #endregion
    }
}
