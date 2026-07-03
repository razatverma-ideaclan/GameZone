using System;
using System.Threading.Tasks;
#if USE_FIREBASE
using Firebase;
using Firebase.Auth;
using Google;
#endif
using UnityEngine;

public class AuthManager : MonoBehaviour
{
    public static AuthManager Instance { get; private set; }

    [Tooltip("Web client ID from google-services.json (oauth_client entry with client_type 3).")]
    public string webClientId = "540695022684-r4v6br88skf8n2lkmqf9hbog3tjol6a0.apps.googleusercontent.com";

#if USE_FIREBASE
    private FirebaseAuth auth;
    private FirebaseUser user;
    private bool firebaseReady = false;
    private TaskScheduler mainThread;
#endif

    public bool IsSignedIn =>
#if USE_FIREBASE
        user != null;
#else
        false;
#endif

    public string DisplayName =>
#if USE_FIREBASE
        user != null ? user.DisplayName : null;
#else
        null;
#endif

    public string Uid =>
#if USE_FIREBASE
        user != null ? user.UserId : null;
#else
        null;
#endif

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
#if USE_FIREBASE
        mainThread = TaskScheduler.FromCurrentSynchronizationContext();

        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                user = auth.CurrentUser;
                firebaseReady = true;
            }
            else
            {
                Debug.LogError("Firebase dependencies not available: " + task.Result);
            }
        }, mainThread);
#endif
    }

    public void SignInWithGoogle(Action<bool> onDone)
    {
#if USE_FIREBASE
        if (!firebaseReady)
        {
            onDone?.Invoke(false);
            return;
        }

        GoogleSignIn.Configuration = new GoogleSignInConfiguration
        {
            WebClientId = webClientId,
            RequestIdToken = true,
            UseGameSignIn = false
        };

        GoogleSignIn.DefaultInstance.SignIn().ContinueWith(googleTask =>
        {
            if (googleTask.IsCanceled || googleTask.IsFaulted)
            {
                onDone?.Invoke(false);
                return;
            }

            Credential credential = GoogleAuthProvider.GetCredential(googleTask.Result.IdToken, null);
            auth.SignInWithCredentialAsync(credential).ContinueWith(authTask =>
            {
                if (authTask.IsCanceled || authTask.IsFaulted)
                {
                    onDone?.Invoke(false);
                    return;
                }
                user = authTask.Result.User;
                onDone?.Invoke(true);
            }, mainThread);
        }, mainThread);
#else
        onDone?.Invoke(false);
#endif
    }

    public void SignOut()
    {
#if USE_FIREBASE
        GoogleSignIn.DefaultInstance.SignOut();
        auth?.SignOut();
        user = null;
#endif
    }
}
