using System.Collections.Generic;

namespace SnakeAI;

public class Snake
{
    /// <summary>
    /// The owner of this snake.
    /// </summary>
    public readonly SnakeGame Owner;

    /// <summary>
    /// The index gives the 1D position.
    /// </summary>
    public List<int> Positions;

    /// <summary>
    /// Use the index to get the next index.
    /// </summary>
    public List<int> Indices;

    /// <summary>
    /// The current index for the head of the snake.
    /// </summary>
    public int Head;

    /// <summary>
    /// The current position of the head.
    /// </summary>
    public Int2 Pos;

    /// <summary>
    /// The current direction the snake is moving. +y is up, +x is right.
    /// </summary>
    public Int2 Dir;

    /// <summary>
    /// The character we use to draw the snake.
    /// </summary>
    public readonly char Character;

    /// <summary>
    /// A flag that indicates whether the snake is alive.
    /// </summary>
    public bool Alive;

    /// <summary>
    /// Initializes a new instance of the Snake class with the given parameters.
    /// </summary>
    /// <param name="dims">The dimensions of the containing grid.</param>
    /// <param name="startPos">The starting position of the snake.</param>
    /// <param name="character">The character to draw the snake.</param>
    public Snake(SnakeGame owner, char character)
    {
        Owner = owner;
        Character = character;
        Positions = new(owner.Size);
        Indices = new(owner.Size);
        Dir = new(1, 0);
        Alive = true;
    }

    /// <summary>
    /// Resets the snake to its initial state.
    /// </summary>
    public void Kill()
    {
        Positions.Clear();
        Indices.Clear();
        Alive = false;
    }

    /// <summary>
    /// Resets the snake to its initial state at the given position.
    /// </summary>
    /// <param name="startPos">The starting position of the snake.</param>
    public void Respawn(int startPos)
    {
        Pos = Owner.IntToInt2(startPos);
        Positions.Add(startPos);
        Head = 0;
        Indices.Add(0);
    }
}
