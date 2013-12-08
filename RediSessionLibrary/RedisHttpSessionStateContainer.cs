using System.Threading.Tasks;
using System.Web;
using System.Web.SessionState;

namespace RediSessionLibrary
{
    /// <summary>
    /// The container for Redis Session States
    /// </summary>
    public class RedisHttpSessionStateContainer : HttpSessionStateContainer
    {
        public RedisSessionStateItemCollection SessionItems { get; private set; }

        /// <summary>
        /// Contrsucts the RedisHttpSessionStateContainer object
        /// </summary>
        /// <param name="SessionId">the session ID</param>
        /// <param name="sessionItems">a prepared RedisSessionStateItemCollection object</param>
        /// <param name="staticObjects">Application objects</param>
        /// <param name="timeout">Session Timeout Value</param>
        /// <param name="newSession">flag to determine if a session is new</param>
        /// <param name="cookieMode">the respecive cookie mode</param>
        /// <param name="mode">The SessionStateMode for the request</param>
        /// <param name="isReadonly">flag to determine if the session object is read only</param>
        public RedisHttpSessionStateContainer(string SessionId,
                                                    RedisSessionStateItemCollection sessionItems,
                                                    HttpStaticObjectsCollection staticObjects,
                                                    int timeout,
                                                    bool newSession,
                                                    HttpCookieMode cookieMode,
                                                    SessionStateMode mode,
                                                    bool isReadonly)
            : base(SessionId, sessionItems, staticObjects, timeout, newSession, cookieMode, mode, isReadonly)
        {
            SessionItems = sessionItems;
        }
        /// <summary>
        /// this task waits (for a specified time) for async writes to complete.
        /// </summary>
        /// <returns>true if successful, false otherwise</returns>
        public bool WaitForAsyncWrites()
        {
            Task[] tasks = SessionItems.GetAllAsyncWriteTasks();
            if (tasks != null && tasks.Length > 0)
                return Task.WaitAll(tasks, 1888);
            return true;
        }
    }
}
