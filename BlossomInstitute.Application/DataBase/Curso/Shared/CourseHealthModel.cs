namespace BlossomInstitute.Application.DataBase.Curso.Shared
{
    public class CourseHealthModel
    {
        public string Level { get; set; } = CourseHealthLevels.Normal;
        public string Label { get; set; } = "Normal";
        public List<string> Reasons { get; set; } = new();
        public string Color { get; set; } = "emerald";
    }

    public static class CourseHealthLevels
    {
        public const string Normal = "normal";
        public const string FollowUp = "follow-up";
        public const string Critical = "critical";
    }

    public static class CourseHealthCalculator
    {
        private const decimal CriticalAttendanceThreshold = 70m;
        private const decimal FollowUpAttendanceThreshold = 85m;
        private const decimal CriticalGradeThreshold = 60m;
        private const decimal FollowUpGradeThreshold = 75m;

        public static CourseHealthModel Calculate(
            decimal? attendanceAverage,
            decimal? academicAverage,
            int studentsAtRiskCount,
            bool teacherAssigned)
        {
            var reasons = new List<string>();
            var hasCritical = false;
            var hasFollowUp = false;

            if (!teacherAssigned)
            {
                reasons.Add("Sin docentes asignados");
                hasCritical = true;
            }

            if (attendanceAverage.HasValue && attendanceAverage.Value < CriticalAttendanceThreshold)
            {
                reasons.Add($"Asistencia baja ({attendanceAverage:0.##}%)");
                hasCritical = true;
            }
            else if (attendanceAverage.HasValue && attendanceAverage.Value < FollowUpAttendanceThreshold)
            {
                reasons.Add($"Asistencia en seguimiento ({attendanceAverage:0.##}%)");
                hasFollowUp = true;
            }

            if (academicAverage.HasValue && academicAverage.Value < CriticalGradeThreshold)
            {
                reasons.Add($"Promedio bajo ({academicAverage:0.##})");
                hasCritical = true;
            }
            else if (academicAverage.HasValue && academicAverage.Value < FollowUpGradeThreshold)
            {
                reasons.Add($"Promedio en seguimiento ({academicAverage:0.##})");
                hasFollowUp = true;
            }

            if (studentsAtRiskCount > 0)
            {
                var label = studentsAtRiskCount == 1 ? "1 alumno en riesgo" : $"{studentsAtRiskCount} alumnos en riesgo";
                reasons.Add(label);
                hasCritical = true;
            }

            if (hasCritical)
            {
                return new CourseHealthModel
                {
                    Level = CourseHealthLevels.Critical,
                    Label = "Crítico",
                    Reasons = reasons,
                    Color = "rose"
                };
            }

            if (hasFollowUp)
            {
                return new CourseHealthModel
                {
                    Level = CourseHealthLevels.FollowUp,
                    Label = "Seguimiento",
                    Reasons = reasons,
                    Color = "amber"
                };
            }

            return new CourseHealthModel
            {
                Level = CourseHealthLevels.Normal,
                Label = "Normal",
                Reasons = reasons.Count == 0 ? new List<string> { "Sin alertas académicas" } : reasons,
                Color = "emerald"
            };
        }
    }
}
