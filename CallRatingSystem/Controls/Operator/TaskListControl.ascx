<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="TaskListControl.ascx.cs" Inherits="CallRatingSystem.Controls.Operator.TaskListControl" %>


<div class="row">
    <div class="col-1">
        <asp:Button ID="btnDueTasks" runat="server" class="hw-btn" Text="Due Tasks" OnClick="btnDueTasks_Click" />
    </div>
    <div class="col-1">
        <asp:Button ID="btnWeek" runat="server" class="hw-btn" Text="Week List" OnClick="btnWeek_Click" />
    </div>
    <div class="col-1">
        <asp:Button ID="btnMonth" runat="server" class="hw-btn" Text="Month List" OnClick="btnMonth_Click" />
    </div>
    <div class="col-1">
        <asp:Button ID="btnYear" runat="server" class="hw-btn" Text="Year List" OnClick="btnYear_Click" />
    </div>
    <div class="col-2">
        <label for="ddlTaskMessage" class="form-label">Task Message</label>
        <asp:DropDownList runat="server" ID="ddlTaskMessage" CssClass="form-control"></asp:DropDownList>
    </div>
    <div class="col-2">
        <label for="ddlServiceType" class="form-label">Service Type</label>
        <asp:DropDownList runat="server" ID="ddlServiceType" CssClass="form-control"></asp:DropDownList>
    </div>
    <asp:Button ID="btnSaveRescheduleTask" Style="display: none;" runat="server" Text="Button" OnClick="btnSaveRescheduleTask_Click" />
    <asp:HiddenField runat="server" ID="hdnRescheduleTaskID" />
</div>


<div class="tableContainer Call_RatingTable">

    <asp:GridView ID="grvTaskList" runat="server" OnRowCommand="grvTaskList_RowCommand" AutoGenerateColumns="false" ShowHeaderWhenEmpty="true">
        <Columns>
            <asp:TemplateField HeaderText="Hiddens" Visible="false">
                <ItemTemplate>
                    <asp:Label ID="lblTaskID" runat="server" Text='<%# Eval("TaskID") %>'> </asp:Label>
                    <asp:Label ID="lblPatientID" runat="server" Text='<%# Eval("PatientID") %>'> </asp:Label>
                    <asp:Label ID="lblTaskMessageID" runat="server" Text='<%# Eval("TaskMessageID") %>'> </asp:Label>
                    <asp:Label ID="lblDueDate" runat="server" Text='<%# Eval("DueDate") %>'> </asp:Label>
                </ItemTemplate>
            </asp:TemplateField>
            <asp:BoundField DataField="HWNumber" HeaderText="Program Number" />
            <asp:BoundField DataField="FirstName" HeaderText="Patient Name" />
            <asp:BoundField DataField="Surname" HeaderText="Patient Surname" />
            <asp:BoundField DataField="TaskMessage" HeaderText="Task Message" />
            <asp:BoundField DataField="Doctor" HeaderText="Doctor" />
            <asp:BoundField DataField="DueDate" HeaderText="Due Date" DataFormatString="{0:dd/MM/yyyy}" />
            <asp:TemplateField HeaderText="Reschedule">
                <ItemTemplate>
                    <asp:Button ID="btnReschedule" runat="server" class="hw-btn"
                        Text="Reschedule" OnClick="btnReschedule_Click" />
                </ItemTemplate>
            </asp:TemplateField>
            <asp:TemplateField HeaderText="Complete Task">
                <ItemTemplate>
                    <asp:Button ID="btnCompleteTask" runat="server" class="hw-btn"
                        Text="Complete" OnClick="btnCompleteTask_Click" Visible="false" />
                </ItemTemplate>
            </asp:TemplateField>
        </Columns>
        <HeaderStyle CssClass="HeaderStyle" />
    </asp:GridView>
</div>

<div class="modal fade modalContainer" id="RescheduleModal" tabindex="-1" role="dialog" aria-labelledby="RescheduleModalLabel" aria-hidden="true">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="RescheduleModalLabel">Reschedule Task</h5>
                <button type="button" class="close" data-dismiss="modal" aria-label="Close">
                    <span aria-hidden="true">&times;</span>
                </button>
            </div>
            <div class="modal-body">
                <table>
                    <tr>
                        <td>Reschedule Date:</td>
                        <td>
                            <asp:TextBox runat="server" ID="txtReschedule" CssClass="form-control" type="date" autocomplete="off"></asp:TextBox>
                        </td>
                    </tr>

                </table>
                <asp:Label ID="lblRescheduleTaskError" Style="display: none; color: red; padding-top: 15px;" runat="server" Text="Label"></asp:Label>
            </div>
            <div class="modal-footer">
                <button type="button" class="btn btn-secondary closeBtn" data-dismiss="modal">Close</button>
                <button type="button" class="btn btn-primary saveBtn" onclick="ValidateRescheduleTask()">Save changes</button>
            </div>
        </div>
    </div>
</div>

<script type="text/javascript">

    function ValidateRescheduleTask() {

        let val = new Validation();

        val.ErrorLable = $("#<%=lblRescheduleTaskError.ClientID%>");

        val.Fields.push(new Fields($("#<%=txtReschedule.ClientID%>"), "Text", "Reschedule Date"))

        var result = ValidateForm(val);

        if (result == true) {
            $("#<%=btnSaveRescheduleTask.ClientID%>").click();
        }

    }

</script>
