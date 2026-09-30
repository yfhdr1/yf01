using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Unity.Services.Authentication;
using Unity.Services.CloudSave;
using Unity.Services.CloudSave.Models;
using Unity.Services.Core;
using UnityEngine;
public class PwCloudValue
{
    public bool Ok;
    public bool Found;
    public int Value;
}
public static class PwCloud
{
    public const string NeedLogin = "NeedLogin";
    public const string NoInternet = "No internet";
    private const string AccountKey = "pw_account";
    private const string AccountTypeKey = "pw_account_type";
    public static event Action Changed;
    private static bool initDone;
    private static bool initRunning;
    private static bool busy;
    public static bool Busy
    {
        get { return busy; }
    }
    public static bool Initialized
    {
        get { return initDone; }
    }
    public static bool SignedIn
    {
        get
        {
            if (!initDone) return false;
            try { return AuthenticationService.Instance != null && AuthenticationService.Instance.IsSignedIn; }
            catch (Exception) { return false; }
        }
    }
    public static string PlayerId
    {
        get
        {
            if (!SignedIn) return "guest";
            string id = AuthenticationService.Instance.PlayerId;
            return string.IsNullOrEmpty(id) ? "guest" : id;
        }
    }
    public static string SavedAccount
    {
        get { return PlayerPrefs.GetString(AccountKey, string.Empty); }
    }
    public static string SavedAccountType
    {
        get { return PlayerPrefs.GetString(AccountTypeKey, string.Empty); }
    }
    public static bool HasAccount
    {
        get { return SavedAccount.Length > 0; }
    }
    public static bool GoogleAvailable
    {
        get { return PwGoogle.Available; }
    }
    public static void Raise()
    {
        Action a = Changed;
        if (a != null) a();
    }
    public static async Task<string> InitAsync()
    {
        if (initDone) return null;
        if (initRunning)
        {
            while (initRunning) await Task.Yield();
            return initDone ? null : NoInternet;
        }
        initRunning = true;
        try
        {
            if (UnityServices.State != ServicesInitializationState.Initialized) await UnityServices.InitializeAsync();
            initDone = UnityServices.State == ServicesInitializationState.Initialized;
            return initDone ? null : NoInternet;
        }
        catch (Exception e)
        {
            Debug.LogWarning("PW_CLOUD init " + e.Message);
            return NoInternet;
        }
        finally
        {
            initRunning = false;
            Raise();
        }
    }
    public static async Task<string> ResumeAsync()
    {
        string err = await InitAsync();
        if (err != null) return err;
        if (SignedIn) return null;
        bool token;
        try { token = AuthenticationService.Instance.SessionTokenExists; }
        catch (Exception) { token = false; }
        if (!token) return NeedLogin;
        busy = true;
        Raise();
        try
        {
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
            return null;
        }
        catch (Exception e)
        {
            try { AuthenticationService.Instance.ClearSessionToken(); }
            catch (Exception) { }
            string mapped = Map(e, NeedLogin);
            return mapped == NoInternet ? NoInternet : NeedLogin;
        }
        finally
        {
            busy = false;
            Raise();
        }
    }
    public static async Task<string> SignInEmailAsync(string email, string password, bool create)
    {
        string err = await InitAsync();
        if (err != null) return err;
        string mail = email == null ? string.Empty : email.Trim();
        if (!ValidEmail(mail)) return "Bad email";
        if (!ValidPassword(password)) return "Weak password";
        string user = UserFromEmail(mail);
        busy = true;
        Raise();
        try
        {
            SignOutSilent();
            if (create)
            {
                try
                {
                    await AuthenticationService.Instance.SignUpWithUsernamePasswordAsync(user, password);
                }
                catch (Exception e)
                {
                    string mapped = Map(e, null);
                    if (mapped == NoInternet) return NoInternet;
                    SignOutSilent();
                    await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(user, password);
                }
            }
            else
            {
                await AuthenticationService.Instance.SignInWithUsernamePasswordAsync(user, password);
            }
            Remember(mail, "email");
            return null;
        }
        catch (Exception e)
        {
            return Map(e, "Wrong email or password");
        }
        finally
        {
            busy = false;
            Raise();
        }
    }
    public static async Task<string> SignInGoogleAsync()
    {
        string err = await InitAsync();
        if (err != null) return err;
        if (!PwGoogle.Available) return "Google sign-in not available";
        busy = true;
        Raise();
        try
        {
            TaskCompletionSource<string[]> wait = new TaskCompletionSource<string[]>();
            PwGoogle.RequestCode((code, error) => wait.TrySetResult(new[] { code, error }));
            string[] result = await wait.Task;
            if (!string.IsNullOrEmpty(result[1])) return result[1];
            if (string.IsNullOrEmpty(result[0])) return "Google sign-in failed";
            SignOutSilent();
            await AuthenticationService.Instance.SignInWithGooglePlayGamesAsync(result[0]);
            Remember("Google", "google");
            return null;
        }
        catch (Exception e)
        {
            return Map(e, "Google sign-in failed");
        }
        finally
        {
            busy = false;
            Raise();
        }
    }
    public static void SignOut()
    {
        try
        {
            if (SignedIn) AuthenticationService.Instance.SignOut(true);
        }
        catch (Exception e)
        {
            Debug.LogWarning("PW_CLOUD signout " + e.Message);
        }
        PlayerPrefs.DeleteKey(AccountKey);
        PlayerPrefs.DeleteKey(AccountTypeKey);
        PlayerPrefs.Save();
        Raise();
    }
    public static async Task<PwCloudValue> LoadIntAsync(string key)
    {
        PwCloudValue value = new PwCloudValue();
        if (!SignedIn) return value;
        try
        {
            Dictionary<string, Item> data = await CloudSaveService.Instance.Data.Player.LoadAsync(new HashSet<string> { key });
            value.Ok = true;
            Item item;
            if (data != null && data.TryGetValue(key, out item) && item != null && item.Value != null)
            {
                value.Found = true;
                value.Value = item.Value.GetAs<int>();
            }
            return value;
        }
        catch (Exception e)
        {
            Debug.LogWarning("PW_CLOUD load " + e.Message);
            value.Ok = false;
            return value;
        }
    }
    public static async Task<bool> SaveIntAsync(string key, int value)
    {
        if (!SignedIn) return false;
        try
        {
            Dictionary<string, object> data = new Dictionary<string, object> { { key, value } };
            await CloudSaveService.Instance.Data.Player.SaveAsync(data);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogWarning("PW_CLOUD save " + e.Message);
            return false;
        }
    }
    private static void SignOutSilent()
    {
        try
        {
            if (SignedIn) AuthenticationService.Instance.SignOut();
        }
        catch (Exception) { }
    }
    private static void Remember(string label, string type)
    {
        PlayerPrefs.SetString(AccountKey, label);
        PlayerPrefs.SetString(AccountTypeKey, type);
        PlayerPrefs.Save();
    }
    private static string Map(Exception e, string fallback)
    {
        RequestFailedException r = e as RequestFailedException;
        if (r != null && (r.ErrorCode == CommonErrorCodes.TransportError || r.ErrorCode == CommonErrorCodes.Timeout || r.ErrorCode == CommonErrorCodes.ServiceUnavailable)) return NoInternet;
        Debug.LogWarning("PW_AUTH " + e.Message);
        return fallback;
    }
    public static bool ValidEmail(string mail)
    {
        if (string.IsNullOrEmpty(mail) || mail.Length < 5 || mail.Length > 64) return false;
        int at = mail.IndexOf('@');
        if (at < 1 || at != mail.LastIndexOf('@')) return false;
        int dot = mail.LastIndexOf('.');
        if (dot < at + 2 || dot >= mail.Length - 1) return false;
        for (int i = 0; i < mail.Length; i++)
        {
            char c = mail[i];
            if (char.IsWhiteSpace(c)) return false;
        }
        return true;
    }
    public static bool ValidPassword(string pass)
    {
        if (string.IsNullOrEmpty(pass) || pass.Length < 8 || pass.Length > 30) return false;
        bool upper = false;
        bool lower = false;
        bool digit = false;
        bool symbol = false;
        for (int i = 0; i < pass.Length; i++)
        {
            char c = pass[i];
            if (char.IsUpper(c)) upper = true;
            else if (char.IsLower(c)) lower = true;
            else if (char.IsDigit(c)) digit = true;
            else if (!char.IsWhiteSpace(c)) symbol = true;
        }
        return upper && lower && digit && symbol;
    }
    public static string UserFromEmail(string email)
    {
        string mail = (email == null ? string.Empty : email.Trim()).ToLowerInvariant();
        if (mail.Length >= 3 && mail.Length <= 20 && SimpleName(mail)) return mail;
        using (SHA256 sha = SHA256.Create())
        {
            byte[] hash = sha.ComputeHash(Encoding.UTF8.GetBytes(mail));
            StringBuilder sb = new StringBuilder("pw");
            for (int i = 0; i < 9; i++) sb.Append(hash[i].ToString("x2"));
            return sb.ToString();
        }
    }
    private static bool SimpleName(string value)
    {
        for (int i = 0; i < value.Length; i++)
        {
            char c = value[i];
            bool ok = (c >= 'a' && c <= 'z') || (c >= 'A' && c <= 'Z') || (c >= '0' && c <= '9') || c == '.' || c == '-' || c == '@' || c == '_';
            if (!ok) return false;
        }
        return true;
    }
}
