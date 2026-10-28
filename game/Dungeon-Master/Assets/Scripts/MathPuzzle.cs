using System.Collections.Generic;
using UnityEngine;

// Step 3: generates a small math question with 3 answer options.
// (Gameplay-driven questions arrive in step 12.)
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
        int op = Random.Range(0, 3);
        int a, b, answer;
        string text;
        if (op == 0)
        {
            a = Random.Range(2, 20);
            b = Random.Range(2, 20);
            answer = a + b;
            text = $"{a} + {b} = ?";
        }
        else if (op == 1)
        {
            a = Random.Range(5, 25);
            b = Random.Range(2, a);
            answer = a - b;
            text = $"{a} - {b} = ?";
        }
        else
        {
            a = Random.Range(2, 10);
            b = Random.Range(2, 10);
            answer = a * b;
            text = $"{a} x {b} = ?";
        }

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
