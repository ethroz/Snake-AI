using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeAI;

static class Program
{
    public const bool Train = false;
    public const int Capacity = 1000;
    public static NeuralNet[] Bots = new NeuralNet[Capacity];
    public static int Running = 1;
    public static int AppleBonus = 10;
    public static Int2 Board = new(10, 10);
    public static Int2 BoardOffset = new(30, 0);
    public static int[] Layers = new int[] { Board.x * Board.y, 50, 1 };
    public static int GenerationNumber = 1;
    public static int NumRounds = 10;

    // Inputs: 3 raycasts (distances), 3 classifications (one for each ray)
    // Outputs: 3 directions from 1 output

    // Fitness calculations:
    // - Collecting apples is good.
    // - Going in circles results in an end of game.
    // - For two bots that win, the one that took a shorter amount of time performed better.
    // - Winning should be worth more than collecting the equivalent number of apples?

    static void Main()
    {
        // Change the font.
        IntPtr hConsoleOutput = ConsoleEx.GetStdHandle(ConsoleEx.STD_OUTPUT_HANDLE);
        CONSOLE_FONT_INFO_EX cfi = new CONSOLE_FONT_INFO_EX();
        cfi.cbSize = (uint)Marshal.SizeOf(cfi);
        cfi.dwFontSize.X = 16; // Width of each character in pixels.
        cfi.dwFontSize.Y = 16; // Height of each character in pixels.
        if (!ConsoleEx.SetCurrentConsoleFontEx(hConsoleOutput, false, ref cfi))
        {
            Console.WriteLine("Error setting console font.");
            Console.ReadKey();
            return;
        }

        if (Train)
        {
            // Maximize the console and restrict the console buffer to that size.
            ConsoleEx.Maximize();
            Console.BufferWidth = Console.WindowWidth;
            Console.BufferHeight = Console.WindowHeight;

            // Create the poulation network.
            for (int j = 0; j < Capacity; ++j)
                Bots[j] = new NeuralNet(Layers);

            // Train the population until the escape key is pressed.
            while (true)
            {
                Console.WriteLine("Generation #" + GenerationNumber);

                // Run the bots in parallel.
                Parallel.For(0, Bots.Length, (j) =>
                {
                    RunNet(j);
                });

                // Calculate statistics.
                var average = 0.0f;
                var max = float.NegativeInfinity;
                var min = float.PositiveInfinity;
                var bestIndex = -1;
                var worstIndex = -1;
                var bestApples = 0;
                for (int j = 0; j < Bots.Length; ++j)
                {
                    if (Bots[j].Fitness > max)
                    {
                        bestIndex = j;
                        max = Bots[j].Fitness;
                        bestApples = Bots[j].Apples;
                    }
                    else if (Bots[j].Fitness < min)
                    {
                        worstIndex = j;
                        min = Bots[j].Fitness;
                    }

                    average += Bots[j].Fitness;
                }
                average /= Bots.Length;
                Console.WriteLine("Average Fitness: " + average);
                Console.WriteLine("Fitness Range: [" + min + ", " + max + "]");
                Console.WriteLine("Most Apples: " + bestApples);
                Console.WriteLine("Watch a replay? (.,Esc)");

                // Check user input.
                var input = Console.ReadKey(true).Key;
                if (input == ConsoleKey.OemPeriod)
                {
                    var (left, top) = Console.GetCursorPosition();
                    RunNet(bestIndex, true);
                    Console.SetCursorPosition(left, top);
                }
                else if (input == ConsoleKey.OemComma)
                {
                    var (left, top) = Console.GetCursorPosition();
                    RunNet(worstIndex, true);
                    Console.SetCursorPosition(left, top);
                }
                else if (input == ConsoleKey.Escape)
                    return;

                // Create the next generation.
                NextGeneration();
            }
        }
        else
        {
            ConsoleKey input;
            do
            {
                // Reset everything.
                Running = 1;
                Console.Clear();

                // The snake game the user will play.
                SnakeGame snakeGame = new(Board);

                // Create a parallel task to get input.
                Task.Run(() =>
                {
                    var ticks = snakeGame.ticks;
                    while (Running != SnakeGame.DEATH_SCORE)
                    {
                        if (snakeGame.ticks != ticks)
                        {
                            var input = Console.ReadKey(true).Key;
                            if (input == ConsoleKey.W && snakeGame.Dir.y != 1)
                            {
                                snakeGame.Dir.y = -1;
                                snakeGame.Dir.x = 0;
                            }
                            else if (input == ConsoleKey.A && snakeGame.Dir.x != 1)
                            {
                                snakeGame.Dir.x = -1;
                                snakeGame.Dir.y = 0;
                            }
                            else if (input == ConsoleKey.S && snakeGame.Dir.y != -1)
                            {
                                snakeGame.Dir.y = 1;
                                snakeGame.Dir.x = 0;
                            }
                            else if (input == ConsoleKey.D && snakeGame.Dir.x != -1)
                            {
                                snakeGame.Dir.x = 1;
                                snakeGame.Dir.y = 0;
                            }
                            else if (input == ConsoleKey.Escape)
                            {
                                Running = SnakeGame.DEATH_SCORE;
                            }

                            ticks = snakeGame.ticks;
                        }
                    }
                });

                // Run the game loop.
                while (Running != SnakeGame.DEATH_SCORE)
                {
                    Thread.Sleep(500);
                    Running = snakeGame.Tick();
                    snakeGame.DrawBoard();
                }

                // Game End.
                Console.WriteLine("\nGAME OVER!");
                Thread.Sleep(1000);
                Console.WriteLine("Try again? (r)");

                // Repeat condition.
                input = Console.ReadKey(true).Key;
            }
            while (input == ConsoleKey.R);
        }
    }

    // Run the bot at \a index for a few trials to avoid the effects of randomness.
    // If \a replayMode is true, renders the game to the console at a watchable speed.
    public static void RunNet(int index, bool replayMode = false)
    {
        // Create some locals.
        float[] input = new float[Layers[0]];
        SnakeGame game;

        // Run for more than one round to reduce the effects of randomness.
        for (int i = 0; i < NumRounds; ++i)
        {
            // Initialize a new game.
            if (replayMode)
                game = new(Board, BoardOffset, Bots[index].Seed[^(NumRounds - i)]);
            else
                game = new(Board, BoardOffset, Bots[index].Seed[^1]);

            // Create a new list of positions.
            List<Int2> positions = new();

            // Run the game until the snake dies.
            while (true)
            {
                // Stop for a bit to make it replayMode.
                if (replayMode)
                {
                    game.DrawBoard();
                    Thread.Sleep(100);
                }

                // Add the current position to the list of positions.
                positions.Add(game.Pos);

                // Set the inputs.

                //Int2 ray1 = game.RayCast(game.Dir ^ -1);
                //Int2 ray2 = game.RayCast(game.Dir);
                //Int2 ray3 = game.RayCast(game.Dir ^ 1);
                //input[0] = ray1.x;
                //input[1] = ray1.y;
                //input[2] = ray2.x;
                //input[3] = ray2.y;
                //input[4] = ray3.x;
                //input[5] = ray3.y;

                //// Normalize the inputs.
                //if (game.Dir.x != 0)
                //{
                //    input[0] /= Board.y;
                //    input[4] /= Board.y;
                //    input[2] /= Board.x;
                //}
                //else
                //{
                //    input[0] /= Board.x;
                //    input[4] /= Board.x;
                //    input[2] /= Board.y;
                //}

                input = game.GetBoard();

                // Process the inputs.
                float[] output = Bots[index].FeedForward(input);

                // Convert the outputs into actions.
                if (output[0] < -1.0f / 3.0f)
                    game.Dir ^= -1;
                else if (output[0] > 1.0f / 3.0f)
                    game.Dir ^= 1;

                // View the response.
                int score = game.Tick();
                
                // Bots going in circles get cancelled.
                if (positions.Contains(game.Pos))
                    score = SnakeGame.DEATH_SCORE;

                // Adjust the snake's fitness.
                Bots[index].Fitness += score;

                // Exit condition.
                if (score == SnakeGame.DEATH_SCORE)
                    break;
                
                // Clear the list of previous positions if an apple has been collected.
                if (score == SnakeGame.APPLE_SCORE)
                {
                    positions.Clear();
                    Bots[index].Apples++;
                }
            }

            if (!replayMode)
                Bots[index].NewSeed();
        }
    }

    public static void NextGeneration()
    {
        ++GenerationNumber;

        // Sort the bots by their fitnesses.
        Array.Sort(Bots);

        // Create children from the best performer.
        for (int i = 0; i < Capacity - 1; ++i)
        {
            Bots[i] = new(Bots[^1]);
            if (i >= Math.Round(19.0 * Capacity / 20.0))
                Bots[i].Mutate(0.5f);
            else if (i >= Math.Round(4.0 * Capacity / 5.0))
                Bots[i].Mutate(2);
            else if (i >= Math.Round(3.0 * Capacity / 5.0))
                Bots[i].Mutate(5);
            else if (i >= Math.Round(Capacity * 3.0 / 10.0))
                Bots[i].Mutate(10);
            else
                Bots[i].Mutate(100);
        }

        // All the bots are new except the last one.
        Bots[^1].Fitness = 0.0f;
        Bots[^1].Apples = 0;
        Bots[^1].NewSeed();
    }
}
