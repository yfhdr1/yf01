using System;
using UnityEngine;
using Object = UnityEngine.Object;
#if PW_GPGS && UNITY_ANDROID
using GooglePlayGames;
using GooglePlayGames.BasicApi;
#endif
public static class PwGoogle
{
#if PW_GPGS && UNITY_ANDROID
    private static bool activated;
#endif
    public static bool Available
    {
        get
        {
#if PW_GPGS && UNITY_ANDROID
            return true;
#else
            return false;
#endif
        }
    }
    public static void RequestCode(Action<string, string> done)
    {
        if (done == null) return;
#if PW_GPGS && UNITY_ANDROID
        try
        {
            if (!activated)
            {
                PlayGamesPlatform.Activate();
                activated = true;
            }
            PlayGamesPlatform.Instance.Authenticate(status =>
            {
                if (status == SignInStatus.Success)
                {
                    Fetch(done);
                    return;
                }
                PlayGamesPlatform.Instance.ManuallyAuthenticate(second =>
                {
                    if (second == SignInStatus.Success) Fetch(done);
                    else done(null, "Google sign-in failed");
                });
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning("PW_GOOGLE " + e.Message);
            done(null, "Google sign-in failed");
        }
#else
        done(null, "Google sign-in not available");
#endif
    }
#if PW_GPGS && UNITY_ANDROID
    private static void Fetch(Action<string, string> done)
    {
        try
        {
            PlayGamesPlatform.Instance.RequestServerSideAccess(false, code =>
            {
                if (string.IsNullOrEmpty(code)) done(null, "Google sign-in failed");
                else done(code, null);
            });
        }
        catch (Exception e)
        {
            Debug.LogWarning("PW_GOOGLE " + e.Message);
            done(null, "Google sign-in failed");
        }
    }
#endif
}
