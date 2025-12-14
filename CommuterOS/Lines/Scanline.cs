namespace CommuterOS.Lines;

public class ScanlineDrawable : IDrawable
{
    public void Draw(ICanvas canvas, RectF dirtyRect)
    {
        canvas.StrokeColor = Colors.Black;
        canvas.StrokeSize = 2;
        canvas.Alpha = 0.25f; 

        for (float y = 0; y < dirtyRect.Height; y += 4)
        {
            canvas.DrawLine(0, y, dirtyRect.Width, y);
        }
    }
}