using System;

namespace SnakeAI;

class PopulationNetwork : IComparable<PopulationNetwork>
{
    /// <summary>
    /// The layers of the neural network.
    /// </summary>
    public int[] Layers;

    /// <summary>
    /// The weights of the neural network.
    /// </summary>
    public float[][,] Weights;

    /// <summary>
    /// The fitness of the neural network.
    /// </summary>
    public float Fitness = 0.0f;

    private readonly Random random = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PopulationNetwork"/> class.
    /// </summary>
    /// <param name="layers">The layers of the neural network.</param>
    public PopulationNetwork(int[] layers)
    {
        // Assigning variables
        Layers = layers;

        // There is one less weight matrix needed because weights are used
        // between layers.
        Weights = new float[Layers.Length - 1][,];

        for (int i = 0; i < Weights.Length; i++)
            Weights[i] = new float[Layers[i], Layers[i + 1]];

        // Assigning random numbers between -0.5 to 0.5 to the weight matrix
        for (int i = 0; i < Weights.Length; i++)
            for (int j = 0; j < Weights[i].GetLength(0); j++)
                for (int k = 0; k < Weights[i].GetLength(1); k++)
                    Weights[i][j, k] = (float)random.NextDouble() - 0.5f;
    }

    /// <summary>
    /// Create a new neural networks from a parent network.
    /// </summary>
    /// <param name="parent">The second parent.</param>
    public PopulationNetwork(PopulationNetwork parent)
    {
        // Copy everything.
        Layers = parent.Layers;
        Weights = new float[parent.Weights.Length][,];
        for (int i = 0; i < Weights.Length; ++i)
        {
            Weights[i] = 
                new float[
                    parent.Weights[i].GetLength(0), 
                    parent.Weights[i].GetLength(1)];
            Array.Copy(parent.Weights[i], Weights[i], parent.Weights[i].Length);
        }
    }

    /// <summary>
    /// Calculates the activation function for a given input.
    /// </summary>
    public float Activation(float input)
    {
        return 1.0f / (1.0f + (float)Math.Exp(-input));
    }

    /// <summary>
    /// Feeds forward the inputs through the neural network and returns the
    /// output.
    /// </summary>
    public float[] FeedForward(float[] inputs)
    {
        float[] input = inputs;
        float[] output = new float[0];

        for (int i = 0; i < Weights.Length; i++)
        {
            // output set to the second dimension of the weight's current
            // layer's weight matrix
            output = new float[Weights[i].GetLength(1)];

            for (int k = 0; k < Weights[i].GetLength(1); k++)
            {
                for (int j = 0; j < Weights[i].GetLength(0); j++)
                {
                    // a sum of all the weights to each output is found
                    output[k] += input[j] * Weights[i][j, k];
                }
                // that sum is then placed in an activation function
                output[k] = Activation(output[k]);
            }
            // for each new layer we feed forward the data by placing it into
            // the input again
            input = output;
        }

        return output;
    }

    /// <summary>
    /// Mutates the weights of the neural network.
    /// </summary>
    public void Mutate()
    {
        for (int i = 0; i < Weights.Length; i++)
        {
            for (int j = 0; j < Weights[i].GetLength(0); j++)
            {
                for (int k = 0; k < Weights[i].GetLength(1); k++)
                {
                    float weight = Weights[i][j, k];

                    // Chances of weight mutation.
                    float randomNumber = random.NextSingle() * 1000.0f;
                    if (randomNumber <= 2.0f)
                    {
                        // Flip sign of weight.
                        weight *= -1f;
                    }
                    else if (randomNumber <= 4.0f)
                    {
                        // Pick a random weight between -1 and 1.
                        weight = (random.NextSingle() - 0.5f) * 2.0f;
                    }
                    else if (randomNumber <= 6.0f)
                    {
                        // Randomly increase current weight.
                        float factor = random.NextSingle() + 1.0f;
                        weight *= factor;
                    }
                    else if (randomNumber <= 8.0f)
                    {
                        // Randomly decrease current weight.
                        float factor = random.NextSingle();
                        weight *= factor;
                    }

                    Weights[i][j,k] = weight;
                }
            }
        }
    }

    /// <summary>
    /// Compares the fitness of two neural networks.
    /// </summary>
    /// <param name="other">The other neural network.</param>
    /// <returns>An integer indicating the comparison result.</returns>
    public int CompareTo(PopulationNetwork? other)
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
