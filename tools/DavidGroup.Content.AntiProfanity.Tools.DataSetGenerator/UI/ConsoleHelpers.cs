namespace DavidGroup.Content.AntiProfanity.Tools.DataSetGenerator.UI;

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
}
