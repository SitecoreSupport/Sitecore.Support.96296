using Sitecore.Diagnostics;
using Sitecore.SessionProvider;
using Sitecore.SessionProvider.Helpers;
using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Web;
using System.Web.SessionState;

namespace Sitecore.SessionProvider.Sql
{
    public class SqlSessionStateProvider : SitecoreSessionStateStoreProvider
    {
        private Guid m_ApplicationId;
        private Sitecore.SessionProvider.Sql.SqlSessionStateStore m_Store;
        private static readonly List<SqlSessionStateProvider> SqlSessionStateProvidersList = new List<SqlSessionStateProvider>();
        private static readonly object ListSyncRoot = new object();

        static SqlSessionStateProvider()
        {
            Trace.WriteLine("SQL Session State Provider is initializing.", "SqlSessionStateProvider");
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            SessionStateStoreData sessionState = new SessionStateStoreData(new SessionStateItemCollection(), new HttpStaticObjectsCollection(), timeout);
            this.m_Store.InsertItem(this.ApplicationId, id, 1, sessionState);
        }

        public override void Dispose()
        {
            object listSyncRoot = ListSyncRoot;
            lock (listSyncRoot)
            {
                SqlSessionStateProvidersList.Remove(this);
                if (base.TimerEnabled && (SqlSessionStateProvidersList.Count > 0))
                {
                    foreach (SqlSessionStateProvider provider in SqlSessionStateProvidersList)
                    {
                        if ((provider.ApplicationId == this.ApplicationId) && provider.TriedToStartTimer)
                        {
                            provider.StartTimer();
                            break;
                        }
                    }
                }
            }
            base.Dispose();
        }

        protected override SessionStateStoreData GetExpiredItemExclusive(DateTime signalTime, SessionStateLockCookie lockCookie, out string id)
        {
            return this.Store.GetExpiredItemExclusive(this.ApplicationId, lockCookie, out id);
        }

        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            locked = false;
            lockAge = TimeSpan.Zero;
            lockId = null;
            actions = SessionStateActions.None;
            int flags = 0;
            SessionStateLockCookie lockCookie = null;
            actions = (SessionStateActions)flags;
            if (lockCookie != null)
            {
                locked = true;
                lockId = lockCookie.Id;
                lockAge = (TimeSpan)(DateTime.UtcNow - lockCookie.Timestamp);
            }
            return this.Store.GetItem(this.ApplicationId, id, out lockCookie, out flags);
        }

        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            lockAge = TimeSpan.Zero;
            actions = SessionStateActions.None;
            int flags = 0;
            SessionStateLockCookie existingLockCookie = null;
            SessionStateLockCookie acquiredLockCookie = SessionStateLockCookie.Generate(DateTime.UtcNow);
            if (existingLockCookie != null)
            {
                locked = true;
                lockAge = (TimeSpan)(DateTime.UtcNow - existingLockCookie.Timestamp);
                lockId = existingLockCookie.Id;
                return this.Store.GetItemExclusive(this.ApplicationId, id, acquiredLockCookie, out existingLockCookie, out flags);
            }
            locked = false;
            lockId = acquiredLockCookie.Id;
            actions = (SessionStateActions)flags;
            return this.Store.GetItemExclusive(this.ApplicationId, id, acquiredLockCookie, out existingLockCookie, out flags);
        }

        public override void Initialize(string name, NameValueCollection config)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(name, "name");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(config, "config");
            base.Initialize(name, config);
            ConfigReader reader1 = new ConfigReader(config, name);
            string str = reader1.GetString("sessionType", true);
            string str2 = reader1.GetString("connectionStringName", false);
            string connectionString = ConfigurationManager.ConnectionStrings[str2].ConnectionString;
            this.m_Store = new Sitecore.SessionProvider.Sql.SqlSessionStateStore(connectionString, reader1.GetBool("compression", false));
            this.m_ApplicationId = this.m_Store.GetApplicationIdentifier(str);
            object listSyncRoot = ListSyncRoot;
            lock (listSyncRoot)
            {
                SqlSessionStateProvidersList.Add(this);
            }
            base.CanStartTimer = new Func<bool>(this.IsTimerOffForAllInstance);
        }

        private bool IsTimerOffForAllInstance()
        {
            bool flag2;
            object listSyncRoot = ListSyncRoot;
            lock (listSyncRoot)
            {
                using (List<SqlSessionStateProvider>.Enumerator enumerator = SqlSessionStateProvidersList.GetEnumerator())
                {
                    while (true)
                    {
                        if (!enumerator.MoveNext())
                        {
                            break;
                        }
                        SqlSessionStateProvider current = enumerator.Current;
                        if ((current.ApplicationId == this.ApplicationId) && (current.TriedToStartTimer && current.TimerEnabled))
                        {
                            flag2 = false;
                            goto TR_0000;
                        }
                    }
                }
                flag2 = true;
                TR_0000:;
            }
            return flag2;
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(lockId, "lockId");
            string lockCookie = System.Convert.ToString(lockId);
            this.Store.ReleaseItem(this.ApplicationId, id, lockCookie);
        }

        protected override void RemoveItem(string id, string lockCookie)
        {
            this.Store.RemoveItem(this.ApplicationId, id, lockCookie);
        }

        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(lockId, "lockId");
            string lockCookie = System.Convert.ToString(lockId);
            try
            {
                base.ExecuteSessionEnd(id, item);
            }
            finally
            {
                this.Store.RemoveItem(this.ApplicationId, id, lockCookie);
            }
        }

        public override void ResetItemTimeout(HttpContext context, string id)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            this.Store.UpdateItemExpiration(this.ApplicationId, id);
        }

        public override void SetAndReleaseItemExclusive(HttpContext context, string id, SessionStateStoreData sessionState, object lockId, bool newItem)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            if (newItem)
            {
                this.Store.InsertItem(this.ApplicationId, id, 0, sessionState);
            }
            else
            {
                string lockCookie = System.Convert.ToString(lockId);
                this.Store.UpdateAndReleaseItem(this.ApplicationId, id, lockCookie, SessionStateActions.None, sessionState);
            }
        }

        private Guid ApplicationId
        {
            get
            {
                return this.m_ApplicationId;
            }
        }

        private Sitecore.SessionProvider.Sql.SqlSessionStateStore Store
        {
            get
            {
                return this.m_Store;
            }
        }
    }
}
