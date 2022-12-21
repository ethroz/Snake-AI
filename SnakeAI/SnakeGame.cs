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

class SnakeGame
{
    public readonly Int2 Offset; // The offset of the board in the console window.
    public readonly Int2 Dims; // The dimensions of the board.
    public readonly int Size; // The total number of tiles.
    private readonly List<int> Snake; // Index gives position.
    private readonly List<int> SnakeIndices; // Index gives next index for position.
    private int head; // The index of the head of the snake.
    public Int2 Pos; // The current position of the head.
    private readonly List<int> AvailablePoints; // Randomly assorted positions
    private readonly int[] AvailableIndices; // The position is the index which gives the index for that position.
    public Int2 Dir = new(1, 0); // The current direction the snake is moving. +y is down, +x is right.
    public const int NumApples = 3;
    public readonly int[] Apples = new int[NumApples]; // The positions of all the apples on the board.
    private readonly Random rnd = new();
    public long ticks = 0;
    public const int DEATH_SCORE = -10;
    public const int APPLE_SCORE = 20;

    public SnakeGame(Int2 size, Int2 offset, int seed = int.MinValue)
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

        // Make the snake.
        Snake = new(Size);
        SnakeIndices = new(Size);
        Pos = Dims / 2;
        Snake.Add(Pos.y * Dims.x + Pos.x);
        head = Snake.Count - 1;
        SnakeIndices.Add(0);

        // Remove the snake positions from the available points.
        for (int i = 0; i < Snake.Count; ++i)
            RemovePoint(Snake[i], -1); // Snake is -1.

        // Spawn apples around the board.
        for (int i = 0; i < NumApples; ++i)
            NewApple(i);
    }

    public SnakeGame(Int2 size) : this(size, new(0, 0)) {}

    private void NewApple(int index)
    {
        int rNum = rnd.Next(AvailablePoints.Count);
        Apples[index] = AvailablePoints[rNum];
        RemovePoint(Apples[index], -2); // Apples are -2.
    }

    private void RemovePoint(int pos, int empty)
    {
        // Get the indices of the given point and the last point.
        var index = AvailableIndices[pos];
        var end = AvailablePoints.Count - 1;

        // Replace the given point with the last point.
        AvailablePoints[index] = AvailablePoints[end];

        // Update the indices of the two points.
        AvailableIndices[AvailablePoints[index]] = index;
        AvailableIndices[pos] = empty;

        // Finally, remove the last point.
        AvailablePoints.RemoveAt(end);
    }

    public int Tick()
    {
        // Keep track of the score earned.
        int score = -1;
        // Progress the snake in the given direction.
        Pos += Dir;

        // Check for collision with border.
        if (Pos.x >= Dims.x || Pos.x < 0 || Pos.y >= Dims.y || Pos.y < 0)
            return DEATH_SCORE;

        // Overwrite the tail.
        var oldTailIndex = SnakeIndices[head];
        var oldTailPos = Snake[oldTailIndex];
        var newHeadPos = Pos.y * Dims.x + Pos.x;

        // Check for self collision.
        var tile = AvailableIndices[newHeadPos];
        if (tile == -1)
            return DEATH_SCORE;
        
        // Check for apple collision.
        if (tile == -2)
        {
            int index = -1;
            for (int i = 0; i < NumApples; ++i)
            {
                if (Apples[i] == newHeadPos)
                {
                    index = i;
                    break;
                }
            }
            Trace.Assert(index > -1);

            // Generate a new apple.
            NewApple(index);

            // Grow the snake.
            SnakeIndices.Add(oldTailIndex);
            SnakeIndices[head] = Snake.Count;
            Snake.Add(newHeadPos);

            // Change the index to occupied by snake.
            AvailableIndices[newHeadPos] = -1;

            // Change the score.
            score = APPLE_SCORE;
        }
        else
        {
            // Update the old tail position and the available points.
            Snake[oldTailIndex] = newHeadPos;
            AvailablePoints[AvailableIndices[newHeadPos]] = oldTailPos;
            AvailableIndices[oldTailPos] = AvailableIndices[newHeadPos];
            AvailableIndices[newHeadPos] = -1;
        }

        // Move the head forwards.
        head = SnakeIndices[head];

        // Check for the win condition.
        if (AvailablePoints.Count == 0)
            score = 10000;

        // Count the ticks and return the result.
        ++ticks;
        return score;
    }

    // Cast a ray in the given \a rayDir.
    // Returns the number of tiles and the tile type.
    public Int2 RayCast(Int2 rayDir)
    {
        // Check along the direction of the ray for a wall or an object.
        Int2 currentPos = Pos + rayDir;
        int count = 0;
        while (true)
        {
            if (currentPos.x >= Dims.x || currentPos.x < 0 ||
                currentPos.y >= Dims.y || currentPos.y < 0)
                return new Int2(count, 1);
            var tile = AvailableIndices[currentPos.y * Dims.x + currentPos.x];
            if (tile < 0)
                return new Int2(count, tile + 1);
            currentPos += rayDir;
            ++count;
        }
    }

    public float[] GetBoard()
    {
        float[] board = new float[Size];

        for (int i = 0; i < Size; ++i)
        {
            var tile = AvailableIndices[i];
            if (tile < 0)
                board[i] = 2 * tile + 3;
            else
                board[i] = 0;
        }

        return board;
    }

    public unsafe void DrawBoard()
    {
        // Create background.
        char[] buffer = new char[Size];
        for (int i = 0; i < Size; ++i)
            buffer[i] = ' ';

        // Draw the snake.
        for (int i = 0; i < Snake.Count; ++i)
            buffer[Snake[i]] = '#';

        // Draw the apples.
        for (int i = 0; i < Apples.Length; ++i)
            buffer[Apples[i]] = '@';

        // Draw each line to the screen.
        //fixed (char* pointer = buffer)
        //{
        //    for (int y = Dims.y; y >= 0; ++y)
        //    {
        //        string line = new string(pointer, y * Dims.x, Dims.x);
        //        Console.SetCursorPosition(Offset.x, Offset.y + y);
        //        Console.Write(line);
        //    }
        //}
        for (int y = 0; y < Dims.y; ++y)
        {
            Console.SetCursorPosition(Offset.x, Offset.y + y);
            for (int x = 0; x < Dims.x; ++x)
                Console.Write(buffer[y * Dims.x + x]);
        }
    }
}
