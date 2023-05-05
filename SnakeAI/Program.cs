using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;

namespace SnakeAI;

static class Program
{
    private const int CAPACITY = 500;
    private const int NUM_ROUNDS = 5;
    private const int BOARD_WIDTH = 8;
    private const int NUM_COMP = 4;
    private const int AI_TIME = 100;
    private const int USER_TIME = 300;
    private const int CIRCLE_PENALTY = SnakeGame.MOVE_SCORE * 4;
    private static Random random = new();
    private static SnakeMode Mode = SnakeMode.Train;
    private static TrainMode Train = TrainMode.Population;
    private static int NumNets = 1;
    private static bool Watch = false;
    private static NeuralNetwork[] Bots = new NeuralNetwork[CAPACITY];
    private static int[] Apples = new int[Bots.Length];
    private static int[] Seeds = new int[Bots.Length];
    private readonly static Int2 Board = new(BOARD_WIDTH, BOARD_WIDTH);
    private readonly static Int2 BoardOffset = new(30, 0);
    private readonly static int[] Layers = new int[]
    //{ (2 * BOARD_WIDTH + 1) * (2 * BOARD_WIDTH + 1), 100, 50, 3 };
    { (2 * BOARD_WIDTH + 1) * (2 * BOARD_WIDTH + 1), 3 };
    private static string WeightsFile = "";
    private static int GenerationNumber = 0;
    private static volatile ConsoleKey Input;
    private static readonly Queue<ConsoleKey> Presses = new();
    private static readonly char[] snakeChars = new char[NUM_COMP] { '█', '▓', '▒', '░' };

    // Static asserts.
    private const uint _0 = CAPACITY % NUM_COMP == 0 ? 0 : -1;

    enum SnakeMode
    {
        Play,
        Train,
        Compete,
    }

    enum TrainMode
    {
        Population,
        Deep,
    }

    enum WatchMode
    {
        None,
        Watch,
        Replay,
    }

    private static void Main()
    {
        // Get the original font.
        IntPtr hConsoleOutput = 
            ConsoleEx.GetStdHandle(ConsoleEx.STD_OUTPUT_HANDLE);
        CONSOLE_FONT_INFO_EX original = new CONSOLE_FONT_INFO_EX();
        if (!ConsoleEx.GetCurrentConsoleFontEx(hConsoleOutput, false, original))
        {
            Console.WriteLine("Error getting console font.");
            Console.ReadKey();
            return;
        }

        // Change the font.
        CONSOLE_FONT_INFO_EX cfi = new CONSOLE_FONT_INFO_EX();
        cfi.dwFontSize.X = 16; // Width of each character in pixels.
        cfi.dwFontSize.Y = 16; // Height of each character in pixels.
        if (!ConsoleEx.SetCurrentConsoleFontEx(hConsoleOutput, false, cfi))
        {
            Console.WriteLine("Error setting console font.");
            Console.ReadKey();
            return;
        }

        // Get the path to the weights.
        WeightsFile = Assembly.GetExecutingAssembly().Location;
        var temp = Path.GetDirectoryName(WeightsFile);
        if (temp is null) throw new NullReferenceException();
        WeightsFile = temp;
        WeightsFile += "\\Weights ";
        for (int i = 0; i < Layers.Length - 1; ++i)
        {
            WeightsFile += Layers[i] + ",";
        }
        WeightsFile += Layers[^1] + ".txt";

        // Prompt for the snake mode.
        Console.Write("What would you like to do?\r\n(Train=1|Compete=2|Play=3) ");
        while (true)
        {
            Input = Console.ReadKey().Key;
            switch (Input)
            {
                case ConsoleKey.D1: Mode = SnakeMode.Train; break;
                case ConsoleKey.D2: Mode = SnakeMode.Compete; break;
                case ConsoleKey.D3: Mode = SnakeMode.Play; break;
                default: Console.Write("\r\nInvalid Key "); continue;
            }
            break;
        }

        switch (Mode)
        {
            case SnakeMode.Train:
                // Maximize the console and restrict the buffer to that size.
                ConsoleEx.Maximize();
                Console.BufferWidth = Console.WindowWidth;
                Console.BufferHeight = Console.WindowHeight;

                // Create random neural networks.
                for (int i = 0; i < Bots.Length; ++i)
                {
                    Bots[i] = new(Layers);
                }

                // If there are weights to use.
                if (File.Exists(WeightsFile))
                {
                    Console.Write("Get bot from Save? (y) ");
                    Input = Console.ReadKey().Key;
                    Console.WriteLine();
                    if (Input == ConsoleKey.Y)
                    {
                        // Create a neural net from our weights file.
                        Bots[0] = new(WeightsFile);

                        // Use this bot as the parent for the others.
                        Bots[0].Fitness = 1.0f;
                        --GenerationNumber;
                        NextGeneration();
                    }
                }

                // Get the desired mode.
                ChangeMode();

                // Train until we stop.
                while (true)
                {
                    // Reset the stats.
                    ResetStats();

                    int left, top;
                    switch (Train)
                    {
                        case TrainMode.Population:
                            if (GenerationNumber == 0) ++GenerationNumber;
                            Console.WriteLine("Population #" + GenerationNumber + " (" + NumNets + ")");

                            // Get the cursor position before a replay.
                            (left, top) = Console.GetCursorPosition();

                            if (Watch)
                            {
                                // Run the bots syncrhonously.
                                for (int i = 0; i < Bots.Length / NumNets; ++i)
                                {
                                    RunNets(i * NumNets, NumNets, WatchMode.None);
                                };
                            }
                            else
                            {
                                // Run the bots in parallel.
                                Parallel.For(0, Bots.Length / NumNets, (i) =>
                                {
                                    RunNets(i * NumNets, NumNets, WatchMode.None);
                                });
                            }
                            break;
                        case TrainMode.Deep:
                            Console.WriteLine("Deep Training");

                            // Get the cursor position before a replay.
                            (left, top) = Console.GetCursorPosition();

                            // Train the first neural network.
                            DeepNet(0, true, Watch);
                            break;
                        default:
                            throw new NotImplementedException();
                    }

                    // Reset cursor position after a replay.
                    Console.SetCursorPosition(left, top);

                    // Calculat the stats.
                    var best = CalculateStats();

                    if (Console.KeyAvailable)
                    {
                        Input = Console.ReadKey(true).Key;
                        if (Input == ConsoleKey.Escape)
                        {
                            break;
                        }
                        else if (Input == ConsoleKey.Spacebar)
                        {
                            if (Train == TrainMode.Population)
                            {
                                // Watch the last play through?
                                Console.Write("Watch replay? (y) ");
                                Input = Console.ReadKey().Key;
                                Console.WriteLine();

                                // Check for exit request.
                                if (Input == ConsoleKey.Escape)
                                {
                                    break;
                                }

                                // Check for replay.
                                if (Input == ConsoleKey.Y)
                                {
                                    // Get the cursor position before the replay.
                                    (left, top) = Console.GetCursorPosition();

                                    RunNets(best, NumNets, WatchMode.Replay);

                                    // Reset cursor position after the replay.
                                    Console.SetCursorPosition(left, top);
                                }
                            }

                            // Watch mode tings.
                            Console.Write("Watch Live? (y|n) ");
                            Input = Console.ReadKey().Key;
                            Console.WriteLine();
                            switch (Input)
                            {
                                case ConsoleKey.N: Watch = false; break;
                                case ConsoleKey.Y: Watch = true; break;
                            }

                            // Change mode.
                            if (Input == ConsoleKey.Escape || ChangeMode())
                            {
                                break;
                            }
                        }
                    }

                    // Do we need a new generation?
                    if (Train == TrainMode.Population)
                    {
                        NextGeneration();
                    }
                }

                // Save weights?
                Console.Write("\nSave best weights? (y|n) ");
                Input = Console.ReadKey().Key;
                Console.WriteLine();
                if (Input == ConsoleKey.Y)
                {
                    BestBotFirst();
                    Bots[0].WeightsToFile(WeightsFile);
                    Console.WriteLine("Saved.");
                    Console.ReadKey();
                }
                break;
            case SnakeMode.Compete:
                // There must be weights or else we have nothing to compete against.
                if (!File.Exists(WeightsFile))
                {
                    Console.WriteLine("There is no AI to compete against.");
                    Console.WriteLine("Please train an AI to compete against.");
                    Console.ReadKey();
                    break;
                }

                // Create a neural net from our weights file.
                Bots[0] = new(WeightsFile);

                // Set the console size.
                Console.WindowHeight = Board.y + 2;
                Console.WindowWidth = Board.x + 2 + 6;
                Console.Clear();

                do
                {
                    // The snake game the user and AI will compete on.
                    SnakeGame snakeGame = new(Board, new Int2(), new char[] { '█', '▒' });

                    // Create a parallel task to get input.
                    GetInputParallel();

                    // Run the game loop.
                    int[] score = new int[2];
                    while (score[0] != SnakeGame.WIN_SCORE && score[1] != SnakeGame.WIN_SCORE)
                    {
                        Thread.Sleep(USER_TIME);
                        if (DoUserMove(snakeGame)) break;
                        DoBotMove(snakeGame, 0, 1);
                        score = snakeGame.Tick();
                        if (!snakeGame.Snakes[0].Alive) snakeGame.RespawnSnake(0);
                        if (!snakeGame.Snakes[1].Alive) snakeGame.RespawnSnake(1);
                        snakeGame.DrawBoard();
                    }

                    // Game End.
                    if (score[0] == SnakeGame.WIN_SCORE)
                        Console.WriteLine("\rWINNER!");
                    else if (score[1] == SnakeGame.WIN_SCORE)
                        Console.WriteLine("\rLOSER!");
                    else
                        Console.WriteLine("\rGAME ENDED");
                    Thread.Sleep(500);
                    Console.WriteLine("Try again?");

                    // Repeat condition.
                    Input = Console.ReadKey(true).Key;
                }
                while (Input == ConsoleKey.Y);
                break;
            case SnakeMode.Play:
                // Set the console size.
                Console.WindowHeight = Board.y + 2;
                Console.WindowWidth = Board.x + 2 + 6;
                Console.Clear();

                do
                {
                    // The snake game the user will play.
                    SnakeGame snakeGame = new(Board);

                    // Create a parallel task to get input.
                    GetInputParallel();

                    // Run the game loop.
                    int score = 0;
                    while (score != SnakeGame.WIN_SCORE)
                    {
                        Thread.Sleep(USER_TIME);
                        if (DoUserMove(snakeGame)) break;
                        score = snakeGame.Tick()[0];
                        if (!snakeGame.Snakes[0].Alive) snakeGame.RespawnSnake(0);
                        snakeGame.DrawBoard();
                    }

                    // Game End.
                    if (score == SnakeGame.WIN_SCORE)
                        Console.WriteLine("\rWINNER!");
                    else
                        Console.WriteLine("\rGAME OVER!");
                    Thread.Sleep(500);
                    Console.WriteLine("Try again?");

                    // Repeat condition.
                    Input = Console.ReadKey(true).Key;
                }
                while (Input == ConsoleKey.Y);
                break;
            default: throw new NotImplementedException();
        }

        if (!ConsoleEx.SetCurrentConsoleFontEx(hConsoleOutput, false, original))
        {
            Console.WriteLine("Error resetting console font.");
            Console.ReadKey();
            return;
        }
    }

    private static bool ChangeMode()
    {
        // Change mode.
        Console.Write("Change Mode? (Pop=1|Deep=2) ");
        Input = Console.ReadKey().Key;
        Console.WriteLine();
        switch (Input)
        {
            case ConsoleKey.D1:
                Train = TrainMode.Population;

                // Change number.
                Console.Write("Bot count? (Min=1|Max=2) ");
                Input = Console.ReadKey().Key;
                Console.WriteLine();
                switch (Input)
                {
                    case ConsoleKey.D1: NumNets = 1; break;
                    case ConsoleKey.D2: NumNets = NUM_COMP; break;
                }
                break;
            case ConsoleKey.D2:
                Train = TrainMode.Deep;
                BestBotFirst();
                break;
        }

        // Return true on exit request.
        return Input == ConsoleKey.Escape;
    }

    private static int CalculateStats()
    {
        // Calculate statistics.
        var average = 0.0f;
        var max = float.NegativeInfinity;
        var min = float.PositiveInfinity;
        var bestIndex = -1;
        var maxApples = 0;
        var bestApples = 0;
        for (int i = 0; i < Bots.Length; ++i)
        {
            if (Bots[i].Fitness > max)
            {
                bestIndex = i;
                max = Bots[i].Fitness;
                maxApples = Apples[i];
            }
            if (Bots[i].Fitness < min)
            {
                min = Bots[i].Fitness;
            }
            if (Apples[i] > bestApples)
            {
                bestApples = Apples[i];
            }

            average += Bots[i].Fitness;
        }
        average /= Bots.Length;

        Console.WriteLine("Average Fitness: " + average);
        Console.WriteLine("Fitness Range: [" + min + ", " + max + " " + maxApples + "]");
        Console.WriteLine("Best Apples: " + bestApples);

        if (bestApples == BOARD_WIDTH * BOARD_WIDTH - 1)
        {
            ConsoleEx.Flash();
            Console.Write("Checkpoint reached!\nPress any key to continue...");
            Console.ReadKey(true);
            Console.WriteLine();
            Thread.Sleep(1000);
        }

        return bestIndex;
    }

    private static void GetInputParallel()
    {
        Task.Run(() =>
        {
            while (Input != ConsoleKey.Escape)
            {
                Input = Console.ReadKey(true).Key;
                if (Presses.Count < 2) Presses.Enqueue(Input);
            }
        });
    }

    private static bool DoUserMove(SnakeGame game)
    {
        if (Presses.TryDequeue(out var input))
        {
            Int2 newDir = new();
            switch (input)
            {
                case ConsoleKey.W: newDir.y = 1; break;
                case ConsoleKey.A: newDir.x = -1; break;
                case ConsoleKey.S: newDir.y = -1; break;
                case ConsoleKey.D: newDir.x = 1; break;
                case ConsoleKey.Escape: return true;
            }
            if (newDir != new Int2() &&
                newDir + game.Snakes[0].Dir != new Int2())
                game.Snakes[0].Dir = newDir;
        }
        return false;
    }

    private static int GetBestMove(SnakeGame game, int botIndex, int snakeIndex)
    {
        // Transform the apple 1D positions into 2D positions.
        Int2[] apples = new Int2[game.Apples.Length];
        for (int i = 0; i < apples.Length; ++i)
        {
            apples[i] = game.IntToInt2(game.Apples[i]);
        }

        // Perform a breadth first search to get the closest apple.
        Queue<Int2> queue = new();
        Dictionary<Int2, Int2> map = new(); // Previous, current.
        HashSet<Int2> visited = new();

        // Enqueue the snake head as the starting point.
        var pos = game.Snakes[snakeIndex].Pos;
        var snakeDir = game.Snakes[snakeIndex].Dir;
        queue.Enqueue(pos);
        map.Add(pos, pos - snakeDir);
        visited.Add(queue.Peek());

        // Search while the queue is not empty.
        while (queue.Count > 0)
        {
            // Get the front cell.
            var current = queue.Dequeue();

            // If the current cell is an apple cell.
            if (game.GetTileState(current) > 0)
            {
                // Until we find our way back to the snake head.
                var previous = current;
                while (current != pos)
                {
                    previous = current;
                    current = map[previous];
                }

                // Calculate the move corresponding to the correct direction.
                var dir = previous - current;
                for (int j = -1; j <= 1; ++j)
                {
                    if (snakeDir * j == dir)
                    {
                        return j;
                    }
                }

                // We should not end here.
                Trace.Assert(false);
            }

            // Our current direction is the difference between this location and the last. 
            var currDir = current - map[current];

            // Try each direction.
            int it = currDir.y == 0 ? 1 : 4;
            for (int i = 0; i < 3; ++i)
            {
                var next = current + currDir * it;

                // If the next cell is within bounds, not a snake, and not visited.
                if (game.WithinBounds(next) && game.GetTileState(next) >= 0 && !visited.Contains(next))
                {
                    // Enqueue it and mark it as visited.
                    queue.Enqueue(next);
                    visited.Add(next);

                    // Also leave a trail from the next cell back to the previous cell.
                    map.Add(next, current);
                }

                it = (it + 2) % 5;
            }
        }

        // Otherwise, just go straight.
        return 0;
    }

    private static void TrainBestMove(int botIndex, int bestMove)
    {
        float[] expected = new float[Layers[^1]];
        expected[bestMove + 1] = 1.0f;
        //expected[0] = bestMove;
        Bots[botIndex].BackProp(expected);
    }

    private static void DoBotMove(SnakeGame game, int botIndex, int snakeIndex)
    {
        // Get the current board state.
        var inputs = game.GetRelativeBoard(snakeIndex);

        /*
        Int2 ray1 = game.RayCast(game.Dir ^ -1);
        Int2 ray2 = game.RayCast(game.Dir);
        Int2 ray3 = game.RayCast(game.Dir ^ 1);
        input[0] = ray1.x;
        input[1] = ray1.y;
        input[2] = ray2.x;
        input[3] = ray2.y;
        input[4] = ray3.x;
        input[5] = ray3.y;

        // Normalize the inputs.
        if (game.Dir.x != 0)
        {
            input[0] /= Board.y;
            input[4] /= Board.y;
            input[2] /= Board.x;
        }
        else
        {
            input[0] /= Board.x;
            input[4] /= Board.x;
            input[2] /= Board.y;
        }
        */

        // Process the inputs.
        float[] output = Bots[botIndex].FeedForward(inputs);

        // Convert the outputs into actions.
        if (output[0] > output[1] && output[0] > output[2])
        //if (output[0] < -1.0f / 3.0f)
        {
            // Turn left.
            game.Snakes[snakeIndex].Dir *= -1;
        }
        else if (output[2] > output[1] && output[2] > output[0])
        //else if (output[0] > 1.0f / 3.0f)
        {
            // Turn right.
            game.Snakes[snakeIndex].Dir *= 1;
        }
    }

    // Run the bot at \a index for a few trials to avoid the effects of
    // randomness. If \a replayMode is true, renders the game to the console at
    // a watchable speed.
    private static void DeepNet(int index, bool train, bool watch)
    {
        // Get a new seed for a new game.
        Seeds[index] = random.Next();

        // Initialize a new game.
        SnakeGame game = new(Board, BoardOffset, snakeChars[0].ToString().ToArray(), Seeds[index]);

        // Run the game until the snake dies.
        while (game.Snakes[0].Alive)
        {
            // Display the board at a reasonable pace to watch the gameplay.
            if (watch)
            {
                game.DrawBoard();
                Thread.Sleep(AI_TIME);
            }

            // Check for no snake.
            if (game.Snakes[0].Positions.Count == 0)
            {
                game.Tick();
                continue;
            }

            // Compute the best move.
            int bestMove = GetBestMove(game, index, 0);

            // Compute the bot's move.
            DoBotMove(game, index, 0);

            // Train the bot for next time.
            if (train)
                TrainBestMove(index, bestMove);

            // Get the result of the move.
            int score = game.Tick()[0];

            Bots[index].Fitness += score;

            // Exit condition.
            if (score == SnakeGame.DEATH_SCORE)
            {
                game.KillSnake(0);
            }
            else if (score == SnakeGame.APPLE_SCORE)
            {
                Apples[index]++;
            }
        }
    }

    // Run the bot at \a index for a few trials to avoid the effects of
    // randomness. If \a replayMode is true, renders the game to the console at
    // a watchable speed.
    private static void RunNets(int bot1, int numSnakes, WatchMode mode)
    {
        // Get a new seed for a new game.
        if (mode != WatchMode.Replay)
            Seeds[bot1] = random.Next();

        // Initialize a new game with the given number of snakes.
        List<char> chars = new();
        for (int i = 0; i < numSnakes; ++i)
        {
            chars.Add(snakeChars[i]);
        }
        SnakeGame game = new(Board, BoardOffset, chars.ToArray(), Seeds[bot1]);

        // Keep track of previously visited positions.
        var positions = new HashSet<Int2>[numSnakes];
        for (int i = 0; i < numSnakes; ++i)
        {
            positions[i] = new();
        }

        // Count the number of apples in each round.
        var apples = new int[numSnakes];

        // Run for more than one round to reduce the effects of randomness.
        for (int i = 0; i < NUM_ROUNDS; ++i)
        {
            // Run the game until all snakes die.
            var alive = numSnakes;
            while (alive > 0 && game.Ticks < BOARD_WIDTH * BOARD_WIDTH * 2)
            {
                // Display the mode at a reasonable pace to watch the gameplay.
                if (mode != WatchMode.None)
                {
                    game.DrawBoard();
                    Thread.Sleep(AI_TIME);
                }

                // Allow all bots to play.
                for (int j = 0; j < numSnakes; ++j)
                {
                    // Check for no snake.
                    if (!game.Snakes[j].Alive)
                    {
                        continue;
                    }

                    // Add the position to the set.
                    positions[j].Add(game.Snakes[j].Pos);

                    // Compute the bot's move.
                    DoBotMove(game, bot1 + j, j);
                }

                // Get the results.
                var scores = game.Tick();

                // Allow all bots to play.
                for (int j = 0; j < numSnakes; ++j)
                {
                    // Going in circles in penalized.
                    if (positions[j].Contains(game.Snakes[j].Pos))
                    {
                        Bots[bot1 + j].Fitness += CIRCLE_PENALTY;
                    }

                    // Add the score to the bot's fitness.
                    Bots[bot1 + j].Fitness += scores[j];

                    // Did the snake die?
                    if (scores[j] == SnakeGame.DEATH_SCORE)
                    {
                        positions[j].Clear();
                        --alive;
                    }
                    else if (scores[j] == SnakeGame.APPLE_SCORE || scores[j] == SnakeGame.WIN_SCORE)
                    {
                        // Clear the list of previous positions if an apple has been
                        // collected.
                        positions[j].Clear();
                        game.Ticks = 0;
                        apples[j]++;
                    }
                }
            }

            // Reset the snakes for the next round.
            for (int j = 0; j < numSnakes; ++j)
            {
                // Respawn the snakes.
                game.RespawnSnake(j);

                // Clear the previous positions.
                positions[j].Clear();

                // Keep the largest number of apples.
                Apples[bot1 + j] = Math.Max(Apples[bot1 + j], apples[j]);
                apples[j] = 0;
            }
            game.Ticks = 0;
        }
    }

    private static void ResetStats()
    {
        // Reset the fitnesses.
        for (int i = 0; i < Bots.Length; ++i)
        {
            Bots[i].Fitness = 0.0f;
        }

        // Reset the apple counts.
        Apples = new int[Bots.Length];
    }

    private static void BestBotFirst()
    {
        // Find the best bot.
        var largest = 0;
        for (int i = 0; i < Bots.Length; ++i)
        {
            if (Bots[i].CompareTo(Bots[largest]) > 0)
            {
                largest = i;
            }
        }

        // Swap the largest with the first bot.
        (Bots[largest], Bots[0]) = (Bots[0], Bots[largest]);
    }

    private static void NextGeneration()
    {
        ++GenerationNumber;

        BestBotFirst();

        // Create children from the best performer.
        for (int i = 1; i < Bots.Length; ++i)
        {
            Bots[i] = new(Bots[0]);
            Bots[i].Mutate();
        }
    }
}
