namespace YTMusicAPI.Model;

public class Resolution
{
    public Resolution(int width, int height)
    {
        Width = width;
        Height = height;
    }

    public int Width { get; }
    public int Height { get; }
}
