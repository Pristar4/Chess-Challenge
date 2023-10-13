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
    private readonly bool _printMoves = false;
        private const int MAX_PIECE_SCORE = 100000;
    private readonly Stopwatch _stopwatch = new();
        private int _localNodeCount;
        private int _pruningCount;

        public int MaxDepth { get; set; } = 4;
        public Dictionary<Move, int> MoveCounts { get; set; } = new();

    public int NodeCount { get; private set; }
    public int BestScore => 0;

    public Move Think(Board board, Timer timer) {
        var legalMoves = board.GetLegalMoves();
        legalMoves = legalMoves.OrderBy(_ => Guid.NewGuid()).ToArray();

        var bestMove = Move.NullMove;
        int bestScore = int.MinValue;

        _stopwatch.Restart();

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

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }

            if (_printMoves) {
                var replace = move.ToString().Replace("\'", "").Replace("Move: ", "");
                Console.WriteLine($"Move: {replace}, Score: {score}");
            }
        }

        _stopwatch.Stop();
        if (_printNodeCount) PrintNodes();

        Console.WriteLine(
                $"QuickMateBot:  Depth: {MaxDepth}, Time: {_stopwatch.ElapsedMilliseconds} ms, Score: {bestScore}, Best Move:{bestMove} Nodes: {NodeCount}");
        return bestMove;
    }
    private int Evaluate(Board board, int depth) {
        int score = 0;
        var allPieces = board.GetAllPieceLists();
        int who2Move;
        if (board.IsWhiteToMove) {
            who2Move = 1;
        } else {
            who2Move = -1;
        }

        if (board.IsInCheckmate()) {
            return who2Move * (MAX_PIECE_SCORE + depth);
              
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
        if (depth == 0 || board.IsInCheckmate() || board.IsInStalemate()) {
            if (board.IsInCheckmate()) {


            }
            _localNodeCount++;
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
            score += GetScore(move.CapturePieceType)* 100;
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