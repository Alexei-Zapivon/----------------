using System.Text;
using Lab6Publisher;

Console.OutputEncoding = Encoding.UTF8;
Console.Title = "Lab6 — Издатель накладных";

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

Console.WriteLine("Подключение к RabbitMQ...");
using var publisher = new Publisher();

while (!cts.IsCancellationRequested)
{
    PrintMenu();
    var choice = Console.ReadLine()?.Trim();
    if (choice == "0") break;

    Console.WriteLine();
    try
    {
        switch (choice)
        {
            case "1":
                publisher.SendOne();
                break;
            case "2":
                publisher.SendBatch(5, cts.Token);
                break;
            case "3":
                publisher.SendWithAttachment();
                break;
            case "4":
                publisher.PrintStats();
                break;
            default:
                Console.WriteLine("  Неверный пункт.");
                break;
        }
    }
    catch (OperationCanceledException)
    {
        Console.WriteLine("  Операция отменена.");
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"  ✗ Ошибка: {ex.Message}");
        Console.ResetColor();
    }

    if (!cts.IsCancellationRequested)
    {
        Console.WriteLine("\n  Нажмите Enter для продолжения...");
        Console.ReadLine();
    }
}

Console.WriteLine("\n  До свидания!");

static void PrintMenu()
{
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.DarkYellow;
    Console.WriteLine("  ╔══════════════════════════════╗");
    Console.WriteLine("  ║   Издатель накладных (Lab6)  ║");
    Console.WriteLine("  ╚══════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine("  1. Отправить одну накладную");
    Console.WriteLine("  2. Отправить пакет (5 штук)");
    Console.WriteLine("  3. Отправить накладную с PDF-вложением");
    Console.WriteLine("  4. Показать статистику отправки");
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.WriteLine("  0. Выход");
    Console.ResetColor();
    Console.Write("\n  Выберите пункт: ");
}
