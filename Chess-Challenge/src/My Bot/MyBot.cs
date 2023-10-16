﻿#region

#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

#endregion

namespace Chess_Challenge.My_Bot;

#endregion

public class MyBot : IChessBot {
    private const int MATE_SCORE = 100000;

    private const int MAX_SCORE = int.MaxValue;
    // CAUTION: MIN_SCORE  Causes integer overflow when negating without +1 or use -int.MaxValue
    private const int MIN_SCORE = int.MinValue + 1;
    private readonly bool _enableMoveOrdering = true;
    private readonly bool _enablePruning = true;
    // WARNING: SAVING NODES DATA WILL SLOW DOWN THE BOT SIGNIFICANTLY
    // TODO : Maybe only save important nodes pv variation ?
    private readonly bool _enableTranspositionTable = true;
    private readonly bool _printMoves = false;
    private readonly bool _printNodeCount = false;
    private readonly Stopwatch _stopwatch = new();
    private int _localNodeCount;
    private int _pruningCount;
    private int cacheHits;

    private bool isPlayerAWhite;

    private int leastPlyMate = MAX_SCORE;
    private int leastPlyMateScore = MAX_SCORE;
    private int totalLookups;

    //Transposition table || Hash Table
    private readonly Dictionary<ulong, int> transpositionTable = new();


    public int MaxDepth { get; set; } = 4;
    public Dictionary<Move, int> MoveCounts { get; set; } = new();

    public int NodeCount { get; private set; }
    public int BestScore
    {
        get => 0;
    }

    public Move Think(Board board, Timer timer) {
        transpositionTable.Clear();
        var legalMoves = board.GetLegalMoves();
        // legalMoves = legalMoves.OrderBy(_ => Guid.NewGuid()).ToArray();

        var bestMove = Move.NullMove;
        int bestScore = MIN_SCORE;
        int timeBuffer = 500;
        int totalTime = timer.MillisecondsRemaining;
        int increment = timer.IncrementMilliseconds;
        double averageTimePerMove = (totalTime - timeBuffer) / 40.0 + increment;
        double mainTimeForCurrentMove = averageTimePerMove + 0.1 * (totalTime - timeBuffer);
        double maxTimeForMove = averageTimePerMove + 1.0 * (totalTime - timeBuffer);
        double minTimeForCurrentMove = 0.05 * totalTime;
        isPlayerAWhite = board.IsWhiteToMove;

        _stopwatch.Restart();

        int ev_sign = 1; //assuming bot is maximizing


        foreach (var move in legalMoves) {
            _localNodeCount = 0;
            // Mate in one
            board.MakeMove(move);

            /*if (board.IsInCheckmate()) {
                Console.WriteLine("Raw  Checkmate in 1");
                board.UndoMove(move);
                return move;
            }*/

            int score = -NegaMax(board, MaxDepth - 1, 1, MIN_SCORE, MAX_SCORE, -ev_sign,
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
                string replace = move.ToString().Replace("\'", "").Replace("Move: ", "");
                Console.WriteLine($"Move: {replace}, Score: {score / 100}");
            }
        }

        if (_printNodeCount) PrintNodes();

        // TODO : print in centipawns so that we can compare with stockfish
        // so the score of one pawn was 100 so print score/100
        Console.WriteLine(
                $"MyBot:  Depth: {MaxDepth}, Time: {_stopwatch.ElapsedMilliseconds} ms, Score: {bestScore / 100}, Best Move:{bestMove} Nodes: {NodeCount} NPS: {(NodeCount / (_stopwatch.ElapsedMilliseconds / 1000.0)).ToString("0.00")}");
        double cacheHitRatio = (double)cacheHits / totalLookups;
        // TODO : print if the best move / bestscore  comes from a lookup;


        _stopwatch.Stop();


        return bestMove;
    }
    /// <summary>
    /// </summary>
    /// <param name="board"></param>
    /// <param name="ply"></param>
    /// <returns>
    ///     Eva
    /// </returns>
    private int Evaluate(Board board, int ply) {
        totalLookups++;

        int score = 0;
        ulong key = board.ZobristKey;
        var allPieces = board.GetAllPieceLists();

        // Check if this position has been evaluated before
        if (_enableTranspositionTable && transpositionTable.TryGetValue(key, out score)) {
            cacheHits++;
            return score;
        }

        if (board.IsInCheckmate()) {
            if (ply < leastPlyMate) {
                leastPlyMate = ply;
                int movesToMate = ply / 2;
                // Console.WriteLine("Mate in " + movesToMate + " moves");
            }
            /*
            score = (isPlayerAWhite == board.IsWhiteToMove)
                    ? MATE_SCORE - (ply - 1)
                    : -(MATE_SCORE - (ply - 1));
                    */


            if (board.IsWhiteToMove) {
                return isPlayerAWhite ? -(MATE_SCORE - (ply - 1)) : MATE_SCORE - (ply - 1);
            }

            return isPlayerAWhite ? MATE_SCORE - (ply - 1) : -(MATE_SCORE - (ply - 1));
        }

        if (!board.IsDraw()) {
            foreach (var pieceList in allPieces) {
                foreach (var piece in pieceList) {
                    // score += (piece.IsWhite == isPlayerAWhite ? 1 : -1) * GetScore(piece.PieceType);

                    int pieceScore = GetScore(piece.PieceType);

                    if (piece.IsWhite == isPlayerAWhite) {
                        score += pieceScore;
                    } else {
                        score -= pieceScore;
                    }
                }
            }
        }

        if (_enableTranspositionTable) {
            transpositionTable[key] = score;
        }

        return score;
    }

    /// <summary>
    ///     Score is in pawn units (1 pawn = 100)
    ///     TODO: Print score in centipawns
    /// </summary>
    /// <param name="pieceType"></param>
    /// <returns></returns>
    private int GetScore(PieceType pieceType) {
        return pieceType switch {
            PieceType.Pawn => 100,
            PieceType.Knight => 300,
            PieceType.Bishop => 300,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => MAX_SCORE / 2 + 1,
            _ => 0,
        };
    }


    /// <summary>
    /// </summary>
    /// <param name="board"></param>
    /// <param name="depth">
    ///     depth ist die Maximaltiefe (in plies), die noch gesucht werden soll.
    /// </param>
    /// <param name="rootDepth">
    ///     rootDepth ist die Anzahl der Halbzüge, die seit dem Start der Suche gemacht wurden.
    /// </param>
    /// <param name="alpha"></param>
    /// <param name="beta"></param>
    /// <param name="ev_sign">
    ///     ev_sign ist das Vorzeichen der Bewertungsfunktion (1 wenn der Bot maximiert, -1 wenn er
    ///     minimiert).
    /// </param>
    /// <param name="prevMove"></param>
    /// <returns></returns>
    private int NegaMax(Board board, int depth, int rootDepth, int alpha, int beta, int ev_sign,
                        Move prevMove) {
        if (board.IsInCheckmate() || board.IsDraw()) {
            _localNodeCount++;
            return ev_sign * Evaluate(board, rootDepth);
        }


        if (depth == 0) {
            _localNodeCount++;

            // Console.WriteLine($"alpha {alpha} beta {beta}");
            return ev_sign * Evaluate(board, rootDepth);
            // return ev_sign * -QuiescenceSearch(board, -beta, -alpha, -ev_sign);
        }


        int positionValue = -MAX_SCORE;
        var moves = board.GetLegalMoves();


        if (_enableMoveOrdering) {
            moves = OrderMoves(moves, board);
        }


        int moveCount = moves.Length;
        int processedMoves = 0;
        // nodeCount += moves.Length;

        foreach (var move in moves) {
            board.MakeMove(move);
            processedMoves++;

            int newValue = -NegaMax(board, depth - 1, rootDepth + 1, -beta, -alpha, -ev_sign, move);
            string moveString = move.ToString().Replace("Move: ", "").Replace("\'", "");
            string preMoveString = prevMove.ToString().Replace("Move: ", "").Replace("\'", "");


            board.UndoMove(move);


            // Update the best local value
            positionValue = Math.Max(positionValue, newValue);


            // Update alpha
            alpha = Math.Max(alpha, newValue);

            if (_enablePruning) { // Beta cut-off
                if (alpha >= beta) {
                    _pruningCount += moveCount - processedMoves;
                    break;
                }
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

    private int QuiescenceSearch(Board board, int alpha, int beta, int ev_sign) {
        //  if board.IsWhiteToMove && isPlayerAWhite = positive score
        //  if board.IsWhiteToMove && !isPlayerAWhite = negative score
        int stand_pat = Evaluate(board, 0);

        if (stand_pat >= beta) {
            return beta;
        }

        if (alpha < stand_pat) {
            alpha = stand_pat;
        }

        var legalMoves = board.GetLegalMoves();

        foreach (var move in legalMoves) {
            if (move.IsCapture) {
                int scoreAfterMove = stand_pat + GetScore(move.CapturePieceType) * 100;

                if (scoreAfterMove < alpha) {
                    break;
                }

                board.MakeMove(move);
                int score = -QuiescenceSearch(board, -beta, -alpha, -ev_sign);
                board.UndoMove(move);
                if (score >= beta)
                    return beta;
                if (score > alpha)
                    alpha = score;
            }
        }

        return alpha;
    }

    private int ScoreMove(Move move, Board board) {
        board.MakeMove(move);
        bool isInCheck = board.IsInCheck();
        bool isInCheckmate = board.IsInCheckmate();
        bool moveIsCapture = move.IsCapture;
        var opponentPieceList = board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove);

        int score = 0;

        foreach (var piece in opponentPieceList) {
            ulong opponentPawnAttacks = BitboardHelper.GetPawnAttacks(piece.Square, piece.IsWhite);

            if (BitboardHelper.SquareIsSet(opponentPawnAttacks, move.TargetSquare)) {
                // Print here the piece putting in danger the movement or simply decrease the score
                // Console.WriteLine("Pawn at {0} could capture piece at {1}", piece.Square.Name, move.TargetSquare.Name);
                score -= 50;
            }
        }

        if (moveIsCapture) {
            score += GetScore(move.CapturePieceType) * 100;
        }

        if (move.IsPromotion) {
            score += GetScore(move.PromotionPieceType) * 100;
        }
        // if movepiece can get captured by opponent pawn after the move

        if (isInCheck) {
            score += 1000;
        }

        if (isInCheckmate) {
            score += 100000;
        }

        board.UndoMove(move);

        return score;
    }
}