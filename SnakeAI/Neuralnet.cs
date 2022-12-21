using System;
using System.Collections.Generic;

namespace SnakeAI;

class NeuralNet : IComparable<NeuralNet>
{
    public int[] Layers;
    public float[][,] Weights;
    public float Fitness = 0.0f;
    public int Apples = 0;
    public readonly List<int> Seed = new();
    private readonly Random rnd = new();

    public NeuralNet(int[] layers)
    {
        // Assigning variables
        Layers = layers;
        NewSeed();

        // There is one less weight matrix needed because weights are used between layers.
        Weights = new float[Layers.Length - 1][,];

        for (int i = 0; i < Weights.Length; i++)
            Weights[i] = new float[Layers[i], Layers[i + 1]];

        // Assigning random numbers betweenn -0.5 to 0.5 to the weight matrix
        for (int i = 0; i < Weights.Length; i++)
            for (int j = 0; j < Weights[i].GetLength(0); j++)
                for (int k = 0; k < Weights[i].GetLength(1); k++)
                    Weights[i][j, k] = (float)rnd.NextDouble() - 0.5f;
    }

    public NeuralNet(NeuralNet other)
    {
        // Generate a new seed.
        NewSeed();

        // Copy everything.
        Layers = other.Layers;
        Weights = new float[other.Weights.Length][,];
        for (int i = 0; i < Weights.Length; ++i)
        {
            Weights[i] = new float[other.Weights[i].GetLength(0), other.Weights[i].GetLength(1)];
            Array.Copy(other.Weights[i], Weights[i], other.Weights[i].Length);
        }
    }

    public float[] FeedForward(float[] inputs)
    {
        float[] input = inputs;
        float[] output = new float[0];

        for (int i = 0; i < Weights.Length; i++)
        {
            // output set to the second dimension of the weight's current layer's weight matrix
            output = new float[Weights[i].GetLength(1)];

            for (int k = 0; k < Weights[i].GetLength(1); k++)
            {
                for (int j = 0; j < Weights[i].GetLength(0); j++)
                {
                    // a sum of all the weights to each output is found
                    output[k] += input[j] * Weights[i][j, k];
                }
                // that sum is then placed in an activation function
                output[k] = (float)Math.Tanh(output[k]);
            }
            // for each new layer we feed forward the data by placing it into the input again
            input = output;
        }

        return output;
    }

    public void Mutate(float chance)
    {
        for (int i = 0; i < Weights.Length; i++)
        {
            for (int j = 0; j < Weights[i].GetLength(0); j++)
            {
                for (int k = 0; k < Weights[i].GetLength(1); k++)
                {
                    // assign a temporary value to test for mutation against the given chance parameter
                    float temp = (float)rnd.NextDouble();

                    if (temp <= chance / 4.0f)
                    {
                        Weights[i][j, k] = (float)rnd.NextDouble() - 0.5f;
                    }
                    else if (temp <= chance / 2.0f)
                    {
                        Weights[i][j, k] *= -1.0f;
                    }
                    else if (temp <= chance)
                    {
                        float factor = (float)rnd.NextDouble() * 2.0f;
                        Weights[i][j, k] *= factor;                            
                    }
                }
            }
        }
    }

    public void NewSeed()
    {
        Seed.Add(rnd.Next());
    }

    public int CompareTo(NeuralNet? other)
    {
        if (other == null)
            throw new NullReferenceException();

        if (Fitness > other.Fitness)
            return -1;
        else if (Fitness < other.Fitness)
            return 1;
        else
            return 0;
    }
}
