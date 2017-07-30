[System.Serializable]
public struct Size
{
    public int Width;
    public int Height;

    public Size(int w, int h)
    {
        Width = w;
        Height = h;
    }
}

[System.Serializable]
public struct SizeF
{
    public float Width;
    public float Height;

    public SizeF(float w, float h)
    {
        Width = w;
        Height = h;
    }
}