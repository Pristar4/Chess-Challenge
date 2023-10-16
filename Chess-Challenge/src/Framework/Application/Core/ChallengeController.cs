#region

using System;
using System.IO;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Chess_Challenge.My_Bot;
using ChessChallenge.Chess;
using ChessChallenge.Example;
using Raylib_cs;
using static ChessChallenge.Application.Settings;
using static ChessChallenge.Application.ConsoleHelper;
using Timer = System.Timers.Timer;

#endregion

namespace ChessChallenge.Application; 

public class ChallengeController {
    public class BotMatchStats {
        public string BotName;
        public int NumDraws;
        public int NumIllegalMoves;
        public int NumLosses;
        public int NumTimeouts;
        public int NumWins;

        public BotMatchStats(string name) {
            BotName = name;
        }
    }
    public enum PlayerType {
        Human,
        MyBot,
        BrutalBot,
        EvilBot,
        MyBotV2,
    }

    // Other
    private readonly BoardUI boardUI;

    // Bot match state
    private readonly string[] botMatchStartFens;
    private readonly int debugTokenCount;
    private readonly MoveGenerator moveGenerator;
    private readonly StringBuilder pgns;

    // Game state
    private readonly Random rng;
    private readonly int tokenCount;
    private Board board;
    private bool botAPlaysWhite;
    private ExceptionDispatchInfo botExInfo;
    private int botMatchGameIndex;


    // Bot task
    private AutoResetEvent botTaskWaitHandle;
    private int gameID;
    private bool hasBotTaskException;
    private bool isPlaying;
    private bool isWaitingToPlayMove;

    private float lastMoveMadeTime;
    private Move moveToPlay;
    private float playMoveTime;
    public ChessPlayer PlayerWhite { get; private set; }
    public ChessPlayer PlayerBlack { get; private set; }
    public bool HumanWasWhiteLastGame { get; private set; }
    public BotMatchStats BotStatsA { get; private set; }
    public BotMatchStats BotStatsB { get; private set; }


    private ChessPlayer PlayerToMove
    {
        get => board.IsWhiteToMove ? PlayerWhite : PlayerBlack;
    }
    private ChessPlayer PlayerNotOnMove
    {
        get => board.IsWhiteToMove ? PlayerBlack : PlayerWhite;
    }

    public int TotalGameCount
    {
        get => botMatchStartFens.Length * 2;
    }
    public int CurrGameNumber
    {
        get => Math.Min(TotalGameCount, botMatchGameIndex + 1);
    }
    public string AllPGNs
    {
        get => pgns.ToString();
    }

    public ChallengeController() {
        Log($"Launching Chess-Challenge version {Settings.Version}");
        (tokenCount, debugTokenCount) = GetTokenCount();
        Warmer.Warm();

        rng = new Random();
        moveGenerator = new MoveGenerator();
        boardUI = new BoardUI();
        board = new Board();
        pgns = new StringBuilder();

        BotStatsA = new BotMatchStats("IBot");
        BotStatsB = new BotMatchStats("IBot");
        botMatchStartFens = FileHelper.ReadResourceFile("Fens.txt").Split('\n')
                .Where(fen => fen.Length > 0).ToArray();
        botTaskWaitHandle = new AutoResetEvent(false);

        StartNewGame(PlayerType.Human, PlayerType.MyBotV2);
    }

    public void Draw() {
        boardUI.Draw();
        string nameW = GetPlayerName(PlayerWhite);
        string nameB = GetPlayerName(PlayerBlack);
        boardUI.DrawPlayerNames(nameW, nameB, PlayerWhite.TimeRemainingMs,
                                PlayerBlack.TimeRemainingMs, isPlaying);
    }

    public void DrawOverlay() {
        BotBrainCapacityUI.Draw(tokenCount, debugTokenCount, MaxTokenCount);
        MenuUI.DrawButtons(this);
        MatchStatsUI.DrawMatchStats(this);
    }

    public void Release() {
        boardUI.Release();
    }

    public void StartNewBotMatch(PlayerType botTypeA, PlayerType botTypeB) {
        EndGame(GameResult.DrawByArbiter, false, false);
        botMatchGameIndex = 0;
        string nameA = GetPlayerName(botTypeA);
        string nameB = GetPlayerName(botTypeB);

        if (nameA == nameB) {
            nameA += " (A)";
            nameB += " (B)";
        }

        BotStatsA = new BotMatchStats(nameA);
        BotStatsB = new BotMatchStats(nameB);
        botAPlaysWhite = true;
        Log($"Starting new match: {nameA} vs {nameB}", false, ConsoleColor.Blue);
        StartNewGame(botTypeA, botTypeB);
    }

    public void StartNewGame(PlayerType whiteType, PlayerType blackType) {
        // End any ongoing game
        EndGame(GameResult.DrawByArbiter, false, false);
        gameID = rng.Next();

        // Stop prev task and create a new one
        if (RunBotsOnSeparateThread) {
            // Allow task to terminate
            botTaskWaitHandle.Set();
            // Create new task
            botTaskWaitHandle = new AutoResetEvent(false);
            Task.Factory.StartNew(BotThinkerThread, TaskCreationOptions.LongRunning);
        }

        // Board Setup
        board = new Board();
        bool isGameWithHuman = whiteType is PlayerType.Human || blackType is PlayerType.Human;
        int fenIndex = isGameWithHuman ? 0 : botMatchGameIndex / 2;
        board.LoadPosition(botMatchStartFens[fenIndex]);

        // Player Setup
        PlayerWhite = CreatePlayer(whiteType);
        PlayerBlack = CreatePlayer(blackType);
        PlayerWhite.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);
        PlayerBlack.SubscribeToMoveChosenEventIfHuman(OnMoveChosen);

        // UI Setup
        boardUI.UpdatePosition(board);
        boardUI.ResetSquareColours();
        SetBoardPerspective();

        // Start
        isPlaying = true;
        NotifyTurnToMove();
    }

    public void Update() {
        if (isPlaying) {
            PlayerWhite.Update();
            PlayerBlack.Update();

            PlayerToMove.UpdateClock(Raylib.GetFrameTime());

            if (PlayerToMove.TimeRemainingMs <= 0) {
                EndGame(PlayerToMove == PlayerWhite
                                ? GameResult.WhiteTimeout
                                : GameResult.BlackTimeout);
            } else {
                if (isWaitingToPlayMove && Raylib.GetTime() > playMoveTime) {
                    isWaitingToPlayMove = false;
                    PlayMove(moveToPlay);
                }
            }
        }

        if (hasBotTaskException) {
            hasBotTaskException = false;
            botExInfo.Throw();
        }
    }

    private void AutoStartNextBotMatchGame(int originalGameID, Timer timer) {
        if (originalGameID == gameID) {
            StartNewGame(PlayerBlack.PlayerType, PlayerWhite.PlayerType);
        }

        timer.Close();
    }

    private void BotThinkerThread() {
        int threadID = gameID;
        //Console.WriteLine("Starting thread: " + threadID);

        while (true) {
            // Sleep thread until notified
            botTaskWaitHandle.WaitOne();

            // Get bot move
            if (threadID == gameID) {
                var move = GetBotMove();

                if (threadID == gameID) {
                    OnMoveChosen(move);
                }
            }

            // Terminate if no longer playing this game
            if (threadID != gameID) {
                break;
            }
        }
        //Console.WriteLine("Exitting thread: " + threadID);
    }

    private ChessPlayer CreatePlayer(PlayerType type) {
        return type switch {
            PlayerType.MyBot => new ChessPlayer(new MyBot(), type, GameDurationMilliseconds),
            PlayerType.MyBotV2 => new ChessPlayer(new MyBotV2(), type, GameDurationMilliseconds),
            PlayerType.BrutalBot =>
                    new ChessPlayer(new BrutalBot(), type, GameDurationMilliseconds),
            PlayerType.EvilBot => new ChessPlayer(new EvilBot(), type, GameDurationMilliseconds),
            _ => new ChessPlayer(new HumanPlayer(boardUI), type),
        };
    }

    private void EndGame(GameResult result, bool log = true, bool autoStartNextBotMatch = true) {
        if (isPlaying) {
            isPlaying = false;
            isWaitingToPlayMove = false;
            gameID = -1;

            if (log) {
                Log("Game Over: " + result, false, ConsoleColor.Blue);
            }

            string pgn = PGNCreator.CreatePGN(board, result, GetPlayerName(PlayerWhite),
                                              GetPlayerName(PlayerBlack));
            pgns.AppendLine(pgn);

            // If 2 bots playing each other, start next game automatically.
            if (PlayerWhite.IsBot && PlayerBlack.IsBot) {
                UpdateBotMatchStats(result);
                botMatchGameIndex++;
                int numGamesToPlay = botMatchStartFens.Length * 2;

                if (botMatchGameIndex < numGamesToPlay && autoStartNextBotMatch) {
                    botAPlaysWhite = !botAPlaysWhite;
                    const int startNextGameDelayMs = 600;
                    Timer autoNextTimer = new(startNextGameDelayMs);
                    int originalGameID = gameID;
                    autoNextTimer.Elapsed += (s, e) =>
                            AutoStartNextBotMatchGame(originalGameID, autoNextTimer);
                    autoNextTimer.AutoReset = false;
                    autoNextTimer.Start();
                } else if (autoStartNextBotMatch) {
                    Log("Match finished", false, ConsoleColor.Blue);
                }
            }
        }
    }

    private Move GetBotMove() {
        API.Board botBoard = new(board);

        try {
            API.Timer timer = new(PlayerToMove.TimeRemainingMs, PlayerNotOnMove.TimeRemainingMs,
                                  GameDurationMilliseconds, IncrementMilliseconds);
            var move = PlayerToMove.Bot.Think(botBoard, timer);
            return new Move(move.RawValue);
        }
        catch (Exception e) {
            Log("An error occurred while bot was thinking.\n" + e, true, ConsoleColor.Red);
            hasBotTaskException = true;
            botExInfo = ExceptionDispatchInfo.Capture(e);
        }

        return Move.NullMove;
    }

    private static string GetPlayerName(ChessPlayer player) {
        return GetPlayerName(player.PlayerType);
    }

    private static string GetPlayerName(PlayerType type) {
        return type.ToString();
    }

    private static (int totalTokenCount, int debugTokenCount) GetTokenCount() {
        string path = Path.Combine(Directory.GetCurrentDirectory(), "src", "My Bot", "MyBot.cs");

        using StreamReader reader = new(path);
        string txt = reader.ReadToEnd();
        return TokenCounter.CountTokens(txt);
    }


    private bool IsLegal(Move givenMove) {
        var moves = moveGenerator.GenerateMoves(board);

        foreach (var legalMove in moves) {
            if (givenMove.Value == legalMove.Value) {
                return true;
            }
        }

        return false;
    }


    private void NotifyTurnToMove() {
        //playerToMove.NotifyTurnToMove(board);
        if (PlayerToMove.IsHuman) {
            PlayerToMove.Human.SetPosition(FenUtility.CurrentFen(board));
            PlayerToMove.Human.NotifyTurnToMove();
        } else {
            if (RunBotsOnSeparateThread) {
                botTaskWaitHandle.Set();
            } else {
                double startThinkTime = Raylib.GetTime();
                var move = GetBotMove();
                double thinkDuration = Raylib.GetTime() - startThinkTime;
                PlayerToMove.UpdateClock(thinkDuration);
                OnMoveChosen(move);
            }
        }
    }

    private void OnMoveChosen(Move chosenMove) {
        if (IsLegal(chosenMove)) {
            PlayerToMove.AddIncrement(IncrementMilliseconds);

            if (PlayerToMove.IsBot) {
                moveToPlay = chosenMove;
                isWaitingToPlayMove = true;
                playMoveTime = lastMoveMadeTime + MinMoveDelay;
            } else {
                PlayMove(chosenMove);
            }
        } else {
            string moveName = MoveUtility.GetMoveNameUCI(chosenMove);
            string log = $"Illegal move: {moveName} in position: {FenUtility.CurrentFen(board)}";
            Log(log, true, ConsoleColor.Red);
            var result = PlayerToMove == PlayerWhite
                    ? GameResult.WhiteIllegalMove
                    : GameResult.BlackIllegalMove;
            EndGame(result);
        }
    }

    private void PlayMove(Move move) {
        if (isPlaying) {
            bool animate = PlayerToMove.IsBot;
            lastMoveMadeTime = (float)Raylib.GetTime();

            board.MakeMove(move, false);
            boardUI.UpdatePosition(board, move, animate);

            var result = Arbiter.GetGameState(board);

            if (result == GameResult.InProgress) {
                NotifyTurnToMove();
            } else {
                EndGame(result);
            }
        }
    }

    private void SetBoardPerspective() {
        // Board perspective
        if (PlayerWhite.IsHuman || PlayerBlack.IsHuman) {
            boardUI.SetPerspective(PlayerWhite.IsHuman);
            HumanWasWhiteLastGame = PlayerWhite.IsHuman;
        } else if (PlayerWhite.Bot is MyBotV2 && PlayerBlack.Bot is MyBotV2) {
            boardUI.SetPerspective(true);
        } else {
            boardUI.SetPerspective(PlayerWhite.Bot is MyBotV2);
        }
    }


    private void UpdateBotMatchStats(GameResult result) {
        UpdateStats(BotStatsA, botAPlaysWhite);
        UpdateStats(BotStatsB, !botAPlaysWhite);

        void UpdateStats(BotMatchStats stats, bool isWhiteStats) {
            // Draw
            if (Arbiter.IsDrawResult(result)) {
                stats.NumDraws++;
            }
            // Win
            else if (Arbiter.IsWhiteWinsResult(result) == isWhiteStats) {
                stats.NumWins++;
            }
            // Loss
            else {
                stats.NumLosses++;
                stats.NumTimeouts += result is GameResult.WhiteTimeout or GameResult.BlackTimeout
                        ? 1
                        : 0;
                stats.NumIllegalMoves +=
                        result is GameResult.WhiteIllegalMove or GameResult.BlackIllegalMove
                                ? 1
                                : 0;
            }
        }
    }
}