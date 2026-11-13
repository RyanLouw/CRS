<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="AdminControl.ascx.cs" Inherits="CallRatingSystem.Controls.Admin.AdminControl" %>

<link href="../Content/MainStyling.css" rel="stylesheet" />


<div class="container-fluid Filterwrapper">
    <div class="row">

        <div class="col-xs-12 col-sm-12 col-md-12 col-lg-12 col-xl-12">
            <label class="lblSetStatus">Select Status</label>
            <asp:DropDownList runat="server" ID="ddlHelpDeskStatusFilter" CssClass="Cust_dropDown" AutoPostBack="true" OnSelectedIndexChanged="ddlHelpDeskStatusFilter_SelectedIndexChanged"></asp:DropDownList>
            <asp:Button ID="btnRefresh" runat="server" Text="Refresh" OnClick="btnRefresh_Click" class="hw-btn push_right" />
            <asp:HiddenField runat="server" ID="hdnPageNum" Value="1" />
        </div>
    </div>
</div>

<asp:Button ID="btnSaveSelectCallIT" Style="display: none;" runat="server" Text="Button" OnClick="btnSaveSelectCallIT_Click" />

<asp:HiddenField runat="server" ID="hdnCallRatingID" />
<asp:HiddenField runat="server" ID="hdnCallTime" />
<asp:HiddenField runat="server" ID="hdnNumberDialed" />
<asp:HiddenField runat="server" ID="hdncall_id" />
<asp:HiddenField runat="server" ID="hdnDisplayName" />
<asp:HiddenField runat="server" ID="hdnExt" />
<asp:HiddenField runat="server" ID="hdnProblems" />
<asp:HiddenField runat="server" ID="hdnOtherProblem" />
<asp:HiddenField runat="server" ID="hdnCallDateTime" />
<asp:HiddenField runat="server" ID="hdnClientIP" />
<asp:HiddenField runat="server" ID="hdnHostIP" />
<asp:HiddenField runat="server" ID="hdnHelpDeskStatusID" />
<asp:HiddenField runat="server" ID="hdnHelpDeskReasonID" />
<asp:HiddenField runat="server" ID="hdnCallDate" />
<asp:HiddenField runat="server" ID="hdnPharmacyID" />
<asp:HiddenField runat="server" ID="hdnPharmacy" />
<asp:HiddenField runat="server" ID="hdnMail" />

<asp:Textbox Style="display: none;" runat="server" ID="txtCallRatingID" />
<asp:Textbox Style="display: none;" runat="server" ID="txtCallTime" />
<asp:Textbox Style="display: none;" runat="server" ID="txtNumberDialed" />
<asp:Textbox Style="display: none;" runat="server" ID="txtcall_id" />
<asp:Textbox Style="display: none;" runat="server" ID="txtDisplayName" />
<asp:Textbox Style="display: none;" runat="server" ID="txtExt" />
<asp:Textbox Style="display: none;" runat="server" ID="txtProblems" />
<asp:Textbox Style="display: none;" runat="server" ID="txtOtherProblem" />
<asp:Textbox Style="display: none;" runat="server" ID="txtCallDateTime" />
<asp:Textbox Style="display: none;" runat="server" ID="txtClientIP" />
<asp:Textbox Style="display: none;" runat="server" ID="txtHostIP" />
<asp:Textbox Style="display: none;" runat="server" ID="txtHelpDeskStatus" />
<asp:Textbox Style="display: none;" runat="server" ID="txtHelpDeskReason" />
<asp:Textbox Style="display: none;" runat="server" ID="txtCallDate" />
<asp:Textbox Style="display: none;" runat="server" ID="txtDisplayITNote" />
<asp:Textbox Style="display: none;" runat="server" ID="txtHelpDeskStatusID" />
<asp:Textbox Style="display: none;" runat="server" ID="txtPharmacyID" />
<asp:Textbox Style="display: none;" runat="server" ID="txtPharmacy" />
<asp:Textbox Style="display: none;" runat="server" ID="txtMail" />



<div class="mainwrapper">


    <asp:Repeater runat="server" ID="rptTickets">
        <ItemTemplate>
            <div class="container-fluid">
                <div class="row">
                    <div class="col-12 col-sm-12 col-md-12 col-lg-12">
                        <div class="btnSelectCallIT">
                            <input type="radio" class="removeF_UI" name="btnSelectCallIT" id="<%# Eval("CallRatingID") %>" onclick="SelectCallIT(this);" />
                            <asp:Button ID="btnSelectCallIT" runat="server" class="hw-btn removeF_UI"
                                Text="Select Call" OnClick="btnSelectCallIT_Click" />

                            <asp:HiddenField ID="hdn2CallRatingID" runat="server" Value='<%# Eval("CallRatingID") %>'></asp:HiddenField>
                            <asp:HiddenField ID="hdn2DisplayName" runat="server" Value='<%# Eval("DisplayName") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2CallDate" runat="server" Value='<%# Eval("Call Date") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2HelpDeskStatus" runat="server" Value='<%# Eval("HelpDeskStatus") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2CallDateTime" runat="server" Value='<%# Eval("CallDateTime") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2call_id" runat="server" Value='<%# Eval("call_id") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2Problems" runat="server" Value='<%# Eval("Problems") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2OtherProblem" runat="server" Value='<%# Eval("OtherProblem") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2ClientIP" runat="server" Value='<%# Eval("ClientIP") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2HostIP" runat="server" Value='<%# Eval("HostIP") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2Ext" runat="server" Value='<%# Eval("Ext") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2NumberDialed" runat="server" Value='<%# Eval("Number Dialed") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2HelpDeskReason" runat="server" Value='<%# Eval("HelpDeskReason") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2ITNote" runat="server" Value='<%# Eval("ITNote") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2HelpDeskStatusID" runat="server" Value='<%# Eval("HelpDeskStatusID") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2CallTime" runat="server" Value='<%# Eval("Call Time") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2PharmacyID" runat="server" Value='<%# Eval("PharmacyID") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2Pharmacy" runat="server" Value='<%# Eval("Pharmacy") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hdn2Mail" runat="server" Value='<%# Eval("Mail") %>'> </asp:HiddenField>

                            <label class="Float__Table" for="<%# Eval("CallRatingID") %>">
                                <div class="Flex_FloatTable">
                                    <div class="col-12 col-sm-12 col-md-7 col-lg-7">
                                        <div class="Flex_FloatTable">
                                            <div>
                                                <i class="fa fa-user-circle-o setIcon"></i>
                                                <asp:Label ID="lblDisplayName" runat="server" Text='<%# Eval("DisplayName") %>'> </asp:Label>
                                            </div>
                                            <div>
                                                <asp:Label ID="lblCallDate" runat="server" Text='<%# Eval("Call Date") %>'> </asp:Label>
                                                <asp:Label ID="lblCallTime" runat="server" Text='<%# Eval("Call Time") %>'> </asp:Label>
                                                <asp:HiddenField ID="lblCallRatingID" runat="server" Value='<%# Eval("CallRatingID") %>'></asp:HiddenField>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="col-12 col-sm-12 col-md-2 col-lg-2 remove__Px">
                                        <div>
                                            <asp:Label ID="lblHelpDeskStatus" CssClass="setStatus" runat="server" Text='<%# Eval("HelpDeskStatus") %>'> </asp:Label>
                                        </div>
                                    </div>
                                    <div class="col-12 col-sm-12 col-md-1 col-lg-1 remove__Px">
                                        <div class="AccessBefore">
                                            <asp:Button ID="btnDownloadCall" runat="server" class="hw-btn"
                                                Text="Download Call" OnClick="btnDownloadCall_Click" />
                                        </div>
                                    </div>
                                    <div class="col-12 col-sm-12 col-md-2 col-lg-2 align_right">
                                        <asp:Label ID="lblCallDateTime" runat="server" Text='<%# Eval("CallDateTime") %>'> </asp:Label>
                                    </div>
                                </div>
                                <div class="Flex_FloatTable">
                                    <div class="col-12 col-sm-12 col-md-7 col-lg-7">
                                        <div class="Float__Table--body">
                                            <div>
                                                <asp:HiddenField ID="lblcall_id" runat="server" Value='<%# Eval("call_id") %>'></asp:HiddenField>
                                                <i class="fa fa-thumbs-down setIcon setIcon--Warning"></i>
                                                <asp:Label ToolTip='<%# Eval("Problems") == null ? "" : Eval("Problems").ToString() %>' ID="lblProblems" runat="server" Text='<%# Eval("Problems") == null ? "" : Eval("Problems").ToString() %>'> </asp:Label>
                                            </div>
                                            <div>
                                                <asp:Label ToolTip='<%# Eval("OtherProblem") == null ? "" : Eval("OtherProblem").ToString() %>' ID="lblOtherProblem" runat="server" Text='<%# Eval("OtherProblem") == null ? "" : Eval("OtherProblem").ToString() %>'> </asp:Label>
                                                <asp:HiddenField ID="lblClientIP" runat="server" Value='<%# Eval("ClientIP") %>'></asp:HiddenField>
                                                <asp:HiddenField ID="lblHostIP" runat="server" Value='<%# Eval("HostIP") %>'></asp:HiddenField>
                                            </div>
                                        </div>
                                    </div>
                                    <div class="col-12 col-sm-12 col-md-2 col-lg-2 remove__Px">
                                        <div>
                                            <span>EXT</span><asp:Label ID="lblExt" runat="server" Text='<%# Eval("Ext") %>'> </asp:Label>
                                        </div>
                                        <i class="fa fa-phone-square setIcon"></i>&nbsp;
                                        <asp:Label ID="lblNumberDialed" runat="server" Text='<%# Eval("Number Dialed") %>'> </asp:Label>
                                    </div>

                                    <div class="col-12 col-sm-12 col-md-1 col-lg-1 remove__Px">&nbsp;</div>
                                    <div class="col-12 col-sm-12 col-md-2 col-lg-2 align_right">
                                        <div>
                                            <asp:Label ID="lblHelpDeskReason" ToolTip='<%# Eval("HelpDeskReason") == null ? "" : Eval("HelpDeskReason").ToString() %>' runat="server" Text='<%# Eval("HelpDeskReason") == null ? "" : Eval("HelpDeskReason").ToString() %>'> </asp:Label>
                                            <asp:HiddenField ID="lblHelpDeskStatusID" runat="server" Value='<%# Eval("HelpDeskStatusID") %>'></asp:HiddenField>
                                        </div>
                                        <div>
                                            <asp:Label ToolTip='<%# Eval("ITNote") == null ? "" : Eval("ITNote").ToString() %>' ID="lblITNote" runat="server" Text='<%# Eval("ITNote") == null ? "" : Eval("ITNote").ToString() %>'> </asp:Label>
                                        </div>
                                    </div>
                                </div>
                            </label>
                        </div>
                    </div>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>
    <div class="container-fluid">
        <div class="row">
            <div class="col-12 col-sm-12 col-md-12 col-lg-12">
                <div class="Pagination">
                    <asp:Button ID="btnPrevPage" runat="server" Visible="false" Text="Previous Page" OnClick="btnPrevPage_Click" class="hw-btn" />
                    <div class="Pagination_Numbers">
                        <asp:Label Text="1" runat="server" ID="currentPage" CssClass="Pagination_Number" />&nbsp;/&nbsp;
                        <asp:Label  runat="server" ID="outOffCount" CssClass="Pagination_Number" />
                    </div>
                    <asp:Button ID="btnNextPage" runat="server" Text="Next Page" OnClick="btnNextPage_Click" class="hw-btn" />
                </div>
            </div>
        </div>
    </div>
</div>


<div class="modal fade modalContainer" id="SelectCallITModal" tabindex="-1" role="dialog" aria-labelledby="SelectCallITModalLabel" aria-hidden="true">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="SelectCallITModalLabel">Update Status</h5>
            </div>
            <div class="modal-body">
                <table>
                    <tr>
                        <td width="100px" class="lblSetStatus">Status: 
                        </td>
                        <td>
                            <asp:DropDownList runat="server" ID="ddlHelpDeskStatus" CssClass="Cust_dropDown">
                            </asp:DropDownList>
                        </td>
                    </tr>
                    <tr>
                        <td width="100px" class="lblSetStatus">Reason: 
                        </td>
                        <td>
                            <asp:DropDownList runat="server" ID="ddlHelpDeskReason" CssClass="Cust_dropDown">
                            </asp:DropDownList>
                        </td>
                    </tr>
                    <tr>
                        <td class="lblSetStatus">Note:
                        </td>
                        <td>
                            <asp:TextBox Width="100%" Height="200px" runat="server" TextMode="MultiLine" ID="txtITNote" CssClass="Cust_dropDown"></asp:TextBox>
                        </td>
                    </tr>

                </table>
                <asp:Label ID="lblSelectCallITError" Style="display: none; color: red; padding-top: 15px;" runat="server" Text="Label"></asp:Label>
            </div>
            <div class="modal-footer">
                <button type="button" class="hw-btn" data-dismiss="modal">Close</button>
                <button type="button" class="hw-btn" onclick="ValidateSelectCallIT()">Update</button>
            </div>
        </div>
    </div>
</div>

<script type="text/javascript">

    function ValidateSelectCallIT() {

        let val = new Validation();

        val.ErrorLable = $("#<%=lblSelectCallITError.ClientID%>");


        var result = ValidateForm(val);

        if (result == true) {
            $("#<%=btnSaveSelectCallIT.ClientID%>").click();
        }

    }

    var setStatus = document.querySelectorAll('.setStatus');
    var btnSelectCallIT = document.querySelectorAll('.btnSelectCallIT input[type=radio]');


    for (var i = 0; i < setStatus.length; i++) {
        var getStatus = setStatus[i].innerHTML;
        if (getStatus == "Open") {
            setStatus[i].classList.add("StatusOpen");
        } else {
            setStatus[i].classList.remove("StatusOpen");
        }
    }

    function SelectCallIT(Event) {
        //var btnToClick = Event.parentElement.children[1];
        //btnToClick.click();

        document.getElementById('<%= ddlHelpDeskStatus.ClientID %>').value = '0';
        document.getElementById('<%= ddlHelpDeskReason.ClientID %>').value = '0';
        document.getElementById('<%= txtITNote.ClientID %>').value = '';


        document.getElementById('<%= txtCallRatingID.ClientID %>').value = '';
        document.getElementById('<%= txtCallTime.ClientID %>').value = '';
        document.getElementById('<%= txtNumberDialed.ClientID %>').value = '';
        document.getElementById('<%= txtcall_id.ClientID %>').value = '';
        document.getElementById('<%= txtDisplayName.ClientID %>').value = '';
        document.getElementById('<%= txtExt.ClientID %>').value = '';
        document.getElementById('<%= txtProblems.ClientID %>').value = '';
        document.getElementById('<%= txtOtherProblem.ClientID %>').value = '';
        document.getElementById('<%= txtCallDateTime.ClientID %>').value = '';
        document.getElementById('<%= txtClientIP.ClientID %>').value = '';
        document.getElementById('<%= txtHostIP.ClientID %>').value = '';
        document.getElementById('<%= txtHelpDeskStatus.ClientID %>').value = '';
        document.getElementById('<%= txtHelpDeskReason.ClientID %>').value = '';
        document.getElementById('<%= txtCallDate.ClientID %>').value = '';
        document.getElementById('<%= txtHelpDeskStatusID.ClientID %>').value = '';
        document.getElementById('<%= txtDisplayITNote.ClientID %>').value = '';


        document.getElementById('<%= txtCallRatingID.ClientID %>').value = Event.parentElement.children[2].value;
        document.getElementById('<%= txtDisplayName.ClientID %>').value = Event.parentElement.children[3].value;
        document.getElementById('<%= txtCallDate.ClientID %>').value = Event.parentElement.children[4].value;
        document.getElementById('<%= txtHelpDeskStatus.ClientID %>').value = Event.parentElement.children[5].value;
        document.getElementById('<%= txtCallDateTime.ClientID %>').value = Event.parentElement.children[6].value;
        document.getElementById('<%= txtcall_id.ClientID %>').value = Event.parentElement.children[7].value;
        document.getElementById('<%= txtProblems.ClientID %>').value = Event.parentElement.children[8].value;
        document.getElementById('<%= txtOtherProblem.ClientID %>').value = Event.parentElement.children[9].value;
        document.getElementById('<%= txtClientIP.ClientID %>').value = Event.parentElement.children[10].value;
        document.getElementById('<%= txtHostIP.ClientID %>').value = Event.parentElement.children[11].value;
        document.getElementById('<%= txtExt.ClientID %>').value = Event.parentElement.children[12].value;
        document.getElementById('<%= txtNumberDialed.ClientID %>').value = Event.parentElement.children[13].value;
        document.getElementById('<%= txtHelpDeskReason.ClientID %>').value = Event.parentElement.children[14].value;
        document.getElementById('<%= txtDisplayITNote.ClientID %>').value = Event.parentElement.children[15].value;
        document.getElementById('<%= txtHelpDeskStatusID.ClientID %>').value = Event.parentElement.children[16].value;
        document.getElementById('<%= txtCallTime.ClientID %>').value = Event.parentElement.children[17].value;
        document.getElementById('<%= txtPharmacyID.ClientID %>').value = Event.parentElement.children[18].value;
        document.getElementById('<%= txtPharmacy.ClientID %>').value = Event.parentElement.children[19].value;
        document.getElementById('<%= txtMail.ClientID %>').value = Event.parentElement.children[20].value;

        $('#SelectCallITModal').modal('show');

    }

    var errorText = document.querySelector(".errorModal .modal-body p"), errorHEAD = document.querySelector(".modal-error-title");

    function toggleErrorHandler(Errorheading, errorContent) {
        errorHEAD.innerText = Errorheading;
        errorText.innerText = errorContent;
        document.getElementById('_toggleErrorHandler').click();
    }

    var successText = document.querySelector(".SuccessModal .modal-body p"), successHEAD = document.querySelector(".modal-success-title");

    function toggleSuccessHandler(Successheading, SuccessContent) {
        successHEAD.innerText = Successheading;
        successText.innerText = SuccessContent;
        document.getElementById('_toggleSuccessHandler').click();;
    }



</script>
