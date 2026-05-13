namespace Lab5Client.Utils;

// Цветной прогресс-бар в консоли: зелёная заполненная часть + серая пустая
public class ColoredProgressReporter : IProgress<int>
{
    private readonly int _total;
    private readonly int _barWidth;

    public ColoredProgressReporter(int total, int barWidth = 40)
    {
        _total    = total;
        _barWidth = barWidth;
    }

    public void Report(int value)
    {
        var percent = _total > 0 ? (double)value / _total : 0;
        var filled  = (int)(percent * _barWidth);
        var empty   = _barWidth - filled;

        Console.Write("\r  [");
        Console.ForegroundColor = ConsoleColor.Green;
        Console.Write(new string('█', filled));   // █
        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.Write(new string('░', empty));    // ░
        Console.ResetColor();
        Console.Write($"] {percent,6:P0}  ({value}/{_total})  ");

        if (value >= _total)
            Console.WriteLine();
    }
}
