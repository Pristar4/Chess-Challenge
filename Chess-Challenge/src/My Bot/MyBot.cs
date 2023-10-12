using System;
using System.Diagnostics;
using System.Linq;
using ChessChallenge.API;


public class MyBot : IChessBot {
    public int maxDepth = 5;
    public int BestScore { get; private set; }
    private Stopwatch stopwatch;
    private double timeToUse;

    public Move Think(Board board, Timer timer)
    {
        var legalMoves = board.GetLegalMoves();
        legalMoves = legalMoves.OrderBy(x => Guid.NewGuid()).ToArray();

        Move bestMove = Move.NullMove;
        int bestScore = Int32.MinValue;

        // ############ Time management ############
        double moveOverhead = 10; // Adjust as needed
        double slowMover = 1;     // Adjust as needed

        double timeLeft = Math.Max(0, timer.MillisecondsRemaining - moveOverhead);
        double gameLength;

        if (board.PlyCount < 40) gameLength = 112.0; // Opening game
        else gameLength = 176.0;                     // Middle/endgame - This is a simplification

        stopwatch = Stopwatch.StartNew();
        timeToUse = timeLeft * slowMover *15 /
                    ((gameLength - board.PlyCount) * slowMover +7.5 - board.PlyCount);
        // #########################################
        int depth = 1;
        // depending on how many ply we have left, we can increase the depth
        // print time to use in seconds
        Console.WriteLine($"Time to use: {timeToUse / 1000} seconds");

        Move lastCompletedDepthBestMove = Move.NullMove;

        //while (stopwatch.ElapsedMilliseconds < timeToUse && depth <= maxDepth) {

        foreach (var move in legalMoves)
        {
            // Mate in one
            board.MakeMove(move);

            if (board.IsInCheckmate())
            {
                board.UndoMove(move);
                return move;
            }

            var score = Minimax(board, maxDepth, Int32.MinValue, Int32.MaxValue, false);
            board.UndoMove(move);

            if (score > bestScore)
            {
                bestScore = score;
                bestMove = move;
            }
        }
        /*
         if (stopwatch.ElapsedMilliseconds < timeToUse) {
            lastCompletedDepthBestMove = bestMove;
        }


        Console.WriteLine(
                $"Depth: {depth}, Time taken: {stopwatch.Elapsed} seconds, Best move: {bestMove}");
        depth++;
    } */

        Console.WriteLine($"Best move: {bestMove} with score {bestScore}");
        BestScore = bestScore;
        return bestMove;
        // return lastCompletedDepthBestMove;
    }

    private int Minimax(Board board, int depth, int alpha, int beta, bool maximizingPlayer)
    {
        if (depth == 0)
        {
            return AssessBoard(board);
        }

        Move[] moves = board.GetLegalMoves();
        // moves = moves.OrderBy(x => Guid.NewGuid()).ToArray();

        if (maximizingPlayer)
        {
            var maxEvaluation = Int32.MinValue;

            foreach (var move in moves)
            {
                board.MakeMove(move);
                var evaluation = Minimax(board, depth - 1, alpha, beta, false);
                board.UndoMove(move);

                maxEvaluation = Math.Max(maxEvaluation, evaluation);
                alpha = Math.Max(alpha, evaluation);

                if (alpha >= beta)
                {
                    break;
                }
            }

            return maxEvaluation;
        }
        else
        {
            var minEvaluation = Int32.MaxValue;

            foreach (var move in moves)
            {
                board.MakeMove(move);
                var evaluation = Minimax(board, depth - 1, alpha, beta, true);
                board.UndoMove(move);

                minEvaluation = Math.Min(minEvaluation, evaluation);
                beta = Math.Min(beta, evaluation);

                if (alpha >= beta)
                {
                    break;
                }
            }

            return minEvaluation;
        }
    }

    private int AssessBoard(Board board)
    {
        int score = 0;
        var allPieces = board.GetAllPieceLists();

        foreach (var pieceList in allPieces)
        {
            foreach (var piece in pieceList)
            {
                var pieceScore = GetScore(piece.PieceType);
                if(piece.IsWhite == board.IsWhiteToMove)
                {
                    score += pieceScore;
                }
                else
                {
                    score -= pieceScore;
                }
            }
        }

        return score;
    }

    private int GetScore(PieceType pieceType)
    {
        return pieceType switch
        {
            PieceType.Pawn => 100,
            PieceType.Knight => 300,
            PieceType.Bishop => 300,
            PieceType.Rook => 500,
            PieceType.Queen => 900,
            PieceType.King => 10000, // Some large number
            _ => 0,
        };
    }
}