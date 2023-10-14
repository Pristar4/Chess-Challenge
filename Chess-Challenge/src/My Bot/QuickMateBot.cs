#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using ChessChallenge.API;
using CsvHelper;

#endregion

namespace Chess_Challenge.My_Bot;

public class QuickMateBot : IChessBot {
    private readonly bool _printNodeCount = true;
    private readonly bool _printMoves = true;
    private readonly bool _saveNodes = false;
    private const int MATE_SCORE = 100000;
    private readonly Stopwatch _stopwatch = new();
    private int _localNodeCount;
    private int _pruningCount;

    public List<TreeNodeInfo> NodesData { get; } = new();


    public int MaxDepth { get; set; } =8;
    public Dictionary<Move, int> MoveCounts { get; set; } = new();

    public int NodeCount { get; private set; }
    public int BestScore => 0;

    //Transposition table
    private Dictionary<ulong, int> transpositionTable = new();

    public Move Think(Board board, Timer timer) {
    if (_saveNodes) {
            if (File.Exists("NodesData.csv")) {
                File.Delete("NodesData.csv");
            }
        }
        transpositionTable.Clear();
        var legalMoves = board.GetLegalMoves();
        // legalMoves = legalMoves.OrderBy(_ => Guid.NewGuid()).ToArray();

        var bestMove = Move.NullMove;
        int bestScore = int.MinValue;
        var timeBuffer = 500;
        var totalTime = timer.MillisecondsRemaining;
        var increment = timer.IncrementMilliseconds;
        double averageTimePerMove = (totalTime - timeBuffer) / 40.0 + increment;
        var mainTimeForCurrentMove = averageTimePerMove + 0.1 * (totalTime - timeBuffer);
        var maxTimeForMove = averageTimePerMove + 1.0 * (totalTime - timeBuffer);
        var minTimeForCurrentMove = 0.05 * totalTime;
        _stopwatch.Restart();

        int ev_sign = 1; //assuming bot is maximizing

        // var depth = 1;

        /*// Iterative deepening
        while (true) {

            // if( /* time since the start of the Think method call is more than minTimeForMove #1# )
            if (timer.MillisecondsElapsedThisTurn > minTimeForCurrentMove)
            {
                Console.WriteLine("Time OVER ");
                break;
            }*/

            //print time to use in seconds

            Console.WriteLine($"time to Use: {minTimeForCurrentMove / 1000.0} seconds");
            foreach (var move in legalMoves) {
                _localNodeCount = 0;
                // Mate in one
                board.MakeMove(move);

                if (board.IsInCheckmate()) {

                    Console.WriteLine("Raw  Checkmate in 1");
                    board.UndoMove(move);
                    return move;
                }


                int score =  -NegaMax(board, MaxDepth-1,0, int.MinValue, int.MaxValue,-ev_sign,move);
                board.UndoMove(move);
                // nodeCount += localnodes;

                if (!MoveCounts.ContainsKey(move)) {
                    MoveCounts.Add(move, _localNodeCount);
                    NodeCount += _localNodeCount;
                }

                if (score > bestScore) {
                    bestScore = score;
                    bestMove = move;
                }

                if (_printMoves) {
                    var replace = move.ToString().Replace("\'", "").Replace("Move: ", "");
                    Console.WriteLine($"Move: {replace}, Score: {score}");
                }
            }

            if (_printNodeCount) PrintNodes();

            Console.WriteLine(
                    $"QuickMateBot:  Depth: {MaxDepth}, Time: {_stopwatch.ElapsedMilliseconds} ms, Score: {bestScore}, Best Move:{bestMove} Nodes: {NodeCount}");

            /*if (_stopwatch.ElapsedMilliseconds >= minTimeForCurrentMove) {
                _stopwatch.Stop();
                Console.WriteLine("Time limit reached");

                return bestMove;
            }
            depth++;
        }*/

        _stopwatch.Stop();

        // Save NodesData to csv
        if (_saveNodes) {
            Console.WriteLine("Saving NodesData to csv");
            // delete NodesData.csv if it exists


            ExportTreeData("NodesData.csv");
            NodesData.Clear();
        }
        return bestMove;
    }
    private int Evaluate(Board board, int ply) {
        int score = 0;
        var allPieces = board.GetAllPieceLists();

        ulong key = board.ZobristKey;

        // Check if this position has been evaluated before
        if (transpositionTable.TryGetValue(key, out score)) {
            // if we have, return the stored evaluation.
            return score;
        }

        if (board.IsInCheckmate()) {
            // Console.WriteLine("Mate in " + ply);
            return MATE_SCORE - ply;
        }

        foreach (var pieceList in allPieces) {
            foreach (var piece in pieceList) {
                int pieceScore = GetScore(piece.PieceType);

                if (piece.IsWhite == board.IsWhiteToMove) {
                    score += pieceScore;
                } else {
                    score -= pieceScore;
                }
            }
        }

        transpositionTable[key] = score;
        return score;
    }

    private int GetScore(PieceType pieceType) {
        return pieceType switch {
            PieceType.Pawn => 100,
            PieceType.Knight => 300,
            PieceType.Bishop => 300,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => int.MaxValue / 2,
            _ => 0,
        };
    }

    /// <summary>
    ///
    /// </summary>
    /// <param name="board"></param>
    /// <param name="depth">
    /// depth ist die Maximaltiefe (in plies), die noch gesucht werden soll.
    /// </param>
    /// <param name="ply">
    /// ply ist die Anzahl der Halbzüge, die seit dem Start der Suche gemacht wurden.
    /// </param>
    /// <param name="alpha"></param>
    /// <param name="beta"></param>
    /// <param name="ev_sign">
    /// ev_sign ist das Vorzeichen der Bewertungsfunktion (1 wenn der Bot maximiert, -1 wenn er minimiert).
    /// </param>
    /// <param name="prevMove"></param>
    /// <returns></returns>
    private int NegaMax(Board board, int depth, int ply , int alpha, int beta,int ev_sign, Move prevMove) {
        if (board.IsInCheckmate() || board.IsInStalemate()) {
            _localNodeCount++;
                return   -(Evaluate(board, ply));

        }

        if (depth == 0) {
            _localNodeCount++;
            //TODO: Check why  using QuiescenceSearch at depth 4 breaks the bot
            // return QuiescenceSearch(board, alpha, beta);
             return  ev_sign *Evaluate(board, ply);
        }


        int positionValue = int.MinValue;
        var moves = board.GetLegalMoves();
        moves = OrderMoves(moves, board);

        int moveCount = moves.Length;
        int processedMoves = 0;
        // nodeCount += moves.Length;

        foreach (var move in moves) {
            board.MakeMove(move);
            processedMoves++;

            int newValue = -NegaMax(board, depth - 1, ply + 1, -beta, -alpha, -ev_sign, move);
             string moveString = move.ToString().Replace("Move: ", "").Replace("\'", "");
            string preMoveString = prevMove.ToString().Replace("Move: ", "").Replace("\'", "");

            if (_saveNodes) {
            NodesData.Add(new TreeNodeInfo {
                PrevMove = preMoveString,
                Move = moveString,
                Score = newValue,
                Depth = depth,
                Alpha = alpha,
                Beta = beta,
                Fen = board.GetFenString(),
            });
            }
            board.UndoMove(move);

            positionValue = Math.Max(positionValue, newValue);


            alpha = Math.Max(alpha, newValue);

            if (alpha >= beta) {
                _pruningCount += (moveCount - processedMoves);
                break;
            }
        }

        return positionValue;
    }

    private Move[] OrderMoves(IEnumerable<Move> moves, Board board) {
        var movesWithScore = moves.Select(move => new {move, score = ScoreMove(move, board)});
        var orderedMoves = movesWithScore.OrderByDescending(x => x.score).Select(x => x.move)
                .ToArray();
        return orderedMoves;
    }
    private void PrintNodes() {
        Console.WriteLine($"Total: {NodeCount}");

        foreach (var move in MoveCounts.OrderBy(m => m.Key.ToString())) {
            Console.WriteLine(
                    $"{move.Key.ToString().Replace("\'", "").Replace("Move: ", "")} - {move.Value}");
        }

        Console.WriteLine($"Pruned nodes count: {_pruningCount}");

        double percentageSaved = (double)_pruningCount / NodeCount * 100;
        Console.WriteLine($"Percentage saved: {percentageSaved:0.00}% of {NodeCount} nodes");
    }

    private int ScoreMove(Move move, Board board) {
        board.MakeMove(move);
        bool isInCheck = board.IsInCheck();
        bool isInCheckmate = board.IsInCheckmate();

        int score = 0;

        if (move.IsCapture) {
            score += GetScore(move.CapturePieceType) * 100;
        }

        if (isInCheck) {
            score += 10000;
        }

        if (isInCheckmate) {
            score += 100000;
        }

        board.UndoMove(move);

        return score;
    }

    private int QuiescenceSearch(Board board, int alpha, int beta) {
        int standPat = Evaluate(board, 0);

        if (standPat >= beta) {
            return beta;
        }

        if (alpha < standPat) {
            alpha = standPat;
        }

        int score = standPat;

        var moves = board.GetLegalMoves();

        foreach (var move in moves) {
            board.MakeMove(move);
            bool isCheck = board.IsInCheck();
            bool isCapture = move.IsCapture;
            board.UndoMove(move);

            if (!isCapture && !isCheck) {
                continue;
            }

            board.MakeMove(move);
            score = -QuiescenceSearch(board, -beta, -alpha);
            board.UndoMove(move);


            if (score >= beta) {
                return beta;
            }

            if (score > alpha) {
                alpha = score;
            }
        }

        return alpha;
    }

    public void ExportTreeData(string path) {
        using var writer = new StreamWriter(path);
        using var csv = new CsvWriter(writer, CultureInfo.InvariantCulture);
        csv.WriteRecords(NodesData); // where NodesData are your data
    }
}
public class TreeNodeInfo {
    public string Move { get; set; }
    public double Score { get; set; }
    public int Depth { get; set; }
    public double Alpha { get; set; }
    public double Beta { get; set; }
    public string Fen { get; set; }
    public string PrevMove { get; set; }
    public int AiWhite { get; set; }
}