using Sitecore.Diagnostics;
using Sitecore.SessionProvider;
using System;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.InteropServices;
using System.Web.SessionState;

namespace Sitecore.SessionProvider.Sql
{
    internal sealed class SqlSessionStateStore
    {
        private readonly string m_ConnectionString;
        private readonly bool m_Compress;

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
                SqlParameter parameter1 = new SqlParameter();
                parameter1.ParameterName = "@name";
                parameter1.SqlDbType = SqlDbType.NVarChar;
                parameter1.Size = 280;
                parameter1.Value = name;
                SqlParameter parameter = parameter1;
                SqlParameter parameter3 = new SqlParameter();
                parameter3.ParameterName = "@id";
                parameter3.SqlDbType = SqlDbType.UniqueIdentifier;
                parameter3.Direction = ParameterDirection.Output;
                SqlParameter parameter2 = parameter3;
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
                SqlParameter parameter1 = new SqlParameter();
                parameter1.ParameterName = "@application";
                parameter1.SqlDbType = SqlDbType.UniqueIdentifier;
                parameter1.Value = application;
                SqlParameter parameter = parameter1;
                SqlParameter parameter6 = new SqlParameter();
                parameter6.ParameterName = "@id";
                parameter6.SqlDbType = SqlDbType.NVarChar;
                parameter6.Size = 0x58;
                parameter6.Direction = ParameterDirection.Output;
                SqlParameter parameter2 = parameter6;
                SqlParameter parameter7 = new SqlParameter();
                parameter7.ParameterName = "@lockTimestamp";
                parameter7.SqlDbType = SqlDbType.DateTime;
                parameter7.Value = lockCookie.Timestamp;
                SqlParameter parameter3 = parameter7;
                SqlParameter parameter8 = new SqlParameter();
                parameter8.ParameterName = "@lockCookie";
                parameter8.SqlDbType = SqlDbType.VarChar;
                parameter8.Size = 0x20;
                parameter8.Value = lockCookie.Id;
                SqlParameter parameter4 = parameter8;
                SqlParameter parameter9 = new SqlParameter();
                parameter9.ParameterName = "@result";
                parameter9.SqlDbType = SqlDbType.Int;
                parameter9.Direction = ParameterDirection.ReturnValue;
                SqlParameter parameter5 = parameter9;
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
                SqlParameter parameter1 = new SqlParameter();
                parameter1.ParameterName = "@application";
                parameter1.SqlDbType = SqlDbType.UniqueIdentifier;
                parameter1.Value = application;
                SqlParameter parameter = parameter1;
                SqlParameter parameter8 = new SqlParameter();
                parameter8.ParameterName = "@id";
                parameter8.SqlDbType = SqlDbType.NVarChar;
                parameter8.Size = 0x58;
                parameter8.Value = id;
                SqlParameter parameter2 = parameter8;
                SqlParameter parameter9 = new SqlParameter();
                parameter9.ParameterName = "@locked";
                parameter9.SqlDbType = SqlDbType.Bit;
                parameter9.Direction = ParameterDirection.Output;
                SqlParameter parameter3 = parameter9;
                SqlParameter parameter10 = new SqlParameter();
                parameter10.ParameterName = "@lockTimestamp";
                parameter10.SqlDbType = SqlDbType.DateTime;
                parameter10.Direction = ParameterDirection.Output;
                SqlParameter parameter4 = parameter10;
                SqlParameter parameter11 = new SqlParameter();
                parameter11.ParameterName = "@lockCookie";
                parameter11.SqlDbType = SqlDbType.VarChar;
                parameter11.Size = 0x20;
                parameter11.Direction = ParameterDirection.Output;
                SqlParameter parameter5 = parameter11;
                SqlParameter parameter12 = new SqlParameter();
                parameter12.ParameterName = "@flags";
                parameter12.SqlDbType = SqlDbType.Int;
                parameter12.Direction = ParameterDirection.Output;
                SqlParameter parameter6 = parameter12;
                SqlParameter parameter13 = new SqlParameter();
                parameter13.ParameterName = "@result";
                parameter13.SqlDbType = SqlDbType.Int;
                parameter13.Direction = ParameterDirection.ReturnValue;
                SqlParameter parameter7 = parameter13;
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
                if (num == 1)
                {
                    flags = (int)parameter6.Value;
                    string str = parameter5.Value.ToString();
                    DateTime timestamp = DateTime.SpecifyKind((DateTime)parameter4.Value, DateTimeKind.Utc);
                    if ((bool)parameter3.Value)
                    {
                        lockCookie = new SessionStateLockCookie(str, timestamp);
                    }
                    else
                    {
                        Assert.IsNotNull(buffer, "The session item was not returned from the database.");
                        data = SessionStateSerializer.Deserialize(buffer);
                    }
                }
            }
            return data;
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
                SqlParameter parameter1 = new SqlParameter();
                parameter1.ParameterName = "@application";
                parameter1.SqlDbType = SqlDbType.UniqueIdentifier;
                parameter1.Value = application;
                SqlParameter parameter = parameter1;
                SqlParameter parameter8 = new SqlParameter();
                parameter8.ParameterName = "@id";
                parameter8.SqlDbType = SqlDbType.NVarChar;
                parameter8.Size = 0x58;
                parameter8.Value = id;
                SqlParameter parameter2 = parameter8;
                SqlParameter parameter9 = new SqlParameter();
                parameter9.ParameterName = "@locked";
                parameter9.SqlDbType = SqlDbType.Bit;
                parameter9.Direction = ParameterDirection.Output;
                SqlParameter parameter3 = parameter9;
                SqlParameter parameter10 = new SqlParameter();
                parameter10.ParameterName = "@lockTimestamp";
                parameter10.SqlDbType = SqlDbType.DateTime;
                parameter10.Direction = ParameterDirection.Output;
                SqlParameter parameter4 = parameter10;
                SqlParameter parameter11 = new SqlParameter();
                parameter11.ParameterName = "@lockCookie";
                parameter11.SqlDbType = SqlDbType.VarChar;
                parameter11.Size = 0x20;
                parameter11.Direction = ParameterDirection.InputOutput;
                parameter11.Value = acquiredLockCookie.Id;
                SqlParameter parameter5 = parameter11;
                SqlParameter parameter12 = new SqlParameter();
                parameter12.ParameterName = "@flags";
                parameter12.SqlDbType = SqlDbType.Int;
                parameter12.Direction = ParameterDirection.Output;
                SqlParameter parameter6 = parameter12;
                SqlParameter parameter13 = new SqlParameter();
                parameter13.ParameterName = "@result";
                parameter13.SqlDbType = SqlDbType.Int;
                parameter13.Direction = ParameterDirection.ReturnValue;
                SqlParameter parameter7 = parameter13;
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
                if (num == 1)
                {
                    flags = (int)parameter6.Value;
                    if ((bool)parameter3.Value)
                    {
                        string str = (string)parameter5.Value;
                        existingLockCookie = new SessionStateLockCookie(str, DateTime.SpecifyKind((DateTime)parameter4.Value, DateTimeKind.Utc));
                    }
                    if (buffer != null)
                    {
                        data = SessionStateSerializer.Deserialize(buffer);
                    }
                }
            }
            return data;
        }

        internal void InsertItem(Guid application, string id, int flags, SessionStateStoreData sessionState)
        {
            try
            {
                byte[] buffer = SessionStateSerializer.Serialize(sessionState, this.m_Compress);
                using (SqlCommand command = new SqlCommand())
                {
                    command.CommandText = "[dbo].[InsertItem]";
                    command.CommandType = CommandType.StoredProcedure;
                    SqlParameter parameter1 = new SqlParameter();
                    parameter1.ParameterName = "@application";
                    parameter1.SqlDbType = SqlDbType.UniqueIdentifier;
                    parameter1.Value = application;
                    SqlParameter parameter = parameter1;
                    SqlParameter parameter6 = new SqlParameter();
                    parameter6.ParameterName = "@id";
                    parameter6.SqlDbType = SqlDbType.NVarChar;
                    parameter6.Size = 0x58;
                    parameter6.Value = id;
                    SqlParameter parameter2 = parameter6;
                    SqlParameter parameter7 = new SqlParameter();
                    parameter7.ParameterName = "@item";
                    parameter7.SqlDbType = SqlDbType.Image;
                    parameter7.Value = buffer;
                    SqlParameter parameter3 = parameter7;
                    SqlParameter parameter8 = new SqlParameter();
                    parameter8.ParameterName = "@timeout";
                    parameter8.SqlDbType = SqlDbType.Int;
                    parameter8.Value = sessionState.Timeout;
                    SqlParameter parameter4 = parameter8;
                    SqlParameter parameter9 = new SqlParameter();
                    parameter9.ParameterName = "@flags";
                    parameter9.SqlDbType = SqlDbType.Int;
                    parameter9.Value = flags;
                    SqlParameter parameter5 = parameter9;
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
            catch (SqlException sqlException)
            {
                if (sqlException.Number == 2601 || sqlException.Number == 2627)
                {
                    Log.Debug("Attempting to insert a duplicate key into the SQL Session Store. Entry skipped. " + sqlException.Message);
                }
                else
                {
                    Log.Error(sqlException.Message, sqlException);
                }
            }
            catch (Exception ex)
            {
                Log.Error("Sitecore.Support.96296: " + ex, this);
            }
        }

        internal void ReleaseItem(Guid application, string id, string lockCookie)
        {
            using (SqlCommand command = new SqlCommand())
            {
                command.CommandText = "[dbo].[ReleaseItem]";
                command.CommandType = CommandType.StoredProcedure;
                SqlParameter parameter1 = new SqlParameter();
                parameter1.ParameterName = "@application";
                parameter1.SqlDbType = SqlDbType.UniqueIdentifier;
                parameter1.Value = application;
                SqlParameter parameter = parameter1;
                SqlParameter parameter5 = new SqlParameter();
                parameter5.ParameterName = "@id";
                parameter5.SqlDbType = SqlDbType.NVarChar;
                parameter5.Size = 0x58;
                parameter5.Value = id;
                SqlParameter parameter2 = parameter5;
                SqlParameter parameter6 = new SqlParameter();
                parameter6.ParameterName = "@lockCookie";
                parameter6.SqlDbType = SqlDbType.VarChar;
                parameter6.Size = 0x20;
                parameter6.Value = lockCookie;
                SqlParameter parameter3 = parameter6;
                SqlParameter parameter7 = new SqlParameter();
                parameter7.ParameterName = "@result";
                parameter7.SqlDbType = SqlDbType.Int;
                parameter7.Direction = ParameterDirection.ReturnValue;
                SqlParameter parameter4 = parameter7;
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
                SqlParameter parameter1 = new SqlParameter();
                parameter1.ParameterName = "@application";
                parameter1.SqlDbType = SqlDbType.UniqueIdentifier;
                parameter1.Value = application;
                SqlParameter parameter = parameter1;
                SqlParameter parameter5 = new SqlParameter();
                parameter5.ParameterName = "@id";
                parameter5.SqlDbType = SqlDbType.NVarChar;
                parameter5.Size = 0x58;
                parameter5.Value = id;
                SqlParameter parameter2 = parameter5;
                SqlParameter parameter6 = new SqlParameter();
                parameter6.ParameterName = "@lockCookie";
                parameter6.SqlDbType = SqlDbType.VarChar;
                parameter6.Size = 0x20;
                parameter6.Value = lockCookie;
                SqlParameter parameter3 = parameter6;
                SqlParameter parameter7 = new SqlParameter();
                parameter7.ParameterName = "@result";
                parameter7.SqlDbType = SqlDbType.Int;
                parameter7.Direction = ParameterDirection.ReturnValue;
                SqlParameter parameter4 = parameter7;
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
                SqlParameter parameter1 = new SqlParameter();
                parameter1.ParameterName = "@application";
                parameter1.SqlDbType = SqlDbType.UniqueIdentifier;
                parameter1.Value = application;
                SqlParameter parameter = parameter1;
                SqlParameter parameter8 = new SqlParameter();
                parameter8.ParameterName = "@id";
                parameter8.SqlDbType = SqlDbType.NVarChar;
                parameter8.Size = 0x58;
                parameter8.Value = id;
                SqlParameter parameter2 = parameter8;
                SqlParameter parameter9 = new SqlParameter();
                parameter9.ParameterName = "@lockCookie";
                parameter9.SqlDbType = SqlDbType.VarChar;
                parameter9.Size = 0x20;
                parameter9.Value = lockCookie;
                SqlParameter parameter3 = parameter9;
                SqlParameter parameter10 = new SqlParameter();
                parameter10.ParameterName = "@flags";
                parameter10.SqlDbType = SqlDbType.Int;
                parameter10.Value = action;
                SqlParameter parameter4 = parameter10;
                SqlParameter parameter11 = new SqlParameter();
                parameter11.ParameterName = "@timeout";
                parameter11.SqlDbType = SqlDbType.Int;
                parameter11.Value = sessionState.Timeout;
                SqlParameter parameter5 = parameter11;
                SqlParameter parameter12 = new SqlParameter();
                parameter12.ParameterName = "@item";
                parameter12.SqlDbType = SqlDbType.Image;
                SqlParameter parameter6 = parameter12;
                SqlParameter parameter13 = new SqlParameter();
                parameter13.ParameterName = "@result";
                parameter13.SqlDbType = SqlDbType.Int;
                parameter13.Direction = ParameterDirection.ReturnValue;
                SqlParameter parameter7 = parameter13;
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
                SqlParameter parameter1 = new SqlParameter();
                parameter1.ParameterName = "@application";
                parameter1.SqlDbType = SqlDbType.UniqueIdentifier;
                parameter1.Value = application;
                SqlParameter parameter = parameter1;
                SqlParameter parameter3 = new SqlParameter();
                parameter3.ParameterName = "@id";
                parameter3.SqlDbType = SqlDbType.NVarChar;
                parameter3.Size = 0x58;
                parameter3.Value = id;
                SqlParameter parameter2 = parameter3;
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
