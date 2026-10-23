using System;
using System.Diagnostics;

namespace TicTacToe
{
    public class SearchResult
    {
        public int Move = -1;
        public int Score;
        public int Nodes;
        public int Depth;
        public double Milliseconds;
        public int?[] MoveScores = new int?[9];
    }

    public class MinimaxAI
    {
        private static readonly int[] MoveOrder = { 4, 0, 2, 6, 8, 1, 3, 5, 7 };
        private readonly Mark aiMark;
        private int nodes;
        private int maximumDepth;

        public MinimaxAI(Mark aiMark)
        {
            if (aiMark == Mark.Empty)
                throw new ArgumentException("Нужно выбрать X или O.");
            this.aiMark = aiMark;
        }

        public SearchResult FindBestMove(GameState state)
        {
            var result = new SearchResult();
            if (state.IsTerminal)
            {
                result.Score = Evaluate(state);
                return result;
            }
            if (state.CurrentPlayer != aiMark)
                throw new InvalidOperationException("Сейчас ход человека.");

            var timer = Stopwatch.StartNew();
            nodes = 0;
            maximumDepth = 0;
            int bestScore = int.MinValue;

            foreach (int move in MoveOrder)
            {
                if (state[move] != Mark.Empty)
                    continue;

                GameState next = state.ApplyMove(move);
                int score = Minimax(next, 1, false);
                result.MoveScores[move] = score;
                bool winsNow = next.Winner == aiMark;

                if (score > bestScore || (score == bestScore && winsNow))
                {
                    bestScore = score;
                    result.Move = move;
                }
            }

            timer.Stop();
            result.Score = bestScore;
            result.Nodes = nodes;
            result.Depth = maximumDepth;
            result.Milliseconds = timer.Elapsed.TotalMilliseconds;
            return result;
        }

        public int Evaluate(GameState state)
        {
            if (state.Winner == aiMark)
                return 10;
            if (state.Winner == GameState.Opponent(aiMark))
                return -10;
            return 0;
        }

        public int Minimax(GameState state, int depth, bool isMaximizing)
        {
            nodes++;
            maximumDepth = Math.Max(maximumDepth, depth);
            if (state.IsTerminal)
                return Evaluate(state);

            int best = isMaximizing ? int.MinValue : int.MaxValue;
            foreach (int move in state.GetLegalMoves())
            {
                GameState next = state.ApplyMove(move);
                int score = Minimax(next, depth + 1, !isMaximizing);

                if (isMaximizing)
                    best = Math.Max(best, score);
                else
                    best = Math.Min(best, score);
            }
            return best;
        }
    }
}
