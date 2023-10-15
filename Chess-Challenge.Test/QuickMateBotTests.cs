using Chess_Challenge.My_Bot;

namespace Chess_Challenge.Test;

#region

using ChessChallenge.API;
using Timer = ChessChallenge.API.Timer;

#endregion

public class QuickMateBotTests {
    [Fact]
    public void TestAvoidBlackCheckmateIn1() {
        // Arrange
        var board = Board.CreateBoardFromFEN("4k3/1Q6/2K5/8/8/8/8/8 b - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot() {
            MaxDepth = 2,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("e8f8", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestAvoidWhiteCheckmateIn1() {
        // Arrange
        var board = Board.CreateBoardFromFEN("4K3/1q6/2k5/8/8/8/8/8 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot() {
            MaxDepth = 2,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("e8f8", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestAvoidWhiteCheckmateIn1Depth4() {
        // Arrange
        var board = Board.CreateBoardFromFEN("4K3/1q6/2k5/8/8/8/8/8 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot() {
            MaxDepth = 4,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("e8f8", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCaptureHighestValuePieceDepth2() {
        // Arrange
        var board = Board.CreateBoardFromFEN("5k2/8/8/4q3/8/3N4/PPP2r2/1KR5 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot() {
            MaxDepth = 2,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("d3e5", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCaptureHighestValuePieceDepth4() {
        // Arrange
        var board = Board.CreateBoardFromFEN("5k2/8/8/4q3/8/3N4/PPP2r2/1KR5 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot() {
            MaxDepth = 4,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("d3e5", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCheckmateIn2() {
        // Arrange
        var board = Board.CreateBoardFromFEN("4k3/1p1NpN2/3R1R2/3K1P2/3PQ3/8/8/8 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 8,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("d5e6", board);
        Assert.Equal(expectedMove, move);
    }

    [Fact]
    public void TestCheckmateIn2_2() {
        // Arrange
        var board = Board.CreateBoardFromFEN("8/3pR3/N1Nk4/1p3Pp1/5q2/6B1/1K6/7Q w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 8,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("e7e4", board);
        Assert.Equal(expectedMove, move);
    }

    [Fact]
    public void TestCheckmateIn2_Depth2() {
        // Arrange
        var board = Board.CreateBoardFromFEN("8/3pR3/N1Nk4/1p3Pp1/5q2/6B1/1K6/7Q w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 4,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("e7e4", board);
        Assert.Equal(expectedMove, move);
    }

    [Fact]
    public void TestCheckmateIn3() {
        // Arrange
        var board = Board.CreateBoardFromFEN("r5rk/5p1p/5R2/4B3/8/8/7P/7K w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 8,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("f6a6", board);

        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCheckmateIn3_3rd() {
        // Arrange
        var board =
                Board.CreateBoardFromFEN("8/8/2K5/4kP2/3N4/7Q/8/2R5 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 8,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("c1h1", board);

        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCheckmateIn2_3() {
        // Arrange
        var board =
                Board.CreateBoardFromFEN("8/8/2K5/5P2/3N1k2/7Q/8/7R w - - 2 2");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 8,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("c6d5", board);

        Assert.Equal(expectedMove, move);
    }
    public void TestCheckmateIn3_2nd() {
        // Arrange
        var board =
                Board.CreateBoardFromFEN("r2q3k/1p2b3/n4pQ1/n7/p1PpPB1N/P7/1P5P/1R5K w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 8,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("f6a6", board);

        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCheckmateIn4() {
        // Arrange
        var board = Board.CreateBoardFromFEN("7Q/8/8/4K1k1/7r/8/8/8 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 8,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("h8g7", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestCheckmateInOneInsteadOfInTwo() {
        // Arrange
        var board = Board.CreateBoardFromFEN("2k5/4b3/2KQ4/8/8/8/8/8 w - - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot() {
            MaxDepth = 2,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("d6c7", board);
        Assert.Equal(expectedMove, move);
    }
    [Fact]
    public void TestDefendCheckmateIn2() {
        // Arrange
        var board =
                Board.CreateBoardFromFEN(
                        "rnbqk2r/ppp2ppp/5b2/3pp3/7P/P4P2/1PPPP3/RNBQKBNR w KQkq - 0 7");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 5,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var notExpectedMove = new Move("h4h5", board);
        Assert.NotEqual(notExpectedMove, move);
    }


    [Fact]
    public void TestForcedSequenceToWinQueen() {
        // Arrange
        var board = Board.CreateBoardFromFEN("r7/5k1p/pppp1NpQ/4p2n/3P4/2P3Pq/Pr6/5RK1 w - - 3 27");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 7,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("h6h7", board);
        Assert.Equal(expectedMove, move);
    }


    [Fact]
    public void TestStartFenScore0() {
        var board =
                Board.CreateBoardFromFEN(
                        "rnbqkbnr/pppppppp/8/8/8/8/PPPPPPPP/RNBQKBNR w KQkq - 0 1");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 4,
        };

        // Act
        bot.Think(board, timer);

        // Assert
        int expectedScore = 0;
        Assert.Equal(expectedScore, bot.BestScore);
    }
    [Fact]
    public void TestTakeQueenInsteadOfLoosing() {
        // Arrange
        var board = Board.CreateBoardFromFEN("8/8/8/8/8/3K3p/6Q1/3k4 b - - 1 2");
        var timer = new Timer(60 * 1000);
        var bot = new QuickMateBot {
            MaxDepth = 4,
        };

        // Act
        var move = bot.Think(board, timer);

        // Assert
        var expectedMove = new Move("h3g2", board);
        Assert.Equal(expectedMove, move);
    }
}