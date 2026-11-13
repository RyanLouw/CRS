using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using System.Web;
using System.Web.UI.WebControls;

namespace CallRatingSystem.Classes
{
    public class DAO
    {
        public static void LoadDropdown(DropDownList dropDownList, string DropdownType)
        {
            List<SqlParameter> parms = new List<SqlParameter>();

            parms.Add(new SqlParameter("@DropdownType", DropdownType));

            var dt = SqlConnections.GetData("CSP_GetDropdownList", parms, CommandType.StoredProcedure);

            LoadDropdown(dropDownList, dt);
        }

        //public static void LoadDropdown(DropDownList dropDownList, string DropdownType, long PatientID)
        //{
        //    List<SqlParameter> parms = new List<SqlParameter>();

        //    parms.Add(new SqlParameter("@DropdownType", DropdownType));
        //    parms.Add(new SqlParameter("@PatientID", PatientID));

        //    var dt = SqlConnections.GetData("CSP_GetDropdownList", parms, CommandType.StoredProcedure);

        //    LoadDropdown(dropDownList, dt);
        //}

        public static void LoadDropdown(DropDownList dropDownList, DataTable dt)
        {
            dropDownList.Items.Add(new ListItem("Please Select", "0"));

            if (dt != null)
            {
                if (dt.Rows.Count > 0)
                {
                    foreach (DataRow datarow in dt.Rows)
                    {
                        string value = datarow.ItemArray[0].ToString();
                        string text = datarow.ItemArray[1].ToString();
                        dropDownList.Items.Add(new ListItem(text, value));
                    }
                }
            }
        }

        public static void PopulateCheckboxList(CheckBoxList checkBoxList, DataTable dt, string DataTextField, string DataValueField)
        {

            checkBoxList.DataSource = dt;
            checkBoxList.DataTextField = DataTextField;
            checkBoxList.DataValueField = DataValueField;
            checkBoxList.DataBind();

        }
    }
}