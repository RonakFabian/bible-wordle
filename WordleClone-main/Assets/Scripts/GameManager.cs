using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using TMPro;
using UnityEngine;
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

    [Header("Keyboard Colors")]
    public Color keyGreyedColor = new(0.4f, 0.4f, 0.4f, 1f);

    [Header("Animation Settings")]
    [SerializeField] private float punchScale    = 0.15f;
    [SerializeField] private float punchDuration = 0.12f;
    [SerializeField] private float flipDuration  = 0.25f;
    [SerializeField] private float flipDelay     = 0.08f;

    [Header("Win / Lose UI")]
    [SerializeField] private GameObject resultPanel;      // panel that fades in
    [SerializeField] private TextMeshProUGUI resultTitle; // "YOU WIN!" or "GAME OVER"
    [SerializeField] private TextMeshProUGUI resultWord;  // shows the answer
    [SerializeField] private GameObject confettiPiece;    // simple UI Image prefab for confetti
    [SerializeField] private RectTransform confettiRoot;  // canvas rect to spawn confetti into
    [SerializeField] private int confettiCount = 60;

    private List<List<GameObject>> allPanels;
    public int currentPanel     = 0;
    public int currentTextIndex = 0;
    private bool isFlipping     = false;
    private bool gameOver       = false;
    private HashSet<char> confirmedPresentLetters = new();

    // Confetti colours
    private static readonly Color[] confettiColors =
    {
        new(1f,   0.2f, 0.2f), new(0.2f, 1f,   0.2f), new(0.2f, 0.6f, 1f),
        new(1f,   0.9f, 0.1f), new(1f,   0.4f, 0.8f), new(0.4f, 1f,   0.9f),
    };

    void Start()
    {
        allPanels = new();
        foreach (Row r in rows) allPanels.Add(r.tiles);

        LoadWordLists();
        PickRandomWord();
        ClearAllPanels();

        if (resultPanel != null) resultPanel.SetActive(false);
    }

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
    }

    public void Backspace()
    {
        if (isFlipping || gameOver) return;
        if (currentGuess.Length == 0) return;

        currentGuess = currentGuess[..^1];
        currentTextIndex = Mathf.Max(0, currentTextIndex - 1);
        allPanels[currentPanel][currentTextIndex].GetComponentInChildren<TextMeshProUGUI>().text = "";
    }

    public void Submit()
    {
        if (isFlipping || gameOver) return;
        if (currentGuess.Length < wordLength) return;
        if (!IsValidGuess(currentGuess))
        {
            Debug.Log("Not a valid word: " + currentGuess);
            ShakeRow(allPanels[currentPanel]);
            return;
        }

        bool won = currentGuess.ToLower() == currentWord.ToLower();
        List<LetterResult> results = GetGuessResult(currentGuess, currentWord);
        List<GameObject>   row     = allPanels[currentPanel];

        string submittedGuess = currentGuess;
        int    submittedPanel = currentPanel;
        isFlipping = true;

        StartCoroutine(FlipRow(row, results, () =>
        {
            GreyOutAbsentKeys(submittedGuess, results);
            isFlipping = false;

            if (won)
            {
                gameOver = true;
                StartCoroutine(WinSequence(row));
            }
            else
            {
                currentPanel++;
                currentGuess     = "";
                currentTextIndex = 0;

                if (currentPanel >= allPanels.Count)
                {
                    gameOver = true;
                    StartCoroutine(LoseSequence());
                }
            }
        }));
    }

    // ── Win / Lose sequences ──────────────────────────────────────────────────

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

        SpawnConfetti();
        ShowResultPanel(true);
    }

    IEnumerator LoseSequence()
    {
        yield return new WaitForSeconds(0.3f);
        ShowResultPanel(false);
    }

    void ShowResultPanel(bool win)
    {
        if (resultPanel == null) return;

        if (resultTitle != null)
            resultTitle.text = win ? "YOU WIN!" : "GAME OVER";

        if (resultWord != null)
            resultWord.text = currentWord.ToUpper();

        resultPanel.SetActive(true);

        // Fade + scale in
        if (!resultPanel.TryGetComponent(out CanvasGroup cg))
            cg = resultPanel.AddComponent<CanvasGroup>();

        resultPanel.TryGetComponent(out RectTransform panelRt);
        cg.alpha = 0f;
        panelRt.localScale = Vector3.one * 0.7f;

        cg.DOFade(1f, 0.35f).SetEase(Ease.OutQuad);
        panelRt.DOScale(1f, 0.35f).SetEase(Ease.OutBack);
    }

    void SpawnConfetti()
    {
        if (confettiPiece == null || confettiRoot == null) return;

        Rect bounds = confettiRoot.rect;

        for (int i = 0; i < confettiCount; i++)
        {
            GameObject piece = Instantiate(confettiPiece, confettiRoot);
            RectTransform rt = piece.GetComponent<RectTransform>();
            Image img        = piece.GetComponent<Image>();

            // Random start position across the top third
            float startX = Random.Range(bounds.xMin, bounds.xMax);
            float startY = Random.Range(bounds.yMax * 0.4f, bounds.yMax);
            rt.anchoredPosition = new Vector2(startX, startY);

            // Random colour, size, rotation
            if (img != null) img.color = confettiColors[Random.Range(0, confettiColors.Length)];
            float size = Random.Range(10f, 22f);
            rt.sizeDelta = new Vector2(size, size * Random.Range(0.4f, 1f));
            rt.rotation  = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

            float delay    = Random.Range(0f, 0.6f);
            float duration = Random.Range(1.2f, 2.2f);
            float endY     = bounds.yMin - 60f;
            float drift    = Random.Range(-120f, 120f);

            // Fall + drift + spin
            rt.DOAnchorPosY(endY, duration)
              .SetDelay(delay)
              .SetEase(Ease.InQuad);
            rt.DOAnchorPosX(rt.anchoredPosition.x + drift, duration)
              .SetDelay(delay)
              .SetEase(Ease.InOutSine);
            rt.DORotate(new Vector3(0, 0, Random.Range(180f, 540f)), duration, RotateMode.FastBeyond360)
              .SetDelay(delay);

            // Destroy after it falls off screen
            Destroy(piece, delay + duration + 0.1f);
        }
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
            int        idx  = i;
            GameObject tile = row[idx];
            if (!tile.TryGetComponent(out RectTransform rt)) continue;
            if (!tile.TryGetComponent(out Image img)) continue;

            rt.DOScaleY(0f, flipDuration)
              .SetEase(Ease.InQuad)
              .OnComplete(() =>
              {
                  img.sprite = results[idx] switch
                  {
                      LetterResult.Green  => spriteGreen,
                      LetterResult.Yellow => spriteYellow,
                      _                  => spriteGrey,
                  };
                  rt.DOScaleY(1f, flipDuration).SetEase(Ease.OutQuad);
              });

            yield return new WaitForSeconds(flipDelay);
        }

        yield return new WaitForSeconds(flipDuration * 2f + flipDelay * (row.Count - 1));
        onComplete?.Invoke();
    }

    void GreyOutAbsentKeys(string guess, List<LetterResult> results)
    {
        for (int i = 0; i < guess.Length; i++)
            if (results[i] != LetterResult.Grey)
                confirmedPresentLetters.Add(guess[i]);

        for (int i = 0; i < guess.Length; i++)
        {
            if (results[i] != LetterResult.Grey) continue;
            if (confirmedPresentLetters.Contains(guess[i])) continue;

            string absent = guess[i].ToString();
            foreach (GameObject key in keyboardKeys)
            {
                TextMeshProUGUI tmp = key.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp == null || !tmp.text.Equals(absent, System.StringComparison.OrdinalIgnoreCase)) continue;
                Image img = key.GetComponentInChildren<Image>();
                if (img != null) img.color = keyGreyedColor;
                break;
            }
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
                result[i]     = LetterResult.Green;
                answerUsed[i] = true;
            }

        for (int i = 0; i < guess.Length; i++)
        {
            if (result[i] == LetterResult.Green) continue;
            for (int j = 0; j < answer.Length; j++)
                if (!answerUsed[j] && guess[i] == answer[j])
                {
                    result[i]     = LetterResult.Yellow;
                    answerUsed[j] = true;
                    break;
                }
        }
        return result;
    }

    public bool IsValidGuess(string guess) => allowedGuesses.Contains(guess.ToLower());

    void ClearAllPanels()
    {
        foreach (List<GameObject> row in allPanels)
            foreach (GameObject tile in row)
                tile.GetComponentInChildren<TextMeshProUGUI>().text = "";
    }
}
