namespace BlossomInstitute.Application.DataBase.Profesor
{
    public static class TeacherFollowUpPolicy
    {
        private const int MinimumRelevantPendingCorrections = 3;

        public static int GetPendingCorrectionsThreshold(int studentsCount)
        {
            if (studentsCount <= 0)
                return MinimumRelevantPendingCorrections;

            return Math.Max(
                MinimumRelevantPendingCorrections,
                (int)Math.Ceiling(studentsCount * 0.5m));
        }

        public static bool HasRelevantPendingCorrections(
            int pendingCorrectionsCount,
            int studentsCount)
        {
            return pendingCorrectionsCount >= GetPendingCorrectionsThreshold(studentsCount);
        }
    }
}
