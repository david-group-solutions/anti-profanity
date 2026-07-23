namespace DavidGroup.Content.AntiProfanity.Tools.DetectionsDatasetGenerator.Helpers;

public static class ConsoleHelpers
{
    public static void TryClearConsole()
    {
        try
        {
            Console.Clear();
        }
        catch (IOException)
        {
            // Output isn't an interactive console (e.g. redirected/piped) — nothing to clear.
        }
    }

    public static void ClearConsole(int startX, int startY, int? endX = null, int? endY = null)
    {
        endX ??= Console.WindowWidth;
        endY ??= Console.WindowHeight;

        int width = endX.Value - startX;

        for (int y = startY; y < endY; y++)
        {
            Console.SetCursorPosition(startX, y);
            Console.Write(new string(' ', width));
        }

        Console.SetCursorPosition(startX, startY);
    }
}
