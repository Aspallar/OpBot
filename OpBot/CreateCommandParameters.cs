using System;

namespace OpBot
{
    internal class CreateCommandParameters
    {
        public string OperationCode { get; internal set; }
        public string Day { get; internal set; }
        public int Size { get; internal set; }
        public string Mode { get; internal set; }
        public TimeSpan Time { get; internal set; }

        public static CreateCommandParameters Parse(string[] commandParts)
        {
            CreateCommandParameters ccp = CreateDefault();

            for (int k = 1; k < commandParts.Length; k++)
                ParsePart(ccp, commandParts[k].ToUpperInvariant());

            return ccp;
        }

        private static void ParsePart(CreateCommandParameters ccp, string part)
        {
            if (DateHelper.IsDayName(part))
            {
                ccp.Day = part;
            }
            else if (part == "GF" || Operation.IsValidOperationCode(part))
            {
                ccp.OperationCode = part;
            }
            else if (Operation.IsValidOperationMode(part))
            {
                ccp.Mode = part;
            }
            else if (Operation.IsValidSize(part))
            {
                ccp.Size = int.Parse(part);
            }
            else
            {
                TimeSpan time;
                if (!TimeSpan.TryParse(part, out time))
                    throw new OpBotInvalidValueException($"Create parameter \"{part}\" does not compute.");
                if (time.TotalHours > 23)
                    throw new OpBotInvalidValueException($"{part} is not a valid time.");
                ccp.Time = time;
            }
        }


        private static CreateCommandParameters CreateDefault()
        {
            DateTime now = DateTime.Now;

            CreateCommandParameters ccp = new CreateCommandParameters()
            {
                OperationCode = "GF",
                Day = DateHelper.DayOfWeekToDay(now.DayOfWeek),
                Size = 8,
                Mode = "SM",
                Time = now.IsDaylightSavingTime() ? new TimeSpan(18, 30, 0) : new TimeSpan(19, 30, 0),
            };
            return ccp;
        }
    }
}
