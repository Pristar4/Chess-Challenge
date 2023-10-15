﻿#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;
#endregion


public class MyBot : IChessBot {
    private readonly bool _printNodeCount = false;
    private readonly bool _printMoves = true;
    private const int MATE_SCORE = 100000;
    private readonly Stopwatch _stopwatch = new();
    private int _localNodeCount;
    private int _pruningCount;

    private int leastPlyMate = int.MaxValue;
    private int leastPlyMateScore = int.MaxValue;


    public int MaxDepth { get; set; } =4;
    public Dictionary<Move, int> MoveCounts { get; set; } = new();

    public int NodeCount { get; private set; }
    public int BestScore => 0;

    private bool isPlayerAWhite;

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
        isPlayerAWhite = board.IsWhiteToMove;

        _stopwatch.Restart();

        int ev_sign = 1; //assuming bot is maximizing


        foreach (var move in legalMoves) {
            _localNodeCount = 0;
            // Mate in one
            board.MakeMove(move);

            if (board.IsInCheckmate()) {
                Console.WriteLine("Raw  Checkmate in 1");
                board.UndoMove(move);
                return move;
            }


            int score = -NegaMax(board, MaxDepth - 1, 1, int.MinValue, int.MaxValue, -ev_sign,
                                 move);
            board.UndoMove(move);

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
                $"QuickMateBotV1:  Depth: {MaxDepth}, Time: {_stopwatch.ElapsedMilliseconds} ms, Score: {bestScore}, Best Move:{bestMove} Nodes: {NodeCount} NPS: {(NodeCount / (_stopwatch.ElapsedMilliseconds / 1000.0)).ToString("0.00")}");

        _stopwatch.Stop();
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

            if (ply < leastPlyMate) {
                leastPlyMate = ply;
                int movesToMate = (ply+1) / 2;
                //TODO: doesnt Print if the bot gets mated from the opponent
                // example output for such a case is:
                //  Move: e3e2, Score: -99994
                // Move: e3f2, Score: -99994
                // Move: e3f4, Score: -99992

                Console.WriteLine("Mate in " + movesToMate + " moves");
            }

            if (board.IsWhiteToMove) {
                return (isPlayerAWhite) ? -(MATE_SCORE - ply) : (MATE_SCORE - ply);
            } else {
                return (isPlayerAWhite) ? (MATE_SCORE - ply) : -(MATE_SCORE - ply);
            }
        } else {
            if (board.IsDraw()) {
                return 0;
            }

        }


        foreach (var pieceList in allPieces) {
            foreach (var piece in pieceList) {
                int pieceScore = GetScore(piece.PieceType);

                if (piece.IsWhite == isPlayerAWhite) {
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
    /// <param name="rootDepth">
    /// rootDepth ist die Anzahl der Halbzüge, die seit dem Start der Suche gemacht wurden.
    /// </param>
    /// <param name="alpha"></param>
    /// <param name="beta"></param>
    /// <param name="ev_sign">
    /// ev_sign ist das Vorzeichen der Bewertungsfunktion (1 wenn der Bot maximiert, -1 wenn er minimiert).
    /// </param>
    /// <param name="prevMove"></param>
    /// <returns></returns>
    private int NegaMax(Board board, int depth, int rootDepth, int alpha, int beta, int ev_sign,
                        Move prevMove) {
        if (board.IsInCheckmate()) {
            _localNodeCount++;
            return ev_sign * Evaluate(board, rootDepth);
            //TODO: Check why  using QuiescenceSearch at depth 4 breaks the bot
        }


        if (depth == 1) {
            _localNodeCount++;

            return ev_sign * Evaluate(board, rootDepth);
        }


        int positionValue = int.MinValue;
        var moves = board.GetLegalMoves();
        moves = OrderMoves(moves, board);

        int moveCount = moves.Length;
        int processedMoves = 0;

        foreach (var move in moves) {
            board.MakeMove(move);
            processedMoves++;

            int newValue = -NegaMax(board, depth - 1, rootDepth + 1, -beta, -alpha, -ev_sign, move);


            board.UndoMove(move);


            // Update the best local value
            positionValue = Math.Max(positionValue, newValue);


            // Update alpha
            alpha = Math.Max(alpha, newValue);

            // Beta cut-off
            if (alpha >= beta) {
                _pruningCount += (moveCount - processedMoves);
                //TODO: Fix that Pruning prunes fastest mate sometimes
                // maybe the problem is just the score of the mate ? i dont know yet

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
        bool moveIsCapture = move.IsCapture;

        int score = 0;


        if (moveIsCapture) {
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



}
