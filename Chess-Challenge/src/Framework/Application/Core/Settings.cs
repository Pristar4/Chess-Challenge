#region

using System.Numerics;

#endregion

namespace ChessChallenge.Application;

public static class Settings {
    public enum LogType {
        None,
        ErrorOnly,
        All,
    }
    public const string Version = "1.20";

    // Game settings
    public const int GameDurationMilliseconds = 60 * 1000;
    public const int IncrementMilliseconds = 1 * 1000;
    public const float MinMoveDelay = 1;

    // Display settings
    public const bool DisplayBoardCoordinates = true;

    // Other settings
    // public const int MaxTokenCount = 1024;
    public const int MaxTokenCount = 2048;
    public const LogType MessagesToLog = LogType.All;
    public static readonly bool RunBotsOnSeparateThread = true;
    public static readonly Vector2 ScreenSizeSmall = new(1280, 720);
    public static readonly Vector2 ScreenSizeBig = new(1920, 1080);
}