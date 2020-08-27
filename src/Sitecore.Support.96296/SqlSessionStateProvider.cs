namespace Sitecore.Support.SessionProvider.Sql
{
    using System;
    using System.Collections.Generic;
    using System.Collections.Specialized;
    using System.Configuration;
    using System.Diagnostics;
    using System.Linq;
    using System.Web;
    using System.Web.Configuration;
    using System.Web.SessionState;
    using Sitecore.Diagnostics;
    using Sitecore.SessionProvider.Helpers;
    using Sitecore.SessionProvider;

    public class SqlSessionStateProvider : SitecoreSessionStateStoreProvider
    {
        #region Fields

        /// <summary>
        /// The m_ application id.
        /// </summary>
        private Guid m_ApplicationId;

        /// <summary>
        /// The m_ store.
        /// </summary>
        private SqlSessionStateStore m_Store;

        /// <summary>
        /// List to store all the instances of this class
        /// </summary>
        private static readonly List<SqlSessionStateProvider> SqlSessionStateProvidersList = new List<SqlSessionStateProvider>();

        /// <summary>
        /// Lock object
        /// </summary>
        private static readonly object ListSyncRoot = new object();

        #endregion

        #region Constructors

        /// <summary>
        /// Initializes static members of the <see cref="SqlSessionStateProvider"/> class. 
        ///   Initializes a new instance of the <see cref="SqlSessionStateProvider"/> class.
        /// </summary>
        static SqlSessionStateProvider()
        {
            Trace.WriteLine("SQL Session State Provider is initializing.", "SqlSessionStateProvider");
        }

        #endregion

        #region Private properties

        /// <summary>
        /// Gets the unique identifier of the current application.
        /// </summary>
        /// <value>
        /// A <see cref="Guid"/> value that identifies session state items in the session state store that belong to the
        ///   current application.
        /// </value>
        private Guid ApplicationId
        {
            get
            {
                return this.m_ApplicationId;
            }
        }

        /// <summary>
        /// Gets a <see cref="SqlSessionStateStore"/> object that provides methods for accessing the session database.
        /// </summary>
        /// <value>
        /// A <see cref="SqlSessionStateStore"/> object that provides methods for accessing the session database.
        /// </value>
        [NotNull]
        private SqlSessionStateStore Store
        {
            get
            {
                System.Diagnostics.Debug.Assert(null != this.m_Store);

                return this.m_Store;
            }
        }

        #endregion

        #region Public methods

        /// <summary>
        /// Adds a new session-state item to the data store.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/> for the current request.
        /// </param>
        /// <param name="id">
        /// The unique identifier of session the new state store item represents.
        /// </param>
        /// <param name="timeout">
        /// The session timeout, in minutes, for the current request.
        /// </param>
        /// <remarks>
        /// <para>
        /// The <see cref="CreateUninitializedItem( HttpContext, string, int )"/> method is used with sessions when the
        ///     <i>Cookieless</i> and <i>RegenerateExpiredSessionId</i> attributes are both <c>true</c>. Having the
        ///     <i>RegenerateExpiredSessionId</i> attribute set to <c>true</c> causes the <see cref="SessionStateModule"/>
        ///     object to generate a new session ID value when an expired session ID value is encountered.
        ///   </para>
        /// <para>
        /// The process of generating a new session ID value requires redirecting the browser to a URL that contains
        ///     the newly generated session ID value. The <see cref="CreateUninitializedItem( HttpContext, string, int )"/>
        ///     method is called during the initial request that contains an expired session ID value. After the
        ///     <see cref="SessionStateModule"/> object acquires a new session ID value to replace the expired value, it
        ///     calls the <see cref="CreateUninitializedItem( HttpContext, string, int )"/> method to add an uninitialized
        ///     entry to the session-state data store. The browser is then redirected to the URL containing the newly
        ///     generated session ID value. The existence of the uninitialized entry in the session data store ensures that
        ///     the redirected request that includes the newly generated session ID value is not mistaken for a request for
        ///     an expired session and is, instead, treated as a new session. 
        ///   </para>
        /// <para>
        /// The uninitialized entry in the session data store is associated with the newly generated session ID value
        ///     and contains only default values, including an expiration date and time and a value that corresponds to the
        ///     action flags parameter of the <see cref="GetItem"/> and <see cref="GetItemExclusive"/> methods. The
        ///     uninitialized entry in the session-state store should include an action flags value equal to the
        ///     <see cref="SessionStateActions.InitializeItem"/> enumeration value. This value is passed to the
        ///     <see cref="SessionStateModule"/> object by the <see cref="GetItem"/> and <see cref="GetItemExclusive"/>
        ///     methods, and informs the <see cref="SessionStateModule"/> object that the current session is a new but
        ///     uninitialized session. The <see cref="SessionStateModule"/> object will then initialize the new session and
        ///     raise the <c>Session_OnStart</c> event.
        ///   </para>
        /// </remarks>
        public override void CreateUninitializedItem(HttpContext context, string id, int timeout)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(id, "id");

            const int flags = (int)SessionStateActions.InitializeItem;
            var sessionItems = new SessionStateItemCollection();
            var staticObjects = new HttpStaticObjectsCollection();
            var sessionState = new SessionStateStoreData(sessionItems, staticObjects, timeout);

            this.m_Store.InsertItem(this.ApplicationId, id, flags, sessionState);
        }

        /// <summary>
        /// The get item.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="locked">
        /// The locked.
        /// </param>
        /// <param name="lockAge">
        /// The lock age.
        /// </param>
        /// <param name="lockId">
        /// The lock id.
        /// </param>
        /// <param name="actions">
        /// The actions.
        /// </param>
        /// <returns>
        /// The <see cref="SessionStateStoreData"/>.
        /// </returns>
        public override SessionStateStoreData GetItem(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(id, "id");

            locked = false;
            lockAge = TimeSpan.Zero;
            lockId = null;
            actions = SessionStateActions.None;

            int flags = 0;
            SessionStateLockCookie lockCookie = null;

            SessionStateStoreData result = this.Store.GetItem(this.ApplicationId, id, out lockCookie, out flags);

            actions = (SessionStateActions)flags;

            if (null != lockCookie)
            {
                locked = true;
                lockId = lockCookie.Id;
                lockAge = DateTime.UtcNow - lockCookie.Timestamp;
            }

            return result;
        }

        /// <summary>
        /// Locks and returns session state data from the session data store.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/> for the current request.
        /// </param>
        /// <param name="id">
        /// The session ID for the current request.
        /// </param>
        /// <param name="locked">
        /// When this method returns, contains a <see cref="bool"/> value that is set to <c>true</c> if a lock is
        ///   successfully obtained; otherwise, <c>false</c>.
        /// </param>
        /// <param name="lockAge">
        /// When this method returns, contains a <see cref="TimeSpan"/> value that is set to the amount of time that the
        ///   item in the session data store has been locked or an <see cref="TimeSpan.Zero"/> if the lock was obtained in
        ///   the current call.
        /// </param>
        /// <param name="lockId">
        /// When this method returns, contains an object that is set to the lock identifier for the current request.
        /// </param>
        /// <param name="actions">
        /// When this method returns, contains one of the <see cref="SessionStateActions"/> values, indicating whether
        ///   the current session is an uninitialized, cookieless session.
        /// </param>
        /// <returns>
        /// A <see cref="SessionStateStoreData"/> object containing the session state data if the requested session state
        ///   store item was succefull locked; otherwise, <c>null</c>.
        /// </returns>
        /// <remarks>
        /// More information can be found <see cref="SessionStateStoreProviderBase.GetItemExclusive">Here</see> and
        ///   <see href="http://msdn.microsoft.com/en-us/library/dd941992.aspx">Here</see>.
        /// </remarks>
        public override SessionStateStoreData GetItemExclusive(HttpContext context, string id, out bool locked, out TimeSpan lockAge, out object lockId, out SessionStateActions actions)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(id, "id");

            // Set the initial values of the out parameters. These are the values to be returned if the requested session
            // state store item does not exist.
            lockAge = TimeSpan.Zero;
            actions = SessionStateActions.None;

            int flags = 0;
            SessionStateLockCookie existingLockCookie = null;
            SessionStateLockCookie acquiredLockCookie = SessionStateLockCookie.Generate(DateTime.UtcNow);

            SessionStateStoreData result = this.Store.GetItemExclusive(this.ApplicationId, id, acquiredLockCookie, out existingLockCookie, out flags);

            if (existingLockCookie != null)
            {
                locked = true;
                lockAge = DateTime.UtcNow - existingLockCookie.Timestamp;
                lockId = existingLockCookie.Id; // ??
            }
            else
            {
                locked = false;
                lockId = acquiredLockCookie.Id; // ??
                actions = (SessionStateActions)flags;
            }

            return result;
        }

        /// <summary>
        /// Initializes the current provider.
        /// </summary>
        /// <param name="name">
        /// The friendly name of the provider.
        /// </param>
        /// <param name="config">
        /// A collection of the name/value pairs representing the provider-specific attributes specified in the
        ///   configuration for this provider.
        /// </param>
        /// <exception cref="Sitecore.Exceptions.ConfigurationException">
        /// The polling interval specified is too small or too high.
        /// </exception>
        public override void Initialize(string name, NameValueCollection config)
        {
            Assert.ArgumentNotNull(name, "name");
            Assert.ArgumentNotNull(config, "config");

            base.Initialize(name, config);

            var configuration = new ConfigReader(config, name);

            string applicationName = configuration.GetString("sessionType", true);
            string connectionName = configuration.GetString("connectionStringName", false);
            string connectionString = ConfigurationManager.ConnectionStrings[connectionName].ConnectionString;

            bool compression = configuration.GetBool("compression", false);

            this.m_Store = new SqlSessionStateStore(connectionString, compression);
            this.m_ApplicationId = this.m_Store.GetApplicationIdentifier(applicationName);

            lock (ListSyncRoot)
            {
                SqlSessionStateProvidersList.Add(this);
            }
            this.CanStartTimer = this.IsTimerOffForAllInstance;
        }

        /// <summary>
        /// Releases managed and unmanaged resources.
        /// </summary>
        public override void Dispose()
        {
            lock (ListSyncRoot)
            {
                SqlSessionStateProvidersList.Remove(this);
                if (this.TimerEnabled && SqlSessionStateProvidersList.Count > 0)
                {
                    foreach (var sqlSessionStateProvider in SqlSessionStateProvidersList)
                    {
                        if (sqlSessionStateProvider.ApplicationId == this.ApplicationId &&
                             sqlSessionStateProvider.TriedToStartTimer)
                        {
                            sqlSessionStateProvider.StartTimer();
                            break;
                        }
                    }
                }
            }
            base.Dispose();
        }

        /// <summary>
        /// Releases a lock on an item in the session data store.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/> for the current request.
        /// </param>
        /// <param name="id">
        /// The session ID for the current request.
        /// </param>
        /// <param name="lockId">
        /// The lock identifier for the current request.
        /// </param>
        /// <remarks>
        /// <param>
        /// The <see cref="SessionStateModule"/> object calls the <see cref="ReleaseItemExclusive"/> method to update
        ///     the expiration date and release a lock on an item in the session data store. It is called at the end of a
        ///     request, during the <see cref="HttpApplication.ReleaseRequestState"/> event, if session values are
        ///     unchanged. If session values have been modified, the <see cref="SessionStateModule"/> object instead calls
        ///     the <see cref="SetAndReleaseItemExclusive"/> method.
        /// </param>
        /// <param>
        /// The <see cref="SessionStateModule"/> object also calls the <see cref="ReleaseItemExclusive"/> method when a
        ///     lock on an item in the session data store has exceeded the <see cref="HttpRuntimeSection.ExecutionTimeout"/>
        ///     value. For more information about locking and details about the lock identifier, see "Locking Session-Store
        ///     Data" in the <see cref="SessionStateStoreProviderBase"/> class overview.
        ///   </param>
        /// <param>
        /// The <see cref="ReleaseItemExclusive"/> method only removes the lock from an item in the session data store
        ///     for the current application that matches the supplied session id and lock id values. If the lock id does not
        ///     match the one in the data store, the <see cref="ReleaseItemExclusive"/> method does nothing.
        /// </param>
        /// </remarks>
        public override void ReleaseItemExclusive([NotNull] HttpContext context, string id, object lockId)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(id, "id");
            Assert.ArgumentNotNull(lockId, "lockId");

            string lockCookie = Convert.ToString(lockId);

            this.Store.ReleaseItem(this.ApplicationId, id, lockCookie);
        }

        /// <summary>
        /// The remove item.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="lockId">
        /// The lock id.
        /// </param>
        /// <param name="item">
        /// The item.
        /// </param>
        public override void RemoveItem(HttpContext context, string id, object lockId, SessionStateStoreData item)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(id, "id");
            Assert.ArgumentNotNull(lockId, "lockId");

            string lockCookie = Convert.ToString(lockId);

            try
            {
                this.ExecuteSessionEnd(id, item);
            }
            finally
            {
                this.Store.RemoveItem(this.ApplicationId, id, lockCookie);
            }
        }

        /// <summary>
        /// The reset item timeout.
        /// </summary>
        /// <param name="context">
        /// The context.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        public override void ResetItemTimeout(HttpContext context, string id)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(id, "id");

            this.Store.UpdateItemExpiration(this.ApplicationId, id);
        }

        /// <summary>
        /// Updates the session-item information in the session-state data store with values from the current request,
        ///   and clears the lock on the data.
        /// </summary>
        /// <param name="context">
        /// The <see cref="HttpContext"/> for the current request.
        /// </param>
        /// <param name="id">
        /// The session ID for the current request.
        /// </param>
        /// <param name="sessionState">
        /// The <see cref="SessionStateStoreData"/> object that contains the current session values to be stored.
        /// </param>
        /// <param name="lockId">
        /// The lock identifier for the current request.
        /// </param>
        /// <param name="newItem">
        /// <c>true</c> to identify the session item as a new item; otherwise, <c>false</c>.
        /// </param>
        /// <remarks>
        /// <para>
        /// The <see cref="SessionStateModule"/> object calls the SetAndReleaseItemExclusive method at the end of a
        ///     request, during the <see cref="HttpApplication.ReleaseRequestState"/> event, to insert current session-
        ///     item information into the data store or update existing session-item information in the data store with
        ///     current values, to update the expiration time on the item, and to release the lock on the data. Only
        ///     session data for the current application that matches the supplied session id and lock id values is
        ///     updated.
        ///   </para>
        /// <para>
        /// If the session values for the current request have not been modified, the
        ///     <see cref="SetAndReleaseItemExclusive"/> method is not called. Instead, the
        ///     <see cref="ReleaseItemExclusive"/> method is called.
        ///   </para>
        /// <para>
        /// If the <see cref="HttpSessionState.Abandon"/> method has been called, the
        ///     <see cref="SetAndReleaseItemExclusive"/> method is not called. Instead, the
        ///     <see cref="SessionStateModule"/> object calls the <see cref="RemoveItem"/> method to delete session-item
        ///     data from the data source.
        ///   </para>
        /// </remarks>
        public override void SetAndReleaseItemExclusive([NotNull] HttpContext context, string id, SessionStateStoreData sessionState, object lockId, bool newItem)
        {
            Assert.ArgumentNotNull(context, "context");
            Assert.ArgumentNotNull(id, "id");

            if (newItem)
            {
                const int flags = (int)SessionStateActions.None;
                this.Store.InsertItem(this.ApplicationId, id, flags, sessionState);
            }
            else
            {
                string lockCookie = Convert.ToString(lockId);
                this.Store.UpdateAndReleaseItem(this.ApplicationId, id, lockCookie, SessionStateActions.None, sessionState);
            }
        }

        #endregion

        #region Protected methods

        /// <summary>
        /// The get expired item exclusive.
        /// </summary>
        /// <param name="signalTime">
        /// The signal time.
        /// </param>
        /// <param name="lockCookie">
        /// The lock cookie.
        /// </param>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <returns>
        /// The <see cref="SessionStateStoreData"/>.
        /// </returns>
        protected override SessionStateStoreData GetExpiredItemExclusive(DateTime signalTime, SessionStateLockCookie lockCookie, out string id)
        {
            return this.Store.GetExpiredItemExclusive(this.ApplicationId, lockCookie, out id);
        }

        /// <summary>
        /// The remove item from sessions storage.
        /// </summary>
        /// <param name="id">
        /// The id.
        /// </param>
        /// <param name="lockCookie">
        /// The lock cookie.
        /// </param>
        protected override void RemoveItem(string id, string lockCookie)
        {
            this.Store.RemoveItem(this.ApplicationId, id, lockCookie);
        }

        #endregion

        #region Private methods

        /// <summary>
        /// Use this method to check whether all the instances of this class has timer off or not.
        /// </summary>
        /// <returns></returns>
        private bool IsTimerOffForAllInstance()
        {
            lock (ListSyncRoot)
            {
                foreach (var sqlSessionStateProvider in SqlSessionStateProvidersList)
                {
                    if (sqlSessionStateProvider.ApplicationId == this.ApplicationId &&
                        sqlSessionStateProvider.TriedToStartTimer &&
                        sqlSessionStateProvider.TimerEnabled)
                        return false;
                }
                return true;
            }
        }
        #endregion
    }
}