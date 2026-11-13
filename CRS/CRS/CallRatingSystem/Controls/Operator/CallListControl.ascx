<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="CallListControl.ascx.cs" Inherits="CallRatingSystem.Controls.Operator.CallListControl"  %>


<style>
    .Float__Table::before {
        visibility: hidden;
        opacity: 0;
        content: '';
        position: absolute;
        z-index: -2;
        left: -50%;
        top: -50%;
        width: 200%;
        height: 200%;
        background-color: #399953;
        background-repeat: no-repeat;
        background-size: 50% 50%, 50% 50%;
        background-position: 0 0, 100% 0, 100% 100%, 0 100%;
        background-image: linear-gradient(var(--tertiary), var(--secondary-100)), linear-gradient(var(--tertiary), var(--secondary-100)), linear-gradient(var(--tertiary), var(--secondary-100)), linear-gradient(var(--tertiary), var(--secondary-100));
        animation: rotate 4s linear;
    }

    .Float__Table::after {
        content: '';
        visibility: hidden;
        opacity: 0;
        position: absolute;
        z-index: -1;
        left: 6px;
        top: 6px;
        width: calc(100% - 10px);
        height: calc(100% - 10px);
        background: white;
        border-radius: 5px;
    }

    .visiibleSelct {
        pointer-events: none;
    }

    .call-card {
        cursor: pointer;
    }

        .visiibleSelct .Float__Table::after, .visiibleSelct .Float__Table::before {
            visibility: visible;
            opacity: 1;
        }

        .visiibleSelct .Float__Table {
            box-shadow: none;
        }

            .visiibleSelct .Float__Table::after {
                background: #fff;
                background-image: linear-gradient(#CED4DA, #CED4DA), linear-gradient(#CED4DA, #CED4DA), linear-gradient(#CED4DA, #CED4DA), linear-gradient(#CED4DA, #CED4DA);
                background-image: linear-gradient(#DEE2E6, #DEE2E6), linear-gradient(#DEE2E6, #DEE2E6), linear-gradient(#DEE2E6, #DEE2E6), linear-gradient(#DEE2E6, #DEE2E6);
                cursor: not-allowed;
            }

            .visiibleSelct .Float__Table::before {
                background-image: linear-gradient(#7BC86C, #2D6A4F), linear-gradient(#7BC86C, #2D6A4F), linear-gradient(#7BC86C, #2D6A4F), linear-gradient(#7BC86C, #2D6A4F);
            }
</style>

<div class="container-fluid Filterwrapper">
    <div class="row">
        <div class="col-xs-12 col-sm-12 col-md-12 col-lg-12 col-xl-12">
            <asp:Button ID="btnRefresh" runat="server" Text="Refresh" OnClick="btnRefresh_Click" class="hw-btn push_right" />
        </div>
    </div>
</div>
<asp:Button ID="btnSaveSelectCall" Style="display: none;" runat="server" Text="Button" OnClick="btnSaveSelectCall_Click" />

<asp:HiddenField runat="server" ID="hdnCallTime" />
<asp:HiddenField runat="server" ID="hdnNumberDialed" />

<asp:TextBox ID="txtCallTime" runat="server" Style="display: none;" />
<asp:TextBox ID="txtNumberDialed" runat="server" Style="display: none;"/>
<asp:TextBox ID="txtcallId" runat="server" Style="display: none;"/>

<asp:Label ID="lblEXT" runat="server" />

<asp:HiddenField runat="server" ID="hdnExt" />
<asp:HiddenField runat="server" ID="hdnGivenName" />
<asp:HiddenField runat="server" ID="hdnSurname" />
<asp:HiddenField runat="server" ID="hdnDisplayName" />

<asp:HiddenField runat="server" ID="hdncallId" />

<asp:HiddenField runat="server" ID="hdnCallsEmailed" />

<div class="tableContainer Call_RatingTable mainwrapper">
    

    <asp:Repeater runat="server" ID="rptTicketsCallList" OnItemDataBound="rptTicketsCallList_ItemDataBound">
        <ItemTemplate>
            <div class="container-fluid">
                <div class="row">
                    <div class="col-12 col-sm-12 col-md-12 col-lg-12">
                        <div class="btnSelectCallIT" runat="server" id="myDiv">
                            <asp:HiddenField ID="lblcall_id2" runat="server" Value='<%# Eval("CallID") %>'></asp:HiddenField>
                            <asp:HiddenField ID="lblNumberDialed2" runat="server" Value='<%# Eval("CallerNumber") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="lblCallTime2" runat="server" Value='<%# Eval("CallDateTime", "{0:yyyy-MM-dd HH:mm:ss}") %>'> </asp:HiddenField>
                            <asp:HiddenField ID="hfRecordingUrl" runat="server" Value='<%# Eval("RecordingURL") %>'></asp:HiddenField>

                                <div class="Float__Table call-card"
                                    role="button"
                                    tabindex="0"
                                    data-call-id='<%# Eval("CallID") %>'
                                    data-recording-url='<%# System.Web.HttpUtility.HtmlAttributeEncode(Convert.ToString(Eval("RecordingURL"))) %>'
                                    data-number-dialed='<%# Eval("CallerNumber") %>'
                                    data-call-time='<%# Eval("CallDateTime", "{0:yyyy-MM-dd HH:mm:ss}") %>'
                                    onclick="SelectCallIT(this);">
                                <div class="Flex_FloatTable">
                                    <div class="col-12 col-sm-12 col-md-7 col-lg-7">
                                        <div class="Flex_FloatTable">
                                            <div>
                                                <i class="fa fa-clock-o setIcon"></i>
                                                <asp:Label ID="lblCallTime" runat="server" Text='<%# Eval("CallDateTime", "{0:yyyy-MM-dd HH:mm:ss}") %>'> </asp:Label>
                                            </div>
                                            <div>
                                                <asp:Label ID="lblNumberDialed" runat="server" Text='<%# Eval("CallerNumber") %>'> </asp:Label>
                                                <asp:HiddenField ID="lblcall_id" runat="server" Value='<%# Eval("CallID") %>'></asp:HiddenField>
                                            </div>
                                        </div>
                                    </div>
                                </div>


                            </div>
                        </div>
                    </div>
                    <div class="col-12 col-sm-12 col-md-1 col-lg-1 remove__Px">
                                        <div class="AccessBefore">
                                            <asp:Button ID="btnDownloadCall" runat="server" class="hw-btn" Visible="false"
                                                Text="Download Call" OnClick="btnDownloadCall_Click" />
                                        </div>
                                    </div>
                </div>
            </div>
        </ItemTemplate>
    </asp:Repeater>

</div>


<div class="modal fade modalContainer modal_CallList" id="SelectCallModal" tabindex="-1" role="dialog" aria-labelledby="SelectCallModalLabel" aria-hidden="true">
    <div class="modal-dialog" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <h5 class="modal-title" id="SelectCallModalLabel">Email Help Desk</h5>
            </div>
            <div class="modal-body">
                <table>
                    <tr>
                        <td class="lblSetStatus">Call Time: 
                        </td>
                        <td class="lblSetStatus">
                            <asp:Label ID="lblDisplayCallTime" runat="server" />
                            
                        </td>
                    </tr>
                    <tr style="height: 60px">
                        <td class="lblSetStatus">Number:
                        </td>
                        <td class="lblSetStatus">
                            <asp:Label ID="lblDisplayNumberDialed" runat="server" />
                            
                        </td>
                    </tr>
                    <tr>
                        <td style="width: 100%;">
                            <span class="lblSetStatus">Pharmacy:</span> 
                        </td>
                        
                    </tr>
                    <tr>
                        <td>
                            <asp:DropDownList runat="server" ID="ddlPharmacy" CssClass="Cust_dropDown">
                            </asp:DropDownList>
                        </td>
                    </tr>
                    <tr>

                        <td style="width: 100%;">
                            <span class="lblSetStatus">Problems:</span>
                            <asp:CheckBoxList runat="server" ID="chklReason" CssClass="MyCheckboxListClass">
                                <%--<asp:ListItem Value="1" Text="Could not hear patient" />
                                <asp:ListItem Value="2" Text="Number does not exist" />
                                <asp:ListItem Value="3" Text="Call drops automatically" />
                                <asp:ListItem Value="5" Text="While talking to patient, call drops" />
                                <asp:ListItem Value="6" Text="Delay in the call" />
                                <asp:ListItem Value="7" Text="Patient could not hear me" />
                                <asp:ListItem Value="9" Text="No ringtone when calling" />
                                <asp:ListItem Value="10" Text="Number does not show on 3CX" />
                                <asp:ListItem Value="12" Text="Patient not answering" />
                                <asp:ListItem Value="11" Text="Other" />--%>
                            </asp:CheckBoxList>

                        </td>
                    </tr>
                    <tr>
                        <td class="lblSetStatus">Other Problems:
                        </td>
                    </tr>
                    <tr>
                        <td>
                            <asp:TextBox runat="server" TextMode="MultiLine" ID="txtComment" CssClass="Cust_dropDown"></asp:TextBox>
                        </td>
                    </tr>

                </table>
                <asp:Label ID="lblSelectCallError" Style="display: none; color: red; padding-top: 15px;" runat="server" Text="Label"></asp:Label>
            </div>
            <div class="modal-footer">
                <button type="button" class="hw-btn" data-dismiss="modal">Close</button>
                <button type="button" class="hw-btn" onclick="ValidateSelectCall()">Send Email</button>
            </div>
        </div>
    </div>
</div>

<button type="button" id="_toggleErrorHandler" style="visibility: hidden; opacity: 0;" data-toggle="modal" data-target="#errorHandler"></button>

<div class="modal fade modalContainer errorModal" id="errorHandler" tabindex="-1" role="dialog" aria-labelledby="errorHandler" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <label class="modal-error-title"></label>
            </div>
            <div class="modal-body">
                <p></p>
            </div>
            <div class="modal-footer">
                <button type="button" class="hw-btn" data-dismiss="modal">Close</button>
            </div>
        </div>
    </div>
</div>

<button type="button" id="_toggleSuccessHandler" style="visibility: hidden; opacity: 0;" data-toggle="modal" data-target="#successHandler"></button>
<div class="modal fade modalContainer SuccessModal" id="successHandler" tabindex="-1" role="dialog" aria-labelledby="successHandler" aria-hidden="true">
    <div class="modal-dialog modal-dialog-centered" role="document">
        <div class="modal-content">
            <div class="modal-header">
                <label class=" modal-success-title"></label>
            </div>
            <div class="modal-body">
                <p></p>
            </div>
            <div class="modal-footer">
                <button type="button" style="display: none; opacity: 0;">&nbsp;</button>
                <button type="button" class="hw-btn" data-dismiss="modal">OKay</button>
            </div>
        </div>
    </div>
</div>

<script type="text/javascript">

    function ValidateSelectCall() {

        let val = new Validation();

        val.ErrorLable = $("#<%=lblSelectCallError.ClientID%>");


        var result = ValidateForm(val);

        if (result == true) {
            $("#<%=btnSaveSelectCall.ClientID%>").click();
        }

}
function SelectCallIT(card) {
    var recordingReference = (card.getAttribute('data-recording-url') || '').trim();
    var numberDialed       = card.getAttribute('data-number-dialed') || '';
    var callTime           = card.getAttribute('data-call-time') || '';

    document.getElementById('<%= txtcallId.ClientID %>').value        = recordingReference;
    document.getElementById('<%= txtNumberDialed.ClientID %>').value  = numberDialed;
    document.getElementById('<%= txtCallTime.ClientID %>').value      = callTime;

    document.getElementById('<%= lblDisplayCallTime.ClientID %>').innerText      = callTime;
    document.getElementById('<%= lblDisplayNumberDialed.ClientID %>').innerText = numberDialed;

    $('#SelectCallModal').modal('show');
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
