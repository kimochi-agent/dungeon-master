using System.Collections.Generic;
using UnityEngine;

// Step 3: generates a small math question with 3 answer options.
// Step 12: half the questions are built from real gameplay stats.
public static class MathPuzzle
{
    public struct Question
    {
        public string text;
        public string[] options;
        public int correctIndex;
    }

    public static Question Generate()
    {
        int mode = Random.Range(0, 4);
        int op = Random.Range(0, 3);
        if (mode == 0 && GameState.Coins > 0)
        {
            int b = Random.Range(1, 11);
            if (op == 0)
                return Build($"{GameState.Coins} + {b} = ?", GameState.Coins + b);
            else if (op == 1)
                return Build($"{GameState.Coins} - {b} = ?", GameState.Coins - b);
            else
                return Build($"{GameState.Coins} x {b} = ?", GameState.Coins * b);
        }
        if (mode == 1 && GameState.Kills > 0)
        {
            int b = Random.Range(2, 6);
            if (op == 0)
                return Build($"{GameState.Kills} + {b} = ?", GameState.Kills + b);
            else if (op == 1)
                return Build($"{GameState.Kills} - {b} = ?", GameState.Kills - b);
            else
                return Build($"{GameState.Kills} x {b} = ?", GameState.Kills * b);
        }
        if (mode == 2 && GameState.DamageTaken > 0)
        {
            int b = Random.Range(1, Mathf.Min(GameState.DamageTaken, 20) + 1);
            if (op == 0)
                return Build($"{GameState.DamageTaken} + {b} = ?", GameState.DamageTaken + b);
            else if (op == 1)
                return Build($"{GameState.DamageTaken} - {b} = ?", GameState.DamageTaken - b);
            else
                return Build($"{GameState.DamageTaken} x {b} = ?", GameState.DamageTaken * b);
        }
        return Plain();
    }

    private static Question Plain()
    {
        int op = Random.Range(0, 3);
        if (op == 0)
        {
            int a = Random.Range(2, 20), b = Random.Range(2, 20);
            return Build($"{a} + {b} = ?", a + b);
        }
        if (op == 1)
        {
            int a = Random.Range(5, 25), b = Random.Range(2, a);
            return Build($"{a} - {b} = ?", a - b);
        }
        int x = Random.Range(2, 10), y = Random.Range(2, 10);
        return Build($"{x} x {y} = ?", x * y);
    }

    private static Question Build(string text, int answer)
    {
        // 3 unique non-negative options including the answer.
        HashSet<int> used = new HashSet<int> { answer };
        List<int> options = new List<int> { answer };
        while (options.Count < 3)
        {
            int distractor = answer + Random.Range(-10, 11);
            if (distractor < 0 || !used.Add(distractor))
                continue;
            options.Add(distractor);
        }

        // Shuffle.
        for (int i = 0; i < options.Count; i++)
        {
            int j = Random.Range(i, options.Count);
            (options[i], options[j]) = (options[j], options[i]);
        }

        string[] texts = new string[3];
        int correct = 0;
        for (int i = 0; i < 3; i++)
        {
            texts[i] = options[i].ToString();
            if (options[i] == answer)
                correct = i;
        }
        return new Question { text = text, options = texts, correctIndex = correct };
    }
}
