using System;

namespace OpBot
{
    internal class OperationParameters
    {

        public string OperationCode { get; internal set; }
        public string Day { get; internal set; }
        public int Size { get; internal set; }
        public string Mode { get; internal set; }

        private TimeSpan _time;
        private bool _hasTime;

        public TimeSpan Time
        {
            get
            {
                return _time;
            }
            internal set
            {
                _hasTime = true;
                _time = value;
            }
        }

        public bool HasOperationCode => !string.IsNullOrEmpty(OperationCode);
        public bool HasDay => !string.IsNullOrEmpty(Day);
        public bool HasSize => Size != 0;
        public bool HasMode => !string.IsNullOrEmpty(Mode);
        public bool HasTime => _hasTime;

        private OperationParameters() { }

        public static OperationParameters ParseWithDefault(string[] commandParts)
        {
            OperationParameters opParams = CreateDefault();
            DoParse(opParams, commandParts);
            return opParams;
        }

        public static OperationParameters Parse(string[] commandParts)
        {
            OperationParameters opParams = new OperationParameters();
            DoParse(opParams, commandParts);
            return opParams;
        }

        private static void DoParse(OperationParameters opParams, string[] commandParts)
        {
            for (int k = 1; k < commandParts.Length; k++)
                ParsePart(opParams, commandParts[k].ToUpperInvariant());
        }

        private static void ParsePart(OperationParameters opParams, string part)
        {
            if (DateHelper.IsDayName(part))
            {
                opParams.Day = part;
            }
            else if (part == "GF" || Operation.IsValidOperationCode(part))
            {
                opParams.OperationCode = part;
            }
            else if (Operation.IsValidOperationMode(part))
            {
                opParams.Mode = part;
            }
            else if (Operation.IsValidSize(part))
            {
                opParams.Size = int.Parse(part);
            }
            else
            {
                TimeSpan time;
                if (!TimeSpan.TryParse(part, out time))
                    throw new OpBotInvalidValueException($"Parameter \"{part}\" does not compute.");
                if (time.TotalHours > 23)
                    throw new OpBotInvalidValueException($"{part} is not a valid time.");
                opParams.Time = time;
            }
        }


        private static OperationParameters CreateDefault()
        {
            DateTime now = DateTime.Now;

            OperationParameters opParams = new OperationParameters()
            {
                OperationCode = "GF",
                Day = DateHelper.DayOfWeekToDay(now.DayOfWeek),
                Size = 8,
                Mode = "SM",
                Time = now.IsDaylightSavingTime() ? new TimeSpan(18, 30, 0) : new TimeSpan(19, 30, 0),
            };
            return opParams;
        }
    }
}
