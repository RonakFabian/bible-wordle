using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public string currentWord;
    private string currentGuess = "";
    private const int maxWordLength = 5; // Wordle words are 5 letters

    private List<string> answerList = new List<string>();
    private HashSet<string> allowedGuesses = new HashSet<string>();
    private System.Random rng = new System.Random();

    void Start()
    {
        LoadWordLists();
        PickRandomWord();
    }

    void LoadWordLists()
    {
        // Load answer list
        TextAsset answerAsset = Resources.Load<TextAsset>("Words/wordle-answers-alphabetical");
        if (answerAsset != null)
        {
            string[] answers = answerAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in answers)
            {
                string w = word.Trim().ToLower();
                if (w.Length == maxWordLength)
                    answerList.Add(w);
            }
        }

        // Load allowed guesses
        TextAsset allowedAsset = Resources.Load<TextAsset>("Words/wordle-allowed-guesses");
        if (allowedAsset != null)
        {
            string[] guesses = allowedAsset.text.Split(new[] { '\n', '\r' }, System.StringSplitOptions.RemoveEmptyEntries);
            foreach (var word in guesses)
            {
                string w = word.Trim().ToLower();
                if (w.Length == maxWordLength)
                    allowedGuesses.Add(w);
            }
        }

        // Add all answers to allowed guesses (Wordle allows this)
        foreach (var w in answerList)
            allowedGuesses.Add(w);
    }

    void PickRandomWord()
    {
        if (answerList.Count > 0)
        {
            int idx = rng.Next(answerList.Count);
            currentWord = answerList[idx];
        }
        else
        {
            currentWord = "apple"; // fallback
        }
    }


    // Called by UI alphabet buttons
    public void AddLetter(string letter)
    {
        if (currentGuess.Length < maxWordLength && letter.Length == 1 && char.IsLetter(letter[0]))
        {
            currentGuess += letter.ToLower();
            // Optionally update UI here
        }
    }

    public enum LetterResult
    {
        Grey,
        Yellow,
        Green
    }

    // Returns a list of results for each letter in the guess
    public List<LetterResult> GetGuessResult(string guess, string answer)
    {
        List<LetterResult> result = new List<LetterResult>(new LetterResult[guess.Length]);
        bool[] answerUsed = new bool[answer.Length];

        // First pass: check for correct position (green)
        for (int i = 0; i < guess.Length; i++)
        {
            if (guess[i] == answer[i])
            {
                result[i] = LetterResult.Green;
                answerUsed[i] = true;
            }
        }

        // Second pass: check for correct letter, wrong position (yellow)
        for (int i = 0; i < guess.Length; i++)
        {
            if (result[i] == LetterResult.Green) continue;
            bool found = false;
            for (int j = 0; j < answer.Length; j++)
            {
                if (!answerUsed[j] && guess[i] == answer[j])
                {
                    found = true;
                    answerUsed[j] = true;
                    break;
                }
            }
            result[i] = found ? LetterResult.Yellow : LetterResult.Grey;
        }
        return result;
    }

    // Checks if a guess is valid (in allowed guesses)
    public bool IsValidGuess(string guess)
    {
        return allowedGuesses.Contains(guess.ToLower());
    }

    public void Submit() { }
    public void Backspace() { }
}
