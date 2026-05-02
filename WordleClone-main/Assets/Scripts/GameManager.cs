using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    public string currentWord;
    private string currentGuess = "";

    [Header("Game Settings")]
    [SerializeField] private int wordLength = 5;
    [SerializeField] private string answersFile = "Words/wordle-answers-alphabetical";
    [SerializeField] private string allowedGuessesFile = "Words/wordle-allowed-guesses";

    private List<string> answerList = new();
    private HashSet<string> allowedGuesses = new();
    private System.Random rng = new();

    [Header("Tile Sprites")]
    public Sprite spriteGrey;
    public Sprite spriteYellow;
    public Sprite spriteGreen;

    [System.Serializable]
    public class Row { public List<GameObject> tiles = new(); }

    [Header("Rows (one per guess attempt)")]
    public List<Row> rows = new();

    [Header("Keyboard Keys")]
    public List<GameObject> keyboardKeys = new();

    [Header("Keyboard Sprites")]
    public Sprite keyGreySprite;
    public Sprite keyYellowSprite;
    public Sprite keyGreenSprite;

    [Header("Animation Settings")]
    [SerializeField] private float punchScale = 0.15f;
    [SerializeField] private float punchDuration = 0.12f;
    [SerializeField] private float flipDuration = 0.25f;
    [SerializeField] private float flipDelay = 0.08f;

    [Header("Game Panel Intro")]
    [SerializeField] private CanvasGroup gamePanel;
    [SerializeField] private float introDelay = 0.7f;
    [SerializeField] private float introDuration = 2f;
    [SerializeField] private float introSlide = 60f;

    [Header("Win / Lose UI")]
    [SerializeField] private GameObject resultPanel;
    [SerializeField] private TextMeshProUGUI resultTitle;
    [SerializeField] private TextMeshProUGUI resultWord;

    private List<List<GameObject>> allPanels;
    public int currentPanel = 0;
    public int currentTextIndex = 0;
    private bool isFlipping = false;
    private bool gameOver = false;
    private Dictionary<char, LetterResult> keyBestResult = new();

    [Header("Audio")]
    private AudioSource audioSource;

    public AudioClip winClip;
    public AudioClip wrongClip;
    public AudioClip keyboardTap;
    public AudioClip backClip;

    void Start()
    {
        allPanels = new();
        foreach (Row r in rows) allPanels.Add(r.tiles);
        audioSource = GetComponent<AudioSource>();
        LoadWordLists();
        PickRandomWord();
        ClearAllPanels();

        if (resultPanel != null) resultPanel.SetActive(false);

        AnimateGamePanelIn();
    }

    void AnimateGamePanelIn()
    {
        if (gamePanel == null) return;

        isFlipping = true;

        RectTransform rt = gamePanel.GetComponent<RectTransform>();
        Vector2 finalPos = rt != null ? rt.anchoredPosition : Vector2.zero;

        gamePanel.alpha = 0f;
        if (rt != null) rt.anchoredPosition = finalPos + Vector2.down * introSlide;

        Sequence seq = DOTween.Sequence();
        seq.AppendInterval(introDelay);
        seq.Append(gamePanel.DOFade(1f, introDuration).SetEase(Ease.OutQuad));
        if (rt != null)
            seq.Join(rt.DOAnchorPos(finalPos, introDuration).SetEase(Ease.OutBack));
        seq.OnComplete(() => isFlipping = false);
    }

    // ── Input ─────────────────────────────────────────────────────────────────

    public void AddLetter(string letter)
    {
        if (isFlipping || gameOver) return;
        if (currentPanel >= allPanels.Count) return;
        if (currentGuess.Length >= wordLength) return;

        currentGuess += letter.ToLower();
        GameObject tile = allPanels[currentPanel][currentTextIndex];
        tile.GetComponentInChildren<TextMeshProUGUI>().text = letter.ToUpper();
        currentTextIndex++;

        PunchTile(tile);
        PunchKey(letter);
        PlaySound(keyboardTap);
    }

    public void Backspace()
    {
        PlaySound(keyboardTap);
        if (isFlipping || gameOver) return;
        if (currentGuess.Length == 0) return;

        currentGuess = currentGuess[..^1];
        currentTextIndex = Mathf.Max(0, currentTextIndex - 1);
        allPanels[currentPanel][currentTextIndex].GetComponentInChildren<TextMeshProUGUI>().text = "";

    }

    public void Submit()
    {
        PlaySound(keyboardTap);

        if (isFlipping || gameOver) return;
        if (currentGuess.Length < wordLength) return;
        if (!IsValidGuess(currentGuess))
        {
            Debug.Log("Not a valid word: " + currentGuess);
            ShakeRow(allPanels[currentPanel]);
            PlaySound(wrongClip);
            return;
        }

        bool won = currentGuess.ToLower() == currentWord.ToLower();
        List<LetterResult> results = GetGuessResult(currentGuess, currentWord);
        List<GameObject> row = allPanels[currentPanel];
        string submittedGuess = currentGuess;
        isFlipping = true;

        StartCoroutine(FlipRow(row, results, () =>
        {
            UpdateKeySprites(submittedGuess, results);
            isFlipping = false;


            if (won)
            {
                gameOver = true;
                StartCoroutine(WinSequence(row));
                PlaySound(winClip);
            }
            else
            {
                currentPanel++;
                currentGuess = "";
                currentTextIndex = 0;

                if (currentPanel >= allPanels.Count)
                {
                    gameOver = true;
                    ShowResultPanel(false);


                }

            }
        }));
    }

    public void PlaySound(AudioClip clip)
    {
        audioSource.PlayOneShot(clip);
    }

    // ── Win / Lose ────────────────────────────────────────────────────────────

    IEnumerator WinSequence(List<GameObject> winRow)
    {
        var tickWait = new WaitForSeconds(0.08f);
        for (int i = 0; i < winRow.Count; i++)
        {
            if (winRow[i].TryGetComponent(out RectTransform rt))
                rt.DOPunchScale(Vector3.one * 0.35f, 0.4f, 6, 0.4f);
            yield return tickWait;
        }

        yield return new WaitForSeconds(0.5f);
        ShowResultPanel(true);
    }

    void ShowResultPanel(bool won)
    {
        if (resultPanel == null) return;

        if (resultTitle != null) resultTitle.text = won ? "Your Word Was:" : "GAME OVER";
        if (resultWord != null) resultWord.text = currentWord.ToUpper();

        resultPanel.SetActive(true);

        if (!resultPanel.TryGetComponent(out CanvasGroup cg))
            cg = resultPanel.AddComponent<CanvasGroup>();
        resultPanel.TryGetComponent(out RectTransform panelRt);

        cg.alpha = 0f;
        if (panelRt != null) panelRt.localScale = Vector3.one * 0.8f;

        cg.DOFade(1f, 0.35f).SetEase(Ease.OutQuad);
        if (panelRt != null) panelRt.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
    }

    // ── Animations ────────────────────────────────────────────────────────────

    void PunchTile(GameObject tile)
    {
        if (!tile.TryGetComponent(out RectTransform rt)) return;
        rt.DOKill();
        rt.localScale = Vector3.one;
        rt.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f);
    }

    void PunchKey(string letter)
    {
        foreach (GameObject key in keyboardKeys)
        {
            TextMeshProUGUI tmp = key.GetComponentInChildren<TextMeshProUGUI>();
            if (tmp == null || !tmp.text.Equals(letter, System.StringComparison.OrdinalIgnoreCase)) continue;
            if (!key.TryGetComponent(out RectTransform rt)) break;
            rt.DOKill();
            rt.localScale = Vector3.one;
            rt.DOPunchScale(Vector3.one * punchScale, punchDuration, 1, 0.5f);
            break;
        }
    }

    IEnumerator FlipRow(List<GameObject> row, List<LetterResult> results, System.Action onComplete)
    {
        for (int i = 0; i < row.Count; i++)
        {
            int idx = i;
            GameObject tile = row[idx];
            if (!tile.TryGetComponent(out RectTransform rt)) continue;
            if (!tile.TryGetComponent(out Image img)) continue;
            TextMeshProUGUI tileTmp = tile.GetComponentInChildren<TextMeshProUGUI>();

            rt.DOScaleY(0f, flipDuration)
              .SetEase(Ease.InQuad)
              .OnComplete(() =>
              {
                  img.sprite = results[idx] switch
                  {
                      LetterResult.Green => spriteGreen,
                      LetterResult.Yellow => spriteYellow,
                      _ => spriteGrey,
                  };
                  if (tileTmp != null) tileTmp.color = Color.white;
                  rt.DOScaleY(1f, flipDuration).SetEase(Ease.OutQuad);
              });

            yield return new WaitForSeconds(flipDelay);
        }

        yield return new WaitForSeconds(flipDuration * 2f + flipDelay * (row.Count - 1));
        onComplete?.Invoke();
    }

    void UpdateKeySprites(string guess, List<LetterResult> results)
    {
        for (int i = 0; i < guess.Length; i++)
        {
            char c = guess[i];
            if (!keyBestResult.TryGetValue(c, out LetterResult current) || results[i] > current)
                keyBestResult[c] = results[i];
        }

        for (int i = 0; i < guess.Length; i++)
        {
            char c = guess[i];
            string letter = c.ToString();
            Sprite target = keyBestResult[c] switch
            {
                LetterResult.Green => keyGreenSprite,
                LetterResult.Yellow => keyYellowSprite,
                _ => keyGreySprite,
            };

            foreach (GameObject key in keyboardKeys)
            {
                TextMeshProUGUI tmp = key.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp == null || !tmp.text.Equals(letter, System.StringComparison.OrdinalIgnoreCase)) continue;
                Image img = key.GetComponentInChildren<Image>();
                if (img != null) img.sprite = target;
                tmp.color = Color.white;
                break;
            }
            PlaySound(backClip);
        }
    }

    void ShakeRow(List<GameObject> row)
    {
        foreach (GameObject tile in row)
        {
            if (!tile.TryGetComponent(out RectTransform rt)) continue;
            rt.DOKill();
            rt.DOShakeAnchorPos(0.4f, new Vector2(10f, 0f), 20, 0, false, true);
        }
    }

    // ── Game Logic ────────────────────────────────────────────────────────────

    public enum LetterResult { Grey, Yellow, Green }

    public List<LetterResult> GetGuessResult(string guess, string answer)
    {
        List<LetterResult> result = new(new LetterResult[guess.Length]);
        bool[] answerUsed = new bool[answer.Length];

        for (int i = 0; i < guess.Length; i++)
            if (guess[i] == answer[i])
            {
                result[i] = LetterResult.Green;
                answerUsed[i] = true;
            }

        for (int i = 0; i < guess.Length; i++)
        {
            if (result[i] == LetterResult.Green) continue;
            for (int j = 0; j < answer.Length; j++)
                if (!answerUsed[j] && guess[i] == answer[j])
                {
                    result[i] = LetterResult.Yellow;
                    answerUsed[j] = true;
                    break;
                }
        }
        return result;
    }

    public bool IsValidGuess(string guess) => allowedGuesses.Contains(guess.ToLower());

    void LoadWordLists()
    {
        TextAsset answerAsset = Resources.Load<TextAsset>(answersFile);
        if (answerAsset != null)
            foreach (var word in answerAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string w = word.Trim().ToLower();
                if (w.Length == wordLength) answerList.Add(w);
            }

        TextAsset allowedAsset = Resources.Load<TextAsset>(allowedGuessesFile);
        if (allowedAsset != null)
            foreach (var word in allowedAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries))
            {
                string w = word.Trim().ToLower();
                if (w.Length == wordLength) allowedGuesses.Add(w);
            }

        foreach (var w in answerList) allowedGuesses.Add(w);
    }

    void PickRandomWord()
    {
        currentWord = answerList.Count > 0
            ? answerList[rng.Next(answerList.Count)]
            : new string('a', wordLength);
    }

    void ClearAllPanels()
    {
        foreach (List<GameObject> row in allPanels)
            foreach (GameObject tile in row)
                tile.GetComponentInChildren<TextMeshProUGUI>().text = "";
    }

    public void ChangeLevel(string s)
    {
        SceneManager.LoadScene(s);

    }
}
