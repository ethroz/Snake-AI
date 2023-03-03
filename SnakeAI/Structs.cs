namespace SnakeAI;

public struct Int2
{
    public int x, y;

    public Int2(int x, int y)
    {
        this.x = x;
        this.y = y;
    }

    public static Int2 operator +(Int2 a, Int2 b)
    {
        return new Int2(a.x + b.x, a.y + b.y);
    }

    public static Int2 operator /(Int2 i, int scalar)
    {
        return new Int2(i.y / scalar, i.x / scalar);
    }

    public static Int2 operator ^(Int2 i, int rotation)
    {
        if (rotation == 0)
            return i;
        else if (rotation > 0)
            return new Int2(i.y, -i.x);
        else
            return new Int2(-i.y, i.x);
    }

    public static bool operator ==(Int2 a, Int2 b)
    {
        return a.x == b.x && a.y == b.y;
    }

    public static bool operator !=(Int2 a, Int2 b)
    {
        return a.x != b.x || a.y != b.y;
    }

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
}
