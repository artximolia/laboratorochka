using System;
using System.Collections.Generic;

namespace TicTacToe
{
    public enum Mark
    {
        Empty,
        Cross,
        Nought
    }

    public class GameState
    {
        private static readonly int[][] Lines =
        {
            new[] { 0, 1, 2 }, new[] { 3, 4, 5 }, new[] { 6, 7, 8 },
            new[] { 0, 3, 6 }, new[] { 1, 4, 7 }, new[] { 2, 5, 8 },
            new[] { 0, 4, 8 }, new[] { 2, 4, 6 }
        };

        private readonly Mark[] board;

        public Mark CurrentPlayer { get; private set; }
        public Mark this[int index] => board[index];
        public int MovesPlayed { get; private set; }
        public bool IsTerminal => Winner != Mark.Empty || MovesPlayed == 9;
        public bool IsDraw => Winner == Mark.Empty && MovesPlayed == 9;

        public Mark Winner
        {
            get
            {
                int line = FindWinningLine();
                if (line == -1)
                    return Mark.Empty;

                return board[Lines[line][0]];
            }
        }

        public GameState()
        {
            board = new Mark[9];
            CurrentPlayer = Mark.Cross;
        }

        private GameState(GameState source)
        {
            board = (Mark[])source.board.Clone();
            CurrentPlayer = source.CurrentPlayer;
            MovesPlayed = source.MovesPlayed;
        }

        public List<int> GetLegalMoves()
        {
            var moves = new List<int>();
            if (IsTerminal)
                return moves;

            for (int i = 0; i < board.Length; i++)
            {
                if (board[i] == Mark.Empty)
                    moves.Add(i);
            }
            return moves;
        }

        public GameState ApplyMove(int index)
        {
            if (index < 0 || index >= 9 || IsTerminal || board[index] != Mark.Empty)
                throw new InvalidOperationException("Недопустимый ход.");

            var next = new GameState(this);
            next.board[index] = CurrentPlayer;
            next.CurrentPlayer = Opponent(CurrentPlayer);
            next.MovesPlayed++;
            return next;
        }

        public int[] GetWinningLine()
        {
            int line = FindWinningLine();
            if (line == -1)
                return Array.Empty<int>();

            return (int[])Lines[line].Clone();
        }

        private int FindWinningLine()
        {
            for (int i = 0; i < Lines.Length; i++)
            {
                int a = Lines[i][0];
                int b = Lines[i][1];
                int c = Lines[i][2];

                if (board[a] != Mark.Empty && board[a] == board[b] && board[b] == board[c])
                    return i;
            }
            return -1;
        }

        public static Mark Opponent(Mark mark)
        {
            return mark == Mark.Cross ? Mark.Nought : Mark.Cross;
        }
    }
}
