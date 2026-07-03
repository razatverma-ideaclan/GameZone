using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading.Tasks;
#if USE_FIREBASE
using Firebase.Firestore;
#endif
using UnityEngine;

public class LeaderboardEntry
{
    public string displayName;
    public long bestScore;
    public string country;
}

public class LeaderboardManager : MonoBehaviour
{
    public static LeaderboardManager Instance { get; private set; }

#if USE_FIREBASE
    private FirebaseFirestore db;
    private TaskScheduler mainThread;
    private string cachedCountry;
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
#endif
    }

#if USE_FIREBASE
    private FirebaseFirestore Db => db ??= FirebaseFirestore.DefaultInstance;

    private string CurrentCountry()
    {
        if (cachedCountry == null)
        {
            try
            {
                cachedCountry = RegionInfo.CurrentRegion.TwoLetterISORegionName;
            }
            catch
            {
                cachedCountry = "US";
            }
        }
        return cachedCountry;
    }
#endif

    public void SubmitScore(long score, Action<bool> onDone = null)
    {
#if USE_FIREBASE
        if (AuthManager.Instance == null || !AuthManager.Instance.IsSignedIn)
        {
            onDone?.Invoke(false);
            return;
        }

        DocumentReference docRef = Db.Collection("players").Document(AuthManager.Instance.Uid);
        var data = new Dictionary<string, object>
        {
            { "displayName", AuthManager.Instance.DisplayName },
            { "bestScore", score },
            { "country", CurrentCountry() },
            { "updatedAt", Timestamp.GetCurrentTimestamp() }
        };

        docRef.SetAsync(data, SetOptions.MergeAll).ContinueWith(task =>
        {
            onDone?.Invoke(!task.IsFaulted && !task.IsCanceled);
        }, mainThread);
#else
        onDone?.Invoke(false);
#endif
    }

    public void UpdateDisplayName(string name, Action<bool> onDone = null)
    {
#if USE_FIREBASE
        if (AuthManager.Instance == null || !AuthManager.Instance.IsSignedIn)
        {
            onDone?.Invoke(false);
            return;
        }

        DocumentReference docRef = Db.Collection("players").Document(AuthManager.Instance.Uid);
        var data = new Dictionary<string, object> { { "displayName", name } };

        docRef.SetAsync(data, SetOptions.MergeAll).ContinueWith(task =>
        {
            onDone?.Invoke(!task.IsFaulted && !task.IsCanceled);
        }, mainThread);
#else
        onDone?.Invoke(false);
#endif
    }

    public void FetchTopGlobal(int n, Action<List<LeaderboardEntry>> callback)
    {
#if USE_FIREBASE
        Query query = Db.Collection("players").OrderByDescending("bestScore").Limit(n);
        RunQuery(query, callback);
#else
        callback?.Invoke(new List<LeaderboardEntry>());
#endif
    }

    public void FetchTopCountry(string countryCode, int n, Action<List<LeaderboardEntry>> callback)
    {
#if USE_FIREBASE
        Query query = Db.Collection("players")
            .WhereEqualTo("country", countryCode)
            .OrderByDescending("bestScore")
            .Limit(n);
        RunQuery(query, callback);
#else
        callback?.Invoke(new List<LeaderboardEntry>());
#endif
    }

    public void FetchTopCurrentCountry(int n, Action<List<LeaderboardEntry>> callback)
    {
#if USE_FIREBASE
        FetchTopCountry(CurrentCountry(), n, callback);
#else
        callback?.Invoke(new List<LeaderboardEntry>());
#endif
    }

#if USE_FIREBASE
    private void RunQuery(Query query, Action<List<LeaderboardEntry>> callback)
    {
        query.GetSnapshotAsync().ContinueWith(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                callback?.Invoke(new List<LeaderboardEntry>());
                return;
            }

            var results = new List<LeaderboardEntry>();
            foreach (DocumentSnapshot doc in task.Result.Documents)
            {
                results.Add(new LeaderboardEntry
                {
                    displayName = doc.ContainsField("displayName") ? doc.GetValue<string>("displayName") : "Player",
                    bestScore = doc.ContainsField("bestScore") ? doc.GetValue<long>("bestScore") : 0,
                    country = doc.ContainsField("country") ? doc.GetValue<string>("country") : ""
                });
            }
            callback?.Invoke(results);
        }, mainThread);
    }
#endif
}
