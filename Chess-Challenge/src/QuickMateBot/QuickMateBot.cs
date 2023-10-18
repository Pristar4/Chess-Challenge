#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

#endregion

namespace Chess_Challenge.QuickMateBot;

public class QuickMateBot : IChessBot {
    private const int MateScore = 100000;

    private const int MaxScore = int.MaxValue;
    // CAUTION: MIN_SCORE  Causes integer overflow when negating without +1 or use -int.MaxValue
    private const int MinScore = int.MinValue + 1;
    private readonly bool _enableMoveOrdering = true;
    private readonly bool _enablePruning = true;
    private readonly bool _enableTranspositionTable = false;
    private readonly bool _printMoves = true;
    private readonly bool _printNodeCount = true;
    private readonly Stopwatch _stopwatch = new();

    //Transposition table || Hash Table
    private readonly Dictionary<ulong, int> _transpositionTable = new();
    private int _cacheHits;

    private bool _isPlayerAWhite;
    private int _localNodeCount;
    private int _pruningCount;
    private int _totalLookups;


    public int MaxDepth { get; init; } = 4;
    private Dictionary<Move, int> MoveCounts { get; } = new();

    private int NodeCount { get; set; }


    public Move Think(Board board, Timer timer) {
        _transpositionTable.Clear();
        var legalMoves = board.GetLegalMoves();

        var bestMove = Move.NullMove;
        int bestScore = MinScore;
        _isPlayerAWhite = board.IsWhiteToMove;

        _stopwatch.Restart();

        const int evSign = 1; //assuming bot is maximizing


        foreach (var move in legalMoves) {
            _localNodeCount = 0;
            // Mate in one
            board.MakeMove(move);

            int score = -Search(board, MaxDepth - 1, 1, MinScore, MaxScore, -evSign);
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
                Console.WriteLine($"Move: {replace}, Score: {score}");
            }
        }

        if (_printNodeCount) PrintNodes();

        Console.WriteLine(
                $"QuickMateBot:  Depth: {MaxDepth}, Time: {_stopwatch.ElapsedMilliseconds} ms, Score: {bestScore}, Best Move:{bestMove} Nodes: {NodeCount} NPS: {NodeCount / (_stopwatch.ElapsedMilliseconds / 1000.0):0.00}");
        double cacheHitRatio = (double)_cacheHits / _totalLookups;
        Console.WriteLine($"Cache Hit Ratio: {cacheHitRatio:0.00}%");


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
        _totalLookups++;


        // Check if this position has been evaluated before
        if (_enableTranspositionTable &&
            _transpositionTable.TryGetValue(board.ZobristKey, out int score)) {
            _cacheHits++;
            return score;
        }

        score = 0;

        if (board.IsInCheckmate()) {
            score = EvaluateCheckmate(board, ply);
        } else if (!board.IsDraw()) {
            score = EvaluateBoard(board);
        }

        if (_enableTranspositionTable) {
            _transpositionTable[board.ZobristKey] = score;
        }

        return score;
    }
    private int EvaluateBoard(Board board) {
        int score = 0;

        foreach (var pieceList in board.GetAllPieceLists()) {
            foreach (var piece in pieceList) {
                int pieceScore = GetScore(piece.PieceType);
                score += piece.IsWhite == _isPlayerAWhite ? pieceScore : -pieceScore;
            }
        }

        return score;
    }
    private int EvaluateCheckmate(Board board, int ply) {
        int score = MateScore - (ply - 1);
        return board.IsWhiteToMove == _isPlayerAWhite ? -score : score;
    }

    private int GetScore(PieceType pieceType) {
        return pieceType switch {
            PieceType.Pawn => 100,
            PieceType.Knight => 300,
            PieceType.Bishop => 300,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => MaxScore / 2 + 1,
            _ => 0,
        };
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
        var opponentPieceList = board.GetPieceList(PieceType.Pawn, !board.IsWhiteToMove);

        int score = 0;

        foreach (var piece in opponentPieceList) {
            ulong opponentPawnAttacks = BitboardHelper.GetPawnAttacks(piece.Square, piece.IsWhite);

            if (BitboardHelper.SquareIsSet(opponentPawnAttacks, move.TargetSquare)) {
                score -= 50;
            }
        }

        if (moveIsCapture) {
            score += GetScore(move.CapturePieceType) * 100;
        }

        if (move.IsPromotion) {
            score += GetScore(move.PromotionPieceType) * 100;
        }

        if (isInCheck) {
            score += 1000;
        }

        if (isInCheckmate) {
            score += 100000;
        }

        board.UndoMove(move);

        return score;
    }
    private int Search(Board board, int depth, int rootDepth, int alpha, int beta, int evSign
    ) {
        if (board.IsInCheckmate() || board.IsDraw()) {
            _localNodeCount++;
            return evSign * Evaluate(board, rootDepth);
        }

        if (depth == 0) {
            _localNodeCount++;

            return evSign * Evaluate(board, rootDepth);
        }


        int positionValue = -MaxScore;
        var moves = board.GetLegalMoves();


        if (_enableMoveOrdering) {
            moves = OrderMoves(moves, board);
        }


        int moveCount = moves.Length;
        int processedMoves = 0;

        foreach (var move in moves) {
            board.MakeMove(move);
            processedMoves++;

            int newValue = -Search(board, depth - 1, rootDepth + 1, -beta, -alpha, -evSign);


            board.UndoMove(move);


            // Update the best local value
            positionValue = Math.Max(positionValue, newValue);


            // Update alpha
            alpha = Math.Max(alpha, newValue);

            if (_enablePruning && alpha >= beta) { // Beta cut-off
                _pruningCount += moveCount - processedMoves;
                break;
            }
        }

        return positionValue;
    }
}