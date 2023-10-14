#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

#endregion

namespace Chess_Challenge.My_Bot;

public class QuickMateBot : IChessBot {
    private readonly bool _printNodeCount = false;
    private readonly bool _printMoves = true;
    private const int MATE_SCORE = 100000;
    private readonly Stopwatch _stopwatch = new();
    private int _localNodeCount;
    private int _pruningCount;


    public int MaxDepth { get; set; } = 8;
    public Dictionary<Move, int> MoveCounts { get; set; } = new();

    public int NodeCount { get; private set; }
    public int BestScore => 0;

    //Transposition table
    private Dictionary<ulong, int> transpositionTable = new();

    public Move Think(Board board, Timer timer) {
        transpositionTable.Clear();
        var legalMoves = board.GetLegalMoves();
        legalMoves = legalMoves.OrderBy(_ => Guid.NewGuid()).ToArray();

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


                int score = -NegaMax(board, MaxDepth-1, int.MinValue, int.MaxValue);
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
        return bestMove;
    }
    private int Evaluate(Board board, int depth) {
        int score = 0;
        var allPieces = board.GetAllPieceLists();
        int who2Move;

        ulong key = board.ZobristKey;

        // Check if this position has been evaluated before
        if (transpositionTable.TryGetValue(key, out score)) {
            // if we have, return the stored evaluation.
            return score;
        }

        if (board.IsWhiteToMove) {
            who2Move = 1;
        } else {
            who2Move = -1;
        }

        if (board.IsInCheckmate()) {
            return who2Move * (MATE_SCORE + depth);
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

    private int NegaMax(Board board, int depth, int alpha, int beta) {
        if (board.IsInCheckmate() || board.IsInStalemate()) {
            _localNodeCount++;
            return Evaluate(board, depth);
        }

        if (depth == 0) {
            _localNodeCount++;
            //TODO: Check why  using QuiescenceSearch at depth 4 breaks the bot
            // return QuiescenceSearch(board, alpha, beta);
            return Evaluate(board, depth);
        }


        int score = int.MinValue;
        var moves = board.GetLegalMoves();
        moves = OrderMoves(moves, board);

        int moveCount = moves.Length;
        int processedMoves = 0;
        // nodeCount += moves.Length;
        // moves = OrderMoves(moves, board);

        foreach (var move in moves) {
            board.MakeMove(move);
            processedMoves++;

            score = Math.Max(score, -NegaMax(board, depth - 1, -beta, -alpha));
            board.UndoMove(move);


            alpha = Math.Max(alpha, score);

            if (alpha >= beta) {
                _pruningCount += (moveCount - processedMoves);
                break;
            }
        }

        return score;
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
}