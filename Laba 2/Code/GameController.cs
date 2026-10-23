using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TicTacToe
{
    public class GameController : MonoBehaviour
    {
        [SerializeField] private BoardCell[] cells;
        [SerializeField] private TMP_Text status;
        [SerializeField] private TMP_Text roles;
        [SerializeField] private TMP_Text roundNote;
        [SerializeField] private TMP_Text humanScore;
        [SerializeField] private TMP_Text drawScore;
        [SerializeField] private TMP_Text aiScore;
        [SerializeField] private TMP_Text searchDetails;
        [SerializeField] private TMP_Text searchSummary;
        [SerializeField] private TMP_Text[] moveScores;
        [SerializeField] private Image[] moveScoreBackgrounds;
        [SerializeField] private Button humanFirstButton;
        [SerializeField] private Button aiFirstButton;
        [SerializeField] private Button newRoundButton;

        private GameState state;
        private MinimaxAI ai;
        private Mark humanMark;
        private bool humanFirst = true;
        private bool thinking;
        private int humanWins;
        private int aiWins;
        private int draws;
        private int lastAiMove = -1;
        private Coroutine aiTurn;

        public GameState State => state;
        public bool IsThinking => thinking;

        private void Start()
        {
            for (int i = 0; i < cells.Length; i++)
                cells[i].Bind(i, this);
            humanFirstButton.onClick.AddListener(() => SetFirstPlayer(true));
            aiFirstButton.onClick.AddListener(() => SetFirstPlayer(false));
            newRoundButton.onClick.AddListener(NewRound);
            NewRound();
        }

        public void SetFirstPlayer(bool playerStarts)
        {
            humanFirst = playerStarts;
            NewRound();
        }

        public void NewRound()
        {
            if (aiTurn != null)
                StopCoroutine(aiTurn);
            aiTurn = null;
            thinking = false;
            lastAiMove = -1;
            state = new GameState();
            humanMark = humanFirst ? Mark.Cross : Mark.Nought;
            ai = new MinimaxAI(GameState.Opponent(humanMark));
            roles.text = humanFirst ? "Вы играете за ×    /    ИИ играет за ○" : "Вы играете за ○    /    ИИ играет за ×";
            humanFirstButton.interactable = !humanFirst;
            aiFirstButton.interactable = humanFirst;
            searchSummary.text = "Ожидание первого хода ИИ";
            searchDetails.text = "Позиции: -\nГлубина: -\nВремя: -";
            for (int i = 0; i < 9; i++)
            {
                moveScores[i].text = "·";
                moveScores[i].color = new Color32(59, 65, 61, 255);
                moveScoreBackgrounds[i].color = new Color32(237, 234, 225, 255);
            }
            RefreshBoard();
            if (!humanFirst)
                BeginAiTurn();
        }

        public void PlayCell(int index)
        {
            if (index < 0 || index >= 9 || thinking || state.IsTerminal ||
                state.CurrentPlayer != humanMark || state[index] != Mark.Empty)
                return;

            state = state.ApplyMove(index);
            RefreshBoard();
            if (!FinishRound())
                BeginAiTurn();
        }

        private void BeginAiTurn()
        {
            thinking = true;
            RefreshBoard();
            aiTurn = StartCoroutine(PlayAiTurn());
        }

        private IEnumerator PlayAiTurn()
        {
            yield return new WaitForSecondsRealtime(0.3f);
            SearchResult result = ai.FindBestMove(state);
            state = state.ApplyMove(result.Move);
            lastAiMove = result.Move;
            ShowSearch(result);
            thinking = false;
            aiTurn = null;
            RefreshBoard();
            FinishRound();
        }

        private bool FinishRound()
        {
            if (!state.IsTerminal)
                return false;
            if (state.IsDraw)
                draws++;
            else if (state.Winner == humanMark)
                humanWins++;
            else
                aiWins++;
            RefreshBoard();
            return true;
        }

        private void RefreshBoard()
        {
            int[] line = state.GetWinningLine();
            bool canPlay = !thinking && !state.IsTerminal && state.CurrentPlayer == humanMark;

            for (int i = 0; i < cells.Length; i++)
            {
                bool winning = System.Array.IndexOf(line, i) >= 0;
                cells[i].Show(state[i], canPlay, winning, i == lastAiMove);
            }

            humanScore.text = humanWins.ToString("00");
            drawScore.text = draws.ToString("00");
            aiScore.text = aiWins.ToString("00");
            if (state.IsDraw)
                status.text = "Ничья. Хорошая партия!";
            else if (state.Winner != Mark.Empty)
                status.text = state.Winner == humanMark ? "Вы победили!" : "ИИ собрал три в ряд";
            else if (thinking)
                status.text = "ИИ выбирает ход...";
            else
                status.text = "Ваш ход";

            if (state.IsTerminal)
                roundNote.text = "Партия завершена. Начните новую.";
            else
                roundNote.text = $"Ход {state.MovesPlayed + 1} из 9 - Соберите три символа в ряд";
        }

        private void ShowSearch(SearchResult result)
        {
            searchSummary.text = $"Клетка {result.Move + 1}   /   Оценка {FormatScore(result.Score)}";
            searchDetails.text = $"Позиции: {result.Nodes:N0}\nГлубина: {result.Depth}\nВремя: {result.Milliseconds:F1} мс";
            for (int i = 0; i < 9; i++)
            {
                if (result.MoveScores[i].HasValue)
                    moveScores[i].text = FormatScore(result.MoveScores[i].Value);
                else
                    moveScores[i].text = "-";

                bool chosen = i == result.Move;
                moveScoreBackgrounds[i].color = chosen ?
                    new Color32(29, 119, 109, 255) : new Color32(237, 234, 225, 255);
                moveScores[i].color = chosen ? Color.white : new Color32(59, 65, 61, 255);
            }
        }

        private static string FormatScore(int value)
        {
            if (value > 0)
                return "+" + value;

            return value.ToString();
        }
    }
}
