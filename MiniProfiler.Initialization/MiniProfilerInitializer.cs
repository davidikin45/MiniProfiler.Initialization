using Database.Initialization;
using Microsoft.Data.Sqlite;
using StackExchange.Profiling.Storage;
using System;
using System.Collections.Generic;
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

        public static async Task EnsureTablesDeletedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            var commands = new List<String>();
            if (string.IsNullOrEmpty(connectionString))
            {

            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                bool dbExists = await DbInitializer.ExistsAsync(connectionString, cancellationToken).ConfigureAwait(false);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TablesAsync(connectionString, cancellationToken).ConfigureAwait(false);

                    using (var conn = new SqliteConnection(connectionString))
                    {
                        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (SqliteTransaction transaction = conn.BeginTransaction())
                        {
                            var deleteTables = Tables.Where(x => persistedTables.Any(p => (p.TableName == x.TableName || p.TableName == $"{x.Schema}.{x.TableName}") && (p.Schema == x.Schema || string.IsNullOrEmpty(p.Schema))));

                            //Drop tables
                            foreach (var tableName in deleteTables)
                            {
                                foreach (var t in deleteTables)
                                {
                                    try
                                    {
                                        var commandSql = $"DROP TABLE [{t.TableName}];";
                                        using (var command = new SqliteCommand(commandSql, conn, transaction))
                                        {
                                            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                        }

                                        commands.Add(commandSql);
                                    }
                                    catch
                                    {

                                    }
                                }
                            }

                            transaction.Rollback();
                        }

                        using (SqliteTransaction transaction = conn.BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                using (var command = new SqliteCommand(commandSql, conn, transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            transaction.Commit();
                        }
                    }
                }
                else
                {
                    await DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken).ConfigureAwait(false);
                }
            }
            else
            {
                bool dbExists = await DbInitializer.ExistsAsync(connectionString, cancellationToken);

                if (dbExists)
                {
                    var persistedTables = await DbInitializer.TablesAsync(connectionString, cancellationToken).ConfigureAwait(false);

                    using (var conn = new SqlConnection(connectionString))
                    {
                        await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                        using (SqlTransaction transaction = conn.BeginTransaction())
                        {
                            var deleteTables = Tables.Where(x => persistedTables.Any(p => p.TableName == x.TableName && p.Schema == x.Schema));

                            //Drop tables
                            foreach (var tableName in deleteTables)
                            {
                                foreach (var t in deleteTables)
                                {
                                    try
                                    {
                                        var commandSql = $"DROP TABLE [{t.Schema}].[{t.TableName}]";
                                        using (var command = new SqlCommand(commandSql, conn, transaction))
                                        {
                                            await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                        }

                                        commands.Add(commandSql);
                                    }
                                    catch
                                    {

                                    }
                                }
                            }

                            transaction.Rollback();
                        }

                        using (SqlTransaction transaction = conn.BeginTransaction())
                        {
                            foreach (var commandSql in commands)
                            {
                                using (var command = new SqlCommand(commandSql, conn, transaction))
                                {
                                    await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                                }
                            }

                            transaction.Commit();
                        }
                    }
                }
                else
                {
                    await DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        public static Task<bool> EnsureDbCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureCreatedAsync(connectionString, cancellationToken);
        }

        public static async Task<bool> EnsureDbAndTablesCreatedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            await EnsureDbCreatedAsync(connectionString, cancellationToken).ConfigureAwait(false);
            if (string.IsNullOrEmpty(connectionString))
            {
                return false;
            }
            else if (ConnectionStringHelper.IsSQLite(connectionString))
            {
                var persistedTables = await DbInitializer.TablesAsync(connectionString, cancellationToken).ConfigureAwait(false);

                var storage = new SqliteStorage("");
                var sqlScripts = storage.TableCreationScripts;
                bool created = false;

                using (var conn = new SqliteConnection(connectionString))
                {
                    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                    using (SqliteTransaction transaction = conn.BeginTransaction())
                    {
                        foreach (var commandSql in sqlScripts.Where(sqlScript => !persistedTables.Any(table => sqlScript.Contains(table.TableName))))
                        {
                            using (var command = new SqliteCommand(commandSql, conn, transaction))
                            {
                                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                            }
                            created = true;
                        }

                        transaction.Commit();
                    }
                }

                return created;
            }
            else
            {
                var persistedTables = await DbInitializer.TablesAsync(connectionString, cancellationToken).ConfigureAwait(false);

                var storage = new SqlServerStorage("");
                var sqlScripts = storage.TableCreationScripts;
                bool created = false;

                //Initialize Schema
                using (var conn = new SqlConnection(connectionString))
                {
                    await conn.OpenAsync(cancellationToken).ConfigureAwait(false);

                    using (SqlTransaction transaction = conn.BeginTransaction())
                    {
                        foreach (var commandSql in sqlScripts.Where(sqlScript => !persistedTables.Any(table => sqlScript.Contains(table.TableName))))
                        {
                            using (var command = new SqlCommand(commandSql, conn, transaction))
                            {
                                await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
                            }
                            created = true;
                        }

                        transaction.Commit();
                    }
                }

                return created;
            }
        }

        public static Task<bool> EnsureDbDestroyedAsync(string connectionString, CancellationToken cancellationToken = default)
        {
            return DbInitializer.EnsureDestroyedAsync(connectionString, cancellationToken);
        }
    }
}
