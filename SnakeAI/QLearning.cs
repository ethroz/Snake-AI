using System;
using System.Collections.Generic;

namespace SnakeAI;

static class QLearning
{
    static readonly Random rnd = new();
    const float BAD = -0.1f;
    const float GOOD = 10.0f;

    public static void Run()
    {
        Console.WriteLine("Begin quality-learning maze demo");
        Console.WriteLine("Setting up maze and rewards");
        int ns = 12;
        int[,] allowed = CreateMaze(ns);
        double[,] quality = CreateQuality(ns);
        Console.WriteLine("Analyzing maze using quality-learning");
        int start = 1;
        int goal = 11;
        double gamma = 0.5;
        double learnRate = 0.5;
        int maxEpochs = 100;
        Train(allowed, quality, goal, gamma, learnRate, maxEpochs);
        Console.WriteLine("Done. quality matrix: ");
        Print(quality);
        Console.WriteLine("Using quality to walk from cell {0} to {1}", start, goal);
        Walk(start, goal, quality);
        Console.WriteLine("End demo");
        Console.ReadLine();
    }

    static void Print(double[,] arr)
    {
        int ns = arr.GetLength(0);
        string output = "";
        for (int i = 0; i < ns; ++i)
        {
            for (int j = 0; j < ns; ++j)
            {
                output += arr[i, j].ToString("F2").PadLeft(5);
                output += " ";
            }
            output += Environment.NewLine;
        }
        Console.Write(output);
    }

    static void Print(int[,] arr)
    {
        int ns = arr.GetLength(0);
        string output = "";
        for (int i = 0; i < ns; ++i)
        {
            for (int j = 0; j < ns; ++j)
            {
                output += arr[i, j].ToString();
                output += " ";
            }
            output += "\n";
        }
        Console.Write(output);
    }

    static int[,] CreateMaze(int ns)
    {
        int[,] FT = new int[ns, ns];
        FT[0, 1] = FT[0, 4] = FT[1, 0] = FT[1, 5] = FT[2, 3] = 1;
        FT[2, 6] = FT[3, 2] = FT[3, 7] = FT[4, 0] = FT[4, 8] = 1;
        FT[5, 1] = FT[5, 6] = FT[5, 9] = FT[6, 2] = FT[6, 5] = 1;
        FT[6, 7] = FT[7, 3] = FT[7, 6] = FT[7, 11] = FT[8, 4] = 1;
        FT[8, 9] = FT[9, 5] = FT[9, 8] = FT[9, 10] = FT[10, 9] = 1;
        FT[11, 11] = 1;  // Goal
        return FT;
    }

    static double[,] CreateQuality(int ns)
    {
        double[,] quality = new double[ns, ns];
        return quality;
    }

    static List<int> GetPossNextStates(int s, int[,] FT)
    {
        List<int> result = new List<int>();
        for (int j = 0; j < FT.GetLength(0); ++j)
            if (FT[s, j] == 1) result.Add(j);
        return result;
    }

    static int GetRandNextState(int s, int[,] FT)
    {
        List<int> possNextStates = GetPossNextStates(s, FT);
        int ct = possNextStates.Count;
        int idx = rnd.Next(0, ct);
        return possNextStates[idx];
    }

    static void Train(int[,] allowed, double[,] quality, int goal, double gamma, double lrnRate, int maxEpochs)
    {
        for (int epoch = 0; epoch < maxEpochs; ++epoch)
        {
            int currState = rnd.Next(0, allowed.GetLength(0));
            while (true)
            {
                int nextState = GetRandNextState(currState, allowed);
                List<int> possNextNextStates = GetPossNextStates(nextState, allowed);
                double maxQ = double.MinValue;
                for (int j = 0; j < possNextNextStates.Count; ++j)
                {
                    int nns = possNextNextStates[j];  // short alias
                    double qual = quality[nextState, nns];
                    if (qual > maxQ) maxQ = qual;
                }
                quality[currState, nextState] = ((1 - lrnRate) * quality[currState, nextState]) + (lrnRate * ((currState == goal ? GOOD : BAD) + (gamma * maxQ)));
                currState = nextState;
                if (currState == goal) break;
            } // while
        } // for
    } // Train

    static void Walk(int start, int goal, double[,] quality)
    {
        int curr = start; int next;
        Console.Write(curr + "->");
        while (curr != goal)
        {
            next = ArgMax(quality, curr);
            Console.Write(next + "->");
            curr = next;
        }
        Console.WriteLine("done");
    }

    static int ArgMax(double[,] vector, int row)
    {
        double maxVal = vector[row, 0]; int idx = 0;
        for (int i = 0; i < vector.GetLength(0); ++i)
        {
            if (vector[row, i] > maxVal)
            {
                maxVal = vector[row, i]; idx = i;
            }
        }
        return idx;
    }
} // Program
// ns