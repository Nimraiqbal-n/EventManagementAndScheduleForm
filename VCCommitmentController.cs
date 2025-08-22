using FACULTY_PORTAL.Models;
using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using System.Data;
using System.IO;
using iTextSharp.text;
using iTextSharp.text.pdf;
using iTextSharp.tool.xml.html;
using Org.BouncyCastle.Asn1.X509;
using static Microsoft.IO.RecyclableMemoryStreamManager;


namespace FACULTY_PORTAL.Controllers
{
    public class VCCommitmentController : Controller
    {
        public ActionResult Index()
        {
            DAL dal = new DAL();
            var ds = dal.SelectSecure("SELECT * FROM VCCommitments ORDER BY EventDate ASC", null);
            ViewBag.Commitments = ds.Tables[0];
            return View();
        }

        public ActionResult Create()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CreateAjax(VCCommitment model)
        {
            try
            {
                if (model.EventDate == default(DateTime) ||
                    string.IsNullOrWhiteSpace(model.Activity) ||
                    string.IsNullOrWhiteSpace(model.Responsible) ||
                    model.StartTime == default(TimeSpan) ||
                    model.EndTime == default(TimeSpan) ||
                    string.IsNullOrWhiteSpace(model.Venue) ||
                    string.IsNullOrWhiteSpace(model.Members))
                {
                    return Json(new { success = false, message = "Please fill all fields." });
                }

                if (model.EndTime <= model.StartTime)
                {
                    return Json(new { success = false, message = "End time must be greater than start time." });
                }

                DAL dal = new DAL();

                // Duplicate check
                string checkDuplicate = "SELECT COUNT(*) FROM VCCommitments WHERE EventDate=@date AND Activity=@activity";
                SqlParameter[] parameters1 = {
            new SqlParameter("@date", model.EventDate),
            new SqlParameter("@activity", model.Activity)
        };
                var dsDuplicate = dal.SelectSecure(checkDuplicate, parameters1);
                if (Convert.ToInt32(dsDuplicate.Tables[0].Rows[0][0]) > 0)
                {
                    return Json(new { success = false, message = "This event already exists for the selected date." });
                }

                // Overlap check
                string checkOverlap = @"SELECT COUNT(*) 
FROM VCCommitments
WHERE EventDate = @date
  AND (@start < EndTime AND @end > StartTime)";
                SqlParameter[] parameters2 = {
          new SqlParameter("@date", SqlDbType.Date) { Value = model.EventDate.Date },
    new SqlParameter("@start", SqlDbType.Time) { Value = model.StartTime },
    new SqlParameter("@end", SqlDbType.Time) { Value = model.EndTime }
        };
                var dsOverlap = dal.SelectSecure(checkOverlap, parameters2);
                if (Convert.ToInt32(dsOverlap.Tables[0].Rows[0][0]) > 0)
                {
                    return Json(new { success = false, message = "Time slot overlaps with another event." });
                }

                // Insert Query
                string insertQuery = @"INSERT INTO VCCommitments 
            (EventDate, Activity, Responsible, StartTime, EndTime, Venue, Members) 
            VALUES (@date, @activity, @resp, @start, @end, @venue, @members)";

                SqlParameter[] insertParams = {
            new SqlParameter("@date", SqlDbType.Date) { Value = model.EventDate.Date },
            new SqlParameter("@activity", SqlDbType.NVarChar) { Value = model.Activity },
            new SqlParameter("@resp", SqlDbType.NVarChar) { Value = model.Responsible },
            new SqlParameter("@start", SqlDbType.Time) { Value = model.StartTime },
            new SqlParameter("@end", SqlDbType.Time) { Value = model.EndTime },
            new SqlParameter("@venue", SqlDbType.NVarChar) { Value = model.Venue },
            new SqlParameter("@members", SqlDbType.NVarChar) { Value = model.Members }
        };

                dal.ExecuteSecure(insertQuery, insertParams);

                return Json(new { success = true, message = "Event created successfully!" });
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.Message);
                return Json(new { success = false, message = "Oops! Something went wrong. Please try again." });
            }
        }

        public ActionResult List()
        {
            DAL dal = new DAL();
            var ds = dal.SelectSecure("SELECT * FROM VCCommitments ORDER BY EventDate, StartTime", null);

            ViewBag.Commitments = (ds != null && ds.Tables.Count > 0) ? ds.Tables[0] : null;

            return View();
        }

        //public ActionResult DownloadPDF()
        //{
        //    DAL dal = new DAL();
        //    var ds = dal.SelectSecure("SELECT * FROM VCCommitments ORDER BY EventDate ASC", null);

        //    using (MemoryStream ms = new MemoryStream())
        //    {
        //        Document doc = new Document(PageSize.A4.Rotate(), 25, 25, 20, 20);
        //        PdfWriter.GetInstance(doc, ms);
        //        doc.Open();

        //        // Fonts - Times New Roman
        //        var titleFont = FontFactory.GetFont("Times New Roman", 14, Font.BOLD, BaseColor.BLACK);
        //        var headerFont = FontFactory.GetFont("Times New Roman", 11, Font.BOLD, BaseColor.WHITE);
        //        var cellFont = FontFactory.GetFont("Times New Roman", 9, Font.NORMAL, BaseColor.BLACK);

        //        // Title
        //        Paragraph title = new Paragraph("📅 Lahore Garrison University - VC Commitments\n\n", titleFont);
        //        title.Alignment = Element.ALIGN_CENTER;
        //        doc.Add(title);

        //        // Table with 8 columns (Day + Event Date + others)
        //        PdfPTable table = new PdfPTable(8);
        //        table.WidthPercentage = 100;
        //        table.SetWidths(new float[] { 10f, 12f, 22f, 12f, 8f, 8f, 15f, 23f });

        //        // Header
        //        string[] headers = { "Day", "Event Date", "Activity", "Responsible", "Start", "End", "Venue", "Members" };
        //        foreach (var h in headers)
        //        {
        //            PdfPCell headerCell = new PdfPCell(new Phrase(h, headerFont));
        //            headerCell.BackgroundColor = new BaseColor(96, 96, 96);
        //            headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
        //            headerCell.Padding = 5f;
        //            table.AddCell(headerCell);
        //        }

        //        // Group rows by EventDate
        //        var groups = ds.Tables[0].AsEnumerable()
        //            .GroupBy(r => Convert.ToDateTime(r["EventDate"]))
        //            .OrderBy(g => g.Key);

        //        foreach (var group in groups)
        //        {
        //            bool firstRow = true;
        //            int rowspan = group.Count();

        //            foreach (var row in group)
        //            {
        //                if (firstRow)
        //                {
        //                    // Day column with rowspan
        //                    PdfPCell dayCell = new PdfPCell(new Phrase(group.Key.ToString("dddd"), cellFont));
        //                    dayCell.Rowspan = rowspan;
        //                    dayCell.VerticalAlignment = Element.ALIGN_MIDDLE;
        //                    dayCell.HorizontalAlignment = Element.ALIGN_CENTER;
        //                    dayCell.Padding = 6f;
        //                    dayCell.BackgroundColor = new BaseColor(240, 240, 240);
        //                    table.AddCell(dayCell);

        //                    // EventDate column with rowspan
        //                    PdfPCell dateCell = new PdfPCell(new Phrase(group.Key.ToString("dd-MMM-yyyy"), cellFont));
        //                    dateCell.Rowspan = rowspan;
        //                    dateCell.VerticalAlignment = Element.ALIGN_MIDDLE;
        //                    dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
        //                    dateCell.Padding = 6f;
        //                    dateCell.BackgroundColor = new BaseColor(240, 240, 240);
        //                    table.AddCell(dateCell);

        //                    firstRow = false;
        //                }

        //                // Activity
        //                table.AddCell(new PdfPCell(new Phrase(row["Activity"].ToString(), cellFont)) { Padding = 5 });

        //                // Responsible
        //                table.AddCell(new PdfPCell(new Phrase(row["Responsible"].ToString(), cellFont)) { Padding = 5 });

        //                // Start
        //                table.AddCell(new PdfPCell(new Phrase(row["StartTime"].ToString(), cellFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });

        //                // End
        //                table.AddCell(new PdfPCell(new Phrase(row["EndTime"].ToString(), cellFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });

        //                // Venue
        //                table.AddCell(new PdfPCell(new Phrase(row["Venue"].ToString(), cellFont)) { Padding = 5 });

        //                // Members
        //                table.AddCell(new PdfPCell(new Phrase(row["Members"].ToString(), cellFont)) { Padding = 5 });
        //            }
        //        }

        //        doc.Add(table);

        //        // Footer
        //        Paragraph footer = new Paragraph("\nGenerated on: " + DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
        //            FontFactory.GetFont("Times New Roman", 8, Font.ITALIC, BaseColor.GRAY));
        //        footer.Alignment = Element.ALIGN_RIGHT;
        //        doc.Add(footer);

        //        doc.Close();
        //        return File(ms.ToArray(), "application/pdf", "VCCommitments.pdf");
        //    }
        //}
        public ActionResult DownloadPDF(int? weekOffset, bool all = false)
        {
            DAL dal = new DAL();
            DataSet ds;
            string heading;

            if (all)
            {
                // Get ALL events
                ds = dal.SelectSecure("SELECT * FROM VCCommitments ORDER BY EventDate ASC", null);
                heading = "All Events";
            }
            else
            {
                DateTime today = DateTime.Today;

                // Start of current week
                int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
                DateTime currentWeekStart = today.AddDays(-diff);

                // Selected week (default = 0)
                int offset = weekOffset ?? 0;
                DateTime weekStart = currentWeekStart.AddDays(7 * offset);
                DateTime weekEnd = weekStart.AddDays(6);

                string query = @"SELECT * FROM VCCommitments 
                         WHERE EventDate BETWEEN @start AND @end 
                         ORDER BY EventDate ASC";

                ds = dal.SelectSecure(query, new SqlParameter[] {
            new SqlParameter("@start", weekStart),
            new SqlParameter("@end", weekEnd)
        });

                heading = $"Events ({weekStart:dd MMM yyyy} - {weekEnd:dd MMM yyyy})";
            }

            using (MemoryStream ms = new MemoryStream())
            {
                Document doc = new Document(PageSize.A4.Rotate(), 25, 25, 20, 20);
                PdfWriter.GetInstance(doc, ms);
                doc.Open();

                var titleFont = FontFactory.GetFont("Times New Roman", 14, Font.BOLD, BaseColor.BLACK);
                var headerFont = FontFactory.GetFont("Times New Roman", 11, Font.BOLD, BaseColor.WHITE);
                var cellFont = FontFactory.GetFont("Times New Roman", 9, Font.NORMAL, BaseColor.BLACK);

                Paragraph title = new Paragraph($"📅 Lahore Garrison University - VC Commitments\n{heading}\n\n", titleFont);
                title.Alignment = Element.ALIGN_CENTER;
                doc.Add(title);

                // Table with 8 columns
                PdfPTable table = new PdfPTable(8);
                table.WidthPercentage = 100;
                table.SetWidths(new float[] { 10f, 12f, 22f, 12f, 8f, 8f, 15f, 23f });

                string[] headers = { "Day", "Event Date", "Activity", "Responsible", "Start", "End", "Venue", "Members" };
                foreach (var h in headers)
                {
                    PdfPCell headerCell = new PdfPCell(new Phrase(h, headerFont));
                    headerCell.BackgroundColor = new BaseColor(96, 96, 96);
                    headerCell.HorizontalAlignment = Element.ALIGN_CENTER;
                    headerCell.Padding = 5f;
                    table.AddCell(headerCell);
                }

                var groups = ds.Tables[0].AsEnumerable()
                    .GroupBy(r => Convert.ToDateTime(r["EventDate"]))
                    .OrderBy(g => g.Key);

                if (!groups.Any())
                {
                    PdfPCell noData = new PdfPCell(new Phrase("No events found", cellFont));
                    noData.Colspan = 8;
                    noData.HorizontalAlignment = Element.ALIGN_CENTER;
                    noData.Padding = 10f;
                    table.AddCell(noData);
                }
                else
                {
                    foreach (var group in groups)
                    {
                        bool firstRow = true;
                        int rowspan = group.Count();

                        foreach (var row in group)
                        {
                            if (firstRow)
                            {
                                PdfPCell dayCell = new PdfPCell(new Phrase(group.Key.ToString("dddd"), cellFont));
                                dayCell.Rowspan = rowspan;
                                dayCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                                dayCell.HorizontalAlignment = Element.ALIGN_CENTER;
                                dayCell.Padding = 6f;
                                dayCell.BackgroundColor = new BaseColor(240, 240, 240);
                                table.AddCell(dayCell);

                                PdfPCell dateCell = new PdfPCell(new Phrase(group.Key.ToString("dd-MMM-yyyy"), cellFont));
                                dateCell.Rowspan = rowspan;
                                dateCell.VerticalAlignment = Element.ALIGN_MIDDLE;
                                dateCell.HorizontalAlignment = Element.ALIGN_CENTER;
                                dateCell.Padding = 6f;
                                dateCell.BackgroundColor = new BaseColor(240, 240, 240);
                                table.AddCell(dateCell);

                                firstRow = false;
                            }

                            table.AddCell(new PdfPCell(new Phrase(row["Activity"].ToString(), cellFont)) { Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(row["Responsible"].ToString(), cellFont)) { Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(row["StartTime"].ToString(), cellFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                            table.AddCell(new PdfPCell(new Phrase(row["EndTime"].ToString(), cellFont)) { Padding = 5, HorizontalAlignment = Element.ALIGN_CENTER });
                            table.AddCell(new PdfPCell(new Phrase(row["Venue"].ToString(), cellFont)) { Padding = 5 });
                            table.AddCell(new PdfPCell(new Phrase(row["Members"].ToString(), cellFont)) { Padding = 5 });
                        }
                    }
                }

                doc.Add(table);
                Paragraph footer = new Paragraph("\nGenerated on: " + DateTime.Now.ToString("dd MMM yyyy hh:mm tt"),
                    FontFactory.GetFont("Times New Roman", 8, Font.ITALIC, BaseColor.GRAY));
                footer.Alignment = Element.ALIGN_RIGHT;
                doc.Add(footer);

                doc.Close();

                string fileName = all
                    ? "VCCommitments_All.pdf"
                    : $"VCCommitments_Week_{DateTime.Now:ddMMyyyy}.pdf";

                return File(ms.ToArray(), "application/pdf", fileName);
            }
        }

        public ActionResult FilterByWeek(int weekOffset = 0)
        {
            DAL dal = new DAL();
            DateTime today = DateTime.Today;

            // Start of current week
            int diff = (7 + (today.DayOfWeek - DayOfWeek.Monday)) % 7;
            DateTime currentWeekStart = today.AddDays(-diff);

            // Calculate start/end of week using offset
            DateTime weekStart = currentWeekStart.AddDays(7 * weekOffset);
            DateTime weekEnd = weekStart.AddDays(6);

            string query = @"SELECT * FROM VCCommitments 
                     WHERE EventDate BETWEEN @start AND @end 
                     ORDER BY EventDate ASC";

            var ds = dal.SelectSecure(query, new SqlParameter[] {
        new SqlParameter("@start", weekStart),
        new SqlParameter("@end", weekEnd)
    });

            ViewBag.Commitments = ds.Tables[0];
            ViewBag.WeekOffset = weekOffset;
            ViewBag.WeekStart = weekStart;
            ViewBag.WeekEnd = weekEnd;

            // Get min/max event dates from DB
            var allDatesDs = dal.SelectSecure("SELECT MIN(EventDate) AS MinDate, MAX(EventDate) AS MaxDate FROM VCCommitments", null);

            if (allDatesDs.Tables[0].Rows.Count > 0 && allDatesDs.Tables[0].Rows[0]["MinDate"] != DBNull.Value)
            {
                var minDate = Convert.ToDateTime(allDatesDs.Tables[0].Rows[0]["MinDate"]);
                var maxDate = Convert.ToDateTime(allDatesDs.Tables[0].Rows[0]["MaxDate"]);

                int minOffset = (int)Math.Floor((minDate - currentWeekStart).TotalDays / 7.0);
                int maxOffset = (int)Math.Ceiling((maxDate - currentWeekStart).TotalDays / 7.0);
                ViewBag.MinOffset = minOffset;
                ViewBag.MaxOffset = maxOffset;
            }
            else
            {
                // fallback if no events in DB
                ViewBag.MinOffset = 0;
                ViewBag.MaxOffset = 0;
            }

            return View("List");
        }



    }
}
