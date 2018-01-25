namespace LiteDB
{
    /// <summary>
    /// Flags describing how this version of LiteDB was built.
    /// </summary>
    /// <remarks>Each field in this class corresponds to various compile-time definitions that can be used to build LiteDB.</remarks>
    public static class LiteDbBuildConfiguration
    {
        // NOTE: We can't use <see cref="Foo"/> in the following doc cumments because they will generate CS1574 warnings on platforms without support for whatever they reference.

        /// <summary>The target framework used to build LiteDB.</summary>
        /// <remarks>This field corresponds to NET35, NET40, NETSTANDARD13, or NETSTANDARD20.</remarks>
        public static readonly LiteDbBuildVariant Variant = LiteDbBuildVariant.Unknown;

        /// <summary>Whether the target environment supports System.AppDomain.</summary>
        /// <remarks>This field corresponds to HAVE_APP_DOMAIN.</remarks>
        public static readonly bool HaveAppDomain;

        /// <summary>Whether the target environment supports System.Diagnostics.Process.</summary>
        /// <remarks>This field corresponds to HAVE_APP_DOMAIN.</remarks>
        public static readonly bool HaveProcess;

        /// <summary>Whether the target environment supports System.Environment.</summary>
        /// <remarks>This field corresponds to HAVE_ENVIRONMENT.</remarks>
        public static readonly bool HaveEnvironment;

        /// <summary>Whether the target environment supports LiteDB.ConnectionString.Async..</summary>
        /// <remarks>This field corresponds to HAVE_SYNC_OVER_ASYNC</remarks>
        public static readonly bool HaveSyncOverAsync;

        /// <summary>Whether the target environment supports file locking.</summary>
        /// <remarks>This field corresponds to HAVE_LOCK.</remarks>
        public static readonly bool HaveLock;

        /// <summary>Whether the target environment supports System.Thrreading.Thread.ManagedThreadId.</summary>
        /// <remarks>This field corresponds to HAVE_THREAD_ID.</remarks>
        public static readonly bool HaveThreadId;

        /// <summary>Whether the target environment supports System.Reflection.TypeInfo.</summary>
        /// <remarks>This field corresponds to HAVE_TYPE_INFO.</remarks>
        public static readonly bool HaveTypeInfo;

        /// <summary>Whether the target environment supports Systen.Linq.Expressions.Expression.Assign.</summary>
        /// <remarks>This field corresponds to HAVE_EXPRESSION_ASSIGN.</remarks>
        public static readonly bool HaveExpressionAssign;

        /// <summary>Whether the target environment supports System.Threading.Tasks.Task.Delay.</summary>
        /// <remarks>This field corresponds to HAVE_TASK_DELAY.</remarks>
        public static readonly bool HaveTaskDelay;

        /// <summary>Whether the target environment supports Attribute.IsDefined.</summary>
        /// <remarks>This field corresponds to HAVE_ATTR_DEFINED.</remarks>
        public static readonly bool HaveAttributeIsDefined;

        static LiteDbBuildConfiguration()
        {
#if NET35
            Variant = LiteDbBuildVariant.DotNet35;
#elif NET40
            Variant = LiteDbBuildVariant.DotNet40;
#elif NETSTANDARD13
            Variant = LiteDbBuildVariant.DotNetStandard13;
#elif NETSTANDARD20
            Variant = LiteDbBuildVariant.DotNetStandard20;
#endif

#if HAVE_APP_DOMAIN
            HaveAppDomain = true;
#endif

#if HAVE_PROCESS
            HaveProcess = true;
#endif

#if HAVE_ENVIRONMENT
            HaveEnvironment = true;
#endif

#if HAVE_SYNC_OVER_ASYNC
            HaveSyncOverAsync = true;
#endif

#if HAVE_LOCK
            HaveLock = true;
#endif

#if HAVE_THREAD_ID
            HaveThreadId = true;
#endif

#if HAVE_TYPE_INFO
            HaveTypeInfo = true;
#endif

#if HAVE_EXPRESSION_ASSIGN
            HaveExpressionAssign = true;
#endif

#if HAVE_TASK_DELAY
            HaveTaskDelay = true;
#endif

#if HAVE_ATTR_DEFINED
            HaveAttributeIsDefined = true;
#endif
        }
    }
}
