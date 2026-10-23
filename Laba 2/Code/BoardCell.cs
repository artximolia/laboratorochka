using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace TicTacToe
{
    public class BoardCell : MonoBehaviour
    {
        [SerializeField] private Button button;
        [SerializeField] private TMP_Text symbol;
        [SerializeField] private TMP_Text number;
        [SerializeField] private Image background;

        private static readonly Color CrossColor = new Color32(29, 119, 109, 255);
        private static readonly Color NoughtColor = new Color32(202, 105, 69, 255);
        private static readonly Color Paper = new Color32(255, 253, 248, 255);

        public void Bind(int index, GameController game)
        {
            var colors = button.colors;
            colors.disabledColor = Color.white;
            button.colors = colors;
            button.onClick.AddListener(() => game.PlayCell(index));
        }

        public void Show(Mark mark, bool canPlay, bool winning, bool lastMove)
        {
            if (mark == Mark.Cross)
            {
                symbol.text = "×";
                symbol.color = CrossColor;
            }
            else if (mark == Mark.Nought)
            {
                symbol.text = "○";
                symbol.color = NoughtColor;
            }
            else
            {
                symbol.text = "";
            }

            number.enabled = mark == Mark.Empty;
            button.interactable = canPlay && mark == Mark.Empty;

            if (winning)
                background.color = new Color32(213, 232, 210, 255);
            else if (lastMove)
                background.color = new Color32(244, 226, 212, 255);
            else
                background.color = Paper;
        }
    }
}
