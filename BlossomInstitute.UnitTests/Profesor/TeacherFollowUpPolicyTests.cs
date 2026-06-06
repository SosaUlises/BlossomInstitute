using BlossomInstitute.Application.DataBase.Profesor;
using FluentAssertions;
using Xunit;

namespace BlossomInstitute.UnitTests.Profesor
{
    public class TeacherFollowUpPolicyTests
    {
        [Theory]
        [InlineData(10, 5)]
        [InlineData(6, 3)]
        [InlineData(2, 3)]
        [InlineData(0, 3)]
        public void GetPendingCorrectionsThreshold_ReturnsExpectedThreshold(
            int studentsCount,
            int expectedThreshold)
        {
            var threshold = TeacherFollowUpPolicy.GetPendingCorrectionsThreshold(studentsCount);

            threshold.Should().Be(expectedThreshold);
        }

        [Theory]
        [InlineData(1, 10, false)]
        [InlineData(3, 6, true)]
        [InlineData(6, 10, true)]
        [InlineData(2, 2, false)]
        [InlineData(3, 2, true)]
        public void HasRelevantPendingCorrections_AppliesInstitutionalThreshold(
            int pendingCorrectionsCount,
            int studentsCount,
            bool expected)
        {
            var isRelevant = TeacherFollowUpPolicy.HasRelevantPendingCorrections(
                pendingCorrectionsCount,
                studentsCount);

            isRelevant.Should().Be(expected);
        }
    }
}
