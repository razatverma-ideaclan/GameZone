using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LeaderboardUI : MonoBehaviour
{
    [Header("Sign In")]
    public GameObject signedOutGroup;
    public Button signInButton;
    public Text signInButtonLabel;

    [Header("Signed In")]
    public GameObject signedInGroup;
    public Text nameText;
    public InputField nameInputField;
    public Button saveNameButton;
    public Button signOutButton;

    [Header("Scope Tabs")]
    public Button globalTabButton;
    public Button countryTabButton;
    public Image globalTabBg;
    public Image countryTabBg;

    [Header("List")]
    public RectTransform listContent;
    public GameObject emptyListLabel;

    public Color activeTabColor = new Color(0.95f, 0.72f, 0.15f);
    public Color inactiveTabColor = new Color(0.25f, 0.25f, 0.3f);

    private bool showingGlobal = true;
    private readonly List<GameObject> spawnedRows = new List<GameObject>();

    private void Awake()
    {
        if (signInButton != null) signInButton.onClick.AddListener(OnSignInClicked);
        if (signOutButton != null) signOutButton.onClick.AddListener(OnSignOutClicked);
        if (saveNameButton != null) saveNameButton.onClick.AddListener(OnSaveNameClicked);
        if (globalTabButton != null) globalTabButton.onClick.AddListener(() => SwitchScope(true));
        if (countryTabButton != null) countryTabButton.onClick.AddListener(() => SwitchScope(false));
    }

    public void Refresh()
    {
        bool signedIn = AuthManager.Instance != null && AuthManager.Instance.IsSignedIn;

        if (signedOutGroup != null) signedOutGroup.SetActive(!signedIn);
        if (signedInGroup != null) signedInGroup.SetActive(signedIn);

        if (signedIn && nameText != null) nameText.text = AuthManager.Instance.DisplayName;
        if (signedIn && nameInputField != null) nameInputField.text = AuthManager.Instance.DisplayName;

        UpdateTabVisuals();
        FetchAndPopulate();
    }

    private void OnSignInClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.PlayClickSound();
        if (signInButtonLabel != null) signInButtonLabel.text = "SIGNING IN...";

        AuthManager.Instance.SignInWithGoogle(success =>
        {
            if (signInButtonLabel != null) signInButtonLabel.text = "SIGN IN WITH GOOGLE";
            if (success) Refresh();
        });
    }

    private void OnSignOutClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.PlayClickSound();
        AuthManager.Instance.SignOut();
        Refresh();
    }

    private void OnSaveNameClicked()
    {
        if (GameManager.Instance != null) GameManager.Instance.PlayClickSound();
        if (nameInputField == null || LeaderboardManager.Instance == null) return;

        string newName = nameInputField.text.Trim();
        if (string.IsNullOrEmpty(newName)) return;

        LeaderboardManager.Instance.UpdateDisplayName(newName, success =>
        {
            if (success && nameText != null) nameText.text = newName;
            if (success) FetchAndPopulate();
        });
    }

    private void SwitchScope(bool global)
    {
        if (GameManager.Instance != null) GameManager.Instance.PlayClickSound();
        showingGlobal = global;
        UpdateTabVisuals();
        FetchAndPopulate();
    }

    private void UpdateTabVisuals()
    {
        if (globalTabBg != null) globalTabBg.color = showingGlobal ? activeTabColor : inactiveTabColor;
        if (countryTabBg != null) countryTabBg.color = showingGlobal ? inactiveTabColor : activeTabColor;
    }

    private void FetchAndPopulate()
    {
        if (LeaderboardManager.Instance == null) return;

        System.Action<List<LeaderboardEntry>> onFetched = entries => PopulateRows(entries);

        if (showingGlobal) LeaderboardManager.Instance.FetchTopGlobal(50, onFetched);
        else LeaderboardManager.Instance.FetchTopCurrentCountry(50, onFetched);
    }

    private void PopulateRows(List<LeaderboardEntry> entries)
    {
        foreach (GameObject row in spawnedRows) Destroy(row);
        spawnedRows.Clear();

        if (listContent == null) return;

        if (emptyListLabel != null) emptyListLabel.SetActive(entries == null || entries.Count == 0);
        if (entries == null) return;

        string myUid = AuthManager.Instance != null ? AuthManager.Instance.Uid : null;

        for (int i = 0; i < entries.Count; i++)
        {
            GameObject row = BuildRow(i + 1, entries[i]);
            row.transform.SetParent(listContent, false);
            spawnedRows.Add(row);
        }
    }

    private GameObject BuildRow(int rank, LeaderboardEntry entry)
    {
        GameObject row = new GameObject("Row" + rank, typeof(RectTransform), typeof(Image));
        RectTransform rowRt = row.GetComponent<RectTransform>();
        rowRt.sizeDelta = new Vector2(0, 64f);

        Image rowBg = row.GetComponent<Image>();
        rowBg.color = rank % 2 == 0 ? new Color(1f, 1f, 1f, 0.04f) : new Color(1f, 1f, 1f, 0.09f);

        Text rankText = CreateRowLabel(row.transform, "Rank", "#" + rank, TextAnchor.MiddleLeft);
        RectTransform rankRt = rankText.GetComponent<RectTransform>();
        rankRt.anchorMin = new Vector2(0f, 0f);
        rankRt.anchorMax = new Vector2(0.18f, 1f);
        rankRt.offsetMin = new Vector2(16f, 0f);
        rankRt.offsetMax = Vector2.zero;

        Text nameLabel = CreateRowLabel(row.transform, "Name", entry.displayName, TextAnchor.MiddleLeft);
        RectTransform nameRt = nameLabel.GetComponent<RectTransform>();
        nameRt.anchorMin = new Vector2(0.18f, 0f);
        nameRt.anchorMax = new Vector2(0.72f, 1f);
        nameRt.offsetMin = Vector2.zero;
        nameRt.offsetMax = Vector2.zero;

        Text scoreLabel = CreateRowLabel(row.transform, "Score", entry.bestScore.ToString(), TextAnchor.MiddleRight);
        RectTransform scoreRt = scoreLabel.GetComponent<RectTransform>();
        scoreRt.anchorMin = new Vector2(0.72f, 0f);
        scoreRt.anchorMax = new Vector2(1f, 1f);
        scoreRt.offsetMin = Vector2.zero;
        scoreRt.offsetMax = new Vector2(-16f, 0f);

        return row;
    }

    private Text CreateRowLabel(Transform parent, string name, string content, TextAnchor anchor)
    {
        GameObject go = new GameObject(name, typeof(RectTransform), typeof(Text));
        go.transform.SetParent(parent, false);
        Text text = go.GetComponent<Text>();
        text.text = content;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26;
        text.color = Color.white;
        text.alignment = anchor;
        text.raycastTarget = false;
        return text;
    }
}
