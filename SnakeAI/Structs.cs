using System;

namespace SnakeAI;

/// <summary>
/// Represents a two-dimensional integer vector.
/// </summary>
public struct Int2
{
    /// <summary>
    /// The x-coordinate of the vector.
    /// </summary>
    public int x;

    /// <summary>
    /// The y-coordinate of the vector.
    /// </summary>
    public int y;

    /// <summary>
    /// Initializes a new instance of the Int2 struct with the specified
    /// coordinates.
    /// </summary>
    /// <param name="x">The x-coordinate of the vector.</param>
    /// <param name="y">The y-coordinate of the vector.</param>
    public Int2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    /// <summary>
    /// Returns the squared magnitude of the vector.
    /// </summary>
    /// <returns>The squared magnitude of the vector.</returns>
    public int SqrMag()
    {
        return x * x + y * y;
    }

    /// <summary>
    /// Returns the squared magnitude of the vector.
    /// </summary>
    /// <returns>The squared magnitude of the vector.</returns>
    public int UnitDistance()
    {
        return Math.Abs(x) + Math.Abs(y);
    }

    /// <summary>
    /// Adds two vectors together.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>The sum of the two vectors.</returns>
    public static Int2 operator +(Int2 a, Int2 b)
    {
        return new Int2(a.x + b.x, a.y + b.y);
    }

    /// <summary>
    /// Subtracts one vector from another.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>The componenet-wise subtraction of a from b.</returns>
    public static Int2 operator -(Int2 a, Int2 b)
    {
        return new Int2(a.x - b.x, a.y - b.y);
    }

    /// <summary>
    /// Divides a vector by a scalar value.
    /// </summary>
    /// <param name="i">The vector to divide.</param>
    /// <param name="scalar">The scalar value to divide by.</param>
    /// <returns>The result of dividing the vector by the scalar
    /// value.</returns>
    public static Int2 operator /(Int2 i, int scalar)
    {
        return new Int2(i.y / scalar, i.x / scalar);
    }

    /// <summary>
    /// Rotates a vector by a specified number of degrees.
    /// </summary>
    /// <param name="i">The vector to rotate.</param>
    /// <param name="rotation">The number of 90 degree turns to rotate
    /// by modulo 4.</param>
    /// <returns>The rotated vector.</returns>
    public static Int2 operator *(Int2 i, int rotation)
    {
        return (rotation - (rotation >> 2 << 2)) switch
        {
            0 => i,
            1 => new Int2(i.y, -i.x),
            2 => new Int2(-i.x, -i.y),
            3 => new Int2(-i.y, i.x),
            _ => throw new NotImplementedException(),
        };
    }

    /// <summary>
    /// Determines whether two vectors are equal.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>True if the vectors are equal; otherwise, false.</returns>
    public static bool operator ==(Int2 a, Int2 b)
    {
        return a.x == b.x && a.y == b.y;
    }

    /// <summary>
    /// Determines whether two vectors are not equal.
    /// </summary>
    /// <param name="a">The first vector.</param>
    /// <param name="b">The second vector.</param>
    /// <returns>True if the vectors are not equal; otherwise,
    /// false.</returns>
    public static bool operator !=(Int2 a, Int2 b)
    {
        return a.x != b.x || a.y != b.y;
    }

    /// <summary>
    /// Determines whether this instance and another specified object are
    /// equal.
    /// </summary>
    /// <param name="obj">The object to compare with this instance.</param>
    /// <returns>True if obj is an instance of Int2 and equals the value of
    /// this instance; otherwise, false.</returns>
    public override bool Equals(object? obj)
    {
        if (obj is Int2 i)
            return this == i;
        return false;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }

    /// <summary>
    /// Returns a string formatted as a coordinate.
    /// </summary>
    /// <returns>The string form of the vector.</returns>
    public override string ToString()
    {
        return "(" + x + ", " + y + ")";
    }
}
