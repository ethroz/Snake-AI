using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace SnakeAI;

// The apples need a list of available positions to spawn.
// The snake is stored as a list of indices representing position that expands as the snake does.
// The result of these constraints is a list of available points, and a list of indices that track
// which points are where.
// As the snake moves, it will loop a pointer over all of its indices. It will overwrite the tail
// with the next position that the snake moves to.

public class SnakeGame
{
    private readonly Random rnd = new();

    /// <summary>
    /// The offset of the board in the console window.
    /// </summary>
    public readonly Int2 Offset;

    /// <summary>
    /// The dimensions of the board.
    /// </summary>
    public readonly Int2 Dims;

    /// <summary>
    /// The total number of tiles on the board.
    /// </summary>
    public readonly int Size;

    /// <summary>
    /// The snakes on the board.
    /// </summary>
    public readonly Snake[] Snakes;

    /// <summary>
    /// The list of randomly assorted positions that are unoccupied on the board.
    /// </summary>
    private readonly List<int> AvailablePoints;

    /// <summary>
    /// The array that maps each position to its index in the available points list.
    /// </summary>
    private readonly int[] AvailableIndices;

    /// <summary>
    /// The array of the 1D positions of all the apples on the 2D board.
    /// </summary>
    public readonly int[] Apples = new int[NUM_APPLES];

    /// <summary>
    /// The number of elapsed game ticks since the start of the game.
    /// </summary>
    public long Ticks = 0;

    public const int NUM_APPLES = 3;
    public const int MOVE_SCORE = 0;
    public const int DEATH_SCORE = -100;
    public const int APPLE_SCORE = 20;
    public const int WIN_SCORE = 10000;
    public const int SNAKE = -1;
    public const int APPLE = -2;

    public SnakeGame(Int2 size, Int2 offset, char[] snakeChars, int seed = int.MinValue)
    {
        // Determine board specifics.
        Dims = size;
        Size = Dims.x * Dims.y;
        Offset = offset;

        // Use any provided seed.
        if (seed > int.MinValue)
            rnd = new(seed);

        // Create the array of available points.
        AvailableIndices = new int[Size];
        for (int i = 0; i < Size; ++i)
            AvailableIndices[i] = i;
        AvailablePoints = new(AvailableIndices);

        // Make sure all the characters are unique.
        HashSet<char> set = new();
        for (int i = 0; i < snakeChars.Length; ++i)
        {
            // Check for duplicates.
            if (set.Contains(snakeChars[i]))
            {
                throw new ArgumentException();
            }

            // Add the item.
            set.Add(snakeChars[i]);
        }

        // Make all the snakes.
        Snakes = new Snake[snakeChars.Length];
        for (int i = 0; i < Snakes.Length; ++i)
        {
            // Create a new snake.
            Snakes[i] = new(this, snakeChars[i]);

            // Spawn the snake.
            NewSnake(i);
        }

        // Spawn apples around the board.
        for (int i = 0; i < NUM_APPLES; ++i)
            NewApple(i);
    }

    public SnakeGame(Int2 size) : this(size, new(0, 0), new char[] { '█' }) {}

    private int GetRandomTile(int owner)
    {
        // Check for available points.
        if (AvailablePoints.Count == 0)
        {
            return -1;
        }

        // Get a random position.
        var pos = AvailablePoints[rnd.Next(AvailablePoints.Count)];

        // Get the indices of the given point and the last point.
        var index = AvailableIndices[pos];
        var end = AvailablePoints.Count - 1;

        // Replace the given point with the last point.
        AvailablePoints[index] = AvailablePoints[end];

        // Update the indices of the two points.
        AvailableIndices[AvailablePoints[index]] = index;
        AvailableIndices[pos] = owner;

        // Finally, remove the last point.
        AvailablePoints.RemoveAt(end);

        return pos;
    }

    public void RespawnSnake(int index)
    {
        KillSnake(index);
        NewSnake(index);
        Snakes[index].Alive = true;
    }

    private bool NewSnake(int index)
    {
        // Get a random tile.
        var pos = GetRandomTile(SNAKE);
        if (pos >= 0)
        {
            // We can only use it if there is one available.
            Snakes[index].Respawn(pos);
        }

        return pos >= 0;
    }

    private void NewApple(int index)
    {
        // Get a random tile.
        Apples[index] = GetRandomTile(APPLE);
    }

    public void KillSnake(int index)
    {
        // Free up the tiles occupied by the snake.
        for (int i = 0; i < Snakes[index].Positions.Count; ++i)
        {
            // Add each position from the snake back to the available points.
            int tempPos = Snakes[index].Positions[i];
            AvailablePoints.Add(tempPos);
            AvailableIndices[tempPos] = AvailablePoints.Count - 1;
        }

        // Kill the snake.
        Snakes[index].Kill();
    }

    public int Int2ToInt(Int2 pos)
    {
        return pos.y * Dims.x + pos.x;
    }

    public Int2 IntToInt2(int pos)
    {
        return new(pos % Dims.x, pos / Dims.x);
    }

    public bool WithinBounds(Int2 pos)
    {
        return pos.x < Dims.x && pos.x >= 0 && pos.y < Dims.y && pos.y >= 0;
    }

    public int GetTileState(Int2 pos)
    {
        int type = AvailableIndices[Int2ToInt(pos)];
        if (type >= 0)
            return 0;
        else if (type == SNAKE)
            return SNAKE;
        else
            return 1;
    }

    public int[] Tick()
    {
        // Keep track of each snake's score.
        int[] scores = new int[Snakes.Length];

        // Calculate the next positions.
        for (int i = 0; i < Snakes.Length; ++i)
        {
            // Is the snake alive?
            if (!Snakes[i].Alive)
            {
                continue;
            }

            // The snakes should lose score on every move.
            scores[i] = MOVE_SCORE;

            // Progress the snakes in their direction.
            Snakes[i].Pos += Snakes[i].Dir;

            // Check for collision with border.
            if (!WithinBounds(Snakes[i].Pos))
            {
                scores[i] = DEATH_SCORE;
                Snakes[i].Alive = false;
            }
        }

        // Get all overlapping heads.
        Dictionary<Int2, int> freq = new();
        for (int i = 0; i < Snakes.Length; ++i)
        {
            // Is the snake alive?
            if (!Snakes[i].Alive)
            {
                continue;
            }

            // Check this snake against the rest.
            if (freq.ContainsKey(Snakes[i].Pos))
            {
                freq[Snakes[i].Pos]++;
            }
            else
            {
                freq.Add(Snakes[i].Pos, 1);
            }
        }

        // Check for head-on collisions.
        for (int i = 0; i < Snakes.Length; ++i)
        {
            // Is the snake alive?
            if (!Snakes[i].Alive)
            {
                continue;
            }

            // If the number of snake heads is higher than 1, then we have a collision.
            if (freq[Snakes[i].Pos] > 1)
            {
                scores[i] = DEATH_SCORE;
                Snakes[i].Alive = false;
            }
        }

        // Move the snakes to the next positions.
        for (int i = 0; i < Snakes.Length; ++i)
        {
            // Is the snake alive?
            if (!Snakes[i].Alive)
            {
                continue;
            }

            // Calculate the changes to the snake.
            var oldTailIndex = Snakes[i].Indices[Snakes[i].Head];
            var oldTailPos = Snakes[i].Positions[oldTailIndex];
            var newHeadPos = Int2ToInt(Snakes[i].Pos);

            // Check for self collision.
            var tile = AvailableIndices[newHeadPos];
            if (tile == SNAKE)
            {
                scores[i] = DEATH_SCORE;
                Snakes[i].Alive = false;
                continue;
            }

            // Check for apple collision.
            if (tile == APPLE)
            {
                int index = -1;
                for (int j = 0; j < NUM_APPLES; ++j)
                {
                    if (Apples[j] == newHeadPos)
                    {
                        index = j;
                        break;
                    }
                }
                Trace.Assert(index != -1);

                // Generate a new apple.
                NewApple(index);

                // Grow the snake.
                Snakes[i].Indices.Add(oldTailIndex);
                Snakes[i].Indices[Snakes[i].Head] = Snakes[i].Positions.Count;
                Snakes[i].Positions.Add(newHeadPos);

                // Set the tile as occupied by a snake.
                AvailableIndices[newHeadPos] = SNAKE;

                // Change the score.
                scores[i] = APPLE_SCORE;
            }
            else
            {
                // Update the old tail position and the available points.
                Snakes[i].Positions[oldTailIndex] = newHeadPos;
                AvailablePoints[AvailableIndices[newHeadPos]] = oldTailPos;
                AvailableIndices[oldTailPos] = AvailableIndices[newHeadPos];
                AvailableIndices[newHeadPos] = SNAKE;
            }

            // Move the head forwards.
            Snakes[i].Head = Snakes[i].Indices[Snakes[i].Head];
        }

        // Check for snake deaths.
        for (int i = 0; i < Snakes.Length; ++i)
        {
            // Is the snake dead and still on the board?
            if (!Snakes[i].Alive && Snakes[i].Positions.Count > 0)
            {
                // Kill the snake.
                KillSnake(i);
            }
        }

        // Check for the win condition.
        int sum = 0;
        for (int i = 0; i < Snakes.Length; ++i)
        {
            sum += Snakes[i].Positions.Count;
        }
        if (Size == sum)
        {
            for (int i = 0; i < Snakes.Length; ++i)
            {
                if (Snakes[i].Alive)
                {
                    scores[i] = WIN_SCORE;
                    KillSnake(i);
                }
            }

            // Spawn the apples back in.
            for (int i = 0; i < Apples.Length; ++i)
            {
                NewApple(i);
            }
        }

        // Increment the ticker and return the scores.
        ++Ticks;
        return scores;
    }

    // Cast a ray in the relative direction from the give snake's.
    // Returns the number of tiles and the tile type.
    public (int, int) RayCast(int relDir, int snakeIndex)
    {
        // Check along the direction of the ray for a wall or an object.
        Int2 rayDir = Snakes[snakeIndex].Dir * relDir;
        Int2 currentPos = Snakes[snakeIndex].Pos + rayDir;
        int count = 0;
        while (true)
        {
            if (!WithinBounds(currentPos))
                return (count, 1);
            var tile = AvailableIndices[Int2ToInt(currentPos)];
            if (tile < 0)
                return (count, tile);
            currentPos += rayDir;
            ++count;
        }
    }

    public float[] GetRelativeBoard(int snakeIndex)
    {
        // Create a grid that can hold the board with the given snake in the middle.
        int s = 2 * Math.Max(Dims.x, Dims.y) + 1;
        float[,] board = new float[s, s];

        // Calculate the offset for the given snake.
        var offset = Dims - Snakes[snakeIndex].Pos;

        // Calculate the rotation of the snake.
        int rot = 0;
        for (int i = 1; i < 4; ++i)
        {
            if (new Int2(0, 1) * i == Snakes[snakeIndex].Dir)
            {
                rot = i;
            }
        }

        // Copy over the board information.
        for (int i = 0; i < s; ++i)
        {
            for (int j = 0; j < s; ++j)
            {
                // Map the position relative to the center of the grid.
                var pos = new Int2(i, j) - Dims;

                // Rotate the position according to the snake's orientation.
                pos *= rot;

                //// Move the point back to where it was.
                pos += Dims;

                //// Move the position relative to the snake's.
                pos -= offset;

                // Is this a valid point?
                if (WithinBounds(pos))
                {
                    board[i, j] = GetTileState(pos);
                }
                // Is this a border?
                else if ((pos.x == -1 || pos.x == Dims.x) && pos.y >= -1 && pos.y <= Dims.y)
                {
                    board[i, j] = SNAKE;
                }
                else if ((pos.y == -1 || pos.y == Dims.y) && pos.x >= -1 && pos.x <= Dims.x)
                {
                    board[i, j] = SNAKE;
                }
            }
        }

        // Translate into a 1D array.
        float[] output = new float[board.Length];
        for (int i = 0; i < s; ++i)
        {
            for (int j = 0; j < s; ++j)
            {
                output[i * s + j] = board[i, j];
            }
        }

        return output;
    }

    public unsafe void DrawBoard()
    {
        // Create background.
        char[] buffer = new char[Size];
        for (int i = 0; i < Size; ++i)
            buffer[i] = ' ';

        // Draw the snakes.
        for (int i = 0; i < Snakes.Length; ++i)
            for (int j = 0; j < Snakes[i].Positions.Count; ++j)
                buffer[Snakes[i].Positions[j]] = Snakes[i].Character;

        // Draw the apples.
        for (int i = 0; i < Apples.Length; ++i)
            if (Apples[i] >= 0)
                buffer[Apples[i]] = '*';

        // Draw each line to the screen.
        fixed (char* pointer = buffer)
        {
            for (int y = 0; y < Dims.y; ++y)
            {
                string line = new string(pointer, (Dims.y - 1 - y) * Dims.x, Dims.x);
                Console.SetCursorPosition(Offset.x, Offset.y + y);
                Console.Write(line);
            }
        }

        for (int i = 0; i < Snakes.Length; ++i)
        {
            if (Snakes[i].Alive)
            {
                Console.SetCursorPosition(Offset.x + Dims.x, Offset.y + i);
                Console.Write("P" + (i + 1) + ": " + Snakes[i].Positions.Count + "   ");
            }
        }
    }

    public override string ToString()
    {
        // Create background.
        char[] buffer = new char[Size + Dims.y];
        for (int i = 0; i < buffer.Length; ++i)
        {
            if ((i + 1) % (Dims.x + 1) == 0)
                buffer[i] = '/';
            else
                buffer[i] = '0';
        }

        // Draw the snakes.
        for (int i = 0; i < Snakes.Length; ++i)
        {
            for (int j = 0; j < Snakes[i].Positions.Count; ++j)
            {
                var pos2 = IntToInt2(Snakes[i].Positions[j]);
                pos2.y = Dims.y - 1 - pos2.y;
                var pos = Int2ToInt(pos2);
                buffer[pos + pos / Dims.x] = Snakes[i].Character;
            }
        }

        // Draw the apples.
        for (int i = 0; i < Apples.Length; ++i)
        {
            if (Apples[i] >= 0)
            {
                var pos2 = IntToInt2(Apples[i]);
                pos2.y = Dims.y - 1 - pos2.y;
                var pos = Int2ToInt(pos2);
                buffer[pos + pos / Dims.x] = '*';
            }
        }

        return new string(buffer);
    }
}
