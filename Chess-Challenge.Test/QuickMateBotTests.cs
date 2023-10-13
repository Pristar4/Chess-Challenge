﻿using Chess_Challenge.My_Bot;

namespace Chess_Challenge.Test;

#region

using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

#endregion


public class QuickMateBotTests {
    [Fact]
    public void TestCaptureHighestValuePiece() {
        // Arrange
        var board = Board.CreateBoardFromFEN("5k2/8/8/4q3/8/3N4/PPP2r2/1KR5 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot() {
            MaxDepth = 3,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("d3e5", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCheckmateInTwo() {
        // Arrange
        var board = Board.CreateBoardFromFEN("8/3pR3/N1Nk4/1p3Pp1/5q2/6B1/1K6/7Q w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 3,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("e7e4", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCheckmateInTwoDepthFive() {
        // Arrange
        var board = Board.CreateBoardFromFEN("8/3pR3/N1Nk4/1p3Pp1/5q2/6B1/1K6/7Q w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 5,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("e7e4", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
         public void TestAvoidCheckmateInOne() {
             // Arrange
             var board = Board.CreateBoardFromFEN("4k3/1Q6/2K5/8/8/8/8/8 b - - 0 1");
             var timer = new Timer(60 * 1000);
             var bot = new QuickMateBot() {
                 MaxDepth = 3,
             };

             // Act
             var move = bot.Think(board, timer);

             // Assert
             var expectedMove = new Move("e8f8", board);
             Assert.Equal(expectedMove, move);
         }
         [Fact]
         public void TestCheckmateInOneInsteadOfInTwo() {
             // Arrange
             var board = Board.CreateBoardFromFEN("2k5/4b3/2KQ4/8/8/8/8/8 w - - 0 1");
             var timer = new Timer(60 * 1000);
             var bot = new QuickMateBot() {
                 MaxDepth = 3,
             };

             // Act
             var move = bot.Think(board, timer);

             // Assert
             var expectedMove = new Move("d6c7", board);
             Assert.Equal(expectedMove, move);
         }


    [Fact]
    public void TestForcedSequenceToWinQueen() {
        // Arrange
        var board = Board.CreateBoardFromFEN("r7/5k1p/pppp1NpQ/4p2n/3P4/2P3Pq/Pr6/5RK1 w - - 3 27");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 6,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("h6h7", board);
        Assert.Equal(expectedMove, move);
    }


    [Fact]
    public void TestStartFenScoreZero() {
        var board =
                Board.CreateBoardFromFEN(
                        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 3,
        };

        // Act
        bot.Think(board, timer);

        // Assert
        int expectedScore = 0;
        Assert.Equal(expectedScore, bot.BestScore);
    }
}
