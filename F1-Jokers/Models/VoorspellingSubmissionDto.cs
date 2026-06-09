namespace F1Jokers.Models
{
    public class VoorspellingSubmissionDto
    {
        public string RaceId { get; set; }
        public string RaceTop10 { get; set; }
        public string SprintTop5 { get; set; }
        public int? PolePositionStartnr { get; set; }
        public int? SnelsteRondeStartnr { get; set; }
        public int? SprintPoleStartnr { get; set; }
        public string SeizoenCoureursTop10 { get; set; }
        public string SeizoenTeamsTop11 { get; set; }
        public int? MeesteRaceWinstStartnr { get; set; }
        public int? MeesteSprintWinstStartnr { get; set; }
        public int? MeesteRacePolesStartnr { get; set; }
        public int? MeesteSprintPolesStartnr { get; set; }
    }
}