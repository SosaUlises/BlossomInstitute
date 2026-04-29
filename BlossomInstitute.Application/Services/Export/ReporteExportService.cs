using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteAttendanceByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteHomeworkByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteMarksByCursoAndTerm;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentMarksDetail;
using BlossomInstitute.Application.DataBase.Reportes.Queries.ReporteStudentSummaryByCursoAndTerm;
using BlossomInstitute.Domain.Entidades.Calificacion;
using ClosedXML.Excel;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using System.Reflection;

namespace BlossomInstitute.Application.Services.Export
{
    public class ReporteExportService : IReporteExportService
    {
        private const string LogoResourceName = "BlossomInstitute.Application.Assets.Reports.institute-logo.png";

        public byte[] ExportMarksByCourseTermToExcel(
            ReporteMarksByCursoAndTermResumenModel resumen,
            List<ReporteMarksByCursoAndTermItemModel> items)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Marks Report");

            var row = 1;

            AddExcelLogo(ws);
            ws.Cell(row, 2).Value = "Blossom Institute";
            ws.Range(row, 2, row, 8).Merge().Style.Font.SetBold().Font.SetFontSize(16);
            row++;

            ws.Cell(row, 1).Value = "Course";
            ws.Cell(row, 2).Value = resumen.CursoNombre;
            row++;

            ws.Cell(row, 1).Value = "Year";
            ws.Cell(row, 2).Value = resumen.Year;
            ws.Cell(row, 3).Value = "Term";
            ws.Cell(row, 4).Value = resumen.Term;
            row++;

            ws.Cell(row, 1).Value = "From";
            ws.Cell(row, 2).Value = resumen.From.ToString("yyyy-MM-dd");
            ws.Cell(row, 3).Value = "To";
            ws.Cell(row, 4).Value = resumen.To.ToString("yyyy-MM-dd");
            row += 2;

            ws.Cell(row, 1).Value = "Total Students";
            ws.Cell(row, 2).Value = resumen.TotalAlumnos;
            ws.Cell(row, 3).Value = "Students With Marks";
            ws.Cell(row, 4).Value = resumen.AlumnosConNotas;
            row++;

            ws.Cell(row, 1).Value = "Total Quizzes";
            ws.Cell(row, 2).Value = resumen.TotalQuizzes;
            ws.Cell(row, 3).Value = "Quiz Avg";
            ws.Cell(row, 4).Value = FormatScoreForReport(TipoCalificacion.Quiz, resumen.PromedioQuizzesCurso);
            row++;

            ws.Cell(row, 1).Value = "Total Tests";
            ws.Cell(row, 2).Value = resumen.TotalTests;
            ws.Cell(row, 3).Value = "Test Avg";
            ws.Cell(row, 4).Value = FormatScoreForReport(TipoCalificacion.Test, resumen.PromedioTestsCurso);
            row++;

            ws.Cell(row, 1).Value = "Total Marks";
            ws.Cell(row, 2).Value = resumen.TotalMarks;
            ws.Cell(row, 3).Value = "General Avg";
            ws.Cell(row, 4).Value = FormatScoreForReport(TipoCalificacion.Quiz, resumen.PromedioGeneralCurso);
            row += 2;

            var headerRow = row;
            ws.Cell(row, 1).Value = "Student";
            ws.Cell(row, 2).Value = "DNI";
            ws.Cell(row, 3).Value = "Email";
            ws.Cell(row, 4).Value = "Quiz Count";
            ws.Cell(row, 5).Value = "Quiz Avg";
            ws.Cell(row, 6).Value = "Test Count";
            ws.Cell(row, 7).Value = "Test Avg";
            ws.Cell(row, 8).Value = "Marks Count";
            ws.Cell(row, 9).Value = "General Avg";

            ws.Range(row, 1, row, 9).Style.Font.SetBold();
            ws.Range(row, 1, row, 9).Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = $"{item.AlumnoApellido}, {item.AlumnoNombre}";
                ws.Cell(row, 2).Value = item.AlumnoDni;
                ws.Cell(row, 3).Value = item.AlumnoEmail;
                ws.Cell(row, 4).Value = item.QuizCount;
                ws.Cell(row, 5).Value = FormatScoreForReport(TipoCalificacion.Quiz, item.QuizPromedio);
                ws.Cell(row, 6).Value = item.TestCount;
                ws.Cell(row, 7).Value = FormatScoreForReport(TipoCalificacion.Test, item.TestPromedio);
                ws.Cell(row, 8).Value = item.MarksCount;
                ws.Cell(row, 9).Value = FormatScoreForReport(TipoCalificacion.Quiz, item.PromedioGeneral);
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportMarksByCourseTermToPdf(
            ReporteMarksByCursoAndTermResumenModel resumen,
            List<ReporteMarksByCursoAndTermItemModel> items)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header => ComposePdfHeader(
                        header,
                        $"Marks Report - {resumen.CursoNombre}",
                        $"Year {resumen.Year} - Term {resumen.Term} ({resumen.From:yyyy-MM-dd} to {resumen.To:yyyy-MM-dd})"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Text("Total Students").Bold();
                            table.Cell().Text(resumen.TotalAlumnos.ToString());
                            table.Cell().Text("Students With Marks").Bold();
                            table.Cell().Text(resumen.AlumnosConNotas.ToString());

                            table.Cell().Text("Total Quizzes").Bold();
                            table.Cell().Text(resumen.TotalQuizzes.ToString());
                            table.Cell().Text("Quiz Avg").Bold();
                            table.Cell().Text(FormatScoreForReport(TipoCalificacion.Quiz, resumen.PromedioQuizzesCurso));

                            table.Cell().Text("Total Tests").Bold();
                            table.Cell().Text(resumen.TotalTests.ToString());
                            table.Cell().Text("Test Avg").Bold();
                            table.Cell().Text(FormatScoreForReport(TipoCalificacion.Test, resumen.PromedioTestsCurso));

                            table.Cell().Text("Total Marks").Bold();
                            table.Cell().Text(resumen.TotalMarks.ToString());
                            table.Cell().Text("General Avg").Bold();
                            table.Cell().Text(FormatScoreForReport(TipoCalificacion.Quiz, resumen.PromedioGeneralCurso));
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            void Header(string text) => table.Cell().BorderBottom(1).Padding(4).Text(text).Bold();

                            Header("Student");
                            Header("DNI");
                            Header("Email");
                            Header("Quiz Count");
                            Header("Quiz Avg");
                            Header("Test Count");
                            Header("Test Avg");
                            Header("Marks Count");
                            Header("General Avg");

                            foreach (var item in items)
                            {
                                table.Cell().Padding(4).Text($"{item.AlumnoApellido}, {item.AlumnoNombre}");
                                table.Cell().Padding(4).Text(item.AlumnoDni.ToString());
                                table.Cell().Padding(4).Text(item.AlumnoEmail ?? "-");
                                table.Cell().Padding(4).Text(item.QuizCount.ToString());
                                table.Cell().Padding(4).Text(FormatScoreForReport(TipoCalificacion.Quiz, item.QuizPromedio));
                                table.Cell().Padding(4).Text(item.TestCount.ToString());
                                table.Cell().Padding(4).Text(FormatScoreForReport(TipoCalificacion.Test, item.TestPromedio));
                                table.Cell().Padding(4).Text(item.MarksCount.ToString());
                                table.Cell().Padding(4).Text(FormatScoreForReport(TipoCalificacion.Quiz, item.PromedioGeneral));
                            }
                        });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated by Blossom Institute - ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] ExportAttendanceByCourseTermToExcel(
            ReporteAttendanceByCursoAndTermResumenModel resumen,
            List<ReporteAttendanceByCursoAndTermItemModel> items)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Attendance Report");

            var row = 1;

            AddExcelLogo(ws);
            ws.Cell(row, 2).Value = "Blossom Institute";
            ws.Range(row, 2, row, 8).Merge().Style.Font.SetBold().Font.SetFontSize(16);
            row++;

            ws.Cell(row, 1).Value = "Course";
            ws.Cell(row, 2).Value = resumen.CursoNombre;
            row++;

            ws.Cell(row, 1).Value = "Year";
            ws.Cell(row, 2).Value = resumen.Year;
            ws.Cell(row, 3).Value = "Term";
            ws.Cell(row, 4).Value = resumen.Term;
            row++;

            ws.Cell(row, 1).Value = "From";
            ws.Cell(row, 2).Value = resumen.From.ToString("yyyy-MM-dd");
            ws.Cell(row, 3).Value = "To";
            ws.Cell(row, 4).Value = resumen.To.ToString("yyyy-MM-dd");
            row += 2;

            ws.Cell(row, 1).Value = "Total Students";
            ws.Cell(row, 2).Value = resumen.TotalAlumnos;
            ws.Cell(row, 3).Value = "Total Classes";
            ws.Cell(row, 4).Value = resumen.TotalClases;
            row++;

            ws.Cell(row, 1).Value = "Total Present";
            ws.Cell(row, 2).Value = resumen.TotalPresentes;
            ws.Cell(row, 3).Value = "Total Absent";
            ws.Cell(row, 4).Value = resumen.TotalAusentes;
            row++;

            ws.Cell(row, 1).Value = "Course Attendance %";
            ws.Cell(row, 2).Value = resumen.PorcentajeAsistenciaCurso;
            row += 2;

            ws.Cell(row, 1).Value = "Student";
            ws.Cell(row, 2).Value = "DNI";
            ws.Cell(row, 3).Value = "Email";
            ws.Cell(row, 4).Value = "Total Classes";
            ws.Cell(row, 5).Value = "Present";
            ws.Cell(row, 6).Value = "Absent";
            ws.Cell(row, 7).Value = "Attendance %";

            ws.Range(row, 1, row, 7).Style.Font.SetBold();
            ws.Range(row, 1, row, 7).Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = $"{item.AlumnoApellido}, {item.AlumnoNombre}";
                ws.Cell(row, 2).Value = item.AlumnoDni;
                ws.Cell(row, 3).Value = item.AlumnoEmail;
                ws.Cell(row, 4).Value = item.ClasesTotales;
                ws.Cell(row, 5).Value = item.Presentes;
                ws.Cell(row, 6).Value = item.Ausentes;
                ws.Cell(row, 7).Value = item.PorcentajeAsistencia;
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportAttendanceByCourseTermToPdf(
            ReporteAttendanceByCursoAndTermResumenModel resumen,
            List<ReporteAttendanceByCursoAndTermItemModel> items)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header => ComposePdfHeader(
                        header,
                        $"Attendance Report - {resumen.CursoNombre}",
                        $"Year {resumen.Year} - Term {resumen.Term} ({resumen.From:yyyy-MM-dd} to {resumen.To:yyyy-MM-dd})"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Text("Total Students").Bold();
                            table.Cell().Text(resumen.TotalAlumnos.ToString());
                            table.Cell().Text("Total Classes").Bold();
                            table.Cell().Text(resumen.TotalClases.ToString());

                            table.Cell().Text("Total Present").Bold();
                            table.Cell().Text(resumen.TotalPresentes.ToString());
                            table.Cell().Text("Total Absent").Bold();
                            table.Cell().Text(resumen.TotalAusentes.ToString());

                            table.Cell().Text("Course Attendance %").Bold();
                            table.Cell().Text(resumen.PorcentajeAsistenciaCurso?.ToString("0.00") ?? "-");
                            table.Cell().Text("");
                            table.Cell().Text("");
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            void Header(string text) => table.Cell().BorderBottom(1).Padding(4).Text(text).Bold();

                            Header("Student");
                            Header("DNI");
                            Header("Email");
                            Header("Total Classes");
                            Header("Present");
                            Header("Absent");
                            Header("Attendance %");

                            foreach (var item in items)
                            {
                                table.Cell().Padding(4).Text($"{item.AlumnoApellido}, {item.AlumnoNombre}");
                                table.Cell().Padding(4).Text(item.AlumnoDni.ToString());
                                table.Cell().Padding(4).Text(item.AlumnoEmail ?? "-");
                                table.Cell().Padding(4).Text(item.ClasesTotales.ToString());
                                table.Cell().Padding(4).Text(item.Presentes.ToString());
                                table.Cell().Padding(4).Text(item.Ausentes.ToString());
                                table.Cell().Padding(4).Text(item.PorcentajeAsistencia.ToString("0.00"));
                            }
                        });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated by Blossom Institute - ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        public byte[] ExportHomeworkByCourseTermToExcel(
            ReporteHomeworkByCursoAndTermResumenModel resumen,
            List<ReporteHomeworkByCursoAndTermItemModel> items)
        {
            using var workbook = new XLWorkbook();
            var ws = workbook.Worksheets.Add("Homework Report");

            var row = 1;

            AddExcelLogo(ws);
            ws.Cell(row, 2).Value = "Blossom Institute";
            ws.Range(row, 2, row, 10).Merge().Style.Font.SetBold().Font.SetFontSize(16);
            row++;

            ws.Cell(row, 1).Value = "Course";
            ws.Cell(row, 2).Value = resumen.CursoNombre;
            row++;

            ws.Cell(row, 1).Value = "Year";
            ws.Cell(row, 2).Value = resumen.Year;
            ws.Cell(row, 3).Value = "Term";
            ws.Cell(row, 4).Value = resumen.Term;
            row++;

            ws.Cell(row, 1).Value = "From";
            ws.Cell(row, 2).Value = resumen.From.ToString("yyyy-MM-dd");
            ws.Cell(row, 3).Value = "To";
            ws.Cell(row, 4).Value = resumen.To.ToString("yyyy-MM-dd");
            row += 2;

            ws.Cell(row, 1).Value = "Total Students";
            ws.Cell(row, 2).Value = resumen.TotalAlumnos;
            ws.Cell(row, 3).Value = "Total Homework";
            ws.Cell(row, 4).Value = resumen.TotalHomework;
            row++;

            ws.Cell(row, 1).Value = "Total Deliveries";
            ws.Cell(row, 2).Value = resumen.TotalEntregas;
            ws.Cell(row, 3).Value = "Not Delivered";
            ws.Cell(row, 4).Value = resumen.TotalSinEntregar;
            row++;

            ws.Cell(row, 1).Value = "Pending Correction";
            ws.Cell(row, 2).Value = resumen.TotalPendientesCorreccion;
            ws.Cell(row, 3).Value = "Rework";
            ws.Cell(row, 4).Value = resumen.TotalRehacer;
            row++;

            ws.Cell(row, 1).Value = "Approved";
            ws.Cell(row, 2).Value = resumen.TotalAprobadas;
            ws.Cell(row, 3).Value = "Homework Avg";
            ws.Cell(row, 4).Value = FormatScoreForReport(TipoCalificacion.Homework, resumen.PromedioHomeworkCurso);
            row += 2;

            ws.Cell(row, 1).Value = "Student";
            ws.Cell(row, 2).Value = "DNI";
            ws.Cell(row, 3).Value = "Email";
            ws.Cell(row, 4).Value = "Homework Total";
            ws.Cell(row, 5).Value = "Delivered";
            ws.Cell(row, 6).Value = "Not Delivered";
            ws.Cell(row, 7).Value = "Pending Correction";
            ws.Cell(row, 8).Value = "Rework";
            ws.Cell(row, 9).Value = "Approved";
            ws.Cell(row, 10).Value = "Homework Avg";

            ws.Range(row, 1, row, 10).Style.Font.SetBold();
            ws.Range(row, 1, row, 10).Style.Fill.BackgroundColor = XLColor.LightGray;
            row++;

            foreach (var item in items)
            {
                ws.Cell(row, 1).Value = $"{item.AlumnoApellido}, {item.AlumnoNombre}";
                ws.Cell(row, 2).Value = item.AlumnoDni;
                ws.Cell(row, 3).Value = item.AlumnoEmail;
                ws.Cell(row, 4).Value = item.HomeworkTotal;
                ws.Cell(row, 5).Value = item.HomeworkEntregadas;
                ws.Cell(row, 6).Value = item.HomeworkSinEntregar;
                ws.Cell(row, 7).Value = item.HomeworkPendientesCorreccion;
                ws.Cell(row, 8).Value = item.HomeworkRehacer;
                ws.Cell(row, 9).Value = item.HomeworkAprobadas;
                ws.Cell(row, 10).Value = FormatScoreForReport(TipoCalificacion.Homework, item.HomeworkPromedio);
                row++;
            }

            ws.Columns().AdjustToContents();

            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
        }

        public byte[] ExportHomeworkByCourseTermToPdf(
            ReporteHomeworkByCursoAndTermResumenModel resumen,
            List<ReporteHomeworkByCursoAndTermItemModel> items)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4.Landscape());
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header => ComposePdfHeader(
                        header,
                        $"Homework Report - {resumen.CursoNombre}",
                        $"Year {resumen.Year} - Term {resumen.Term} ({resumen.From:yyyy-MM-dd} to {resumen.To:yyyy-MM-dd})"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(10);

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                                columns.RelativeColumn();
                            });

                            table.Cell().Text("Total Students").Bold();
                            table.Cell().Text(resumen.TotalAlumnos.ToString());
                            table.Cell().Text("Total Homework").Bold();
                            table.Cell().Text(resumen.TotalHomework.ToString());

                            table.Cell().Text("Total Deliveries").Bold();
                            table.Cell().Text(resumen.TotalEntregas.ToString());
                            table.Cell().Text("Not Delivered").Bold();
                            table.Cell().Text(resumen.TotalSinEntregar.ToString());

                            table.Cell().Text("Pending Correction").Bold();
                            table.Cell().Text(resumen.TotalPendientesCorreccion.ToString());
                            table.Cell().Text("Rework").Bold();
                            table.Cell().Text(resumen.TotalRehacer.ToString());

                            table.Cell().Text("Approved").Bold();
                            table.Cell().Text(resumen.TotalAprobadas.ToString());
                            table.Cell().Text("Homework Avg").Bold();
                            table.Cell().Text(FormatScoreForReport(TipoCalificacion.Homework, resumen.PromedioHomeworkCurso));
                        });

                        column.Item().Table(table =>
                        {
                            table.ColumnsDefinition(columns =>
                            {
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(2);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                                columns.RelativeColumn(1);
                            });

                            void Header(string text) => table.Cell().BorderBottom(1).Padding(4).Text(text).Bold();

                            Header("Student");
                            Header("DNI");
                            Header("Email");
                            Header("HW Total");
                            Header("Delivered");
                            Header("Not Delivered");
                            Header("Pending");
                            Header("Rework");
                            Header("Approved");
                            Header("HW Avg");

                            foreach (var item in items)
                            {
                                table.Cell().Padding(4).Text($"{item.AlumnoApellido}, {item.AlumnoNombre}");
                                table.Cell().Padding(4).Text(item.AlumnoDni.ToString());
                                table.Cell().Padding(4).Text(item.AlumnoEmail ?? "-");
                                table.Cell().Padding(4).Text(item.HomeworkTotal.ToString());
                                table.Cell().Padding(4).Text(item.HomeworkEntregadas.ToString());
                                table.Cell().Padding(4).Text(item.HomeworkSinEntregar.ToString());
                                table.Cell().Padding(4).Text(item.HomeworkPendientesCorreccion.ToString());
                                table.Cell().Padding(4).Text(item.HomeworkRehacer.ToString());
                                table.Cell().Padding(4).Text(item.HomeworkAprobadas.ToString());
                                table.Cell().Padding(4).Text(FormatScoreForReport(TipoCalificacion.Homework, item.HomeworkPromedio));
                            }
                        });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated by Blossom Institute - ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }
        public byte[] ExportStudentAssessmentsDetailByCourseTermToPdf(
             ReporteStudentMarksDetailResponseModel data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header => ComposePdfHeader(
                        header,
                        $"Student Assessments Detail - {data.CursoNombre}",
                        $"Year {data.Year} - Term {data.Term} ({data.From:yyyy-MM-dd} to {data.To:yyyy-MM-dd})"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);

                        // Datos alumno
                        column.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Spacing(4);
                            c.Item().Text("Student Information").Bold().FontSize(12);

                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                });

                                table.Cell().Padding(2).Text("Student").Bold();
                                table.Cell().Padding(2).Text($"{data.AlumnoApellido}, {data.AlumnoNombre}");

                                table.Cell().Padding(2).Text("DNI").Bold();
                                table.Cell().Padding(2).Text(data.AlumnoDni.ToString());

                                table.Cell().Padding(2).Text("Email").Bold();
                                table.Cell().Padding(2).Text(data.AlumnoEmail ?? "-");

                                table.Cell().Padding(2).Text("Course").Bold();
                                table.Cell().Padding(2).Text(data.CursoNombre);
                            });
                        });

                        // Resumen
                        column.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Spacing(4);
                            c.Item().Text("Summary").Bold().FontSize(12);

                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Padding(2).Text("Year").Bold();
                                table.Cell().Padding(2).Text(data.Year.ToString());

                                table.Cell().Padding(2).Text("Term").Bold();
                                table.Cell().Padding(2).Text(data.Term.ToString());

                                table.Cell().Padding(2).Text("From").Bold();
                                table.Cell().Padding(2).Text(data.From.ToString("yyyy-MM-dd"));

                                table.Cell().Padding(2).Text("To").Bold();
                                table.Cell().Padding(2).Text(data.To.ToString("yyyy-MM-dd"));

                                table.Cell().Padding(2).Text("Total Assessments").Bold();
                                table.Cell().Padding(2).Text(data.Total.ToString());

                                table.Cell().Padding(2).Text("");
                                table.Cell().Padding(2).Text("");
                            });
                        });

                        if (data.Items == null || data.Items.Count == 0)
                        {
                            column.Item()
                                .Border(1)
                                .Padding(8)
                                .Text("No assessments found for the selected filters.");
                        }
                        else
                        {
                            foreach (var item in data.Items)
                            {
                                column.Item().Border(1).Padding(8).Column(c =>
                                {
                                    c.Spacing(6);

                                    c.Item().Text($"{GetTipoCalificacionLabel(item.Tipo)} - {item.Titulo}")
                                         .Bold()
                                         .FontSize(12);

                                    c.Item().Text($"Date: {item.Fecha:yyyy-MM-dd} | Grade: {FormatScoreForReport(item.Tipo, item.Nota)}");

                                    if (!string.IsNullOrWhiteSpace(item.Descripcion))
                                        c.Item().Text($"Description: {item.Descripcion}");

                                    if (item.Tipo == TipoCalificacion.Homework)
                                    {
                                        c.Item().Text("Generated from homework");
                                    }

                                    if (item.Skills == null || item.Skills.Count == 0)
                                    {
                                        c.Item()
                                            .PaddingTop(4)
                                            .Text("No skill detail available.");
                                    }
                                    else
                                    {
                                        c.Item().PaddingTop(4).Table(table =>
                                        {
                                            table.ColumnsDefinition(columns =>
                                            {
                                                columns.RelativeColumn(2);
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                                columns.RelativeColumn();
                                            });

                                            void Header(string text) =>
                                                table.Cell().BorderBottom(1).Padding(4).Text(text).Bold();

                                            Header("Skill");
                                            Header("Obtained");
                                            Header("Maximum");
                                            Header("%");

                                            foreach (var skill in item.Skills)
                                            {
                                                table.Cell().Padding(4).Text(skill.Skill.ToString());
                                                table.Cell().Padding(4).Text(skill.PuntajeObtenido.ToString("0.##"));
                                                table.Cell().Padding(4).Text(skill.PuntajeMaximo.ToString("0.##"));
                                                table.Cell().Padding(4).Text(skill.Porcentaje?.ToString("0.00") ?? "-");
                                            }
                                        });
                                    }
                                });
                            }
                        }
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated by Blossom Institute - ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private static string GetTipoCalificacionLabel(TipoCalificacion tipo)
        {
            return tipo switch
            {
                TipoCalificacion.Homework => "Homework",
                TipoCalificacion.Quiz => "Quiz",
                TipoCalificacion.Test => "Test",
                TipoCalificacion.Participation => "Participation",
                TipoCalificacion.Behaviour => "Behaviour",
                _ => tipo.ToString()
            };
        }

        private static string FormatScoreForReport(TipoCalificacion tipo, decimal? nota)
        {
            if (tipo == TipoCalificacion.Participation || tipo == TipoCalificacion.Behaviour)
            {
                return nota switch
                {
                    100m => "E",
                    90m => "VG",
                    80m => "G",
                    65m => "R",
                    _ => "-"
                };
            }

            return MapScoreToGrade(nota);
        }

        private static string MapScoreToGrade(decimal? score)
        {
            if (!score.HasValue)
                return "-";

            return score.Value switch
            {
                100 => "A+",
                >= 95 => "A",
                >= 90 => "A-",
                >= 87 => "B+",
                >= 84 => "B",
                >= 80 => "B-",
                >= 77 => "C+",
                >= 74 => "C",
                >= 70 => "C-",
                >= 68 => "D+",
                >= 66 => "D",
                >= 65 => "D-",
                _ => "F"
            };
        }

        public byte[] ExportStudentSummaryByCourseTermToPdf(
             ReporteStudentSummaryByCursoAndTermResponseModel data)
        {
            var document = Document.Create(container =>
            {
                container.Page(page =>
                {
                    page.Margin(20);
                    page.Size(PageSizes.A4);
                    page.DefaultTextStyle(x => x.FontSize(10));

                    page.Header().Element(header => ComposePdfHeader(
                        header,
                        $"Student Summary Report - {data.CursoNombre}",
                        $"Year {data.Year} - Term {data.Term} ({data.From:yyyy-MM-dd} to {data.To:yyyy-MM-dd})"));

                    page.Content().Column(column =>
                    {
                        column.Spacing(12);

                        // Datos alumno
                        column.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Spacing(4);
                            c.Item().Text("Student Information").Bold().FontSize(12);

                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                    columns.RelativeColumn();
                                    columns.RelativeColumn(2);
                                });

                                table.Cell().Padding(2).Text("Student").Bold();
                                table.Cell().Padding(2).Text($"{data.AlumnoApellido}, {data.AlumnoNombre}");

                                table.Cell().Padding(2).Text("DNI").Bold();
                                table.Cell().Padding(2).Text(data.AlumnoDni.ToString());

                                table.Cell().Padding(2).Text("Email").Bold();
                                table.Cell().Padding(2).Text(data.AlumnoEmail ?? "-");

                                table.Cell().Padding(2).Text("Course").Bold();
                                table.Cell().Padding(2).Text(data.CursoNombre);
                            });
                        });

                        // Attendance
                        column.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Spacing(4);
                            c.Item().Text("Attendance").Bold().FontSize(12);

                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Padding(2).Text("Total Classes").Bold();
                                table.Cell().Padding(2).Text(data.Attendance.ClasesTotales.ToString());

                                table.Cell().Padding(2).Text("Present").Bold();
                                table.Cell().Padding(2).Text(data.Attendance.Presentes.ToString());

                                table.Cell().Padding(2).Text("Absent").Bold();
                                table.Cell().Padding(2).Text(data.Attendance.Ausentes.ToString());

                                table.Cell().Padding(2).Text("Attendance %").Bold();
                                table.Cell().Padding(2).Text(data.Attendance.PorcentajeAsistencia.ToString("0.00"));
                            });
                        });

                        // Homework
                        column.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Spacing(4);
                            c.Item().Text("Homework").Bold().FontSize(12);

                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Padding(2).Text("Total").Bold();
                                table.Cell().Padding(2).Text(data.Homework.HomeworkTotal.ToString());

                                table.Cell().Padding(2).Text("Delivered").Bold();
                                table.Cell().Padding(2).Text(data.Homework.HomeworkEntregadas.ToString());

                                table.Cell().Padding(2).Text("Not Delivered").Bold();
                                table.Cell().Padding(2).Text(data.Homework.HomeworkSinEntregar.ToString());

                                table.Cell().Padding(2).Text("Pending Correction").Bold();
                                table.Cell().Padding(2).Text(data.Homework.HomeworkPendientesCorreccion.ToString());

                                table.Cell().Padding(2).Text("Rework").Bold();
                                table.Cell().Padding(2).Text(data.Homework.HomeworkRehacer.ToString());

                                table.Cell().Padding(2).Text("Approved").Bold();
                                table.Cell().Padding(2).Text(data.Homework.HomeworkAprobadas.ToString());

                            });
                        });

                        // Marks
                        column.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Spacing(4);
                            c.Item().Text("Marks").Bold().FontSize(12);

                            c.Item().Table(table =>
                            {
                                table.ColumnsDefinition(columns =>
                                {
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                    columns.RelativeColumn();
                                });

                                table.Cell().Padding(2).Text("Quiz Count").Bold();
                                table.Cell().Padding(2).Text(data.Marks.QuizCount.ToString());

                                table.Cell().Padding(2).Text("Quiz Avg").Bold();
                                table.Cell().Padding(2).Text(FormatScoreForReport(TipoCalificacion.Quiz, data.Marks.QuizPromedio));

                                table.Cell().Padding(2).Text("Test Count").Bold();
                                table.Cell().Padding(2).Text(data.Marks.TestCount.ToString());

                                table.Cell().Padding(2).Text("Test Avg").Bold();
                                table.Cell().Padding(2).Text(FormatScoreForReport(TipoCalificacion.Test, data.Marks.TestPromedio));

                                table.Cell().Padding(2).Text("Marks Count").Bold();
                                table.Cell().Padding(2).Text(data.Marks.MarksCount.ToString());

                                table.Cell().Padding(2).Text("General Avg").Bold();
                                table.Cell().Padding(2).Text(FormatScoreForReport(TipoCalificacion.Quiz, data.Marks.PromedioGeneral));
                            });
                        });

                        // Skills
                        column.Item().Border(1).Padding(8).Column(c =>
                        {
                            c.Spacing(4);
                            c.Item().Text("Skills").Bold().FontSize(12);

                            if (data.Skills == null || data.Skills.Count == 0)
                            {
                                c.Item().Text("No skill breakdown available for this term.");
                            }
                            else
                            {
                                c.Item().Table(table =>
                                {
                                    table.ColumnsDefinition(columns =>
                                    {
                                        columns.RelativeColumn(2);
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                        columns.RelativeColumn();
                                    });

                                    void Header(string text) => table.Cell().BorderBottom(1).Padding(4).Text(text).Bold();

                                    Header("Skill");
                                    Header("Assessments");
                                    Header("Obtained");
                                    Header("Maximum");
                                    Header("%");

                                    foreach (var item in data.Skills)
                                    {
                                        table.Cell().Padding(4).Text(item.Skill.ToString());
                                        table.Cell().Padding(4).Text(item.EvaluacionesCount.ToString());
                                        table.Cell().Padding(4).Text(item.TotalObtenido.ToString("0.##"));
                                        table.Cell().Padding(4).Text(item.TotalMaximo.ToString("0.##"));
                                        table.Cell().Padding(4).Text(item.Porcentaje?.ToString("0.00") ?? "-");
                                    }
                                });
                            }
                        });
                    });

                    page.Footer()
                        .AlignCenter()
                        .Text(x =>
                        {
                            x.Span("Generated by Blossom Institute - ");
                            x.CurrentPageNumber();
                            x.Span(" / ");
                            x.TotalPages();
                        });
                });
            });

            return document.GeneratePdf();
        }

        private static void ComposePdfHeader(IContainer container, string title, string period)
        {
            container.Row(row =>
            {
                row.RelativeItem().Column(column =>
                {
                    column.Spacing(4);
                    column.Item().Text("Blossom Institute").Bold().FontSize(18);
                    column.Item().Text(title);
                    column.Item().Text(period);
                });

                row.ConstantItem(70).Height(45).Image(GetLogoBytes()).FitArea();
            });
        }

        private static void AddExcelLogo(IXLWorksheet ws)
        {
            using var stream = new MemoryStream(GetLogoBytes());
            ws.Row(1).Height = 38;
            ws.Column(1).Width = 12;
            ws.AddPicture(stream, "Institute Logo")
                .MoveTo(ws.Cell(1, 1))
                .Scale(0.12);
        }

        private static byte[] GetLogoBytes()
        {
            using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(LogoResourceName)
                ?? throw new InvalidOperationException($"Embedded resource not found: {LogoResourceName}");
            using var memoryStream = new MemoryStream();
            stream.CopyTo(memoryStream);
            return memoryStream.ToArray();
        }
    }
}
