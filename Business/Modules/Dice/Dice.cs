using SuperNova.DiscordBot.Common.Utils;
using System;
using System.Text.RegularExpressions;

namespace SuperNova.DiscordBot.Business.Modules.Dice
{
    public class Dice
    {
        public struct RollResults
        {
            public string Arithmetic { get; set; }
            public int Result { get; set; }
            public bool Valid { get; set; }
            public string TextResult { get; set; }
        }
        public RollResults Roll(string DiceExpression) => RollExpanded(DiceExpression);

        #region fields/private helpers
        private readonly Random _rand = new Random();
        private readonly Regex _regex = new Regex(@"^(?<numdice>\d*)
													 (?<dsides>
														(?<separator>[d|D])
														(?<numsides>\d+)
													 )
													 (?<modifier>
														(?<sign>[\+\-])
														(?<addend>\d*)
													 )?$");
        private RollResults RollExpanded(string expression)
        {
            if (!ValidateDiceExpression(expression))
            {
                return new RollResults
                {
                    Arithmetic = string.Empty,
                    Result = 0,
                    Valid = false,
                    TextResult = $"Invalid: {expression}"

                };
            }

            int diceCount = 1;
            int numSides = 0;
            int modifier = 0;

            var match = _regex.Match(expression);

            if (!string.IsNullOrWhiteSpace(match.Groups["numdice"]?.Value))
            {
                diceCount = int.Parse(match.Groups["numdice"].Value);
            }
            if (!string.IsNullOrWhiteSpace(match.Groups["numsides"]?.Value))
            {
                numSides = int.Parse(match.Groups["numsides"].Value);
            }
            if (!string.IsNullOrWhiteSpace(match.Groups["modifier"]?.Value))
            {
                modifier = int.Parse(match.Groups["modifier"]?.Value);
            }

            var result = new RollResults();
            for (int i = 1; i <= diceCount; i++)
            {
                int roll = numSides == 0
                    ? numSides
                    : _rand.Next(numSides) + 1;
                result.Arithmetic += $"{roll}";
                result.Result += roll;
                if (i < diceCount) result.Arithmetic += " + ";
            }

            if (modifier != 0)
            {
                result.Arithmetic += modifier < 0
                        ? $" - {Math.Abs(modifier)}"
                        : $" + {modifier}";
            }

            result.Result += modifier;
            result.Arithmetic += $" = {result.Result}";

            result.Valid = true;
            result.TextResult = NumberToText(result.Result).Trim().ToLower();
            return result;
        }
        private string NumberToText(int n)
        {
            if (n < 0)
                return "Negative " + NumberToText(-n);
            else if (n == 0)
                return "Zero";
            else if (n <= 19)
                return new string[] {"One", "Two", "Three", "Four", "Five", "Six", "Seven", "Eight",
         "Nine", "Ten", "Eleven", "Twelve", "Thirteen", "Fourteen", "Fifteen", "Sixteen",
         "Seventeen", "Eighteen", "Nineteen"}[n - 1] + " ";
            else if (n <= 99)
                return new string[] {"Twenty", "Thirty", "Forty", "Fifty", "Sixty", "Seventy",
         "Eighty", "Ninety"}[n / 10 - 2] + " " + NumberToText(n % 10);
            else if (n <= 199)
                return "One Hundred " + NumberToText(n % 100);
            else if (n <= 999)
                return NumberToText(n / 100) + "Hundred " + NumberToText(n % 100);
            else if (n <= 1999)
                return "One Thousand " + NumberToText(n % 1000);
            else if (n <= 999999)
                return NumberToText(n / 1000) + "Thousand " + NumberToText(n % 1000);
            else if (n <= 1999999)
                return "One Million " + NumberToText(n % 1000000);
            else if (n <= 999999999)
                return NumberToText(n / 1000000) + "Million " + NumberToText(n % 1000000);
            else if (n <= 1999999999)
                return "One Billion " + NumberToText(n % 1000000000);
            else
                return NumberToText(n / 1000000000) + "Billion " + NumberToText(n % 1000000000);
        }
        private bool ValidateDiceExpression(string expression) => !expression.IsNullOrEmpty() && _regex.IsMatch(expression);

        public Dice() { }

        #endregion
    }

}
