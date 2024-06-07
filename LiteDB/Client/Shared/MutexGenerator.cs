using System;
using System.Collections.Generic;

using System.Text;
using System.Threading;
using System.Diagnostics;

#if NETFRAMEWORK || NETSTANDARD2_0_OR_GREATER || NET6_0_OR_GREATER
using System.Security.AccessControl;
using System.Security.Principal;
#endif

namespace LiteDB.Client.Shared;

internal static class MutexGenerator
{
#if NET6_0_OR_GREATER
    private static Mutex CreateMutexForNet6OrGreater(string name)
    {
        if (!OperatingSystem.IsWindows())
        {
            return new Mutex(false, "Global\\" + name + ".Mutex");
        }

        var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                   MutexRights.FullControl, AccessControlType.Allow);

        var securitySettings = new MutexSecurity();
        securitySettings.AddAccessRule(allowEveryoneRule);

        return MutexAcl.Create(false, "Global\\" + name + ".Mutex", out _, securitySettings);
    }
#endif

#if NETSTANDARD2_0_OR_GREATER
    private static Mutex CreateMutexForNetStandard(string name)
    {
        var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                   MutexRights.FullControl, AccessControlType.Allow);

        var securitySettings = new MutexSecurity();
        securitySettings.AddAccessRule(allowEveryoneRule);

        var mutex = new Mutex(false, "Global\\" + name + ".Mutex");
        ThreadingAclExtensions.SetAccessControl(mutex, securitySettings);

        return mutex;
    }
#endif

#if NETFRAMEWORK
    private static Mutex CreateMutexForNetFramework(string name)
    {
        var allowEveryoneRule = new MutexAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null),
                   MutexRights.FullControl, AccessControlType.Allow);

        var securitySettings = new MutexSecurity();
        securitySettings.AddAccessRule(allowEveryoneRule);

        return new Mutex(false, "Global\\" + name + ".Mutex", out _, securitySettings);
    }
#endif

    public static Mutex CreateMutex(string name)
    {
#if NET6_0_OR_GREATER
        return CreateMutexForNet6OrGreater(name);
#endif
#if NETSTANDARD2_0_OR_GREATER
        return CreateMutexForNetStandard(name);
#endif
#if NETFRAMEWORK
        return CreateMutexForNetFramework(name);
#endif

        return new Mutex(false, "Global\\" + name + ".Mutex");
    }
}