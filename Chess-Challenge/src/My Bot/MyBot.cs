#region

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;

#endregion


public class MyBot : IChessBot {
    public int MaxDepth = 3;
    private Stopwatch stopwatch = new Stopwatch();
    public long PositionsEvaluated { get; private set; }
    public int BestScore { get; private set; }

    public Move Think(Board board, Timer timer) {
        PositionsEvaluated = 0;
        var legalMoves = board.GetLegalMoves();
        legalMoves = legalMoves.OrderBy(_ => Guid.NewGuid()).ToArray();

        var bestMove = Move.NullMove;
        int bestScore = int.MinValue;

        stopwatch.Restart();

        foreach (var move in legalMoves) {
            // Mate in one
            board.MakeMove(move);

            if (board.IsInCheckmate()) {
                Console.WriteLine("Raw  Checkmate in 1");
                board.UndoMove(move);
                return move;
            }

            int score = -NegaMax(board, MaxDepth, int.MinValue, int.MaxValue);
            board.UndoMove(move);

            if (score > bestScore) {
                bestScore = score;
                bestMove = move;
            }
        }

        stopwatch.Stop();
        Console.WriteLine(
                $"Depth: {MaxDepth}, Time: {stopwatch.ElapsedMilliseconds} ms, Score: {bestScore}, Best Move:{bestMove} Positions: {PositionsEvaluated}");
        return bestMove;
    }
    private int AssessBoard(Board board) {
        int score = 0;
        var allPieces = board.GetAllPieceLists();
        // if checkmate, return max score
        if (board.IsInCheckmate()) {
            return int.MaxValue;
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

    private int NegaMax(Board board, int depth, int alpha, int beta ) {
        if (depth == 0 || board.IsInCheckmate() || board.IsInStalemate()) {
            if (board.IsInCheckmate()) {
            }
            PositionsEvaluated++;
            return AssessBoard(board);
        }

        var moves = board.GetLegalMoves();
        // Console.WriteLine("Moves: {moves}");

        moves = OrderMoves(moves, board);
        // Console.WriteLine($"Ordered moves: {moves}");

        int score = int.MinValue;

        foreach (var move in moves) {
            board.MakeMove(move);
            score = Math.Max(score, -NegaMax(board, depth - 1, -beta, -alpha));
            board.UndoMove(move);

            alpha = Math.Max(alpha, score);

            if (alpha >= beta) {
                break;
            }
        }

        return score;
    }
    private static readonly Random
            rng = new Random(); // used to randomly order moves of equal value

    private Move[] OrderMoves(IEnumerable<Move> moves, Board board) {
        var movesWithScore = moves.Select(move => new {move, score = ScoreMove(move, board)});
        var orderedMoves = movesWithScore.OrderByDescending(x => x.score).Select(x => x.move)
                .ToArray();
        return orderedMoves;
    }

    private int ScoreMove(Move move, Board board) {
        board.MakeMove(move);

        var score = 0;

        if (board.IsInCheck()) {
            score += 10000;
        } else if (move.IsCapture != null) {
            score += 2000;
        }

        // score += rng.Next(10);

        board.UndoMove(move);

        return score;
    }
}