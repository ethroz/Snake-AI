using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;

namespace SnakeAI;

public class NeuralNetwork : IComparable<NeuralNetwork>
{
    /// <summary>
    /// The layers of the neural network.
    /// </summary>
    private Layer[] Layers;

    /// <summary>
    /// The fitness of the neural network.
    /// </summary>
    public float Fitness;

    /// <summary>
    /// Initializes a new instance of the <see cref="NeuralNetwork"/> class.
    /// </summary>
    /// <param name="layers">The layers of the neural network.</param>
    public NeuralNetwork(int[] layers)
    {
        // Create each layer of the network.
        Layers = new Layer[layers.Length - 1];
        for (int i = 0; i < Layers.Length; ++i)
        {
            Layers[i] = new Layer(layers[i], layers[i + 1]);
        }

        Fitness = 0.0f;
    }

    /// <summary>
    /// Copy a neural networks from a parent network.
    /// </summary>
    /// <param name="parent">The parent.</param>
    public NeuralNetwork(NeuralNetwork parent)
    {
        // Copy everything.
        Layers = new Layer[parent.Layers.Length];
        for (int i = 0; i < Layers.Length; ++i)
        {
            Layers[i] = new Layer(parent.Layers[i]);
        }

        // Reset the fitness.
        Fitness = 0.0f;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeuralNetwork"/> class.
    /// </summary>
    /// <param name="weights">The weights to use for the neural network.</param>
    public NeuralNetwork(float[][,] weights)
    {
        // Create each layer of the network.
        Layers = new Layer[weights.Length];
        for (int i = 0; i < Layers.Length; ++i)
        {
            Layers[i] = new Layer(weights[i].GetLength(1), weights[i].GetLength(0));
            Array.Copy(weights[i], Layers[i].Weights, weights[i].Length);
        }

        Fitness = 0.0f;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="NeuralNetwork"/> class.
    /// </summary>
    /// <param name="weightFile">The file to the weights to use for the neural network.</param>
    public NeuralNetwork(string weightFile) : this(WeightsFromFile(weightFile)) { }

    /// <summary>
    /// Feeds forward the inputs through all the layers of the neural network
    /// and returns the output.
    /// </summary>
    public float[] FeedForward(float[] inputs)
    {
        Layers[0].FeedForward(inputs);

        for (int i = 1; i < Layers.Length; ++i)
        {
            Layers[i].FeedForward(Layers[i - 1].Outputs);
        }

        return Layers[^1].Outputs;
    }

    /// <summary>
    /// Mutates the weights of the neural network.
    /// </summary>
    public void Mutate()
    {
        for (int i = 0; i < Layers.Length; i++)
        {
            Layers[i].Mutate();
        }
    }

    public void BackProp(float[] expected)
    {
        Layers[^1].BackPropOutput(expected);

        for (int i = Layers.Length - 2; i >= 0; --i)
        {
            Layers[i].BackPropHidden(Layers[i + 1].Gamma, Layers[i + 1].Weights);
        }

        for (int i = 0; i < Layers.Length; ++i)
        {
            Layers[i].UpdateWeights();
        }
    }

    public static float[][,] WeightsFromFile(string path)
    {
        var lines = File.ReadAllLines(path);
        List<int> layers = new();
        for (int i = 0; i < lines[0].Length; ++i)
        {
            // Is this a digit?
            if (char.IsDigit(lines[0][i]))
            {
                // Get the number of digits in the number.
                int j = i + 1;
                for (; j < lines[0].Length; ++j)
                {
                    // Is this not a digit?
                    if (!char.IsDigit(lines[0][j]))
                    {
                        break;
                    }
                }

                // Parse the number.
                var num = int.Parse(lines[0].Substring(i, j - i));
                layers.Add(num);
                i = j;
            }
        }

        // Setup the weight array.
        float[][,] weights = new float[layers.Count - 1][,];
        for (int i = 0; i < weights.Length; ++i)
        {
            weights[i] = new float[layers[i + 1], layers[i]];
        }

        // Get the weights.
        int w = 0, y = 0;
        for (int i = 1; i < lines.Length; ++i)
        {
            // # lines indicate the start of a new weight matrix.
            if (lines[i] == "#")
            {
                Trace.Assert(y == weights[w].GetLength(0));
                y = 0;
                ++w;
                continue;
            }

            // Go through all the characters in each line.
            int x = 0;
            for (int j = 0; j < lines[i].Length; ++j)
            {
                // Is this the start of a weight?
                if (char.IsDigit(lines[i][j]) || lines[i][j] == '-')
                {
                    int k = j + 1;
                    for (; k < lines[i].Length; ++k)
                    {
                        // Is this the end of a weight?
                        if (lines[i][k] == ' ')
                        {
                            break;
                        }
                    }

                    // Parse the number.
                    var subst = lines[i].Substring(j, k - j);
                    var weight = float.Parse(subst);
                    weights[w][y, x] = weight;
                    ++x;
                    j = k;
                }
            }
            Trace.Assert(x == weights[w].GetLength(1));
            ++y;
        }

        return weights;
    }

    public void WeightsToFile(string path)
    {
        // Specify the layer sizes.
        StringBuilder sb = new("{");
        for (int i = 0; i < Layers.Length; i++)
        {
            sb.Append(Layers[i].numberOfInputs);
            sb.Append(',');
        }
        sb.Append(Layers[^1].numberOfOutputs);
        sb.Append('}');
        sb.AppendLine();

        // Get the weights.
        for (int i = 0; i < Layers.Length; i++)
        {
            for (int j = 0; j < Layers[i].numberOfOutputs; j++)
            {
                for (int k = 0; k < Layers[i].numberOfInputs; k++)
                {
                    sb.Append(Layers[i].Weights[j, k]);
                    sb.Append(' ');
                }
                sb.AppendLine();
            }
            sb.Append('#');
            sb.AppendLine();
        }

        // Write the weights to the file at the given path.
        using StreamWriter file = new(path);
        file.Write(sb.ToString());
    }

    /// <summary>
    /// Compares the fitness of two neural networks.
    /// </summary>
    /// <param name="other">The other neural network.</param>
    /// <returns>An integer indicating the comparison result.</returns>
    public int CompareTo(NeuralNetwork? other)
    {
        if (other == null)
            throw new NullReferenceException();

        if (Fitness > other.Fitness)
            return 1;
        else if (Fitness < other.Fitness)
            return -1;
        else
            return 0;
    }

    /// <summary>
    /// A single layer of the neural network.
    /// </summary>
    private class Layer
    {
        public int numberOfInputs;
        public int numberOfOutputs;
        public float[] Outputs;
        public float[] Inputs;
        public float[,] Weights;
        public float[,] WeightsDelta;
        public float[] Gamma;
        public float[] Error;
        public float LearningRate = 0.0333333f;
        //public float LearningRate = 0.1f;
        //public float LearningRate = 0.333333f;

        private Random random = new();

        public Layer(int numberOfInputs, int numberOfOutputs)
        {
            this.numberOfInputs = numberOfInputs;
            this.numberOfOutputs = numberOfOutputs;

            Inputs = new float[numberOfInputs];
            Outputs = new float[numberOfOutputs];
            Weights = new float[numberOfOutputs, numberOfInputs];
            WeightsDelta = new float[numberOfOutputs, numberOfInputs];
            Gamma = new float[numberOfOutputs];
            Error = new float[numberOfOutputs];

            InitializeWeights();
        }

        /// <summary>
        /// Copy a layer from a parent layer.
        /// </summary>
        /// <param name="parent">The parent.</param>
        public Layer(Layer parent)
        {
            // Copy everything.
            numberOfInputs = parent.numberOfInputs;
            numberOfOutputs = parent.numberOfOutputs;

            Inputs = new float[numberOfInputs];
            Outputs = new float[numberOfOutputs];
            Weights = new float[numberOfOutputs, numberOfInputs];
            WeightsDelta = new float[numberOfOutputs, numberOfInputs];
            Gamma = new float[numberOfOutputs];
            Error = new float[numberOfOutputs];

            // Copy the weights.
            Array.Copy(parent.Weights, Weights, Weights.Length);
        }

        public void InitializeWeights()
        {
            for (int i = 0; i < numberOfOutputs; ++i)
            {
                for (int j = 0; j < numberOfInputs; ++j)
                {
                    Weights[i, j] = random.NextSingle() - 0.5f;
                }
            }
        }

        /// <summary>
        /// Calculates the activation function for a given value.
        /// </summary>
        public float Activation(float value)
        {
            return 1.0f / (1.0f + (float)Math.Exp(-value));
            //return (float)Math.Tanh(value);
        }

        /// <summary>
        /// Calculates the derivative of the activation function for a given
        /// value.
        /// </summary>
        public float Derivative(float value)
        {
            float sig = Activation(value);
            return sig * (1 - sig);
            //return 1 - (value * value);
        }

        /// <summary>
        /// Feeds forward the inputs through the layer.
        /// </summary>
        public float[] FeedForward(float[] inputs)
        {
            this.Inputs = inputs;

            for (int i = 0; i < numberOfOutputs; ++i)
            {
                Outputs[i] = 0;

                for (int j = 0; j < numberOfInputs; ++j)
                {
                    Outputs[i] += inputs[j] * Weights[i, j];
                }

                Outputs[i] = Activation(Outputs[i]);
            }

            return Outputs;
        }

        /// <summary>
        /// Mutates the weights for this layer.
        /// </summary>
        public void Mutate()
        {
            for (int i = 0; i < numberOfOutputs; i++)
            {
                for (int j = 0; j < numberOfInputs; j++)
                {
                    float weight = Weights[i, j];

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

                    Weights[i, j] = weight;
                }
            }
        }

        public void BackPropOutput(float[] expected)
        {
            for (int i = 0; i < numberOfOutputs; ++i)
                Error[i] = Outputs[i] - expected[i];

            for (int i = 0; i < numberOfOutputs; ++i)
                Gamma[i] = Error[i] * Derivative(Outputs[i]);

            for (int i = 0; i < numberOfOutputs; ++i)
            {
                for (int j = 0; j < numberOfInputs; ++j)
                {
                    WeightsDelta[i, j] = Gamma[i] * Inputs[j];
                }
            }
        }

        public void BackPropHidden(float[] gammaForward, float[,] weightsForward)
        {
            for (int i = 0; i < numberOfOutputs; ++i)
            {
                Gamma[i] = 0;

                for (int j = 0; j < gammaForward.Length; ++j)
                {
                    Gamma[i] += gammaForward[j] * weightsForward[j, i];
                }

                Gamma[i] *= Derivative(Outputs[i]);
            }

            for (int i = 0; i < numberOfOutputs; ++i)
            {
                for (int j = 0; j < numberOfInputs; ++j)
                {
                    WeightsDelta[i, j] = Gamma[i] * Inputs[j];
                }
            }
        }

        public void UpdateWeights()
        {
            for (int i = 0; i < numberOfOutputs; ++i)
            {
                for (int j = 0; j < numberOfInputs; ++j)
                {
                    Weights[i, j] -= WeightsDelta[i, j] * LearningRate;
                }
            }
        }
    }
}
