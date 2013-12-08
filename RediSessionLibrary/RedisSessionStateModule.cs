using BookSleeve;
using System;
using System.Configuration;
using System.Web;
using System.Web.Configuration;
using System.Web.SessionState;

namespace RediSessionLibrary
{
    /// <summary>
    /// The Redis Session State Module is responsible for attaching a 
    /// Redis Session State Container to a request and waiting on pending tasks
    /// </summary>
    public sealed class RedisSessionStateModule : IHttpModule, IDisposable
    {
        #region private components
        private bool isInitialized = false;
        private bool releaseCalled = false;
        private ISessionIDManager sessionIDManager;
        private RedisConnection redisConnection = null;
        private RedisConfiguration redisConfigurationInfo = null;
        #endregion

        /// <summary>
        /// The Start event handler (called when session starts)
        /// </summary>
        public event EventHandler Start;

        #region Public Functions [ Creatation / Tear down ]
        public void Init(HttpApplication app)
        {
            if (!isInitialized)
            {
                lock (typeof(RedisSessionStateModule))
                {
                    if (!isInitialized) // initialize variables, read configs
                    {
                        SessionStateSection sssec = (SessionStateSection)ConfigurationManager.GetSection("system.web/sessionState");

                        redisConnection = null;
                        redisConfigurationInfo = new RedisConfiguration(sssec);

                        // Add all the event handlers
                        app.AcquireRequestState += new EventHandler(this.OnAcquireRequestState);
                        app.ReleaseRequestState += new EventHandler(this.OnReleaseRequestState);
                        app.EndRequest += new EventHandler(this.OnEndRequest);

                        // Create a SessionIDManager
                        sessionIDManager = new SessionIDManager();
                        sessionIDManager.Initialize();
                        redisConnection = new RedisConnection(redisConfigurationInfo.Host, redisConfigurationInfo.Port);

                        isInitialized = true;
                    }
                }
            }
        }

        /// <summary>
        /// obligatory Dispose call to release resources
        /// </summary>
        public void Dispose()
        {
            if (redisConnection != null)
            {
                lock (typeof(RedisSessionStateModule))
                {
                    if (redisConnection != null)
                    {
                        redisConnection.Dispose();
                        redisConnection = null;
                    }
                }
            }
        }
        #endregion

        #region Private Helper Functions
        /// <summary>
        /// Retrieves a valid Redis connection
        /// </summary>
        /// <returns>a Redis connection, null if it failed</returns>
        private RedisConnection GetRedisConnection()
        {
            if (ConnectionNeedsReset())
            {
                lock (typeof(RedisSessionStateModule))
                {
                    if (ConnectionNeedsReset())
                    {
                        redisConnection = new RedisConnection(redisConfigurationInfo.Host, redisConfigurationInfo.Port);
                        redisConnection.Closed += (object sender, EventArgs e) =>
                        {
                            if (HttpContext.Current != null)
                                HttpContext.Current.Trace.Write("Redis connection closed.");
                        };
                        redisConnection.Open();
                    }
                }
            }
            return redisConnection;
        }

        /// <summary>
        /// Determines if a connectione needs to be reset.
        /// </summary>
        /// <returns>true if the connection needs to be reset, false otherwise</returns>
        private bool ConnectionNeedsReset()
        {
            return
                redisConnection == null ||
                (redisConnection.State != RedisConnectionBase.ConnectionState.Opening &&
                    redisConnection.State != RedisConnectionBase.ConnectionState.Open
                );
        }

        /// <summary>
        /// Determines if a request needs a Session State object
        /// </summary>
        /// <param name="context">The request context</param>
        /// <returns>true if the request needs to have a session object</returns>
        private bool RequiresSessionState(HttpContextBase context)
        {
            if (context.Session != null && (context.Session.Mode == SessionStateMode.Off))
                return false;
            return (context.Handler is IRequiresSessionState || context.Handler is IReadOnlySessionState);
        }
        #endregion

        #region Event Functions
        /// <summary>
        /// Called when the request needs to aquire the sesssion state
        /// </summary>
        /// <param name="source">the HttpApplication object</param>
        /// <param name="args">(ignored)</param>
        private void OnAcquireRequestState(object source, EventArgs args)
        {
            HttpApplication app = (HttpApplication)source;
            HttpContext context = app.Context;
            bool isNew = false;
            string sessionId;

            RedisSessionStateItemCollection sessionItemCollection = null;
            bool supportSessionIDReissue = true;

            sessionIDManager.InitializeRequest(context, false, out supportSessionIDReissue);
            sessionId = sessionIDManager.GetSessionID(context);

            if (sessionId == null)
            {
                bool redirected, cookieAdded;
                isNew = true;
                sessionId = sessionIDManager.CreateSessionID(context);
                sessionIDManager.SaveSessionID(context, sessionId, out redirected, out cookieAdded);
                if (redirected) // nothing else to do
                    return;
            }

            if (!RequiresSessionState(new HttpContextWrapper(context)))
                return; // again, nothing else to do

            // otherwise we need to go get the session object.
            releaseCalled = false; // set a flag to tell us that new

            sessionItemCollection = new RedisSessionStateItemCollection(sessionId, GetRedisConnection(), redisConfigurationInfo.SessionTimeout);

            // Add the session state data to the current request
            SessionStateUtility.AddHttpSessionStateToContext(context,
                new RedisHttpSessionStateContainer(
                    sessionId,
                    sessionItemCollection,
                    SessionStateUtility.GetSessionStaticObjects(context),
                    redisConfigurationInfo.SessionTimeout,
                    isNew,
                    HttpCookieMode.UseCookies, // only cookie mode is supported
                    SessionStateMode.Custom,
                    false));

            // Execute the Session_OnStart event for a new session.
            if (isNew && Start != null)
                Start(this, EventArgs.Empty);
        }

        /// <summary>
        /// Called when an request is ending
        /// </summary>
        /// <param name="source">the HttpApplication object</param>
        /// <param name="eventArgs">ignored</param>
        private void OnEndRequest(object source, EventArgs eventArgs)
        {
            if (!releaseCalled)
            {
                OnReleaseRequestState(source, eventArgs);
            }
        }


        /// <summary>
        /// Called when an request is ending and it needs to releaser the current request state
        /// </summary>
        /// <param name="source">the HttpApplication object</param>
        /// <param name="eventArgs">ignored</param>
        private void OnReleaseRequestState(object source, EventArgs args)
        {
            HttpApplication app = (HttpApplication)source;
            HttpContext context = app.Context;

            if (context == null || context.Session == null)
                return; // nothing to do

            releaseCalled = true;

            // Read the session state from the context
            var stateContainer = (RedisHttpSessionStateContainer)SessionStateUtility.GetHttpSessionStateFromContext(context);

            // If Session.Abandon() was called, remove the session data from the local Hashtable
            // and execute the Session_OnEnd event from the Global.asax file.
            if (stateContainer.IsAbandoned)
            {
                stateContainer.Clear();
                SessionStateUtility.RaiseSessionEnd(stateContainer, this, EventArgs.Empty);
            }

            // ensure all async updates are completed.
            stateContainer.WaitForAsyncWrites();

            SessionStateUtility.RemoveHttpSessionStateFromContext(context);
        }
        #endregion
    }
}
