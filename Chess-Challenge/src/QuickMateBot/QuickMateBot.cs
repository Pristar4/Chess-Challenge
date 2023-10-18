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
    private readonly bool _enablePrintNodes = false;
    private readonly bool _enablePruning = true;
    private readonly bool _printMoves = false;
    private readonly Stopwatch _stopwatch = new();

    private int _cacheHits;
    private bool _isPlayerAWhite;
    private int _localNodeCount;
    private int _pruningCount;
    private int _totalLookups;
    // Time to use in milliseconds
    private const int TimeToUse = 5000;
    private bool _outOfTime;
    private Move _bestPreviousMove = Move.NullMove;
    private int _bestPreviousScore = -MaxScore;


    public int MaxDepth { get; init; } = 256;
    private int NodeCount { get; set; }
    private Dictionary<Move, int> MoveCounts { get; } = new();


    public Move Think(Board board, Timer timer) {
        InitializeThink(board);
        IdentifyBestMove(board, out var bestMove, out int bestScore);
        PrintStatistics(bestScore, bestMove);
        _stopwatch.Stop();
        return bestMove;
    }
    private int Evaluate(Board board, int ply) {
        _totalLookups++;



        int score = 0;

        if (board.IsInCheckmate()) {
            score = EvaluateCheckmate(board, ply);
        } else if (!board.IsDraw()) {
            score = EvaluateBoard(board);
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
    private void IdentifyBestMove(Board board, out Move bestMove, out int bestScore) {
        var legalMoves = board.GetLegalMoves();
        bestMove = Move.NullMove;
        bestScore = MinScore;

        const int evSign = 1; //assuming bot is maximizing

        for (int depth = 1; (depth <= MaxDepth) && !_outOfTime; depth++) {
            bestMove = Move.NullMove;
            bestScore = MinScore;
            MoveCounts.Clear();
            NodeCount = 0;
            foreach (var move in legalMoves) {
                if (_stopwatch.ElapsedMilliseconds >= TimeToUse) {
                    _outOfTime = true;
                    bestMove = _bestPreviousMove;
                    bestScore = _bestPreviousScore;
                    break;
                }
                _localNodeCount = 0;

                board.MakeMove(move);
                int score = -Search(board, depth - 1, 1, MinScore, MaxScore, -evSign);
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
            Console.WriteLine(
                    $"QuickMateBot:  Depth: {depth}, Time: {_stopwatch.ElapsedMilliseconds} ms, Score: {bestScore}, Best Move:{bestMove} Nodes: {NodeCount} NPS: {NodeCount / (_stopwatch.ElapsedMilliseconds / 1000.0):0.00}");

            // for now only if depth is even


            if (_outOfTime) {
                break;
            }

            if (depth % 2 == 0){
                _bestPreviousMove = bestMove;
                _bestPreviousScore = bestScore;
            }

        }

    }
    private void InitializeThink(Board board) {
        _stopwatch.Restart();
        _outOfTime = false;
        _isPlayerAWhite = board.IsWhiteToMove;
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
    private void PrintStatistics(int bestScore, Move bestMove) {
        if (_enablePrintNodes) {
            PrintNodes();
        }

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

            return evSign * SearchAllCaptures(board,alpha,beta, rootDepth,evSign);
        }
        int positionValue = -MaxScore;
        var moves = board.GetLegalMoves().ToList();


        if (_enableMoveOrdering) {
            moves = OrderMoves(moves, board).ToList();
        }


        int moveCount = moves.Count;
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

    private int SearchAllCaptures(Board board, int alpha, int beta, int rootDepth,int evSign) {
        // Captures aren't typically forced, so see what the eval is before making a capture.
        // Otherwise if only bad captures are available, the position will be evaluated as bad.
        // even if good non-capture moves exist.
        int eval = evSign * Evaluate(board, rootDepth);

        if (eval >= beta) {
            return beta;
        }
        alpha = Math.Max(alpha, eval);
        var captureMoves = board.GetLegalMoves().Where(m => m.IsCapture);
        if (_enableMoveOrdering) {
            captureMoves = OrderMoves(captureMoves, board);
        }

        foreach (var captureMove in captureMoves) {
            board.MakeMove(captureMove);
            eval = -SearchAllCaptures(board, -beta, -alpha, rootDepth + 1,-evSign);
            board.UndoMove(captureMove);

            if (eval >= beta) {
                return beta;
            }
            alpha = Math.Max(alpha, eval);

        }

        return alpha;


    }
}