namespace Sitecore.Support.SessionProvider.Sql
{
    using Sitecore.Diagnostics;
    using Sitecore.SessionProvider;
    using Sitecore.SessionProvider.Helpers;
    using System;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Diagnostics;
    using System.Runtime.InteropServices;
    using System.Web;
    using System.Web.SessionState;

    public class SqlSessionStateProvider : SitecoreSessionStateStoreProvider
    {
        private Guid m_ApplicationId;
        private Sitecore.Support.SessionProvider.Sql.SqlSessionStateStore m_Store;

        static SqlSessionStateProvider()
        {
            Trace.WriteLine("SQL Session State Provider is initializing.", "SqlSessionStateProvider");
        }

        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            HttpStaticObjectsCollection staticObjects = new HttpStaticObjectsCollection();
            SessionStateStoreData sessionState = new SessionStateStoreData(new SessionStateItemCollection(), staticObjects, timeout);
            this.m_Store.InsertItem(this.ApplicationId, id, 1, sessionState);
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
            SessionStateStoreData data = this.Store.GetItemExclusive(this.ApplicationId, id, acquiredLockCookie, out existingLockCookie, out flags);
            if (existingLockCookie != null)
            {
                locked = true;
                lockAge = (TimeSpan)(DateTime.UtcNow - existingLockCookie.Timestamp);
                lockId = existingLockCookie.Id;
                return data;
            }
            locked = false;
            lockId = acquiredLockCookie.Id;
            actions = (SessionStateActions)flags;
            return data;
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
            bool @bool = reader1.GetBool("compression", false);
            this.m_Store = new Sitecore.Support.SessionProvider.Sql.SqlSessionStateStore(connectionString, @bool);
            this.m_ApplicationId = this.m_Store.GetApplicationIdentifier(str);
        }

        public override void ReleaseItemExclusive(HttpContext context, string id, object lockId)
        {
            Sitecore.Diagnostics.Assert.ArgumentNotNull(context, "context");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(id, "id");
            Sitecore.Diagnostics.Assert.ArgumentNotNull(lockId, "lockId");
            string lockCookie = Convert.ToString(lockId);
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
            string lockCookie = Convert.ToString(lockId);
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
                string lockCookie = Convert.ToString(lockId);
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

        private Sitecore.Support.SessionProvider.Sql.SqlSessionStateStore Store
        {
            get
            {
                return this.m_Store;
            }
        }
    }
}
