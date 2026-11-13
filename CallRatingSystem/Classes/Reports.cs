using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace CallRatingSystem.Classes
{
    public class Reports
    {
        public static void ExportToExcelFile(DataTable dt, HttpResponse Response)
        {
            var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add(dt.TableName);
            worksheet.Cell(1, 1).InsertTable(dt);
            worksheet.Columns().AdjustToContents();
            MemoryStream Ms = new MemoryStream();
            workbook.SaveAs(Ms);
            workbook.Dispose();

            byte[] bytesInStream = Ms.ToArray();

            Response.Clear();
            Response.ContentType = "application/force-download";
            Response.AddHeader("content-disposition", "attachment;filename=" + dt.TableName + ".xlsx");
            Response.BinaryWrite(bytesInStream);
            Response.End();

        }

        private static void dataExportExcel_ItemDataBound(object sender, DataGridItemEventArgs e)
        {
            if (e.Item.ItemType == ListItemType.Header)
            {
                //Header Text Format can be done as follows

                e.Item.Font.Bold = true;

                //Adding Filter/Sorting functionality for the Excel

                int cellIndex = 0;

                while (cellIndex < e.Item.Cells.Count)
                {
                    e.Item.Cells[cellIndex].Attributes.Add("x:autofilter", "all");

                    e.Item.Cells[cellIndex].Width = 200;

                    e.Item.Cells[cellIndex].HorizontalAlign = HorizontalAlign.Center;

                    cellIndex++;
                }

                if (e.Item.ItemType == ListItemType.Item || e.Item.ItemType == ListItemType.AlternatingItem)
                {
                    cellIndex = 0;

                    while (cellIndex < e.Item.Cells.Count)

                        //Any Cell specific formatting should be done here

                        e.Item.Cells[cellIndex].HorizontalAlign = HorizontalAlign.Left;

                    cellIndex++;
                }
            }
        }
    }
}