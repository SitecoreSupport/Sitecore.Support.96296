namespace Sitecore.Support.SessionProvider.Sql
{
    using Sitecore.Diagnostics;
    using Sitecore.SessionProvider;
    using System;
    using System.Data;
    using System.Data.SqlClient;
    using System.Runtime.InteropServices;
    using System.Web.SessionState;

    internal sealed class SqlSessionStateStore
    {
        private readonly bool m_Compress;
        private readonly string m_ConnectionString;

        internal SqlSessionStateStore(string connectionString, bool compress)
        {
            this.m_ConnectionString = connectionString;
            this.m_Compress = compress;
        }

        internal Guid GetApplicationIdentifier(string name)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[GetApplicationId]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@name",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 280,
                    Value = name
                };
                SqlParameter parameter2 = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.UniqueIdentifier,
                    Direction = ParameterDirection.Output
                };
                command.Parameters.Add(parameter);
                command.Parameters.Add(parameter2);
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                }
                return (Guid)parameter2.Value;
            }
        }

        internal SessionStateStoreData GetExpiredItemExclusive(Guid application, SessionStateLockCookie lockCookie, out string id)
        {
            id = null;
            SessionStateStoreData data = null;
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[GetExpiredItemExclusive]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@application",
                    SqlDbType = SqlDbType.UniqueIdentifier,
                    Value = application
                };
                SqlParameter parameter2 = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 0x58,
                    Direction = ParameterDirection.Output
                };
                SqlParameter parameter3 = new SqlParameter
                {
                    ParameterName = "@lockTimestamp",
                    SqlDbType = SqlDbType.DateTime,
                    Value = lockCookie.Timestamp
                };
                SqlParameter parameter4 = new SqlParameter
                {
                    ParameterName = "@lockCookie",
                    SqlDbType = SqlDbType.VarChar,
                    Size = 0x20,
                    Value = lockCookie.Id
                };
                SqlParameter parameter5 = new SqlParameter
                {
                    ParameterName = "@result",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(parameter);
                command.Parameters.Add(parameter2);
                command.Parameters.Add(parameter3);
                command.Parameters.Add(parameter4);
                command.Parameters.Add(parameter5);
                int num = 0;
                byte[] buffer = null;
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            buffer = (byte[])reader[0];
                        }
                    }
                    num = (int)parameter5.Value;
                }
                if (num == 1)
                {
                    id = (string)parameter2.Value;
                    data = SessionStateSerializer.Deserialize(buffer);
                }
            }
            return data;
        }

        internal SessionStateStoreData GetItem(Guid application, string id, out SessionStateLockCookie lockCookie, out int flags)
        {
            lockCookie = null;
            flags = 0;
            SessionStateStoreData data = null;
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[GetItem]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@application",
                    SqlDbType = SqlDbType.UniqueIdentifier,
                    Value = application
                };
                SqlParameter parameter2 = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 0x58,
                    Value = id
                };
                SqlParameter parameter3 = new SqlParameter
                {
                    ParameterName = "@locked",
                    SqlDbType = SqlDbType.Bit,
                    Direction = ParameterDirection.Output
                };
                SqlParameter parameter4 = new SqlParameter
                {
                    ParameterName = "@lockTimestamp",
                    SqlDbType = SqlDbType.DateTime,
                    Direction = ParameterDirection.Output
                };
                SqlParameter parameter5 = new SqlParameter
                {
                    ParameterName = "@lockCookie",
                    SqlDbType = SqlDbType.VarChar,
                    Size = 0x20,
                    Direction = ParameterDirection.Output
                };
                SqlParameter parameter6 = new SqlParameter
                {
                    ParameterName = "@flags",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };
                SqlParameter parameter7 = new SqlParameter
                {
                    ParameterName = "@result",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(parameter);
                command.Parameters.Add(parameter2);
                command.Parameters.Add(parameter3);
                command.Parameters.Add(parameter4);
                command.Parameters.Add(parameter5);
                command.Parameters.Add(parameter6);
                command.Parameters.Add(parameter7);
                int num = 0;
                byte[] buffer = null;
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            buffer = (byte[])reader[0];
                        }
                    }
                    num = (int)parameter7.Value;
                }
                if (num != 1)
                {
                    return data;
                }
                flags = (int)parameter6.Value;
                string str = parameter5.Value.ToString();
                DateTime time = (DateTime)parameter4.Value;
                time = DateTime.SpecifyKind(time, DateTimeKind.Utc);
                if ((bool)parameter3.Value)
                {
                    lockCookie = new SessionStateLockCookie(str, time);
                    return data;
                }
                Assert.IsNotNull(buffer, "The session item was not returned from the database.");
                return SessionStateSerializer.Deserialize(buffer);
            }
        }

        internal SessionStateStoreData GetItemExclusive(Guid application, string id, SessionStateLockCookie acquiredLockCookie, out SessionStateLockCookie existingLockCookie, out int flags)
        {
            flags = 0;
            existingLockCookie = null;
            SessionStateStoreData data = null;
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[GetItemExclusive]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@application",
                    SqlDbType = SqlDbType.UniqueIdentifier,
                    Value = application
                };
                SqlParameter parameter2 = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 0x58,
                    Value = id
                };
                SqlParameter parameter3 = new SqlParameter
                {
                    ParameterName = "@locked",
                    SqlDbType = SqlDbType.Bit,
                    Direction = ParameterDirection.Output
                };
                SqlParameter parameter4 = new SqlParameter
                {
                    ParameterName = "@lockTimestamp",
                    SqlDbType = SqlDbType.DateTime,
                    Direction = ParameterDirection.Output
                };
                SqlParameter parameter5 = new SqlParameter
                {
                    ParameterName = "@lockCookie",
                    SqlDbType = SqlDbType.VarChar,
                    Size = 0x20,
                    Direction = ParameterDirection.InputOutput,
                    Value = acquiredLockCookie.Id
                };
                SqlParameter parameter6 = new SqlParameter
                {
                    ParameterName = "@flags",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.Output
                };
                SqlParameter parameter7 = new SqlParameter
                {
                    ParameterName = "@result",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(parameter);
                command.Parameters.Add(parameter2);
                command.Parameters.Add(parameter3);
                command.Parameters.Add(parameter4);
                command.Parameters.Add(parameter5);
                command.Parameters.Add(parameter6);
                command.Parameters.Add(parameter7);
                int num = 0;
                byte[] buffer = null;
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            buffer = (byte[])reader[0];
                        }
                    }
                    num = (int)parameter7.Value;
                }
                if (num != 1)
                {
                    return data;
                }
                if ((bool)parameter3.Value)
                {
                    string str = (string)parameter5.Value;
                    DateTime time = (DateTime)parameter4.Value;
                    time = DateTime.SpecifyKind(time, DateTimeKind.Utc);
                    existingLockCookie = new SessionStateLockCookie(str, time);
                }
                if (buffer != null)
                {
                    data = SessionStateSerializer.Deserialize(buffer);
                }
            }
            return data;
        }

        internal void InsertItem(Guid application, string id, int flags, SessionStateStoreData sessionState)
        {
            try
            {
                if (this.IsItemExist(id))
                {
                    Log.Debug("Attempting to insert a duplicate key into the SQL Session Store. Entry skipped.");
                }
                byte[] buffer = SessionStateSerializer.Serialize(sessionState, this.m_Compress);
                using (SqlCommand command = new SqlCommand())
                {
                    command.CommandText = "[dbo].[InsertItem]";
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter parameter = new SqlParameter
                    {
                        ParameterName = "@application",
                        SqlDbType = SqlDbType.UniqueIdentifier,
                        Value = application
                    };
                    SqlParameter parameter2 = new SqlParameter
                    {
                        ParameterName = "@id",
                        SqlDbType = SqlDbType.NVarChar,
                        Size = 0x58,
                        Value = id
                    };
                    SqlParameter parameter3 = new SqlParameter
                    {
                        ParameterName = "@item",
                        SqlDbType = SqlDbType.Image,
                        Value = buffer
                    };
                    SqlParameter parameter4 = new SqlParameter
                    {
                        ParameterName = "@timeout",
                        SqlDbType = SqlDbType.Int,
                        Value = sessionState.Timeout
                    };
                    SqlParameter parameter5 = new SqlParameter
                    {
                        ParameterName = "@flags",
                        SqlDbType = SqlDbType.Int,
                        Value = flags
                    };
                    command.Parameters.Add(parameter);
                    command.Parameters.Add(parameter2);
                    command.Parameters.Add(parameter3);
                    command.Parameters.Add(parameter4);
                    command.Parameters.Add(parameter5);
                    using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                    {
                        connection.Open();
                        command.Connection = connection;
                        command.ExecuteNonQuery();
                    }
                }
            }
            catch (Exception exception)
            {
                Log.Error("Sitecore.Support.439438#Error occured: " + exception, this);
            }
        }

        private bool IsItemExist(string id)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "SELECT [id] FROM [dbo].[SessionState] WHERE [id]=@id";
                command.CommandType = CommandType.Text;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 0x58,
                    Value = id
                };
                command.Parameters.Add(parameter);
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    using (SqlDataReader reader = command.ExecuteReader(CommandBehavior.SingleRow))
                    {
                        if (reader.Read() && !reader.IsDBNull(0))
                        {
                            return true;
                        }
                    }
                }
            }
            return false;
        }

        internal void ReleaseItem(Guid application, string id, string lockCookie)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[ReleaseItem]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@application",
                    SqlDbType = SqlDbType.UniqueIdentifier,
                    Value = application
                };
                SqlParameter parameter2 = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 0x58,
                    Value = id
                };
                SqlParameter parameter3 = new SqlParameter
                {
                    ParameterName = "@lockCookie",
                    SqlDbType = SqlDbType.VarChar,
                    Size = 0x20,
                    Value = lockCookie
                };
                SqlParameter parameter4 = new SqlParameter
                {
                    ParameterName = "@result",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(parameter);
                command.Parameters.Add(parameter2);
                command.Parameters.Add(parameter3);
                command.Parameters.Add(parameter4);
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    int num1 = (int)parameter4.Value;
                }
            }
        }

        internal void RemoveItem(Guid application, string id, string lockCookie)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[RemoveItem]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@application",
                    SqlDbType = SqlDbType.UniqueIdentifier,
                    Value = application
                };
                SqlParameter parameter2 = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 0x58,
                    Value = id
                };
                SqlParameter parameter3 = new SqlParameter
                {
                    ParameterName = "@lockCookie",
                    SqlDbType = SqlDbType.VarChar,
                    Size = 0x20,
                    Value = lockCookie
                };
                SqlParameter parameter4 = new SqlParameter
                {
                    ParameterName = "@result",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.ReturnValue
                };
                command.Parameters.Add(parameter);
                command.Parameters.Add(parameter2);
                command.Parameters.Add(parameter3);
                command.Parameters.Add(parameter4);
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    int num1 = (int)parameter4.Value;
                }
            }
        }

        internal void UpdateAndReleaseItem(Guid application, string id, string lockCookie, SessionStateActions action, SessionStateStoreData sessionState)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[SetAndReleaseItem]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@application",
                    SqlDbType = SqlDbType.UniqueIdentifier,
                    Value = application
                };
                SqlParameter parameter2 = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 0x58,
                    Value = id
                };
                SqlParameter parameter3 = new SqlParameter
                {
                    ParameterName = "@lockCookie",
                    SqlDbType = SqlDbType.VarChar,
                    Size = 0x20,
                    Value = lockCookie
                };
                SqlParameter parameter4 = new SqlParameter
                {
                    ParameterName = "@flags",
                    SqlDbType = SqlDbType.Int,
                    Value = action
                };
                SqlParameter parameter5 = new SqlParameter
                {
                    ParameterName = "@timeout",
                    SqlDbType = SqlDbType.Int,
                    Value = sessionState.Timeout
                };
                SqlParameter parameter6 = new SqlParameter
                {
                    ParameterName = "@item",
                    SqlDbType = SqlDbType.Image
                };
                SqlParameter parameter7 = new SqlParameter
                {
                    ParameterName = "@result",
                    SqlDbType = SqlDbType.Int,
                    Direction = ParameterDirection.ReturnValue
                };
                parameter6.Value = SessionStateSerializer.Serialize(sessionState, this.m_Compress);
                command.Parameters.Add(parameter);
                command.Parameters.Add(parameter2);
                command.Parameters.Add(parameter3);
                command.Parameters.Add(parameter4);
                command.Parameters.Add(parameter5);
                command.Parameters.Add(parameter6);
                command.Parameters.Add(parameter7);
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                    int num1 = (int)parameter7.Value;
                }
            }
        }

        internal void UpdateItemExpiration(Guid application, string id)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[ResetItemTimeout]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter = new SqlParameter
                {
                    ParameterName = "@application",
                    SqlDbType = SqlDbType.UniqueIdentifier,
                    Value = application
                };
                SqlParameter parameter2 = new SqlParameter
                {
                    ParameterName = "@id",
                    SqlDbType = SqlDbType.NVarChar,
                    Size = 0x58,
                    Value = id
                };
                command.Parameters.Add(parameter);
                command.Parameters.Add(parameter2);
                using (SqlConnection connection = new SqlConnection(this.m_ConnectionString))
                {
                    connection.Open();
                    command.Connection = connection;
                    command.ExecuteNonQuery();
                }
            }
        }
    }
}
